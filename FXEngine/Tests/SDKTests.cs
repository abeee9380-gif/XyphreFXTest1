using FXEngine.SDK;

namespace FXEngine.Tests;

public class SDKTests
{
    [Fact]
    public void Context_CreatesAStablePublicSurface()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();

        var context = new FXEngineContext(configuration, logger);

        Assert.Same(configuration, context.Configuration);
        Assert.Same(logger, context.Logger);
        Assert.NotNull(context.Services);
        Assert.NotNull(context.EventBus);
        Assert.NotNull(context.Applications);
        Assert.NotNull(context.Themes);
        Assert.NotNull(context.Plugins);
        Assert.NotNull(context.Assets);
        Assert.NotNull(context.Settings);
    }

    [Fact]
    public void Services_RegisterAndResolveThroughTheContext()
    {
        var context = CreateContext();
        context.Services.Register<ITestService>(new TestService("resolved"));

        var service = context.Services.Resolve<ITestService>();

        Assert.NotNull(service);
        Assert.Equal("resolved", service.Value);
    }

    [Fact]
    public async Task EventBus_PublishesAndSubscribesThroughTheContext()
    {
        var context = CreateContext();
        var received = new List<string>();

        using var subscription = context.EventBus.Subscribe("sdk.test", evt =>
        {
            received.Add(evt.Payload?.ToString() ?? string.Empty);
            return Task.CompletedTask;
        });

        await context.EventBus.PublishAsync(new FXEvent("sdk.test", "hello"));

        Assert.Contains("hello", received);
    }

    [Fact]
    public async Task Plugin_LifecycleRunsThroughThePublicContract()
    {
        var context = CreateContext();
        var plugin = new TestPlugin();

        await plugin.OnLoadAsync(context);
        await plugin.OnUnloadAsync(context);

        Assert.True(plugin.Loaded);
        Assert.True(plugin.Unloaded);
    }

    [Fact]
    public async Task Application_LifecycleRunsThroughThePublicContract()
    {
        var context = CreateContext();
        var app = new TestApplication();

        await app.InitializeAsync(context);
        await app.StartAsync(context);
        await app.StopAsync(context);
        await app.DisposeAsync(context);

        Assert.True(app.Initialized);
        Assert.True(app.Started);
        Assert.True(app.Stopped);
        Assert.True(app.Disposed);
    }

    [Fact]
    public async Task Theme_LifecycleRunsThroughThePublicContract()
    {
        var context = CreateContext();
        var theme = new TestTheme();

        await theme.InitializeAsync(context);
        await theme.ApplyAsync(context);
        await theme.RemoveAsync(context);

        Assert.True(theme.Initialized);
        Assert.True(theme.Applied);
        Assert.True(theme.Removed);
    }

    private static FXEngineContext CreateContext()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        return new FXEngineContext(configuration, new TestLogger());
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null) { }

        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Debug(string message) { }
    }

    private interface ITestService
    {
        string Value { get; }
    }

    private sealed class TestService(string value) : ITestService
    {
        public string Value { get; } = value;
    }

    private sealed class TestPlugin : IFXPlugin
    {
        public string Id => "test.plugin";
        public string Name => "Test Plugin";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public bool Loaded { get; private set; }
        public bool Unloaded { get; private set; }

        public Task OnLoadAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Loaded = true;
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Unloaded = true;
            return Task.CompletedTask;
        }

        public IEnumerable<string> RegisterEvents() => Array.Empty<string>();
        public IEnumerable<FXServiceRegistration> RegisterServices() => Array.Empty<FXServiceRegistration>();
    }

    private sealed class TestApplication : IFXApplication
    {
        public string Id => "test.app";
        public string Name => "Test App";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public string Description => "Test application";
        public bool Initialized { get; private set; }
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }

        public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return Task.CompletedTask;
        }

        public Task StartAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Stopped = true;
            return Task.CompletedTask;
        }

        public Task DisposeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Disposed = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TestTheme : IFXTheme
    {
        public string Id => "test.theme";
        public string Name => "Test Theme";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public string PrimaryColor => "#000000";
        public string SecondaryColor => "#FFFFFF";
        public string AccentColor => "#00FF00";
        public string FontFamily => "Segoe UI";
        public IReadOnlyList<string> Icons => new List<string> { "sun", "moon" };
        public IReadOnlyList<string> Animations => new List<string> { "fade" };
        public bool Initialized { get; private set; }
        public bool Applied { get; private set; }
        public bool Removed { get; private set; }

        public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return Task.CompletedTask;
        }

        public Task ApplyAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Applied = true;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Removed = true;
            return Task.CompletedTask;
        }
    }
}
