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
        return Read<List<WindowRule>>(_paths.RulesPath) ?? [];
    }

    public void SaveRules(IEnumerable<WindowRule> rules)
    {
        Write(_paths.RulesPath, rules.ToList());
    }

    public Dictionary<string, RuleStats> LoadStats()
    {
        return Read<Dictionary<string, RuleStats>>(_paths.StatsPath) ?? [];
    }

    public void SaveStats(Dictionary<string, RuleStats> stats)
    {
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
        return Read<List<MatchLogEntry>>(_paths.LogsPath) ?? [];
    }

    public void SaveLogs(IEnumerable<MatchLogEntry> logs)
    {
        Write(_paths.LogsPath, logs.TakeLast(500).ToList());
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
        catch
        {
            return default;
        }
    }

    private void Write<T>(string path, T value)
    {
        var tempPath = $"{path}.tmp";
        using (var stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, value, _jsonOptions);
        }

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }
}
