namespace WindowFilterTray.Models;

public sealed class MatchLogEntry
{
    public DateTimeOffset At { get; set; } = DateTimeOffset.Now;
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
