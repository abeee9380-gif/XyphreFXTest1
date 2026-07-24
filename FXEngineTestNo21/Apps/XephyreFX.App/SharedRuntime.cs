using XephyreFX.App.Config;
using XephyreFX.App.Platform;
using XephyreFX.App.Sim;

namespace XephyreFX.App;

/// <summary>
/// Everything that both windows need to share -- the simulation, the config, the weather
/// override, the event list. Both SceneWindow and SettingsWindow hold the same instance of
/// each, so a change made in the settings panel (a different window/process-level object,
/// but the same in-memory instance) is visible to the scene renderer on its very next frame
/// with no extra plumbing.
/// </summary>
public sealed class SharedRuntime
{
    public WeatherSceneComposer Scene { get; } = new();
    public WeatherState State { get; } = new();
    public WeatherOverrideService Override { get; } = new();
    public ConfigService Config { get; } = new();
    public SpecialEventService Events { get; } = new();
    public StartupService Startup { get; } = new();
}
