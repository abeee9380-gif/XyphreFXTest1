using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using XephyreFX.App.Config;
using XephyreFX.App.Sim;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace XephyreFX.App.Rendering;

/// <summary>
/// Immediate-mode renderer: every frame it clears the canvas and redraws whatever the
/// <see cref="WeatherSceneComposer"/> currently holds. This is the only class in the project
/// that touches Avalonia drawing types -- everything upstream of it (Sim/*) is plain C# and
/// knows nothing about UI frameworks.
/// </summary>
public sealed class AvaloniaSceneRenderer
{
    private static readonly Dictionary<string, Bitmap?> ImageCache = new();
    private readonly Dictionary<string, Rect> _lastElementBounds = new();

    /// <summary>Screen-space bounds of each text element (keys: Time, Date, Temperature, Condition, Forecast) from the most recent Draw() call, used by MainWindow for click-to-select and drag-to-reposition.</summary>
    public IReadOnlyDictionary<string, Rect> LastElementBounds => _lastElementBounds;

    /// <summary>Which element (if any) should be drawn with a selection outline. Set by MainWindow when the user clicks a text element.</summary>
    public string? SelectedElementKey { get; set; }

    public void Draw(Canvas canvas, WeatherSceneComposer scene, WeatherState state, Vec2 blobCenter, XephyreFXConfig config)
    {
        canvas.Children.Clear();

        double w = canvas.Bounds.Width;
        double h = canvas.Bounds.Height;
        if (w <= 0 || h <= 0) return;

        // No background fill here on purpose -- this is meant to sit on top of the desktop
        // eventually (Rainmeter-style), so it shouldn't paint its own backdrop. The window
        // itself is set to a transparent background in MainWindow.axaml.

        if (state.IsNight)
        {
            DrawStars(canvas, scene.Stars);
        }

        if (scene.PreviousCelestial is { } prev)
        {
            DrawCelestial(canvas, prev, blobCenter, config.Celestial, selectable: false);
        }
        if (scene.CurrentCelestial is { } cur)
        {
            DrawCelestial(canvas, cur, blobCenter, config.Celestial, selectable: true);
        }

        bool eventActive = state.IsValentinesToday || state.HasActiveEvent;
        Color eventTint = ParseColorOr(state.EventTintHex, Color.FromRgb(255, 90, 130));

        if (eventActive)
        {
            DrawGlow(canvas, blobCenter, Color.FromArgb(90, eventTint.R, eventTint.G, eventTint.B), scene.Blob.BaseRadius * 1.8);
        }

        // Everything is lit from wherever the sun/moon actually is right now, not a fixed
        // corner -- drag the celestial body and the blob's (and clouds') highlight follows.
        var celestialOffset = new Vec2(config.Celestial.OffsetX, config.Celestial.OffsetY);

        // Blob's body goes down first -- clouds/rain/lightning render on top of it, in front,
        // rather than being hidden behind it. The info text is drawn last (see bottom of this
        // method) so it stays legible even when a cloud passes over that spot.
        DrawBlobShape(canvas, scene.Blob, blobCenter, eventActive, eventTint, config.Blob, celestialOffset);

        DrawClouds(canvas, scene.Clouds, state.Intensity, state.Period, celestialOffset);
        DrawRain(canvas, scene.Rain);
        DrawLightning(canvas, scene.Lightning);
        DrawCloudAnchorHandle(canvas, blobCenter, config.CloudAnchor);

        if (scene.Lightning.FlashOpacity > 0.001)
        {
            var flash = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = Brushes.White,
                Opacity = scene.Lightning.FlashOpacity * 0.7
            };
            Canvas.SetLeft(flash, 0);
            Canvas.SetTop(flash, 0);
            canvas.Children.Add(flash);
        }

        DrawTextElements(canvas, blobCenter, state, config.Text);
        DrawCustomElements(canvas, blobCenter, config.CustomElements);
        DrawHearts(canvas, scene.Valentines, eventTint);
    }

    private static Color ParseColorOr(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return Color.Parse(hex); }
        catch { return fallback; }
    }

    private static Bitmap? TryLoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (ImageCache.TryGetValue(path, out var cached)) return cached;

        Bitmap? bmp = null;
        try
        {
            if (File.Exists(path))
            {
                using var stream = File.OpenRead(path);
                bmp = new Bitmap(stream);
            }
        }
        catch
        {
            // Bad/corrupt/missing image -- fall back to the procedural sun/moon instead of
            // crashing. Cached as null so we don't retry the broken file every single frame.
        }

        ImageCache[path] = bmp;
        return bmp;
    }

    private static void DrawStars(Canvas canvas, StarField stars)
    {
        foreach (var s in stars.Stars)
        {
            double op = stars.TwinkleOpacity(s);
            if (op <= 0.01) continue;

            // Real starlight isn't uniformly white -- a bit of per-star warm/cool drift reads
            // more like an actual night sky than a field of identical white dots.
            double warmth = NoiseGen.Smooth1D(s.TwinkleSeed + 5, 0.2); // 0..1
            var starColor = Color.FromRgb(
                (byte)Math.Clamp(215 + warmth * 40, 0, 255),
                (byte)Math.Clamp(222 + warmth * 25, 0, 255),
                (byte)Math.Clamp(255 - warmth * 45, 0, 255));

            var dot = new Ellipse
            {
                Width = s.Radius * 2,
                Height = s.Radius * 2,
                Fill = new SolidColorBrush(starColor),
                Opacity = op
            };
            Canvas.SetLeft(dot, s.X - s.Radius);
            Canvas.SetTop(dot, s.Y - s.Radius);
            canvas.Children.Add(dot);
        }
    }

    private void DrawCelestial(Canvas canvas, CelestialBody body, Vec2 blobCenter, CelestialConfig config, bool selectable)
    {
        double opacity = body.Life.Opacity;
        if (opacity <= 0.01) return;

        // Plain pixel offset from the blob's center -- draggable in the scene like every other
        // element, rather than being locked to a fixed multiple of the blob's radius.
        var pos = new Point(blobCenter.X + config.OffsetX, blobCenter.Y + config.OffsetY);
        double radius = body.Radius * (config.Scale <= 0 ? 1.0 : config.Scale);

        string? imagePath = config.ImagesByPeriod.TryGetValue(body.Period.ToString(), out var p) ? p : null;
        var customImage = TryLoadImage(imagePath);
        if (customImage is not null)
        {
            var image = new Image
            {
                Source = customImage,
                Width = radius * 2,
                Height = radius * 2,
                Opacity = opacity,
                Stretch = Stretch.Uniform
            };
            Canvas.SetLeft(image, pos.X - radius);
            Canvas.SetTop(image, pos.Y - radius);
            canvas.Children.Add(image);
        }
        else
        {
            DrawGlow(canvas, new Vec2(pos.X, pos.Y), ToAvaloniaColor(body.GlowColor), body.GlowRadius, opacity);

            var coreBrush = new RadialGradientBrush
            {
                GradientOrigin = new RelativePoint(0.36, 0.32, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Shade(body.CoreColor, 0.3), 0),
                    new GradientStop(ToAvaloniaColor(body.CoreColor), 0.62),
                    new GradientStop(Shade(body.CoreColor, -0.2), 1)
                }
            };
            var core = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = coreBrush,
                Opacity = opacity
            };
            Canvas.SetLeft(core, pos.X - radius);
            Canvas.SetTop(core, pos.Y - radius);
            canvas.Children.Add(core);
        }

        if (!selectable) return; // the fading-out previous body during a day/night transition isn't draggable, only the current one is

        var bounds = new Rect(pos.X - radius, pos.Y - radius, radius * 2, radius * 2);
        _lastElementBounds["Celestial"] = bounds;
        if (SelectedElementKey == "Celestial") DrawSelectionOutline(canvas, bounds);
    }

    /// <summary>Soft radial glow, reused for celestial bodies and the Valentine's blob halo.</summary>
    private static void DrawGlow(Canvas canvas, Vec2 center, Color color, double radius, double opacity = 1)
    {
        var transparent = Color.FromArgb(0, color.R, color.G, color.B);
        var brush = new RadialGradientBrush
        {
            GradientStops = { new GradientStop(color, 0), new GradientStop(transparent, 1) }
        };
        var glow = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = brush, Opacity = opacity };
        Canvas.SetLeft(glow, center.X - radius);
        Canvas.SetTop(glow, center.Y - radius);
        canvas.Children.Add(glow);
    }

    /// <summary>A small, mostly-transparent, always-present drag handle marking where clouds spawn/gather, since the cloud cluster itself is a shifting group of particles with no fixed single thing to click.</summary>
    private void DrawCloudAnchorHandle(Canvas canvas, Vec2 blobCenter, PositionConfig anchor)
    {
        double x = blobCenter.X + anchor.OffsetX;
        double y = blobCenter.Y + anchor.OffsetY;
        const double r = 11;

        var dot = new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = new SolidColorBrush(Color.FromArgb(130, 255, 255, 255))
        };
        Canvas.SetLeft(dot, x - r);
        Canvas.SetTop(dot, y - r);
        canvas.Children.Add(dot);

        var bounds = new Rect(x - r, y - r, r * 2, r * 2);
        _lastElementBounds["CloudAnchor"] = bounds;
        if (SelectedElementKey == "CloudAnchor") DrawSelectionOutline(canvas, bounds);
    }

    /// <summary>
    /// Turns the offset from the blob to the celestial body into a light direction for gradient
    /// brushes, so the blob (and clouds) look lit from wherever the sun/moon actually is right
    /// now instead of a hardcoded corner. Also returns an intensity factor (0..1) that's highest
    /// when the sun/moon is close to the blob and fades as it moves farther away.
    /// </summary>
    private static (RelativePoint highlight, RelativePoint shadow, double intensity) LightDirection(Vec2 celestialOffset)
    {
        double len = Math.Sqrt(celestialOffset.X * celestialOffset.X + celestialOffset.Y * celestialOffset.Y);
        if (len < 1)
        {
            return (new RelativePoint(1, 0, RelativeUnit.Relative), new RelativePoint(0, 1, RelativeUnit.Relative), 0.3);
        }

        double nx = celestialOffset.X / len;
        double ny = celestialOffset.Y / len;

        double hx = Math.Clamp(0.5 + nx * 0.5, 0, 1);
        double hy = Math.Clamp(0.5 + ny * 0.5, 0, 1);
        double sx = Math.Clamp(0.5 - nx * 0.5, 0, 1);
        double sy = Math.Clamp(0.5 - ny * 0.5, 0, 1);

        // Close to the blob = bright (strong, direct light); dragged far away = dim. Roughly
        // 80..500px maps 1..0 -- this was backwards before, which is why it looked wrong.
        double intensity = 1 - Math.Clamp((len - 80) / 420.0, 0, 1);

        return (new RelativePoint(hx, hy, RelativeUnit.Relative), new RelativePoint(sx, sy, RelativeUnit.Relative), intensity);
    }

    private static void DrawClouds(Canvas canvas, CloudSystem clouds, double intensity, SkyPeriod period, Vec2 celestialOffset)
    {
        var baseColor = CloudSystem.ColorForIntensity(intensity, period);
        var (highlightOrigin, _, _) = LightDirection(celestialOffset);

        foreach (var c in clouds.Clouds)
        {
            double op = c.Life.Opacity * 0.94;
            if (op <= 0.01) continue;

            // Flat, slightly darker underside first -- reads as the shadowed bottom of a real
            // cumulus cloud instead of everything being one flat, uniform color.
            var shadowBrush = new SolidColorBrush(Shade(baseColor, -0.22));
            AddPuff(canvas, c.X, c.Y + c.Height * 0.22, c.Width * 0.92, c.Height * 0.55, shadowBrush, op);

            // A handful of overlapping puffs along a bump curve, bigger toward the middle and
            // smaller/lower toward the edges (like a real cloud's rounded top), each with its
            // own small size jitter and a soft radial highlight (brightest near the upper-left,
            // fading toward the puff's own shade at the rim) for a rounded, lit look instead of
            // a flat disc. Everything here is derived purely from the cloud's own seed, so it's
            // stable frame to frame without needing extra stored state.
            int puffCount = 5 + (int)(NoiseGen.Smooth1D(c.DriftSeed + 7, 0.5) * 3); // 5..7
            for (int i = 0; i < puffCount; i++)
            {
                double t = puffCount == 1 ? 0 : (double)i / (puffCount - 1) - 0.5; // -0.5..0.5
                double bump = Math.Max(0.35, 1 - Math.Abs(t) * 1.5);

                double jitterX = NoiseGen.SmoothSigned(c.DriftSeed + i * 31, 0.41) * c.Width * 0.05;
                double jitterY = NoiseGen.SmoothSigned(c.DriftSeed + i * 53, 0.77) * c.Height * 0.15;
                double sizeJitter = 0.85 + NoiseGen.Smooth1D(c.DriftSeed + i * 19, 0.6) * 0.3;

                double puffW = c.Width * (0.34 + bump * 0.34) * sizeJitter;
                double puffH = puffW * (0.72 + NoiseGen.Smooth1D(c.DriftSeed + i * 41, 0.3) * 0.2);
                double cx = c.X + t * c.Width * 0.85 + jitterX;
                double cy = c.Y - bump * c.Height * 0.42 + jitterY;

                double lightness = (NoiseGen.Smooth1D(c.DriftSeed + i * 61, 0.9) - 0.5) * 0.16;
                var puffBrush = new RadialGradientBrush
                {
                    GradientOrigin = highlightOrigin,
                    GradientStops =
                    {
                        new GradientStop(Shade(baseColor, lightness + 0.28), 0),
                        new GradientStop(Shade(baseColor, lightness + 0.04), 0.45),
                        new GradientStop(Shade(baseColor, lightness - 0.09), 1)
                    }
                };

                AddPuff(canvas, cx, cy, puffW, puffH, puffBrush, op);
            }
        }
    }

    /// <summary>Lightens (positive delta) or darkens (negative delta) a color, roughly -0.3..0.3.</summary>
    private static Color Shade(RgbColor c, double delta)
    {
        double factor = 1 + delta;
        byte r = (byte)Math.Clamp(c.R * factor, 0, 255);
        byte g = (byte)Math.Clamp(c.G * factor, 0, 255);
        byte b = (byte)Math.Clamp(c.B * factor, 0, 255);
        return Color.FromArgb(c.A, r, g, b);
    }

    private static void AddPuff(Canvas canvas, double cx, double cy, double width, double height, IBrush brush, double opacity)
    {
        var ellipse = new Ellipse { Width = width, Height = height, Fill = brush, Opacity = opacity };
        Canvas.SetLeft(ellipse, cx - width / 2);
        Canvas.SetTop(ellipse, cy - height / 2);
        canvas.Children.Add(ellipse);
    }

    private static void DrawRain(Canvas canvas, RainSystem rain)
    {
        var brush = new SolidColorBrush(Color.FromArgb(190, 190, 210, 235));
        foreach (var d in rain.Drops)
        {
            var line = new Line
            {
                StartPoint = new Point(d.X, d.Y),
                EndPoint = new Point(d.X + d.Drift * 0.05, d.Y + d.Length),
                Stroke = brush,
                StrokeThickness = d.Thickness
            };
            canvas.Children.Add(line);
        }
    }

    private static void DrawLightning(Canvas canvas, LightningSystem lightning)
    {
        foreach (var bolt in lightning.Bolts)
        {
            double op = 1 - Easing.InOutCubic(bolt.Age / bolt.MaxAge);
            if (op <= 0.02 || bolt.Points.Length < 2) continue;

            var poly = new Polyline { Stroke = Brushes.White, StrokeThickness = bolt.Thickness, Opacity = op };
            foreach (var p in bolt.Points) poly.Points.Add(new Point(p.X, p.Y));
            canvas.Children.Add(poly);
        }
    }

    private static void DrawBlobShape(Canvas canvas, BlobShape blob, Vec2 center, bool eventActive, Color eventTint, BlobConfig config, Vec2 celestialOffset)
    {
        var points = blob.GetPoints(center);
        var geometry = CatmullRom.BuildClosedCurve(points);

        IBrush fill;
        double lightIntensity = 0.3;

        if (eventActive)
        {
            fill = new SolidColorBrush(DarkenTowardBlack(eventTint, 0.94));
        }
        else
        {
            var highlight = ParseColorOr(config.HighlightColorHex, Color.FromRgb(30, 28, 34));
            var baseColor = ParseColorOr(config.BaseColorHex, Colors.Black);
            var (highlightPoint, shadowPoint, intensity) = LightDirection(celestialOffset);
            lightIntensity = intensity;

            // The closer the sun/moon is to the blob, the brighter/lighter this highlight reads.
            // A third mid-stop (instead of just two) smooths the falloff instead of a blunt
            // two-tone gradient.
            var brightHighlight = LightenTowardWhite(highlight, intensity * 0.55);
            var midTone = LightenTowardWhite(highlight, intensity * 0.2);

            fill = new LinearGradientBrush
            {
                // Follows wherever the sun/moon actually is -- the blob reads as lit from that
                // direction rather than a fixed corner, so dragging the celestial body around
                // visibly shifts the blob's highlight too.
                StartPoint = highlightPoint,
                EndPoint = shadowPoint,
                GradientStops =
                {
                    new GradientStop(brightHighlight, 0),
                    new GradientStop(midTone, 0.35),
                    new GradientStop(baseColor, 1)
                }
            };
        }

        var path = new AvaloniaPath { Data = geometry, Fill = fill };
        canvas.Children.Add(path);

        if (!eventActive)
        {
            DrawBlobSpecular(canvas, center, blob.BaseRadius, celestialOffset, lightIntensity);
        }
    }

    /// <summary>A soft, subtle shine near the blob's light-facing side -- kept small and close to center so it stays inside the blob's outline even as the wobbly edge shifts.</summary>
    private static void DrawBlobSpecular(Canvas canvas, Vec2 center, double blobRadius, Vec2 celestialOffset, double intensity)
    {
        double len = Math.Sqrt(celestialOffset.X * celestialOffset.X + celestialOffset.Y * celestialOffset.Y);
        if (len < 1) return;

        double nx = celestialOffset.X / len;
        double ny = celestialOffset.Y / len;

        double sx = center.X + nx * blobRadius * 0.4;
        double sy = center.Y + ny * blobRadius * 0.4;
        double specRadius = blobRadius * (0.16 + intensity * 0.08);

        byte alpha = (byte)(40 + intensity * 60);
        var brush = new RadialGradientBrush
        {
            GradientStops =
            {
                new GradientStop(Color.FromArgb(alpha, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
            }
        };

        var shine = new Ellipse { Width = specRadius * 2, Height = specRadius * 2, Fill = brush };
        Canvas.SetLeft(shine, sx - specRadius);
        Canvas.SetTop(shine, sy - specRadius);
        canvas.Children.Add(shine);
    }

    private static Color DarkenTowardBlack(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte r = (byte)(c.R * (1 - amount));
        byte g = (byte)(c.G * (1 - amount));
        byte b = (byte)(c.B * (1 - amount));
        return Color.FromRgb(r, g, b);
    }

    private static Color LightenTowardWhite(Color c, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte r = (byte)(c.R + (255 - c.R) * amount);
        byte g = (byte)(c.G + (255 - c.G) * amount);
        byte b = (byte)(c.B + (255 - c.B) * amount);
        return Color.FromRgb(r, g, b);
    }

    private void DrawTextElements(Canvas canvas, Vec2 center, WeatherState state, TextElementsConfig text)
    {
        var fontFamily = new FontFamily(string.IsNullOrWhiteSpace(text.FontFamily) ? "Segoe UI" : text.FontFamily);

        DrawTextElement(canvas, center, "Time", state.LocalTime.ToString("h:mm tt"), 26, FontWeight.SemiBold, text.Time, fontFamily);
        DrawTextElement(canvas, center, "Date", state.LocalTime.ToString("dddd, MMM d"), 26, FontWeight.Normal, text.Date, fontFamily);
        DrawTextElement(canvas, center, "Temperature", $"{state.TemperatureC:0}°", 40, FontWeight.Bold, text.Temperature, fontFamily);
        DrawTextElement(canvas, center, "Condition", ConditionLabel(state), 26, FontWeight.Normal, text.Condition, fontFamily);
        DrawForecastElement(canvas, center, state, text.Forecast, fontFamily);
    }

    /// <summary>Draws one independent text element (time, date, temperature, or condition) and records its screen bounds for click-to-select / drag-to-reposition.</summary>
    private void DrawTextElement(Canvas canvas, Vec2 center, string key, string text, double baseFontSize, FontWeight weight, TextElementConfig config, FontFamily fallbackFontFamily)
    {
        double scale = config.Scale <= 0 ? 1.0 : config.Scale;
        var fontFamily = string.IsNullOrWhiteSpace(config.FontFamily) ? fallbackFontFamily : new FontFamily(config.FontFamily);

        var block = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(ParseColorOr(config.ColorHex, Colors.White)),
            FontFamily = fontFamily,
            FontSize = baseFontSize * scale,
            FontWeight = weight,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double left = center.X - block.DesiredSize.Width / 2 + config.OffsetX;
        double top = center.Y - block.DesiredSize.Height / 2 + config.OffsetY;
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
        canvas.Children.Add(block);

        var bounds = new Rect(left, top, block.DesiredSize.Width, block.DesiredSize.Height);
        _lastElementBounds[key] = bounds;
        if (key == SelectedElementKey) DrawSelectionOutline(canvas, bounds);
    }

    /// <summary>The 3-line forecast block, treated as a single draggable/colorable/resizable unit.</summary>
    private void DrawForecastElement(Canvas canvas, Vec2 center, WeatherState state, TextElementConfig config, FontFamily fontFamily)
    {
        double scale = config.Scale <= 0 ? 1.0 : config.Scale;
        var brush = new SolidColorBrush(ParseColorOr(config.ColorHex, Colors.White));

        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var f in state.Forecast)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"{f.Label}  {f.TempC:0}°  {ConditionLabel(f.Condition)}",
                Foreground = brush,
                FontFamily = fontFamily,
                FontSize = 10.5 * scale,
                Opacity = 0.75,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });
        }

        panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double left = center.X - panel.DesiredSize.Width / 2 + config.OffsetX;
        double top = center.Y - panel.DesiredSize.Height / 2 + config.OffsetY;
        Canvas.SetLeft(panel, left);
        Canvas.SetTop(panel, top);
        canvas.Children.Add(panel);

        var bounds = new Rect(left, top, panel.DesiredSize.Width, panel.DesiredSize.Height);
        _lastElementBounds["Forecast"] = bounds;
        if (SelectedElementKey == "Forecast") DrawSelectionOutline(canvas, bounds);
    }

    /// <summary>Faint dashed-look outline (via a thin solid border) around whichever element is currently selected in the settings panel.</summary>
    private static void DrawSelectionOutline(Canvas canvas, Rect bounds)
    {
        var outline = new Rectangle
        {
            Width = bounds.Width + 10,
            Height = bounds.Height + 8,
            Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
            StrokeThickness = 1.5,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(outline, bounds.X - 5);
        Canvas.SetTop(outline, bounds.Y - 4);
        canvas.Children.Add(outline);
    }

    /// <summary>User-added divider lines and images (e.g. a line separating time from date, or a small icon). Each gets a "Custom:{id}" key so it's selectable/draggable the same way the built-in text elements are.</summary>
    private void DrawCustomElements(Canvas canvas, Vec2 center, List<CustomElementConfig> elements)
    {
        foreach (var el in elements)
        {
            string key = "Custom:" + el.Id;
            double cx = center.X + el.OffsetX;
            double cy = center.Y + el.OffsetY;
            Rect bounds;

            if (el.Type == CustomElementType.Line)
            {
                var brush = new SolidColorBrush(ParseColorOr(el.ColorHex, Colors.White));
                double half = Math.Max(el.Length, 4) / 2;
                var line = el.Horizontal
                    ? new Line { StartPoint = new Point(cx - half, cy), EndPoint = new Point(cx + half, cy), Stroke = brush, StrokeThickness = Math.Max(el.Thickness, 0.5) }
                    : new Line { StartPoint = new Point(cx, cy - half), EndPoint = new Point(cx, cy + half), Stroke = brush, StrokeThickness = Math.Max(el.Thickness, 0.5) };
                canvas.Children.Add(line);

                bounds = el.Horizontal
                    ? new Rect(cx - half, cy - Math.Max(el.Thickness, 6), el.Length, Math.Max(el.Thickness, 6) * 2)
                    : new Rect(cx - Math.Max(el.Thickness, 6), cy - half, Math.Max(el.Thickness, 6) * 2, el.Length);
            }
            else
            {
                var bitmap = TryLoadImage(el.ImagePath);
                double size = 48 * (el.Scale <= 0 ? 1.0 : el.Scale);

                if (bitmap is not null)
                {
                    var image = new Image { Source = bitmap, Width = size, Height = size, Stretch = Stretch.Uniform };
                    Canvas.SetLeft(image, cx - size / 2);
                    Canvas.SetTop(image, cy - size / 2);
                    canvas.Children.Add(image);
                }
                else
                {
                    // No image set/found yet -- draw a faint placeholder box so there's still
                    // something to see, click, and drag while picking an image.
                    var placeholder = new Rectangle
                    {
                        Width = size,
                        Height = size,
                        Stroke = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                        StrokeThickness = 1,
                        Fill = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
                    };
                    Canvas.SetLeft(placeholder, cx - size / 2);
                    Canvas.SetTop(placeholder, cy - size / 2);
                    canvas.Children.Add(placeholder);
                }

                bounds = new Rect(cx - size / 2, cy - size / 2, size, size);
            }

            _lastElementBounds[key] = bounds;
            if (SelectedElementKey == key) DrawSelectionOutline(canvas, bounds);
        }
    }

    private static string ConditionLabel(WeatherState state) => ConditionLabel(state.Condition);

    private static string ConditionLabel(WeatherCondition c) => c switch
    {
        WeatherCondition.Clear => "Clear",
        WeatherCondition.Cloudy => "Cloudy",
        WeatherCondition.Rain => "Rain",
        WeatherCondition.Thunderstorm => "Thunderstorm",
        _ => ""
    };

    private static void DrawHearts(Canvas canvas, ValentinesOverlay valentines, Color tint)
    {
        foreach (var h in valentines.Hearts)
        {
            double op = h.Life.Opacity;
            if (op <= 0.01) continue;

            var path = new AvaloniaPath
            {
                Data = BuildHeartGeometry(),
                Fill = new SolidColorBrush(tint),
                Opacity = op,
                RenderTransform = new ScaleTransform(h.Scale, h.Scale)
            };
            double sway = NoiseGen.SmoothSigned(h.SwaySeed, h.Life.Opacity * 3) * 8;
            Canvas.SetLeft(path, h.X - 7 * h.Scale + sway);
            Canvas.SetTop(path, h.Y - 7 * h.Scale);
            canvas.Children.Add(path);
        }
    }

    private static Geometry BuildHeartGeometry()
    {
        // Simple two-lobe heart, ~14x14 units, positioned around the origin.
        const string data = "M 7,13 C -3,5 -3,-3 3,-3 C 6,-3 7,-1 7,0 C 7,-1 8,-3 11,-3 C 17,-3 17,5 7,13 Z";
        return Geometry.Parse(data);
    }

    private static Color ToAvaloniaColor(RgbColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);
}
