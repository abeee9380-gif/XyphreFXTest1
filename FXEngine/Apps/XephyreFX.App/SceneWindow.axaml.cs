using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using XephyreFX.App.Config;
using XephyreFX.App.Platform;
using XephyreFX.App.Rendering;
using XephyreFX.App.Sim;

namespace XephyreFX.App;

public partial class SceneWindow : Window
{
    private readonly SharedRuntime _runtime;

    /// <summary>Set by App.axaml.cs after both windows exist -- double-clicking the scene brings Settings to front.</summary>
    public SettingsWindow? SettingsWindowRef { get; set; }
    private readonly AvaloniaSceneRenderer _renderer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer;
    private DispatcherTimer? _weatherFetchTimer;
    private WeatherCondition? _liveCondition;
    private double _liveTempC;

    private double _lastFrameSeconds;

    public string? SelectedElementKey { get; private set; }

    /// <summary>Raised whenever the user clicks (not drags) an element in the scene, so SettingsWindow can refresh its panel.</summary>
    public event Action<string?>? SelectionChanged;

    public bool IsDesktopEmbedded { get; private set; }

    public SceneWindow(SharedRuntime runtime)
    {
        _runtime = runtime;
        InitializeComponent();

        SceneCanvas.PointerPressed += OnScenePointerPressed;
        SceneCanvas.PointerMoved += OnScenePointerMoved;
        SceneCanvas.PointerReleased += OnScenePointerReleased;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
        _timer.Start();

        _weatherFetchTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _weatherFetchTimer.Tick += async (_, _) => await RefreshLiveWeatherAsync();
        _weatherFetchTimer.Start();
        _ = RefreshLiveWeatherAsync();

        // The native window handle doesn't exist until the window is actually realized, so
        // this waits for Opened rather than trying (and failing) from the constructor.
        Opened += async (_, _) =>
        {
            if (_runtime.Config.Current.DesktopMode.StartEmbedded)
            {
                await SetDesktopModeAsync(true);
            }
        };
    }

    /// <summary>Fetches live weather now. Called on startup, every 10 minutes, and whenever Settings saves a new API key/city.</summary>
    public async Task RefreshLiveWeatherAsync()
    {
        var cfg = _runtime.Config.Current.WeatherApi;
        if (!cfg.Enabled) return;

        var result = await _runtime.WeatherApi.FetchAsync(cfg.Provider, cfg.ApiKey, cfg.City);
        if (result.HasValue)
        {
            _liveCondition = result.Value.condition;
            _liveTempC = result.Value.tempC;
        }
    }

    public void RerollBlob() => _runtime.Scene.RerollBlob();

    public void SelectElement(string? key)
    {
        SelectedElementKey = key;
        SelectionChanged?.Invoke(key);
    }

    /// <summary>Turns desktop-embed mode on/off. Windows-only; a no-op elsewhere. Returns whether it took effect, plus why/why not.</summary>
    public async Task<(bool success, string reason)> SetDesktopModeAsync(bool enabled)
    {
        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        if (enabled)
        {
            var (ok, reason) = await DesktopEmbedService.EmbedBehindDesktopIconsAsync(handle);
            if (ok)
            {
                IsDesktopEmbedded = true;
                FillPrimaryScreen();
                StartWatchdog();
            }
            return (ok, reason);
        }

        StopWatchdog();
        DesktopEmbedService.Detach(handle);
        IsDesktopEmbedded = false;
        Width = 1040;
        Height = 760;
        return (true, "Detached -- back to a normal (still borderless) window.");
    }

    /// <summary>Resizes/repositions to cover the whole primary screen -- without this, a successful embed still just shows as a small floating rectangle wherever the window happened to be, easy to miss against a fully transparent background.</summary>
    private void FillPrimaryScreen()
    {
        try
        {
            var screen = Screens.Primary;
            if (screen is null) return;

            Position = screen.Bounds.Position;
            Width = screen.Bounds.Width;
            Height = screen.Bounds.Height;
        }
        catch
        {
            // Best-effort -- worst case it just keeps its previous size/position.
        }
    }

    // If Explorer restarts (crashes, "Restart Windows Explorer" from Task Manager, a shell
    // update, etc.), the WorkerW we attached to stops existing and the scene silently ends up
    // detached. This periodically checks and re-embeds instead of leaving it invisible/adrift.
    private DispatcherTimer? _watchdog;

    private void StartWatchdog()
    {
        if (_watchdog is not null) return;

        _watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _watchdog.Tick += async (_, _) =>
        {
            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero || DesktopEmbedService.IsStillAttached(handle)) return;

            var (ok, _) = await DesktopEmbedService.EmbedBehindDesktopIconsAsync(handle);
            if (ok) FillPrimaryScreen();
        };
        _watchdog.Start();
    }

    private void StopWatchdog()
    {
        _watchdog?.Stop();
        _watchdog = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = _lastFrameSeconds == 0 ? 0 : Math.Min(now - _lastFrameSeconds, 0.1);
        _lastFrameSeconds = now;

        var state = _runtime.State;
        state.LocalTime = DateTime.Now;
        state.Period = WeatherState.PeriodFromClock(state.LocalTime);
        _runtime.Override.Apply(state);

        var config = _runtime.Config.Current;
        if (!_runtime.Override.Enabled && config.WeatherApi.Enabled && _liveCondition.HasValue)
        {
            state.Condition = _liveCondition.Value;
            state.TemperatureC = _liveTempC;
        }

        var activeEvent = _runtime.Events.GetActiveEvent(state.LocalTime.Month, state.LocalTime.Day);
        state.EventTintHex = activeEvent?.TintColorHex;
        state.EventName = activeEvent?.Name;

        _renderer.SelectedElementKey = SelectedElementKey;

        double width = SceneCanvas.Bounds.Width;
        double height = SceneCanvas.Bounds.Height;
        var blobCenter = new Vec2(width / 2, height / 2);

        var cloudAnchorOffset = new Vec2(config.CloudAnchor.OffsetX, config.CloudAnchor.OffsetY);

        _runtime.Scene.Tick(dt, state, width, height, blobCenter, cloudAnchorOffset);
        _renderer.Draw(SceneCanvas, _runtime.Scene, state, blobCenter, config);
    }

    private void OnScenePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            SettingsWindowRef?.Show();
            SettingsWindowRef?.Activate();
            return;
        }

        var pos = e.GetPosition(SceneCanvas);
        SelectElement(HitTestElement(pos));
    }

    private void OnScenePointerMoved(object? sender, PointerEventArgs e)
    {
        // Dragging removed -- everything (including the cloud spawn point) is now positioned
        // from the Settings window only. Simpler and avoids the drag-vs-click/double-click
        // conflicts that kept causing bugs.
    }

    private void OnScenePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Nothing to do -- selection happens on press, and there's no drag to finalize anymore.
    }

    private string? HitTestElement(Point pos)
    {
        foreach (var kv in _renderer.LastElementBounds)
        {
            if (kv.Value.Contains(pos)) return kv.Key;
        }
        return null;
    }

    private TextElementConfig GetTextElementConfig(string key)
    {
        var t = _runtime.Config.Current.Text;
        return key switch
        {
            "Time" => t.Time,
            "Date" => t.Date,
            "Temperature" => t.Temperature,
            "Condition" => t.Condition,
            "Forecast" => t.Forecast,
            _ => t.Time
        };
    }

    private CustomElementConfig? FindCustomElement(string key)
    {
        string id = key.Substring("Custom:".Length);
        return _runtime.Config.Current.CustomElements.FirstOrDefault(c => c.Id == id);
    }
}
