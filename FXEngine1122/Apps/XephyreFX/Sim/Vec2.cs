namespace XephyreFX.Sim;

/// <summary>Minimal 2D point/vector so the simulation layer has zero WPF dependency.</summary>
public readonly struct Vec2
{
    public Vec2(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.X * s, a.Y * s);
}
