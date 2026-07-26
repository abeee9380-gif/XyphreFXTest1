namespace XephyreFX.Sim;

public sealed class RainDrop
{
    public double X;
    public double Y;
    public double Length;
    public double FallSpeed;
    public double Drift;
    public double Thickness;
    public double Age;
    public double MaxAge;
}

/// <summary>
/// Rain drops fall from whichever clouds are currently on screen. Spawn rate, fall speed,
/// streak length, and thickness all scale with <see cref="WeatherState.Intensity"/> -- that's
/// what "how hard it's raining" means visually, pushed further so light vs. heavy rain
/// actually looks distinct rather than just "slightly more dots".
/// </summary>
public sealed class RainSystem
{
    private readonly List<RainDrop> _drops = new();
    private readonly Random _rng;
    private double _spawnAccumulator;

    public IReadOnlyList<RainDrop> Drops => _drops;

    public RainSystem(Random rng)
    {
        _rng = rng;
    }

    public void Tick(double dt, bool active, double intensity, IReadOnlyList<Cloud> clouds, double sceneBottom)
    {
        if (active && clouds.Count > 0)
        {
            double dropsPerSecond = 8 + intensity * 70;
            _spawnAccumulator += dropsPerSecond * dt;
            while (_spawnAccumulator >= 1)
            {
                SpawnDrop(clouds, intensity);
                _spawnAccumulator -= 1;
            }
        }

        foreach (var d in _drops)
        {
            d.Age += dt;
            d.Y += d.FallSpeed * dt;
            d.X += d.Drift * dt;
        }

        _drops.RemoveAll(d => d.Y > sceneBottom || d.Age > d.MaxAge);
    }

    private void SpawnDrop(IReadOnlyList<Cloud> clouds, double intensity)
    {
        var cloud = clouds[_rng.Next(clouds.Count)];
        _drops.Add(new RainDrop
        {
            X = cloud.X + (_rng.NextDouble() - 0.5) * cloud.Width,
            Y = cloud.Y + cloud.Height * 0.4,
            Length = 8 + intensity * 16 + _rng.NextDouble() * 10,
            FallSpeed = 220 + intensity * 520 + _rng.NextDouble() * 60,
            Drift = (_rng.NextDouble() - 0.5) * (16 + intensity * 20),
            Thickness = 1.1 + intensity * 1.6,
            MaxAge = 4
        });
    }
}
