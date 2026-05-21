using System.Text.Json.Serialization;

namespace WindowFilterTray.Models;

public sealed class WindowSnapshot
{
    public IntPtr HWnd { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public WindowRect Rect { get; set; }
    public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public string Summary => string.IsNullOrWhiteSpace(Title)
        ? $"{ProcessName} / {ClassName}"
        : $"{Title} ({ProcessName})";
}

public readonly record struct WindowRect(int Left, int Top, int Right, int Bottom)
{
    [JsonIgnore]
    public int Width => Math.Max(0, Right - Left);
    [JsonIgnore]
    public int Height => Math.Max(0, Bottom - Top);
}
