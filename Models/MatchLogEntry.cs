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

    public string DisplayAction => Action switch
    {
        nameof(WindowActionType.Minimize) => "작게 내림",
        nameof(WindowActionType.HideWindow) => "숨김",
        nameof(WindowActionType.CloseWindow) => "닫음",
        nameof(WindowActionType.Ignore) => "기록만",
        _ => Action
    };
}
