namespace XephyreFX.Sim;

/// <summary>
/// Cheap, dependency-free 1D smooth-noise generator. Used anywhere something needs to
/// wander around randomly but *continuously* over time instead of jumping between values
/// every frame -- e.g. the blob's edge wobble, cloud drift, star twinkle.
/// </summary>
public static class NoiseGen
{
    /// <summary>Deterministic hash -> pseudo-random value in [0, 1) for an integer lattice point.</summary>
    public static double Hash(int seed, int x)
    {
        unchecked
        {
            int h = seed * 374761393 + x * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (double)int.MaxValue;
        }
    }

    /// <summary>
    /// Smoothly interpolated 1D noise for any real-valued <paramref name="t"/>.
    /// Returns a value in [0, 1). Continuous and derivative-friendly (smoothstep blend).
    /// </summary>
    public static double Smooth1D(int seed, double t)
    {
        int i0 = (int)Math.Floor(t);
        int i1 = i0 + 1;
        double f = t - i0;
        double v0 = Hash(seed, i0);
        double v1 = Hash(seed, i1);
        double u = f * f * (3 - 2 * f);
        return v0 + (v1 - v0) * u;
    }

    /// <summary>Same as <see cref="Smooth1D"/> but remapped to [-1, 1].</summary>
    public static double SmoothSigned(int seed, double t) => Smooth1D(seed, t) * 2 - 1;
}
