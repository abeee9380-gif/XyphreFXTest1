using Avalonia;
using Avalonia.Media;
using XephyreFX.App.Sim;

namespace XephyreFX.App.Rendering;

/// <summary>Turns a ring of points into a smooth closed curve so the blob never looks polygonal.</summary>
public static class CatmullRom
{
    public static PathGeometry BuildClosedCurve(Vec2[] points)
    {
        int n = points.Length;
        var figure = new PathFigure
        {
            StartPoint = new Point(points[0].X, points[0].Y),
            IsClosed = true,
            IsFilled = true,
            Segments = new PathSegments()
        };

        for (int i = 0; i < n; i++)
        {
            Vec2 p0 = points[(i - 1 + n) % n];
            Vec2 p1 = points[i];
            Vec2 p2 = points[(i + 1) % n];
            Vec2 p3 = points[(i + 2) % n];

            var b1 = new Point(p1.X + (p2.X - p0.X) / 6.0, p1.Y + (p2.Y - p0.Y) / 6.0);
            var b2 = new Point(p2.X - (p3.X - p1.X) / 6.0, p2.Y - (p3.Y - p1.Y) / 6.0);
            var b3 = new Point(p2.X, p2.Y);

            figure.Segments.Add(new BezierSegment { Point1 = b1, Point2 = b2, Point3 = b3 });
        }

        var geometry = new PathGeometry { Figures = new PathFigures() };
        geometry.Figures.Add(figure);
        return geometry;
    }
}
