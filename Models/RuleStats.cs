namespace WindowFilterTray.Models;

public sealed class RuleStats
{
    public DateTimeOffset? LastSeenUtc { get; set; }
    public string? LastSeenLocalDate { get; set; }
    public int TodayImpressions { get; set; }
    public int SessionImpressions { get; set; }
    public int TotalImpressions { get; set; }
    public DateTimeOffset? LastBlockedUtc { get; set; }
}
