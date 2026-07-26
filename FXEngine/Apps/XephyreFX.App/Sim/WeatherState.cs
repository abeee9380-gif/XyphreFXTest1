namespace XephyreFX.App.Sim;

public enum SkyPeriod
{
    Morning,
    Noon,
    Afternoon,
    Evening,
    Sunset,
    Night
}

public enum WeatherCondition
{
    Clear,
    Cloudy,
    Rain,
    Thunderstorm
}

public sealed class ForecastEntry
{
    public ForecastEntry(string label, double tempC, WeatherCondition condition)
    {
        Label = label;
        TempC = tempC;
        Condition = condition;
    }

    public string Label { get; }
    public double TempC { get; }
    public WeatherCondition Condition { get; }
}

/// <summary>
/// Single source of truth for "what should the scene look like right now". The composer
/// only ever reads from this -- it never cares whether the values came from a real weather
/// feed, the system clock, or the debug override panel.
/// </summary>
public sealed class WeatherState
{
    public DateTime LocalTime { get; set; } = DateTime.Now;
    public double TemperatureC { get; set; } = 21;
    public WeatherCondition Condition { get; set; } = WeatherCondition.Clear;

    /// <summary>0..1. Drives rain/lightning frequency and cloud darkness. Ignored when Condition is Clear/Cloudy-only-light.</summary>
    public double Intensity { get; set; } = 0.4;

    public SkyPeriod Period { get; set; } = SkyPeriod.Noon;
    public bool ForceValentines { get; set; }

    public List<ForecastEntry> Forecast { get; } = new()
    {
        new ForecastEntry("Tomorrow", 22, WeatherCondition.Clear),
        new ForecastEntry("Wed", 19, WeatherCondition.Cloudy),
        new ForecastEntry("Thu", 17, WeatherCondition.Rain),
    };

    public bool IsNight => Period == SkyPeriod.Night;

    public bool IsValentinesToday => ForceValentines || (LocalTime.Month == 2 && LocalTime.Day == 14);

    /// <summary>Set by whichever custom Events/*.json file matches today's date, if any. Null means no custom event is active (Valentine's Day can still trigger independently via IsValentinesToday).</summary>
    public string? EventTintHex { get; set; }
    public string? EventName { get; set; }
    public bool HasActiveEvent => EventTintHex is not null;

    public static SkyPeriod PeriodFromClock(DateTime time)
    {
        int h = time.Hour;
        return h switch
        {
            >= 5 and < 9 => SkyPeriod.Morning,
            >= 9 and < 14 => SkyPeriod.Noon,
            >= 14 and < 17 => SkyPeriod.Afternoon,
            >= 17 and < 19 => SkyPeriod.Evening,
            >= 19 and < 21 => SkyPeriod.Sunset,
            _ => SkyPeriod.Night
        };
    }
}
