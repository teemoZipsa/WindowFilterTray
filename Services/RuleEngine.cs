using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class RuleEngine
{
    private readonly SafetyPolicy _safetyPolicy;
    private readonly Dictionary<string, RuleStats> _stats;

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
            .OrderByDescending(result => result.Score)
            .FirstOrDefault();

        if (best is null)
        {
            return MatchDecision.NoMatch("일치 규칙 없음");
        }

        var config = ModeConfig.For(mode);
        if (mode == FilteringMode.Off)
        {
            TouchStats(best.Rule, blocked: false);
            return MatchDecision.Match(best.Rule, best.Score, WindowActionType.Ignore, 0, "꺼짐 모드");
        }

        if (best.Score < Math.Max(best.Rule.Matcher.MinScore, config.MinScore))
        {
            return MatchDecision.NoMatch($"점수 부족: {best.Score}");
        }

        if (mode == FilteringMode.Low && !(best.ProcessMatched && best.ClassMatched))
        {
            return MatchDecision.NoMatch("약함 모드는 프로세스명과 창 클래스가 모두 필요함");
        }

        var shouldBlock = ShouldBlockByFrequency(best.Rule);
        var effectiveAction = shouldBlock ? best.Rule.Action : WindowActionType.Ignore;
        var reason = shouldBlock ? "차단 조건 통과" : "빈도 제한 내 노출";
        var graceMs = Math.Max(best.Rule.GraceMs, config.GraceMs);

        return MatchDecision.Match(best.Rule, best.Score, effectiveAction, graceMs, reason);
    }

    public void MarkBlocked(WindowRule rule)
    {
        var stats = EnsureStats(rule.Id);
        stats.LastBlockedUtc = DateTimeOffset.UtcNow;
    }

    private RuleScore? Score(WindowRule rule, WindowSnapshot snapshot, FilteringMode mode)
    {
        var matcher = rule.Matcher;
        var score = 0;
        var processMatched = false;
        var classMatched = false;

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
        }

        if (matcher.UseTitle)
        {
            if (string.IsNullOrWhiteSpace(matcher.TitleContains)
                || !snapshot.Title.Contains(matcher.TitleContains, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            score += 20;
        }

        if (matcher.UseSize)
        {
            if (matcher.SizeHint is null || !MatchesSize(snapshot.Rect, matcher.SizeHint))
            {
                if (mode != FilteringMode.Strong)
                {
                    return null;
                }
            }
            else
            {
                score += 10;
            }
        }

        if (matcher.UsePosition)
        {
            if (matcher.PositionHint is null || !MatchesPosition(snapshot.Rect, matcher.PositionHint))
            {
                if (mode != FilteringMode.Strong)
                {
                    return null;
                }
            }
            else
            {
                score += 10;
            }
        }

        return new RuleScore(rule, score, processMatched, classMatched);
    }

    private bool ShouldBlockByFrequency(WindowRule rule)
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

    private RuleStats TouchStats(WindowRule rule, bool blocked)
    {
        var stats = EnsureStats(rule.Id);
        var today = DateTimeOffset.Now.ToString("yyyy-MM-dd");

        if (!string.Equals(stats.LastSeenLocalDate, today, StringComparison.Ordinal))
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
        return wDelta <= hint.W * hint.Tolerance && hDelta <= hint.H * hint.Tolerance;
    }

    private static bool MatchesPosition(WindowRect rect, PositionHint hint)
    {
        var xDelta = Math.Abs(rect.Left - hint.X);
        var yDelta = Math.Abs(rect.Top - hint.Y);
        return xDelta <= Math.Max(1, hint.X) * hint.Tolerance
            && yDelta <= Math.Max(1, hint.Y) * hint.Tolerance;
    }

    private sealed record RuleScore(WindowRule Rule, int Score, bool ProcessMatched, bool ClassMatched)
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
                FilteringMode.Low => new ModeConfig(90, 1500),
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
