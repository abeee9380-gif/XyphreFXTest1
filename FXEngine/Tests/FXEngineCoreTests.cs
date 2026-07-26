using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Tests;

public class FXEngineCoreTests
{
    [Fact]
    public async Task SettingsManager_SerializesAndLoadsSettings()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "fxengine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var configuration = new FXEngineConfiguration
        {
            BaseDirectory = tempDirectory,
            SettingsFileName = "settings.json"
        };
        var logger = new FXLogger(configuration);
        var settingsManager = new SettingsManager(configuration, logger);

        await settingsManager.SetValueAsync("General", "Theme", "Dark");
        await settingsManager.SetValueAsync("General", "Language", "en");
        await settingsManager.SaveAsync();

        var reloaded = new SettingsManager(configuration, logger);
        await reloaded.LoadAsync();

        Assert.Equal("Dark", await reloaded.GetValueAsync<string>("General", "Theme"));
        Assert.Equal("en", await reloaded.GetValueAsync<string>("General", "Language"));
    }

    [Fact]
    public async Task EventBus_PublishesToSubscribers()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new FXLogger(configuration);
        var eventBus = new EventBus(logger);
        var received = new List<string>();

        using var subscription = eventBus.Subscribe("test.event", evt =>
        {
            received.Add(evt.Payload?.ToString() ?? string.Empty);
            return Task.CompletedTask;
        });

        await eventBus.PublishAsync(new FXEvent("test.event", "hello"));

        Assert.Contains("hello", received);
    }

    [Fact]
    public async Task ApplicationManager_RegistersAndStartsApplications()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new FXLogger(configuration);
        var eventBus = new EventBus(logger);
        var applicationManager = new ApplicationManager(logger, eventBus);

        var app = new TestApplication();
        applicationManager.Register(app);

        await applicationManager.InitializeAsync();
        await applicationManager.StartAsync();

        Assert.True(app.Initialized);
        Assert.True(app.Started);
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

        public Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
