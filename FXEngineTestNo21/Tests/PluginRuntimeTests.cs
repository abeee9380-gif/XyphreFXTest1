using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Tests;

public class PluginRuntimeTests
{
    [Fact]
    public async Task PluginManager_LoadsEnablesDisablesAndUnloadsPlugins()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var context = new FXEngineContext(configuration, logger);
        var pluginManager = new PluginManager(logger);
        var plugin = new TestPlugin();
        pluginManager.Register(plugin);

        await pluginManager.LoadPluginAsync(plugin.Id, context);
        await pluginManager.EnablePluginAsync(plugin.Id, context);
        await pluginManager.DisablePluginAsync(plugin.Id, context);
        await pluginManager.UnloadPluginAsync(plugin.Id, context);

        Assert.True(plugin.Loaded);
        Assert.True(plugin.Initialized);
        Assert.True(plugin.Enabled);
        Assert.True(plugin.Disabled);
        Assert.True(plugin.Unloaded);
    }

    [Fact]
    public async Task PluginManager_RegistersServicesCommandsAndPublishesEvents()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var context = new FXEngineContext(configuration, logger);
        var pluginManager = new PluginManager(logger);
        var plugin = new TestPlugin();
        pluginManager.Register(plugin);

        await pluginManager.LoadPluginAsync(plugin.Id, context);
        await pluginManager.EnablePluginAsync(plugin.Id, context);

        Assert.NotNull(pluginManager.ResolveCommand("hello"));
        Assert.NotNull(pluginManager.ResolveService<TestService>());
        Assert.NotNull(context.EventBus);
    }

    [Fact]
    public async Task PluginManager_RecoverGracefullyFromPluginFailure()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var context = new FXEngineContext(configuration, logger);
        var pluginManager = new PluginManager(logger);
        var plugin = new FailingPlugin();
        pluginManager.Register(plugin);

        await pluginManager.LoadPluginAsync(plugin.Id, context);
        await pluginManager.EnablePluginAsync(plugin.Id, context);

        Assert.False(plugin.Enabled);
        Assert.True(plugin.Failed);
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Debug(string message) { }
    }

    private sealed class TestService
    {
        public string Value => "service";
    }

    private sealed class TestPlugin : IFXPlugin
    {
        public string Id => "test.plugin";
        public string Name => "Test Plugin";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public bool Loaded { get; private set; }
        public bool Initialized { get; private set; }
        public bool Enabled { get; private set; }
        public bool Disabled { get; private set; }
        public bool Unloaded { get; private set; }

        public Task OnLoadAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Loaded = true;
            return Task.CompletedTask;
        }

        public Task OnInitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return Task.CompletedTask;
        }

        public Task OnEnableAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Enabled = true;
            return Task.CompletedTask;
        }

        public Task OnDisableAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Disabled = true;
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Unloaded = true;
            return Task.CompletedTask;
        }

        public IEnumerable<FXServiceRegistration> RegisterServices() => new[] { new FXServiceRegistration(typeof(TestService), new TestService()) };
        public IEnumerable<string> RegisterEvents() => new[] { "plugin.test" };
        public IEnumerable<FXPluginCommand> RegisterCommands() => new[] { new FXPluginCommand("hello", "Says hello", _ => Task.CompletedTask) };
    }

    private sealed class FailingPlugin : IFXPlugin
    {
        public string Id => "failing.plugin";
        public string Name => "Failing Plugin";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public bool Enabled { get; private set; }
        public bool Failed { get; private set; }

        public Task OnEnableAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Failed = true;
            throw new InvalidOperationException("boom");
        }

        public Task OnLoadAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OnInitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OnDisableAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OnUnloadAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<FXServiceRegistration> RegisterServices() => Array.Empty<FXServiceRegistration>();
        public IEnumerable<string> RegisterEvents() => Array.Empty<string>();
        public IEnumerable<FXPluginCommand> RegisterCommands() => Array.Empty<FXPluginCommand>();
    }
}
