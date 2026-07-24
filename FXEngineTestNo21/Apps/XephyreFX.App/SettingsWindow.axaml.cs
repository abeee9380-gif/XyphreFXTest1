using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using XephyreFX.App.Config;
using XephyreFX.App.Sim;

namespace XephyreFX.App;

public partial class SettingsWindow : Window
{
    private const string CustomKeyPrefix = "Custom:";

    private static readonly string[] FallbackFonts =
    {
        "Segoe UI", "Arial", "Verdana", "Tahoma", "Consolas", "Calibri", "Times New Roman"
    };

    private readonly SharedRuntime _runtime;
    private readonly SceneWindow _sceneWindow;
    private readonly DispatcherTimer _pushTimer;

    private Action? _pendingConfirmAction;

    public SettingsWindow(SharedRuntime runtime, SceneWindow sceneWindow)
    {
        _runtime = runtime;
        _sceneWindow = sceneWindow;
        InitializeComponent();

        ConditionCombo.ItemsSource = Enum.GetValues(typeof(WeatherCondition));
        ConditionCombo.SelectedItem = WeatherCondition.Clear;
        PeriodCombo.ItemsSource = Enum.GetValues(typeof(SkyPeriod));
        PeriodCombo.SelectedItem = WeatherState.PeriodFromClock(DateTime.Now);
        CelestialPeriodCombo.ItemsSource = Enum.GetValues(typeof(SkyPeriod));
        CelestialPeriodCombo.SelectedItem = SkyPeriod.Noon;

        var fonts = GetAvailableFontNames();
        TextFontCombo.ItemsSource = fonts;
        SelectedFontCombo.ItemsSource = fonts;

        _sceneWindow.SelectionChanged += _ => RefreshSelectedElementPanel();

        PopulateSettingsFields();
        RefreshStartupStatus();
        RefreshSelectedElementPanel();
        RefreshDesktopModeStatus();
        RefreshStartEmbeddedStatus();

        // Pushes this window's live control values (weather override, blob speed) into the
        // shared runtime on a short interval. Decoupled from SceneWindow's own 60fps render
        // timer on purpose -- this window doesn't need to run anywhere near that fast.
        _pushTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _pushTimer.Tick += (_, _) => PushLiveValues();
        _pushTimer.Start();
    }

    private void PushLiveValues()
    {
        _runtime.Override.Enabled = OverrideEnabledCheck.IsChecked == true;
        _runtime.Override.Condition = ConditionCombo.SelectedItem as WeatherCondition?;
        _runtime.Override.Period = PeriodCombo.SelectedItem as SkyPeriod?;
        _runtime.Override.Intensity = IntensitySlider.Value;
        _runtime.Override.TemperatureC = TempSlider.Value;
        _runtime.Override.ForceValentines = ValentinesCheck.IsChecked == true ? true : null;

        // Deliberately left at whatever the user sets -- per feedback, the default (1.0) is
        // already the "right" speed, this slider is purely optional.
        _runtime.Scene.Blob.SpeedMultiplier = MorphSpeedSlider.Value;
    }

    private void OnRerollClick(object? sender, RoutedEventArgs e) => _sceneWindow.RerollBlob();

    private void OnResetTricksterClick(object? sender, RoutedEventArgs e)
    {
        OverrideEnabledCheck.IsChecked = false;
        ValentinesCheck.IsChecked = false;
        ConditionCombo.SelectedItem = WeatherCondition.Clear;
        PeriodCombo.SelectedItem = WeatherState.PeriodFromClock(DateTime.Now);
        IntensitySlider.Value = 0.4;
        TempSlider.Value = 21;
        MorphSpeedSlider.Value = 1;
        _runtime.Override.Clear();
    }

    // --- Desktop mode ---

    private async void OnEnableDesktopModeClick(object? sender, RoutedEventArgs e)
    {
        DesktopModeStatusText.Text = "Trying...";
        var (_, reason) = await _sceneWindow.SetDesktopModeAsync(true);
        DesktopModeStatusText.Text = reason;
    }

    private async void OnDisableDesktopModeClick(object? sender, RoutedEventArgs e)
    {
        var (_, reason) = await _sceneWindow.SetDesktopModeAsync(false);
        DesktopModeStatusText.Text = reason;
    }

    private void RefreshDesktopModeStatus()
    {
        DesktopModeStatusText.Text = _sceneWindow.IsDesktopEmbedded
            ? "Scene is pinned to the desktop, behind your icons."
            : "Scene is floating on top, not embedded, right now.";
    }

    private void OnRememberStartEmbeddedClick(object? sender, RoutedEventArgs e)
    {
        _runtime.Config.Current.DesktopMode.StartEmbedded = true;
        _runtime.Config.Save();
        RefreshStartEmbeddedStatus();
    }

    private void OnForgetStartEmbeddedClick(object? sender, RoutedEventArgs e)
    {
        _runtime.Config.Current.DesktopMode.StartEmbedded = false;
        _runtime.Config.Save();
        RefreshStartEmbeddedStatus();
    }

    private void RefreshStartEmbeddedStatus()
    {
        StartEmbeddedStatusText.Text = _runtime.Config.Current.DesktopMode.StartEmbedded
            ? "Will try to start in desktop mode automatically next launch."
            : "Will start as a normal window next launch (default).";
    }

    // --- Selected-element panel (built-in text, sun/moon, cloud anchor, or custom line/image) ---

    private void RefreshSelectedElementPanel()
    {
        string? key = _sceneWindow.SelectedElementKey;

        if (key is null)
        {
            SelectedElementLabel.Text = "Selected: none";
            SelectedColorBox.Text = "";
            SelectedScaleBox.Text = "";
            SelectedFontCombo.SelectedItem = null;
            SelectedLineLengthBox.Text = "";
            SelectedLineThicknessBox.Text = "";
            return;
        }

        if (key.StartsWith(CustomKeyPrefix))
        {
            var el = FindCustomElement(key);
            if (el is null) return;

            SelectedElementLabel.Text = $"Selected: {(el.Type == CustomElementType.Line ? "Line" : "Image")}";
            SelectedColorBox.Text = el.ColorHex;
            SelectedScaleBox.Text = el.Scale.ToString();
            SelectedFontCombo.SelectedItem = null;
            SelectedLineLengthBox.Text = el.Length.ToString();
            SelectedLineThicknessBox.Text = el.Thickness.ToString();
        }
        else if (key == "Celestial")
        {
            SelectedElementLabel.Text = "Selected: Sun/moon";
            SelectedColorBox.Text = "";
            SelectedScaleBox.Text = _runtime.Config.Current.Celestial.Scale.ToString();
            SelectedFontCombo.SelectedItem = null;
            SelectedLineLengthBox.Text = "";
            SelectedLineThicknessBox.Text = "";
        }
        else if (key == "CloudAnchor")
        {
            SelectedElementLabel.Text = "Selected: Cloud spawn point (drag to move; nothing else to edit here)";
            SelectedColorBox.Text = "";
            SelectedScaleBox.Text = "";
            SelectedFontCombo.SelectedItem = null;
            SelectedLineLengthBox.Text = "";
            SelectedLineThicknessBox.Text = "";
        }
        else
        {
            var cfg = GetTextElementConfig(key);
            SelectedElementLabel.Text = $"Selected: {key}";
            SelectedColorBox.Text = cfg.ColorHex;
            SelectedScaleBox.Text = cfg.Scale.ToString();
            SelectedFontCombo.SelectedItem = cfg.FontFamily;
            SelectedLineLengthBox.Text = "";
            SelectedLineThicknessBox.Text = "";
        }
    }

    private void OnApplySelectedClick(object? sender, RoutedEventArgs e)
    {
        string? key = _sceneWindow.SelectedElementKey;
        if (key is null) return;

        if (key.StartsWith(CustomKeyPrefix))
        {
            var el = FindCustomElement(key);
            if (el is null) return;

            if (!string.IsNullOrWhiteSpace(SelectedColorBox.Text)) el.ColorHex = SelectedColorBox.Text;
            if (double.TryParse(SelectedScaleBox.Text, out var scale) && scale > 0) el.Scale = scale;
            if (double.TryParse(SelectedLineLengthBox.Text, out var len) && len > 0) el.Length = len;
            if (double.TryParse(SelectedLineThicknessBox.Text, out var thick) && thick > 0) el.Thickness = thick;
        }
        else if (key == "Celestial")
        {
            if (double.TryParse(SelectedScaleBox.Text, out var scale) && scale > 0)
            {
                _runtime.Config.Current.Celestial.Scale = scale;
            }
        }
        else if (key != "CloudAnchor")
        {
            var cfg = GetTextElementConfig(key);
            if (!string.IsNullOrWhiteSpace(SelectedColorBox.Text)) cfg.ColorHex = SelectedColorBox.Text;
            if (double.TryParse(SelectedScaleBox.Text, out var scale) && scale > 0) cfg.Scale = scale;
            cfg.FontFamily = SelectedFontCombo.SelectedItem as string; // null/blank = inherit the shared font
        }

        _runtime.Config.Save();
    }

    private void OnDeleteSelectedClick(object? sender, RoutedEventArgs e)
    {
        string? key = _sceneWindow.SelectedElementKey;
        if (key is null || !key.StartsWith(CustomKeyPrefix)) return; // built-ins can't be deleted

        var el = FindCustomElement(key);
        if (el is not null)
        {
            _runtime.Config.Current.CustomElements.Remove(el);
            _runtime.Config.Save();
        }

        _sceneWindow.SelectElement(null);
    }

    private void OnAddLineClick(object? sender, RoutedEventArgs e)
    {
        var el = new CustomElementConfig { Type = CustomElementType.Line };
        _runtime.Config.Current.CustomElements.Add(el);
        _runtime.Config.Save();
        _sceneWindow.SelectElement(CustomKeyPrefix + el.Id);
    }

    private void OnAddImageClick(object? sender, RoutedEventArgs e)
    {
        string? path = NewImagePathBox.Text;
        var el = new CustomElementConfig
        {
            Type = CustomElementType.Image,
            ImagePath = string.IsNullOrWhiteSpace(path) ? null : path
        };
        _runtime.Config.Current.CustomElements.Add(el);
        _runtime.Config.Save();
        _sceneWindow.SelectElement(CustomKeyPrefix + el.Id);
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
        string id = key.Substring(CustomKeyPrefix.Length);
        return _runtime.Config.Current.CustomElements.FirstOrDefault(c => c.Id == id);
    }

    // --- Native file picker ("Browse...") ---

    private async Task<string?> PickImageFileAsync()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose an image",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        }
        catch
        {
            // No file picker available on this platform/setup, or the user's environment
            // doesn't support it -- fall back to letting them type a path manually.
            return null;
        }
    }

    private async void OnBrowseNewImageClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (path is not null) NewImagePathBox.Text = path;
    }

    private async void OnBrowseCelestialImageClick(object? sender, RoutedEventArgs e)
    {
        var path = await PickImageFileAsync();
        if (path is null) return;

        string period = (CelestialPeriodCombo.SelectedItem ?? SkyPeriod.Noon).ToString()!;
        _runtime.Config.Current.Celestial.ImagesByPeriod[period] = path;
        _runtime.Config.Save();
        RefreshCelestialImageStatus();
    }

    private void OnClearCelestialImageClick(object? sender, RoutedEventArgs e)
    {
        string period = (CelestialPeriodCombo.SelectedItem ?? SkyPeriod.Noon).ToString()!;
        _runtime.Config.Current.Celestial.ImagesByPeriod.Remove(period);
        _runtime.Config.Save();
        RefreshCelestialImageStatus();
    }

    private void RefreshCelestialImageStatus()
    {
        string period = (CelestialPeriodCombo.SelectedItem ?? SkyPeriod.Noon).ToString()!;
        var images = _runtime.Config.Current.Celestial.ImagesByPeriod;
        CelestialImageStatusText.Text = images.TryGetValue(period, out var path) && !string.IsNullOrWhiteSpace(path)
            ? $"{period}: using {path}"
            : $"{period}: using default";
    }

    // --- Font list ---

    private static List<string> GetAvailableFontNames()
    {
        try
        {
            var names = FontManager.Current.SystemFonts
                .Select(f => f.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            return names.Count > 0 ? names : FallbackFonts.ToList();
        }
        catch
        {
            return FallbackFonts.ToList();
        }
    }

    // --- Startup ---

    private void OnEnableStartupClick(object? sender, RoutedEventArgs e)
    {
        _runtime.Startup.SetEnabled(true);
        RefreshStartupStatus();
    }

    private void OnDisableStartupClick(object? sender, RoutedEventArgs e)
    {
        _runtime.Startup.SetEnabled(false);
        RefreshStartupStatus();
    }

    private void RefreshStartupStatus()
    {
        StartupStatusText.Text = _runtime.Startup.IsEnabled()
            ? "XephyreFX will launch automatically at login."
            : "XephyreFX will NOT launch automatically at login.";
    }

    // --- Save / reset / reload, all behind a confirmation for anything destructive ---

    private void ShowConfirm(string message, Action onYes)
    {
        ConfirmMessageText.Text = message;
        _pendingConfirmAction = onYes;
        ConfirmOverlay.IsVisible = true;
    }

    private void OnConfirmYesClick(object? sender, RoutedEventArgs e)
    {
        ConfirmOverlay.IsVisible = false;
        var action = _pendingConfirmAction;
        _pendingConfirmAction = null;
        action?.Invoke();
    }

    private void OnConfirmNoClick(object? sender, RoutedEventArgs e)
    {
        ConfirmOverlay.IsVisible = false;
        _pendingConfirmAction = null;
    }

    private void OnSaveSettingsClick(object? sender, RoutedEventArgs e)
    {
        ShowConfirm("Save appearance settings? This overwrites config.json.", DoSaveAppearance);
    }

    private void DoSaveAppearance()
    {
        var cfg = _runtime.Config.Current;

        if (!string.IsNullOrWhiteSpace(BlobBaseColorBox.Text)) cfg.Blob.BaseColorHex = BlobBaseColorBox.Text;
        if (!string.IsNullOrWhiteSpace(BlobHighlightColorBox.Text)) cfg.Blob.HighlightColorHex = BlobHighlightColorBox.Text;
        if (TextFontCombo.SelectedItem is string selectedFont) cfg.Text.FontFamily = selectedFont;

        if (double.TryParse(CelestialOffsetXBox.Text, out var cox)) cfg.Celestial.OffsetX = cox;
        if (double.TryParse(CelestialOffsetYBox.Text, out var coy)) cfg.Celestial.OffsetY = coy;
        if (double.TryParse(CelestialScaleBox.Text, out var cs)) cfg.Celestial.Scale = cs;

        _runtime.Config.Save();
    }

    private void OnResetAppearanceClick(object? sender, RoutedEventArgs e)
    {
        ShowConfirm("Reset ALL appearance settings to defaults? This can't be undone.", DoResetAppearance);
    }

    private void DoResetAppearance()
    {
        _runtime.Config.ResetToDefaults();
        PopulateSettingsFields();
        RefreshSelectedElementPanel();
    }

    private void OnReloadSettingsClick(object? sender, RoutedEventArgs e)
    {
        _runtime.Config.Load();
        _runtime.Events.Load();
        PopulateSettingsFields();
        RefreshSelectedElementPanel();
    }

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = AppContext.BaseDirectory, UseShellExecute = true });
        }
        catch
        {
            // Non-fatal -- just means we couldn't launch a file browser on this platform/setup.
        }
    }

    private void PopulateSettingsFields()
    {
        var cfg = _runtime.Config.Current;
        BlobBaseColorBox.Text = cfg.Blob.BaseColorHex;
        BlobHighlightColorBox.Text = cfg.Blob.HighlightColorHex;
        TextFontCombo.SelectedItem = cfg.Text.FontFamily;
        CelestialOffsetXBox.Text = cfg.Celestial.OffsetX.ToString();
        CelestialOffsetYBox.Text = cfg.Celestial.OffsetY.ToString();
        CelestialScaleBox.Text = cfg.Celestial.Scale.ToString();
        CreditsText.Text = $"Made by {cfg.Credits.CreatorName} -- {cfg.Credits.YouTube}";
        RefreshCelestialImageStatus();
    }
}
