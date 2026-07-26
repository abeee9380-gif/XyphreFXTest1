namespace XephyreFX.Sim;

public sealed class Star
{
    public double X;
    public double Y;
    public double Radius;
    public int TwinkleSeed;
    public Lifecycle Life { get; } = new();
}

/// <summary>
/// Night-only star field. Uses the exact same random-gen + overlap-allowed + graph-based
/// appear/disappear approach as <see cref="CloudSystem"/>, spawned in a ring around the blob
/// so the stars stay clustered near the blob/cloud group instead of scattering across the
/// whole window, plus a gentle per-star twinkle.
/// </summary>
public sealed class StarField
{
    private readonly List<Star> _stars = new();
    private readonly Random _rng;
    private double _spawnTimer;
    private double _time;

    public IReadOnlyList<Star> Stars => _stars;

    public StarField(Random rng)
    {
        _rng = rng;
    }

    public void Tick(double dt, bool active, Vec2 blobCenter, double blobRadius)
    {
        _time += dt;

        if (active)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0 && _stars.Count < 50)
            {
                SpawnStar(blobCenter, blobRadius);
                _spawnTimer = 0.08 + _rng.NextDouble() * 0.3;
            }
        }
        else
        {
            foreach (var s in _stars) s.Life.RequestDespawn();
        }

        foreach (var s in _stars) s.Life.Tick(dt);
        _stars.RemoveAll(s => s.Life.IsDead);
    }

    /// <summary>Combined lifecycle opacity + twinkle oscillation for a given star, 0..1.</summary>
    public double TwinkleOpacity(Star s)
    {
        double twinkle = 0.55 + 0.45 * NoiseGen.Smooth1D(s.TwinkleSeed, _time * 0.6);
        return s.Life.Opacity * twinkle;
    }

    private void SpawnStar(Vec2 blobCenter, double blobRadius)
    {
        double angle = _rng.NextDouble() * Math.PI * 2;
        // Ring starting just outside the cloud cluster, out to a bit further -- keeps stars
        // wrapped around the blob + clouds instead of drifting off across the whole scene.
        double dist = blobRadius * (1.3 + _rng.NextDouble() * 2.0);

        var star = new Star
        {
            X = blobCenter.X + Math.Cos(angle) * dist,
            Y = blobCenter.Y + Math.Sin(angle) * dist * 0.85,
            Radius = 0.8 + _rng.NextDouble() * 1.8,
            TwinkleSeed = _rng.Next()
        };
        star.Life.FadeInDuration = 0.8 + _rng.NextDouble() * 1.5;
        star.Life.FadeOutDuration = 0.8 + _rng.NextDouble() * 1.5;
        _stars.Add(star);
    }
}
