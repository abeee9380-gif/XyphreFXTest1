namespace XephyreFX.Sim;

/// <summary>
/// The "trick the app" testing feature. When <see cref="Enabled"/> is true, any field that
/// has a value here replaces the real value on the <see cref="WeatherState"/> for that frame.
/// Fields left null fall through to whatever the app would normally compute (real clock, etc.),
/// so you can override just the one thing you're testing (e.g. only Condition) without losing
/// the real time.
/// </summary>
public sealed class WeatherOverrideService
{
    public bool Enabled { get; set; }

    public WeatherCondition? Condition { get; set; }
    public SkyPeriod? Period { get; set; }
    public double? Intensity { get; set; }
    public bool? ForceValentines { get; set; }
    public DateTime? Time { get; set; }
    public double? TemperatureC { get; set; }

    public void Apply(WeatherState state)
    {
        if (!Enabled) return;

        if (Time.HasValue) state.LocalTime = Time.Value;
        if (Condition.HasValue) state.Condition = Condition.Value;
        if (Period.HasValue) state.Period = Period.Value;
        if (Intensity.HasValue) state.Intensity = Intensity.Value;
        if (ForceValentines.HasValue) state.ForceValentines = ForceValentines.Value;
        if (TemperatureC.HasValue) state.TemperatureC = TemperatureC.Value;
    }

    public void Clear()
    {
        Enabled = false;
        Condition = null;
        Period = null;
        Intensity = null;
        ForceValentines = null;
        Time = null;
        TemperatureC = null;
    }
}
