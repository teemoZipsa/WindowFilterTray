namespace WindowFilterTray.Models;

public sealed class AppSettings
{
    public FilteringMode FilteringMode { get; set; } = FilteringMode.Optimal;
    public bool IsPaused { get; set; }
    public bool AutoStart { get; set; }
}
