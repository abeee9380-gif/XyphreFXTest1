namespace XephyreFX.Sim;

public sealed class HeartParticle
{
    public double X;
    public double Y;
    public double Scale;
    public double RiseSpeed;
    public int SwaySeed;
    public Lifecycle Life { get; } = new();
}

/// <summary>
/// Feb 14 special (or forced via the debug panel). Layers on top of whatever weather is already
/// happening -- rain on Valentine's Day still rains, it just also gets hearts drifting near the
/// blob and a soft pink tint. Uses the same particle/lifecycle pattern as clouds and stars.
/// </summary>
public sealed class ValentinesOverlay
{
    private readonly List<HeartParticle> _hearts = new();
    private readonly Random _rng;
    private double _spawnTimer;

    public IReadOnlyList<HeartParticle> Hearts => _hearts;

    public ValentinesOverlay(Random rng)
    {
        _rng = rng;
    }

    public void Tick(double dt, bool active, Vec2 blobCenter, double blobRadius)
    {
        if (active)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0 && _hearts.Count < 14)
            {
                SpawnHeart(blobCenter, blobRadius);
                _spawnTimer = 0.4 + _rng.NextDouble() * 0.7;
            }
        }
        else
        {
            foreach (var h in _hearts) h.Life.RequestDespawn();
        }

        foreach (var h in _hearts)
        {
            h.Life.Tick(dt);
            h.Y -= h.RiseSpeed * dt;
        }

        _hearts.RemoveAll(h => h.Life.IsDead);
    }

    private void SpawnHeart(Vec2 blobCenter, double blobRadius)
    {
        double angle = _rng.NextDouble() * Math.PI * 2;
        double dist = blobRadius * (0.6 + _rng.NextDouble() * 1.1);
        var heart = new HeartParticle
        {
            X = blobCenter.X + Math.Cos(angle) * dist,
            Y = blobCenter.Y + Math.Sin(angle) * dist,
            Scale = 0.5 + _rng.NextDouble() * 0.9,
            RiseSpeed = 8 + _rng.NextDouble() * 14,
            SwaySeed = _rng.Next()
        };
        heart.Life.FadeInDuration = 0.8;
        heart.Life.HoldDuration = 4 + _rng.NextDouble() * 3;
        heart.Life.FadeOutDuration = 1.2;
        _hearts.Add(heart);
    }
}
