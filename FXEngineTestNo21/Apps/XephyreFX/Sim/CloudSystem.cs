namespace XephyreFX.Sim;

public sealed class Cloud
{
    public double X;
    public double Y;
    public double Width;
    public double Height;
    public double SpeedX;
    public int DriftSeed;
    public Lifecycle Life { get; } = new();
}

/// <summary>
/// Many overlapping, randomly generated clouds that drift around near the blob. Color shifts
/// from light grey to storm-dark as <see cref="WeatherState.Intensity"/> climbs -- that's the
/// "how fast it's raining determines the cloud color" rule.
/// </summary>
public sealed class CloudSystem
{
    private readonly List<Cloud> _clouds = new();
    private readonly Random _rng;
    private double _spawnTimer;

    public IReadOnlyList<Cloud> Clouds => _clouds;

    public CloudSystem(Random rng)
    {
        _rng = rng;
    }

    public void Tick(double dt, bool active, Vec2 blobCenter, double blobRadius)
    {
        if (active)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0 && _clouds.Count < 9)
            {
                SpawnCloud(blobCenter, blobRadius);
                _spawnTimer = 0.6 + _rng.NextDouble() * 1.2;
            }
        }
        else
        {
            foreach (var c in _clouds) c.Life.RequestDespawn();
        }

        // Bug fix: nothing used to stop a cloud from drifting indefinitely, so over time
        // clouds wandered off across the whole window. Now anything that strays too far from
        // the blob gets told to fade out (a new one spawns nearby to replace it) instead of
        // continuing to drift away forever.
        double maxDrift = blobRadius * 2.6;
        foreach (var c in _clouds)
        {
            c.Life.Tick(dt);
            c.X += c.SpeedX * dt;

            if (Math.Abs(c.X - blobCenter.X) > maxDrift)
            {
                c.Life.RequestDespawn();
            }
        }

        _clouds.RemoveAll(c => c.Life.IsDead);
    }

    private void SpawnCloud(Vec2 blobCenter, double blobRadius)
    {
        double angle = _rng.NextDouble() * Math.PI * 2;
        double dist = blobRadius * (0.55 + _rng.NextDouble() * 0.9);
        var cloud = new Cloud
        {
            X = blobCenter.X + Math.Cos(angle) * dist,
            Y = blobCenter.Y + Math.Sin(angle) * dist * 0.6 - blobRadius * 0.3,
            Width = blobRadius * (0.5 + _rng.NextDouble() * 0.5),
            Height = blobRadius * (0.22 + _rng.NextDouble() * 0.18),
            // Gentler drift than before -- clouds should wander a little, not migrate away.
            SpeedX = (_rng.NextDouble() - 0.5) * 6,
            DriftSeed = _rng.Next()
        };
        cloud.Life.FadeInDuration = 1.0 + _rng.NextDouble() * 0.8;
        cloud.Life.FadeOutDuration = 1.0 + _rng.NextDouble() * 0.8;
        _clouds.Add(cloud);
    }

    /// <summary>
    /// Light drizzle grey to near-black storm slate as intensity climbs, blended with a
    /// period-appropriate sky tint -- real clouds pick up the color of the light around them
    /// (pink at sunset, gold in the morning, cool blue at night), and heavier storms wash that
    /// tint out toward neutral grey.
    /// </summary>
    public static RgbColor ColorForIntensity(double intensity, SkyPeriod period)
    {
        intensity = Math.Clamp(intensity, 0, 1);
        byte lo = 28, hi = 205;
        byte gr = (byte)(hi - (hi - lo) * intensity);
        byte gg = (byte)(hi - (hi - (lo + 2)) * intensity);
        byte gb = (byte)((hi + 8) - ((hi + 8) - (lo + 10)) * intensity);

        (byte r, byte g, byte b) tint = period switch
        {
            SkyPeriod.Sunset => (255, 140, 120),
            SkyPeriod.Evening => (255, 175, 140),
            SkyPeriod.Morning => (255, 214, 170),
            SkyPeriod.Night => (140, 150, 205),
            _ => (gr, gg, gb) // noon/afternoon: no tint, just natural grey
        };

        double tintStrength = 0.24 * (1 - intensity * 0.7);
        byte fr = (byte)Math.Clamp(gr * (1 - tintStrength) + tint.r * tintStrength, 0, 255);
        byte fg = (byte)Math.Clamp(gg * (1 - tintStrength) + tint.g * tintStrength, 0, 255);
        byte fb = (byte)Math.Clamp(gb * (1 - tintStrength) + tint.b * tintStrength, 0, 255);
        return new RgbColor(fr, fg, fb);
    }
}
