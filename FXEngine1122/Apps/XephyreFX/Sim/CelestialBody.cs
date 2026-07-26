namespace XephyreFX.Sim;

/// <summary>
/// The moon and every flavor of sun (morning/noon/afternoon/evening/sunset) are all the same
/// object type. They sit at the same fixed spot, don't arc across the sky, and don't get any
/// unique animation of their own -- the only thing that changes between periods is the random-
/// generated color/size style, and it fades in/out like everything else via <see cref="Life"/>.
/// </summary>
public sealed class CelestialBody
{
    public SkyPeriod Period { get; private set; }
    public double Radius { get; private set; }
    public double GlowRadius { get; private set; }
    public RgbColor CoreColor { get; private set; }
    public RgbColor GlowColor { get; private set; }
    public Lifecycle Life { get; } = new();

    private CelestialBody() { }

    public static CelestialBody Create(Random rng, SkyPeriod period)
    {
        var body = new CelestialBody { Period = period };
        body.Radius = 30 + rng.NextDouble() * 12;
        body.GlowRadius = body.Radius * (1.5 + rng.NextDouble() * 0.9);

        (RgbColor core, RgbColor glow) style = period switch
        {
            SkyPeriod.Night => (new RgbColor(232, 233, 242), new RgbColor(160, 172, 255, 90)),
            SkyPeriod.Morning => (new RgbColor(255, 236, 179), new RgbColor(255, 214, 140, 90)),
            SkyPeriod.Noon => (new RgbColor(255, 246, 214), new RgbColor(255, 240, 180, 90)),
            SkyPeriod.Afternoon => (new RgbColor(255, 222, 160), new RgbColor(255, 190, 120, 90)),
            SkyPeriod.Evening => (new RgbColor(255, 175, 120), new RgbColor(255, 120, 90, 100)),
            SkyPeriod.Sunset => (new RgbColor(255, 120, 90), new RgbColor(210, 70, 90, 110)),
            _ => (new RgbColor(255, 255, 255), new RgbColor(255, 255, 255, 80))
        };
        body.CoreColor = style.core;
        body.GlowColor = style.glow;

        // Small random jitter so it never looks like a stamped-out asset even within the same period.
        body.Radius *= 0.94 + rng.NextDouble() * 0.12;

        body.Life.FadeInDuration = 1.4;
        body.Life.FadeOutDuration = 1.4;
        body.Life.HoldDuration = -1;
        return body;
    }

    public void Tick(double dt) => Life.Tick(dt);
}
