using System.Text.Json.Serialization;

namespace WindowFilterTray.Models;

public sealed class WindowRule
{
    public string Id { get; set; } = $"rule-{Guid.NewGuid():N}";
    public string DisplayName { get; set; } = "정리할 창";
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public RuleMatcher Matcher { get; set; } = new();
    public WindowActionType Action { get; set; } = WindowActionType.Minimize;
    public int GraceMs { get; set; } = 1000;
    public FrequencyCap FrequencyCap { get; set; } = new();
    public string? ThumbnailPath { get; set; }

    [JsonIgnore]
    public string DisplayAction => Action switch
    {
        WindowActionType.Minimize => "작게 내리기",
        WindowActionType.HideWindow => "숨기기",
        WindowActionType.CloseWindow => "닫기",
        WindowActionType.Ignore => "기록만",
        _ => Action.ToString()
    };

    [JsonIgnore]
    public string DisplayBasis
    {
        get
        {
            var parts = new List<string>();
            if (Matcher.UseProcessName)
            {
                parts.Add("앱");
            }

            if (Matcher.UseWindowClass)
            {
                parts.Add("창 모양");
            }

            if (Matcher.UseTitle)
            {
                parts.Add("제목");
            }

            if (Matcher.UseSize)
            {
                parts.Add("크기");
            }

            if (Matcher.UsePosition)
            {
                parts.Add("위치");
            }

            return parts.Count == 0 ? "사용자 설정" : string.Join(", ", parts);
        }
    }
}
