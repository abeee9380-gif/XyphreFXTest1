namespace XephyreFX.App.Sim;

/// <summary>
/// Owns every subsystem and decides, each frame, which ones should be active based on the
/// current <see cref="WeatherState"/>. This is deliberately the only place that branches on
/// weather condition/time-of-day -- every combination (rain + night, thunderstorm + Valentine's,
/// clear + morning, ...) falls out of the same handful of independent on/off flags rather than
/// needing its own special case.
/// </summary>
public sealed class WeatherSceneComposer
{
    private readonly Random _rng;
    private SkyPeriod? _lastPeriod;

    public BlobShape Blob { get; }
    public CloudSystem Clouds { get; }
    public RainSystem Rain { get; }
    public LightningSystem Lightning { get; }
    public StarField Stars { get; }
    public ValentinesOverlay Valentines { get; }

    public CelestialBody? CurrentCelestial { get; private set; }
    public CelestialBody? PreviousCelestial { get; private set; }

    public WeatherSceneComposer(int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        Blob = new BlobShape(_rng, 150);
        Clouds = new CloudSystem(_rng);
        Rain = new RainSystem(_rng);
        Lightning = new LightningSystem(_rng);
        Stars = new StarField(_rng);
        Valentines = new ValentinesOverlay(_rng);
    }

    /// <summary>Re-randomizes the blob's wobble personality (used by the debug panel).</summary>
    public void RerollBlob() => Blob.RerollStyle(_rng);

    public void Tick(double dt, WeatherState state, double sceneWidth, double sceneHeight, Vec2 blobCenter, Vec2 cloudAnchorOffset = default)
    {
        Blob.Tick(dt);

        bool cloudsActive = state.Condition is WeatherCondition.Cloudy or WeatherCondition.Rain or WeatherCondition.Thunderstorm;
        bool rainActive = state.Condition is WeatherCondition.Rain or WeatherCondition.Thunderstorm;
        bool stormActive = state.Condition == WeatherCondition.Thunderstorm;

        var cloudAnchor = blobCenter + cloudAnchorOffset;
        Clouds.Tick(dt, cloudsActive, cloudAnchor, Blob.BaseRadius);
        Rain.Tick(dt, rainActive, state.Intensity, Clouds.Clouds, sceneHeight + 40);
        Lightning.Tick(dt, stormActive, state.Intensity, Clouds.Clouds, sceneHeight);
        Stars.Tick(dt, state.IsNight, blobCenter, Blob.BaseRadius);
        Valentines.Tick(dt, state.IsValentinesToday || state.HasActiveEvent, blobCenter, Blob.BaseRadius);

        if (_lastPeriod != state.Period)
        {
            PreviousCelestial = CurrentCelestial;
            PreviousCelestial?.Life.RequestDespawn();
            CurrentCelestial = CelestialBody.Create(_rng, state.Period);
            _lastPeriod = state.Period;
        }

        CurrentCelestial?.Tick(dt);
        PreviousCelestial?.Tick(dt);
        if (PreviousCelestial is not null && PreviousCelestial.Life.IsDead)
        {
            PreviousCelestial = null;
        }
    }
}
