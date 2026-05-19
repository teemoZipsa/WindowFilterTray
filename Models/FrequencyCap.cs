namespace WindowFilterTray.Models;

public sealed class FrequencyCap
{
    public FrequencyCapMode Mode { get; set; } = FrequencyCapMode.PerDay;
    public int MaxImpressions { get; set; } = 1;
}
