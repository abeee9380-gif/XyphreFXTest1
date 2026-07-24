namespace XephyreFX.App.Sim;

/// <summary>
/// Easing curves used to drive every "appear / disappear" transition in the scene
/// (clouds, stars, the sun/moon, hearts, the blob's own spawn). Everything fades
/// along one of these curves instead of popping in or out.
/// </summary>
public static class Easing
{
    public static double InOutCubic(double t)
    {
        t = Clamp01(t);
        return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
    }

    public static double InOutSine(double t)
    {
        t = Clamp01(t);
        return -(Math.Cos(Math.PI * t) - 1) / 2;
    }

    public static double OutQuad(double t)
    {
        t = Clamp01(t);
        return 1 - (1 - t) * (1 - t);
    }

    /// <summary>Sharp attack, slow decay — used for lightning flashes.</summary>
    public static double SpikeDecay(double t)
    {
        t = Clamp01(t);
        if (t < 0.08) return t / 0.08;
        double tail = (t - 0.08) / (1 - 0.08);
        return Math.Pow(1 - tail, 2.2);
    }

    private static double Clamp01(double t) => t < 0 ? 0 : (t > 1 ? 1 : t);
}
