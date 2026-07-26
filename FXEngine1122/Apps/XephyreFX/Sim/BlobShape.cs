namespace XephyreFX.Sim;

/// <summary>
/// The pure-black central blob. Randomly generated (point count, wobble amplitude/speed) and
/// its outline drifts smoothly and continuously forever -- never a hard cut between shapes.
/// </summary>
public sealed class BlobShape
{
    public int Seed { get; private set; }
    public int PointCount { get; private set; }
    public double BaseRadius { get; set; }
    public double WobbleAmplitude { get; private set; }
    public double WobbleSpeed { get; private set; }

    /// <summary>User-adjustable multiplier on top of the randomized base speed -- 1.0 = normal, higher = faster morphing, lower = slower. Driven by the "Blob morph speed" slider.</summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    private double _time;

    public BlobShape(Random rng, double baseRadius)
    {
        BaseRadius = baseRadius;
        RerollStyle(rng);
    }

    /// <summary>Re-randomizes the blob's personality (used by the debug panel's "reroll" button) without a visual pop -- the new wobble simply takes over on the next tick.</summary>
    public void RerollStyle(Random rng)
    {
        Seed = rng.Next();
        PointCount = rng.Next(10, 15);
        WobbleAmplitude = BaseRadius * (0.08 + rng.NextDouble() * 0.10);
        WobbleSpeed = 0.05 + rng.NextDouble() * 0.07;
    }

    public void Tick(double dt) => _time += dt;

    /// <summary>Returns the current ring of outline points around <paramref name="center"/>.</summary>
    public Vec2[] GetPoints(Vec2 center)
    {
        var pts = new Vec2[PointCount];
        for (int i = 0; i < PointCount; i++)
        {
            double angle = 2 * Math.PI / PointCount * i;
            double n = NoiseGen.SmoothSigned(Seed + i * 97, _time * WobbleSpeed * SpeedMultiplier);
            double r = BaseRadius + n * WobbleAmplitude;
            pts[i] = new Vec2(center.X + Math.Cos(angle) * r, center.Y + Math.Sin(angle) * r);
        }
        return pts;
    }
}
