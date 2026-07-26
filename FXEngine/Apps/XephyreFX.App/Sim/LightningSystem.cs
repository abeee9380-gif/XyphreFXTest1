namespace XephyreFX.App.Sim;

public sealed class LightningBolt
{
    public Vec2[] Points = Array.Empty<Vec2>();
    public double Thickness;
    public double Age;
    public double MaxAge;
}

/// <summary>
/// Thunderstorm-only: jagged bolts randomly generated via midpoint displacement, plus a brief
/// whole-scene flash. Frequency, thickness, length, and flash brightness all scale with
/// intensity, same as the rain -- a light storm gets the occasional thin bolt, a heavy one
/// gets frequent thick strikes with occasional simultaneous double-strikes.
/// </summary>
public sealed class LightningSystem
{
    private readonly List<LightningBolt> _bolts = new();
    private readonly Random _rng;
    private double _nextStrikeIn;

    public IReadOnlyList<LightningBolt> Bolts => _bolts;

    /// <summary>Current flash brightness, 0..1. Add this as a white overlay across the whole scene.</summary>
    public double FlashOpacity { get; private set; }

    public LightningSystem(Random rng)
    {
        _rng = rng;
        _nextStrikeIn = 2 + rng.NextDouble() * 3;
    }

    public void Tick(double dt, bool active, double intensity, IReadOnlyList<Cloud> clouds, double sceneBottom)
    {
        if (active && clouds.Count > 0)
        {
            _nextStrikeIn -= dt;
            if (_nextStrikeIn <= 0)
            {
                Strike(clouds, sceneBottom, intensity);
                if (intensity > 0.7 && _rng.NextDouble() < 0.35)
                {
                    Strike(clouds, sceneBottom, intensity); // occasional double-strike in a heavy storm
                }
                double gap = Math.Max(0.35, 4.2 - intensity * 3.6);
                _nextStrikeIn = gap * (0.6 + _rng.NextDouble() * 0.8);
            }
        }
        else
        {
            _nextStrikeIn = Math.Max(_nextStrikeIn, 2);
        }

        double flash = 0;
        foreach (var b in _bolts)
        {
            b.Age += dt;
            flash = Math.Max(flash, Easing.SpikeDecay(b.Age / b.MaxAge));
        }
        FlashOpacity = flash;

        _bolts.RemoveAll(b => b.Age > b.MaxAge);
    }

    private void Strike(IReadOnlyList<Cloud> clouds, double sceneBottom, double intensity)
    {
        var cloud = clouds[_rng.Next(clouds.Count)];
        double startX = cloud.X + (_rng.NextDouble() - 0.5) * cloud.Width * 0.5;
        double startY = cloud.Y + cloud.Height * 0.3;
        double reach = 140 + intensity * 110 + _rng.NextDouble() * 90;
        double endY = Math.Min(sceneBottom, startY + reach);

        _bolts.Add(new LightningBolt
        {
            Points = BuildBoltPath(new Vec2(startX, startY), new Vec2(startX + (_rng.NextDouble() - 0.5) * 30, endY), 5),
            Thickness = 1.8 + intensity * 2.4,
            MaxAge = 0.55
        });
    }

    private Vec2[] BuildBoltPath(Vec2 start, Vec2 end, int subdivisions)
    {
        var points = new List<Vec2> { start, end };
        for (int pass = 0; pass < subdivisions; pass++)
        {
            var next = new List<Vec2> { points[0] };
            double spread = 18.0 / (pass + 1);
            for (int i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var mid = new Vec2((a.X + b.X) / 2 + (_rng.NextDouble() - 0.5) * spread, (a.Y + b.Y) / 2);
                next.Add(mid);
                next.Add(b);
            }
            points = next;
        }
        return points.ToArray();
    }
}
