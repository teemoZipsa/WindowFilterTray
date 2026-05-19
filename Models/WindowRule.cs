namespace WindowFilterTray.Models;

public sealed class WindowRule
{
    public string Id { get; set; } = $"rule-{Guid.NewGuid():N}";
    public string DisplayName { get; set; } = "새 규칙";
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public RuleMatcher Matcher { get; set; } = new();
    public WindowActionType Action { get; set; } = WindowActionType.CloseWindow;
    public int GraceMs { get; set; } = 1000;
    public FrequencyCap FrequencyCap { get; set; } = new();
    public string? ThumbnailPath { get; set; }
}
