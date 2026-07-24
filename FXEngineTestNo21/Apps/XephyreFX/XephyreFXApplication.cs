using System.Diagnostics;
using System.Threading;
using FXEngine.SDK;
using XephyreFX.Sim;

namespace XephyreFX;

/// <summary>
/// XephyreFX as a real engine plugin. The simulation itself (<see cref="Sim"/> namespace) is
/// the exact same code that runs in the standalone <c>XephyreFX.App</c> preview -- it has no
/// UI-framework dependency, so it works identically here.
///
/// The engine doesn't have a concrete renderer or frame-loop implementation yet (see
/// Docs/architecture.md), so this class runs its own background tick loop and simply exposes
/// the current scene state via <see cref="Scene"/>/<see cref="State"/>. Whatever renderer
/// eventually gets built reads from those two properties every frame -- see
/// Apps/XephyreFX.App/Rendering/AvaloniaSceneRenderer.cs for a working example of exactly
/// what to read and how.
/// </summary>
public sealed class XephyreFXApplication : IFXApplication
{
    public string Id => "xephyrefx";
    public string Name => "XephyreFX";
    public string Version => "0.1.0";
    public string Author => "XephyreFX contributors";
    public string Description => "Animated weather scene: a morphing blob showing time, temperature, and forecast, with clouds, rain, thunderstorms, stars, sun/moon, and a Valentine's Day mode.";

    public WeatherSceneComposer Scene { get; } = new();
    public WeatherState State { get; } = new();
    public WeatherOverrideService Override { get; } = new();

    private CancellationTokenSource? _loopCancellation;
    private Task? _loopTask;
    private IFXLogger? _logger;

    public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        _logger = context.Logger;
        _logger.Info($"{Name} v{Version} initialized ({Scene.Blob.PointCount}-point blob, seed-random).");
        return Task.CompletedTask;
    }

    public Task StartAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        _loopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunTickLoopAsync(_loopCancellation.Token), CancellationToken.None);
        _logger?.Info($"{Name} tick loop started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        _loopCancellation?.Cancel();
        if (_loopTask is not null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
        _logger?.Info($"{Name} tick loop stopped.");
    }

    private async Task RunTickLoopAsync(CancellationToken token)
    {
        var clock = Stopwatch.StartNew();
        double lastSeconds = 0;

        while (!token.IsCancellationRequested)
        {
            double now = clock.Elapsed.TotalSeconds;
            double dt = Math.Min(now - lastSeconds, 0.1);
            lastSeconds = now;

            State.LocalTime = DateTime.Now;
            State.Period = WeatherState.PeriodFromClock(State.LocalTime);
            Override.Apply(State);

            // No window/canvas exists yet, so there's no real width/height to react to.
            // Fixed 400x400 stage keeps particle spawn geometry sane until a real renderer
            // (and its actual surface size) is wired in.
            const double stageSize = 400;
            var blobCenter = new Vec2(stageSize / 2, stageSize / 2);
            Scene.Tick(dt, State, stageSize, stageSize, blobCenter);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(16), token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
