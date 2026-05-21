using System.Text.Json.Serialization;

namespace WindowFilterTray.Models;

public sealed class MatchLogEntry
{
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int Score { get; set; }
    public WindowActionType Action { get; set; } = WindowActionType.Ignore;
    public string Reason { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayAction => Action switch
    {
        WindowActionType.Minimize => "작게 내림",
        WindowActionType.HideWindow => "숨김",
        WindowActionType.CloseWindow => "닫음",
        WindowActionType.Ignore => "기록만",
        _ => Action.ToString()
    };
}
