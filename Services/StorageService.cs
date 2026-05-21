using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class StorageService
{
    private readonly AppPaths _paths;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public StorageService(AppPaths paths)
    {
        _paths = paths;
    }

    public List<WindowRule> LoadRules()
    {
        var rules = Read<List<WindowRule>>(_paths.RulesPath) ?? [];
        foreach (var rule in rules)
        {
            NormalizeRule(rule);
        }

        return rules;
    }

    public void SaveRules(IEnumerable<WindowRule> rules)
    {
        var normalized = rules.ToList();
        foreach (var rule in normalized)
        {
            NormalizeRule(rule);
        }

        Write(_paths.RulesPath, normalized);
    }

    public Dictionary<string, RuleStats> LoadStats()
    {
        var stats = Read<Dictionary<string, RuleStats>>(_paths.StatsPath) ?? [];
        foreach (var value in stats.Values)
        {
            NormalizeStats(value);
        }

        return stats;
    }

    public void SaveStats(Dictionary<string, RuleStats> stats)
    {
        foreach (var value in stats.Values)
        {
            NormalizeStats(value);
        }

        Write(_paths.StatsPath, stats);
    }

    public AppSettings LoadSettings()
    {
        return Read<AppSettings>(_paths.SettingsPath) ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        Write(_paths.SettingsPath, settings);
    }

    public List<MatchLogEntry> LoadLogs()
    {
        var logs = Read<List<MatchLogEntry>>(_paths.LogsPath) ?? [];
        foreach (var log in logs)
        {
            NormalizeLog(log);
        }

        return logs;
    }

    public void SaveLogs(IEnumerable<MatchLogEntry> logs)
    {
        var normalized = logs.TakeLast(500).ToList();
        foreach (var log in normalized)
        {
            NormalizeLog(log);
        }

        Write(_paths.LogsPath, normalized);
    }

    private T? Read<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, _jsonOptions);
        }
        catch (Exception ex)
        {
            BackupUnreadableFile(path, ex);
            return default;
        }
    }

    private void Write<T>(string path, T value)
    {
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        var backupPath = $"{path}.bak";

        try
        {
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, value, _jsonOptions);
            }

            if (File.Exists(path))
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static void BackupUnreadableFile(string path, Exception exception)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = $"{path}.broken-{timestamp}";
            File.Move(path, backupPath);
            Trace.TraceWarning($"Moved unreadable settings file to {backupPath}: {exception.Message}");
        }
        catch (Exception backupException)
        {
            Trace.TraceWarning($"Failed to back up unreadable settings file '{path}': {backupException.Message}");
        }
    }

    private static void NormalizeRule(WindowRule rule)
    {
        rule.CreatedAt = rule.CreatedAt.ToUniversalTime();
        rule.DisplayName = string.IsNullOrWhiteSpace(rule.DisplayName) ? "정리할 창" : rule.DisplayName.Trim();
        rule.GraceMs = Math.Max(0, rule.GraceMs);
        rule.Matcher ??= new RuleMatcher();
        rule.FrequencyCap ??= new FrequencyCap();
        rule.FrequencyCap.MaxImpressions = Math.Max(0, rule.FrequencyCap.MaxImpressions);
        rule.Matcher.ProcessName ??= string.Empty;
        rule.Matcher.WindowClass ??= string.Empty;
        rule.Matcher.TitleContains ??= string.Empty;
        rule.Matcher.MinScore = Math.Clamp(rule.Matcher.MinScore, 0, 100);
        NormalizeSize(rule.Matcher.Size);
        NormalizePosition(rule.Matcher.Position);
    }

    private static void NormalizeSize(SizeHint? hint)
    {
        if (hint is null)
        {
            return;
        }

        hint.W = Math.Max(0, hint.W);
        hint.H = Math.Max(0, hint.H);
        hint.Tolerance = NormalizeTolerance(hint.Tolerance);
    }

    private static void NormalizePosition(PositionHint? hint)
    {
        if (hint is null)
        {
            return;
        }

        hint.Tolerance = NormalizeTolerance(hint.Tolerance);
    }

    private static double NormalizeTolerance(double tolerance)
    {
        return double.IsFinite(tolerance) && tolerance >= 0
            ? Math.Min(tolerance, 1)
            : 0.2;
    }

    private static void NormalizeStats(RuleStats stats)
    {
        if (stats.LastSeenUtc is not null)
        {
            stats.LastSeenUtc = stats.LastSeenUtc.Value.ToUniversalTime();
        }

        if (stats.LastBlockedUtc is not null)
        {
            stats.LastBlockedUtc = stats.LastBlockedUtc.Value.ToUniversalTime();
        }

        stats.TodayImpressions = Math.Max(0, stats.TodayImpressions);
        stats.SessionImpressions = Math.Max(0, stats.SessionImpressions);
        stats.TotalImpressions = Math.Max(0, stats.TotalImpressions);
    }

    private static void NormalizeLog(MatchLogEntry log)
    {
        log.At = log.At.ToUniversalTime();
        log.RuleId ??= string.Empty;
        log.RuleName ??= string.Empty;
        log.WindowTitle ??= string.Empty;
        log.ProcessName ??= string.Empty;
        log.Score = Math.Clamp(log.Score, 0, 100);
        log.Reason ??= string.Empty;
    }
}
