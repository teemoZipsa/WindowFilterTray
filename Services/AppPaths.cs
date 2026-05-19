namespace WindowFilterTray.Services;

public sealed class AppPaths
{
    public const string AppName = "WindowFilterTray";

    public AppPaths()
    {
        Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);
        Thumbnails = Path.Combine(Root, "thumbs");
        RulesPath = Path.Combine(Root, "rules.json");
        StatsPath = Path.Combine(Root, "stats.json");
        SettingsPath = Path.Combine(Root, "settings.json");
        LogsPath = Path.Combine(Root, "match-log.json");

        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Thumbnails);
    }

    public string Root { get; }
    public string Thumbnails { get; }
    public string RulesPath { get; }
    public string StatsPath { get; }
    public string SettingsPath { get; }
    public string LogsPath { get; }
}
