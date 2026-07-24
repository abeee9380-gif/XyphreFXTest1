using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Tests;

public class AnimationRuntimeTests
{
    [Fact]
    public async Task AnimationManager_PlaysAndStopsRegisteredAnimations()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var context = new FXEngineContext(configuration, logger);
        var manager = new AnimationManager(logger);
        var animation = new TestAnimation();
        manager.Register(animation);

        await manager.PlayAsync(animation.Id, context);
        await manager.StopAsync(animation.Id, context);

        Assert.True(animation.Played);
        Assert.True(animation.Stopped);
    }

    [Fact]
    public async Task AnimationManager_RejectsUnknownAnimation()
    {
        var logger = new TestLogger();
        var manager = new AnimationManager(logger);
        var context = new FXEngineContext(new FXEngineConfiguration(), logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.PlayAsync("missing", context));
        Assert.Contains("Unknown animation", ex.Message);
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Debug(string message) { }
    }

    private sealed class TestAnimation : IFXAnimation
    {
        public string Id => "fade";
        public string Name => "Fade";
        public string Type => "fade";
        public bool Played { get; private set; }
        public bool Stopped { get; private set; }

        public Task PlayAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Played = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Stopped = true;
            return Task.CompletedTask;
        }
    }
}
