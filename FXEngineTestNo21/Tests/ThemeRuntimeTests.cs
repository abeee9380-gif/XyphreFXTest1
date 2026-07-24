using FXEngine.SDK;
using FXEngine.Core;

namespace FXEngine.Tests;

public class ThemeRuntimeTests
{
    [Fact]
    public async Task ThemeManager_LoadsAppliesAndUnloadsThemes()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var themeManager = new ThemeManager(logger);
        var context = new FXEngineContext(configuration, logger);
        var theme = new TestTheme();
        themeManager.Register(theme);

        await themeManager.InitializeAsync(context);
        await themeManager.LoadThemeAsync(theme.Id, context);
        await themeManager.ApplyThemeAsync(theme.Id, context);
        await themeManager.UnloadThemeAsync(theme.Id, context);

        Assert.True(theme.Loaded);
        Assert.True(theme.Applied);
        Assert.True(theme.Removed);
        Assert.True(theme.Unloaded);
    }

    [Fact]
    public async Task ThemeManager_SwitchesThemesWithoutRestart()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var themeManager = new ThemeManager(logger);
        var context = new FXEngineContext(configuration, logger);
        var firstTheme = new TestTheme("first");
        var secondTheme = new TestTheme("second");
        themeManager.Register(firstTheme);
        themeManager.Register(secondTheme);

        await themeManager.LoadThemeAsync(firstTheme.Id, context);
        await themeManager.ApplyThemeAsync(firstTheme.Id, context);
        await themeManager.SwitchThemeAsync(secondTheme.Id, context);

        Assert.True(firstTheme.Removed);
        Assert.True(secondTheme.Loaded);
        Assert.True(secondTheme.Applied);
    }

    [Fact]
    public void ThemeManager_RejectsUnknownTheme()
    {
        var logger = new TestLogger();
        var themeManager = new ThemeManager(logger);
        var context = new FXEngineContext(new FXEngineConfiguration(), logger);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => themeManager.ApplyThemeAsync("missing", context));
        Assert.Contains("Unknown theme", ex.Result.Message);
    }

    [Fact]
    public void ThemeValidation_RejectsInvalidThemeMetadata()
    {
        var logger = new TestLogger();
        var themeManager = new ThemeManager(logger);
        var context = new FXEngineContext(new FXEngineConfiguration(), logger);

        var validation = themeManager.ValidateThemePackage("", context);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("manifest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ThemeResources_ExposeAssetsFontsAndAnimations()
    {
        var context = new FXEngineContext(new FXEngineConfiguration(), new TestLogger());
        context.ThemeContext.Resources.RegisterAsset("weather", "icons/weather.png");
        context.ThemeContext.Resources.RegisterFont("Segoe UI", "fonts/segoe.ttf");
        context.ThemeContext.Resources.RegisterAnimation("fade", "fade");

        Assert.Contains("weather", context.ThemeContext.Resources.Assets.Keys);
        Assert.Contains("Segoe UI", context.ThemeContext.Resources.Fonts.Keys);
        Assert.Contains("fade", context.ThemeContext.Resources.Animations.Keys);
    }

    [Fact]
    public void ThemePalette_ProvidesDefaultColors()
    {
        var palette = new ThemePalette();

        Assert.Equal("#1E1E1E", palette.Primary);
        Assert.Equal("#4FC3F7", palette.Accent);
        Assert.Equal("#F44336", palette.Danger);
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Debug(string message) { }
    }

    private sealed class TestTheme : IFXTheme
    {
        public TestTheme(string id = "test.theme")
        {
            Id = id;
        }

        public string Id { get; }
        public string Name => "Test Theme";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public string PrimaryColor => "#111111";
        public string SecondaryColor => "#222222";
        public string AccentColor => "#333333";
        public string FontFamily => "Segoe UI";
        public IReadOnlyList<string> Icons => new List<string> { "weather" };
        public IReadOnlyList<string> Animations => new List<string> { "fade" };
        public bool Loaded { get; private set; }
        public bool Applied { get; private set; }
        public bool Removed { get; private set; }
        public bool Unloaded { get; private set; }

        public Task OnLoadAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Loaded = true;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Applied = true;
            return Task.CompletedTask;
        }

        public Task OnApplyAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Applied = true;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Removed = true;
            return Task.CompletedTask;
        }

        public Task OnRemoveAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Removed = true;
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Unloaded = true;
            return Task.CompletedTask;
        }

        public IEnumerable<ThemeAssetRegistration> RegisterAssets() => Array.Empty<ThemeAssetRegistration>();
        public IEnumerable<ThemeFontRegistration> RegisterFonts() => Array.Empty<ThemeFontRegistration>();
        public IEnumerable<ThemeAnimationRegistration> RegisterAnimations() => Array.Empty<ThemeAnimationRegistration>();
        public IEnumerable<ThemeColorRegistration> RegisterColors() => Array.Empty<ThemeColorRegistration>();
    }
}
