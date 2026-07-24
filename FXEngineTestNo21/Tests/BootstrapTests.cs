using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Tests;

public class BootstrapTests
{
    [Fact]
    public async Task StartupSequence_TransitionsStatesAndPublishesEvents()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "fxengine-bootstrap", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var configuration = new FXEngineConfiguration { BaseDirectory = tempDirectory };
        var logger = new TestLogger();
        var engine = new EngineManager(configuration, logger);
        var eventLog = new List<string>();

        engine.EventManager.Subscribe("ConfigurationLoaded", evt => { eventLog.Add("ConfigurationLoaded"); return Task.CompletedTask; });
        engine.EventManager.Subscribe("PackagesDiscovered", evt => { eventLog.Add("PackagesDiscovered"); return Task.CompletedTask; });
        engine.EventManager.Subscribe("PackagesValidated", evt => { eventLog.Add("PackagesValidated"); return Task.CompletedTask; });
        engine.EventManager.Subscribe("PackagesLoaded", evt => { eventLog.Add("PackagesLoaded"); return Task.CompletedTask; });
        engine.EventManager.Subscribe("ApplicationsLoaded", evt => { eventLog.Add("ApplicationsLoaded"); return Task.CompletedTask; });
        engine.EventManager.Subscribe("EngineStarted", evt => { eventLog.Add("EngineStarted"); return Task.CompletedTask; });

        await engine.StartAsync();

        Assert.Equal(EngineState.Running, engine.CurrentState);
        Assert.Contains("ConfigurationLoaded", eventLog);
        Assert.Contains("PackagesDiscovered", eventLog);
        Assert.Contains("PackagesValidated", eventLog);
        Assert.Contains("PackagesLoaded", eventLog);
        Assert.Contains("ApplicationsLoaded", eventLog);
        Assert.Contains("EngineStarted", eventLog);
    }

    [Fact]
    public async Task ShutdownSequence_UnloadsAndStopsGracefully()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "fxengine-shutdown", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var configuration = new FXEngineConfiguration { BaseDirectory = tempDirectory };
        var logger = new TestLogger();
        var engine = new EngineManager(configuration, logger);
        engine.RegisterApplication(new TestApplication());

        await engine.StartAsync();
        await engine.StopAsync();

        Assert.Equal(EngineState.Stopped, engine.CurrentState);
    }

    [Fact]
    public async Task FailureRecovery_AllowsStartupToContinueWhenPackageFails()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "fxengine-recovery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Plugins"));
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Themes"));
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Packages"));
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Packages", "bad.pluginfx"));
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Packages", "good.pluginfx"));

        File.WriteAllText(Path.Combine(tempDirectory, "Packages", "bad.pluginfx", "manifest.fxmanifest"), "{\"Name\":\"Bad\",\"Author\":\"x\",\"Version\":\"bad\",\"EngineVersion\":\"1.0.0\",\"PackageType\":\"plugin\",\"EntryPoint\":\"x.dll\"}");
        File.WriteAllText(Path.Combine(tempDirectory, "Packages", "good.pluginfx", "manifest.fxmanifest"), "{\"Name\":\"Good\",\"Author\":\"x\",\"Version\":\"1.0.0\",\"EngineVersion\":\"1.0.0\",\"PackageType\":\"plugin\",\"EntryPoint\":\"x.dll\"}");

        var configuration = new FXEngineConfiguration { BaseDirectory = tempDirectory };
        var logger = new TestLogger();
        var engine = new EngineManager(configuration, logger);

        await engine.StartAsync();

        Assert.Equal(EngineState.Running, engine.CurrentState);
        Assert.Contains(engine.BootReport.Errors, error => error.Contains("bad", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestApplication : IFXApplication
    {
        public string Id => "test.app";
        public string Name => "Test App";
        public string Version => "1.0.0";
        public string Author => "FX Engine";
        public string Description => "Test application";

        public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StartAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null)
        {
        }
    }
}
