using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class RuleEngine
{
    private readonly SafetyPolicy _safetyPolicy;
    private readonly Dictionary<string, RuleStats> _stats;
    private readonly object _statsLock = new();

    public RuleEngine(SafetyPolicy safetyPolicy, Dictionary<string, RuleStats> stats)
    {
        _safetyPolicy = safetyPolicy;
        _stats = stats;
    }

    public MatchDecision Evaluate(
        WindowSnapshot snapshot,
        IReadOnlyCollection<WindowRule> rules,
        FilteringMode mode,
        bool paused)
    {
        if (_safetyPolicy.IsExcluded(snapshot, out var excludedReason))
        {
            return MatchDecision.NoMatch(excludedReason);
        }

        if (paused)
        {
            return MatchDecision.NoMatch("일시 정지됨");
        }

        var best = rules
            .Where(rule => rule.Enabled)
            .Select(rule => Score(rule, snapshot, mode))
            .OfType<RuleScore>()
            .Where(result => result.IsCandidate)
            .Where(result => IsAllowedByMode(result, mode))
            .OrderByDescending(result => result.Score)
            .FirstOrDefault();

        if (best is null)
        {
            return MatchDecision.NoMatch("일치 규칙 없음");
        }

        var config = ModeConfig.For(mode);
        if (mode == FilteringMode.Off)
        {
            ObserveStats(best.Rule);
            return MatchDecision.Match(best.Rule, best.Score, WindowActionType.Ignore, 0, "구경만 모드");
        }

        if (best.Score < Math.Max(best.Rule.Matcher.MinScore, config.MinScore))
        {
            return MatchDecision.NoMatch($"점수 부족: {best.Score}");
        }

        var shouldBlock = ShouldBlockByFrequency(best.Rule);
        var effectiveAction = shouldBlock ? best.Rule.Action : WindowActionType.Ignore;
        var reason = shouldBlock ? "정리 조건 통과" : "처음에는 그냥 보여둠";
        var graceMs = Math.Max(best.Rule.GraceMs, config.GraceMs);

        return MatchDecision.Match(best.Rule, best.Score, effectiveAction, graceMs, reason);
    }

    public void MarkBlocked(WindowRule rule)
    {
        lock (_statsLock)
        {
            var stats = EnsureStats(rule.Id);
            stats.LastBlockedUtc = DateTimeOffset.UtcNow;
        }
    }

    private RuleScore? Score(WindowRule rule, WindowSnapshot snapshot, FilteringMode mode)
    {
        var matcher = rule.Matcher;
        var score = 0;
        var processMatched = false;
        var classMatched = false;
        var secondarySignalMatched = false;

        if (matcher.UseProcessName)
        {
            processMatched = snapshot.ProcessName.Equals(matcher.ProcessName, StringComparison.OrdinalIgnoreCase);
            if (!processMatched)
            {
                return null;
            }

            score += 40;
        }

        if (matcher.UseWindowClass)
        {
            classMatched = snapshot.ClassName.Equals(matcher.WindowClass, StringComparison.OrdinalIgnoreCase);
            if (!classMatched)
            {
                return null;
            }

            score += 30;
            secondarySignalMatched = true;
        }

        if (matcher.UseTitle)
        {
            if (string.IsNullOrWhiteSpace(matcher.TitleContains)
                || !snapshot.Title.Contains(matcher.TitleContains, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            score += GetTitleScore(matcher.TitleContains);
            secondarySignalMatched = true;
        }

        if (matcher.UseSize)
        {
            if (matcher.Size is null || !MatchesSize(snapshot.Rect, matcher.Size))
            {
                if (mode != FilteringMode.Strong)
                {
                    return null;
                }
            }
            else
            {
                score += 10;
                secondarySignalMatched = true;
            }
        }

        if (matcher.UsePosition)
        {
            if (matcher.Position is null || !MatchesPosition(snapshot.Rect, matcher.Position))
            {
                if (mode != FilteringMode.Strong)
                {
                    return null;
                }
            }
            else
            {
                score += 10;
                secondarySignalMatched = true;
            }
        }

        return new RuleScore(rule, score, processMatched, classMatched, secondarySignalMatched);
    }

    private bool ShouldBlockByFrequency(WindowRule rule)
    {
        lock (_statsLock)
        {
            var stats = TouchStats(rule, blocked: false);
            var cap = rule.FrequencyCap;
            var max = Math.Max(0, cap.MaxImpressions);

            return cap.Mode switch
            {
                FrequencyCapMode.None => true,
                FrequencyCapMode.PerCount => stats.TotalImpressions > max,
                FrequencyCapMode.PerSession => stats.SessionImpressions > max,
                FrequencyCapMode.PerDay => stats.TodayImpressions > max,
                _ => true
            };
        }
    }

    private void ObserveStats(WindowRule rule)
    {
        lock (_statsLock)
        {
            var stats = EnsureStats(rule.Id);
            stats.LastSeenUtc = DateTimeOffset.UtcNow;
        }
    }

    private RuleStats TouchStats(WindowRule rule, bool blocked)
    {
        var stats = EnsureStats(rule.Id);
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (stats.LastSeenLocalDate != today)
        {
            stats.TodayImpressions = 0;
            stats.LastSeenLocalDate = today;
        }

        stats.LastSeenUtc = DateTimeOffset.UtcNow;
        stats.TodayImpressions++;
        stats.SessionImpressions++;
        stats.TotalImpressions++;

        if (blocked)
        {
            stats.LastBlockedUtc = DateTimeOffset.UtcNow;
        }

        return stats;
    }

    private RuleStats EnsureStats(string ruleId)
    {
        if (!_stats.TryGetValue(ruleId, out var stats))
        {
            stats = new RuleStats();
            _stats[ruleId] = stats;
        }

        return stats;
    }

    private static bool MatchesSize(WindowRect rect, SizeHint hint)
    {
        var wDelta = Math.Abs(rect.Width - hint.W);
        var hDelta = Math.Abs(rect.Height - hint.H);
        var wTolerance = Math.Max(20.0, hint.W * hint.Tolerance);
        var hTolerance = Math.Max(20.0, hint.H * hint.Tolerance);
        return (double)wDelta <= wTolerance && (double)hDelta <= hTolerance;
    }

    private static int GetTitleScore(string titleContains)
    {
        var signalLength = titleContains.Count(c => !char.IsWhiteSpace(c));
        return signalLength >= 6 ? 70 : 30;
    }

    private static bool MatchesPosition(WindowRect rect, PositionHint hint)
    {
        var xDelta = Math.Abs(rect.Left - hint.X);
        var yDelta = Math.Abs(rect.Top - hint.Y);
        var xTolerance = Math.Max(20.0, Math.Abs(hint.X) * hint.Tolerance);
        var yTolerance = Math.Max(20.0, Math.Abs(hint.Y) * hint.Tolerance);
        return (double)xDelta <= xTolerance && (double)yDelta <= yTolerance;
    }

    private static bool IsAllowedByMode(RuleScore score, FilteringMode mode)
    {
        if (mode != FilteringMode.Low)
        {
            return true;
        }

        return score.Rule.Matcher.UseProcessName
            && score.ProcessMatched
            && score.SecondarySignalMatched;
    }

    private sealed record RuleScore(
        WindowRule Rule,
        int Score,
        bool ProcessMatched,
        bool ClassMatched,
        bool SecondarySignalMatched)
    {
        public bool IsCandidate => Score > 0;
    }

    private sealed record ModeConfig(int MinScore, int GraceMs)
    {
        public static ModeConfig For(FilteringMode mode)
        {
            return mode switch
            {
                FilteringMode.Off => new ModeConfig(int.MaxValue, 0),
                FilteringMode.Low => new ModeConfig(70, 1500),
                FilteringMode.Optimal => new ModeConfig(70, 1000),
                FilteringMode.Strong => new ModeConfig(50, 500),
                _ => new ModeConfig(70, 1000)
            };
        }
    }
}

public sealed class MatchDecision
{
    private MatchDecision(
        bool matched,
        WindowRule? rule,
        int score,
        WindowActionType action,
        int graceMs,
        string reason)
    {
        Matched = matched;
        Rule = rule;
        Score = score;
        Action = action;
        GraceMs = graceMs;
        Reason = reason;
    }

    public bool Matched { get; }
    public WindowRule? Rule { get; }
    public int Score { get; }
    public WindowActionType Action { get; }
    public int GraceMs { get; }
    public string Reason { get; }

    public static MatchDecision NoMatch(string reason)
    {
        return new MatchDecision(false, null, 0, WindowActionType.Ignore, 0, reason);
    }

    public static MatchDecision Match(WindowRule rule, int score, WindowActionType action, int graceMs, string reason)
    {
        return new MatchDecision(true, rule, score, action, graceMs, reason);
    }
}
