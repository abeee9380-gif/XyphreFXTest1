namespace XephyreFX.App.Config;

/// <summary>
/// One entry in the Events/ folder. Drop a .json file shaped like this in and it becomes a
/// new date-triggered overlay -- same drifting-particle-plus-glow effect Valentine's Day
/// already uses, just with your own name, date, and color. No recompiling needed.
/// </summary>
public sealed class SpecialEventConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Month { get; set; } = 1;
    public int Day { get; set; } = 1;
    public string TintColorHex { get; set; } = "#FF5A82";
    public string ParticleColorHex { get; set; } = "#FF5C82";
}
