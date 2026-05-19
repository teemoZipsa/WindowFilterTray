namespace WindowFilterTray.Models;

public sealed class RuleMatcher
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowClass { get; set; } = string.Empty;
    public string? TitleContains { get; set; }
    public SizeHint? SizeHint { get; set; }
    public PositionHint? PositionHint { get; set; }
    public bool UseProcessName { get; set; } = true;
    public bool UseWindowClass { get; set; } = true;
    public bool UseTitle { get; set; }
    public bool UseSize { get; set; }
    public bool UsePosition { get; set; }
    public int MinScore { get; set; } = 60;
}

public sealed class SizeHint
{
    public int W { get; set; }
    public int H { get; set; }
    public double Tolerance { get; set; } = 0.2;
}

public sealed class PositionHint
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Tolerance { get; set; } = 0.2;
}
