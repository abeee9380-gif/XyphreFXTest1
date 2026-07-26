using System.Text.Json;
using System.Collections.Concurrent;

namespace FXEngine.SDK;

/// <summary>
/// Defines the contract for a plugin that can be loaded by the engine.
/// Plugin implementations should remain focused on their own behavior and consume the engine through the <see cref="FXEngineContext"/>.
/// </summary>
public interface IFXPlugin
{
    /// <summary>
    /// Gets the plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the plugin display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the plugin author or publisher.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Called when the plugin is loaded into the host.
    /// </summary>
    Task OnLoadAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called after the plugin has been initialized.
    /// </summary>
    Task OnInitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the plugin is enabled.
    /// </summary>
    Task OnEnableAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the plugin is disabled.
    /// </summary>
    Task OnDisableAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the plugin is unloaded from the host.
    /// </summary>
    Task OnUnloadAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Returns the events that the plugin wants to subscribe to.
    /// </summary>
    IEnumerable<string> RegisterEvents() => Array.Empty<string>();

    /// <summary>
    /// Returns the services that the plugin wants to expose to the host.
    /// </summary>
    IEnumerable<FXServiceRegistration> RegisterServices() => Array.Empty<FXServiceRegistration>();

    /// <summary>
    /// Returns the commands that the plugin wants to expose.
    /// </summary>
    IEnumerable<FXPluginCommand> RegisterCommands() => Array.Empty<FXPluginCommand>();
}

/// <summary>
/// Defines the contract for a user interface theme.
/// Themes should be stateless and apply their visual configuration through the supplied context.
/// </summary>
public interface IFXTheme
{
    /// <summary>
    /// Gets the theme identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the theme display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the theme version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the theme author.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the primary color used by the theme.
    /// </summary>
    string PrimaryColor { get; }

    /// <summary>
    /// Gets the secondary color used by the theme.
    /// </summary>
    string SecondaryColor { get; }

    /// <summary>
    /// Gets the accent color used by the theme.
    /// </summary>
    string AccentColor { get; }

    /// <summary>
    /// Gets the font family used by the theme.
    /// </summary>
    string FontFamily { get; }

    /// <summary>
    /// Gets the icon asset names used by the theme.
    /// </summary>
    IReadOnlyList<string> Icons { get; }

    /// <summary>
    /// Gets the animation identifiers used by the theme.
    /// </summary>
    IReadOnlyList<string> Animations { get; }

    /// <summary>
    /// Called once during theme initialization.
    /// </summary>
    Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the theme is loaded into the runtime.
    /// </summary>
    Task OnLoadAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Applies the theme to the running host.
    /// </summary>
    Task ApplyAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the theme is applied to the host.
    /// </summary>
    Task OnApplyAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Removes the theme from the active host state.
    /// </summary>
    Task RemoveAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the theme is removed from the active host state.
    /// </summary>
    Task OnRemoveAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Called when the theme is unloaded from the runtime.
    /// </summary>
    Task OnUnloadAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Returns the assets that the theme wants to register.
    /// </summary>
    IEnumerable<ThemeAssetRegistration> RegisterAssets() => Array.Empty<ThemeAssetRegistration>();

    /// <summary>
    /// Returns the fonts that the theme wants to register.
    /// </summary>
    IEnumerable<ThemeFontRegistration> RegisterFonts() => Array.Empty<ThemeFontRegistration>();

    /// <summary>
    /// Returns the animations that the theme wants to register.
    /// </summary>
    IEnumerable<ThemeAnimationRegistration> RegisterAnimations() => Array.Empty<ThemeAnimationRegistration>();

    /// <summary>
    /// Returns the colors that the theme wants to expose.
    /// </summary>
    IEnumerable<ThemeColorRegistration> RegisterColors() => Array.Empty<ThemeColorRegistration>();
}

/// <summary>
/// Defines the contract for a desktop application hosted by the engine.
/// Applications should never access the engine internals directly and should use <see cref="FXEngineContext"/> for runtime services and lifecycle integration.
/// </summary>
public interface IFXApplication
{
    /// <summary>
    /// Gets the application identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the application display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the application version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the application author.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the application description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Initializes the application before it is started.
    /// </summary>
    Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Starts the application once initialization is complete.
    /// </summary>
    Task StartAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Stops the application during shutdown or when the host requests it.
    /// </summary>
    Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Releases any application resources when the engine disposes the instance.
    /// </summary>
    Task DisposeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>
/// Defines the contract for an installable package.
/// Packages provide metadata, dependencies, and optional assets that the engine can manage across startup and shutdown.
/// </summary>
public interface IFXPackage
{
    /// <summary>
    /// Gets the package identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the package display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the package version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the package author.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets metadata associated with the package.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the package dependencies.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the asset names supplied by the package.
    /// </summary>
    IReadOnlyList<string> Assets { get; }

    /// <summary>
    /// Installs the package into the host environment.
    /// </summary>
    Task InstallAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Uninstalls the package from the host environment.
    /// </summary>
    Task UninstallAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>
/// Describes a package discovered by the extension system.
/// </summary>
public interface IPackageDescriptor
{
    /// <summary>
    /// Gets the package identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the package display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the package version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the package type.
    /// </summary>
    string PackageType { get; }

    /// <summary>
    /// Gets the package entry point.
    /// </summary>
    string EntryPoint { get; }

    /// <summary>
    /// Gets the engine version required by the package.
    /// </summary>
    string? EngineVersion { get; }

    /// <summary>
    /// Gets the resolved dependencies.
    /// </summary>
    Dictionary<string, string> Dependencies { get; }

    /// <summary>
    /// Gets the manifest path.
    /// </summary>
    string ManifestPath { get; }

    /// <summary>
    /// Gets the package root directory.
    /// </summary>
    string RootPath { get; }
}

/// <summary>
/// Defines the contract for a renderer implementation.
/// Renderers should expose their behavior through the public context without requiring access to internal engine implementation details.
/// </summary>
public interface IFXRenderer
{
    /// <summary>
    /// Gets the renderer identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the renderer display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the renderer version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Initializes the renderer before it is used.
    /// </summary>
    Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Renders the current frame.
    /// </summary>
    Task RenderAsync(FXEngineContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>
/// Defines the contract for a playable animation effect.
/// </summary>
public interface IFXAnimation
{
    /// <summary>
    /// Gets the animation identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the animation display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the animation type.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Starts the animation.
    /// </summary>
    Task PlayAsync(FXEngineContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the animation.
    /// </summary>
    Task StopAsync(FXEngineContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a render surface that can be drawn onto by a renderer.
/// </summary>
public sealed class FXRenderSurface
{
    public FXRenderSurface(string id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;
    }

    public string Id { get; }
    public int Width { get; }
    public int Height { get; }
    public List<FXRenderLayer> Layers { get; } = new();
}

/// <summary>
/// Represents a render layer within a surface.
/// </summary>
public sealed class FXRenderLayer
{
    public FXRenderLayer(string id)
    {
        Id = id;
    }

    public string Id { get; }
    public List<FXRenderOperation> Operations { get; set; } = new();
}

/// <summary>
/// Represents a single render operation on a layer.
/// </summary>
public sealed class FXRenderOperation
{
    public FXRenderOperation(string type, object? payload = null)
    {
        Type = type;
        Payload = payload;
    }

    public string Type { get; }
    public object? Payload { get; }
}

/// <summary>
/// Represents the logging levels exposed by the engine.
/// </summary>
public enum FXLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Provides the engine configuration and folder locations.
/// </summary>
public sealed class FXEngineConfiguration : IFXConfiguration
{
    /// <summary>
    /// Gets or sets the base directory used by the engine.
    /// </summary>
    public string BaseDirectory { get; set; } = AppContext.BaseDirectory;

    /// <summary>
    /// Gets or sets the logs folder name relative to the base directory.
    /// </summary>
    public string LogsDirectory { get; set; } = "Logs";

    /// <summary>
    /// Gets or sets the settings file name.
    /// </summary>
    public string SettingsFileName { get; set; } = "settings.json";

    /// <summary>
    /// Gets or sets the profile directory name.
    /// </summary>
    public string ProfileDirectory { get; set; } = "Profiles";

    /// <summary>
    /// Gets or sets the plugin directory name.
    /// </summary>
    public string PluginDirectory { get; set; } = "Plugins";

    /// <summary>
    /// Gets or sets the theme directory name.
    /// </summary>
    public string ThemeDirectory { get; set; } = "Themes";

    /// <summary>
    /// Gets or sets the package directory name.
    /// </summary>
    public string PackageDirectory { get; set; } = "Packages";

    /// <summary>
    /// Gets or sets the layout directory name.
    /// </summary>
    public string LayoutDirectory { get; set; } = "Layouts";

    /// <summary>
    /// Gets or sets the asset directory name.
    /// </summary>
    public string AssetDirectory { get; set; } = "Assets";

    /// <summary>
    /// Gets or sets the renderer directory name.
    /// </summary>
    public string RendererDirectory { get; set; } = "Renderers";

    /// <summary>
    /// Gets or sets the user profile extension.
    /// </summary>
    public string ProfileFileExtension { get; set; } = ".xephyrefx";

    /// <summary>
    /// Gets or sets the plugin extension.
    /// </summary>
    public string PluginFileExtension { get; set; } = ".pluginfx";

    /// <summary>
    /// Gets or sets the theme extension.
    /// </summary>
    public string ThemeFileExtension { get; set; } = ".themefx";

    /// <summary>
    /// Gets or sets the manifest file name.
    /// </summary>
    public string ManifestFileName { get; set; } = "manifest.fxmanifest";
}

/// <summary>
/// Exposes the engine runtime context to applications and plugins.
/// This is the only public object that external components should depend on for runtime access.
/// </summary>
public sealed class FXEngineContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FXEngineContext"/> class.
    /// </summary>
    public FXEngineContext(FXEngineConfiguration configuration, IFXLogger logger)
    {
        Configuration = configuration;
        Logger = logger;
        EventBus = new FXEventBus();
        ThemeContext = new ThemeContext();
        Services = new FXServiceContainer();
        Assets = new FXAssetLoader();
        Settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        Applications = new List<IFXApplication>();
        Themes = new List<IFXTheme>();
        Plugins = new List<IFXPlugin>();
    }

    /// <summary>
    /// Gets the engine configuration.
    /// </summary>
    public IFXConfiguration Configuration { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    public IFXLogger Logger { get; }

    /// <summary>
    /// Gets the event bus exposed to external components.
    /// </summary>
    public IFXEventBus EventBus { get; }

    /// <summary>
    /// Gets the service container used to resolve runtime services.
    /// </summary>
    public FXServiceContainer Services { get; }

    /// <summary>
    /// Gets the asset loader used by external components.
    /// </summary>
    public IFXAssetLoader Assets { get; }

    /// <summary>
    /// Gets the settings bag exposed to external components.
    /// </summary>
    public IDictionary<string, object> Settings { get; }

    /// <summary>
    /// Gets the active theme context for the current runtime session.
    /// </summary>
    public ThemeContext ThemeContext { get; }

    /// <summary>
    /// Gets the currently registered applications.
    /// </summary>
    public IList<IFXApplication> Applications { get; }

    /// <summary>
    /// Gets the currently registered themes.
    /// </summary>
    public IList<IFXTheme> Themes { get; }

    /// <summary>
    /// Gets the currently registered plugins.
    /// </summary>
    public IList<IFXPlugin> Plugins { get; }

    /// <summary>
    /// Gets or sets the active renderer.
    /// </summary>
    public IFXRenderer? Renderer { get; set; }

    /// <summary>
    /// Gets a dictionary of engine state values.
    /// </summary>
    public IDictionary<string, object> State { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents a single event published through the engine event bus.
/// </summary>
public sealed class FXEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FXEvent"/> class.
    /// </summary>
    public FXEvent(string name, object? payload = null, string? source = null)
    {
        Name = name;
        Payload = payload;
        Source = source;
    }

    /// <summary>
    /// Gets the event name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the payload associated with the event.
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    /// Gets the event source.
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents an FX manifest file.
/// </summary>
public sealed class FXManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Gets or sets the manifest name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manifest author.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manifest version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the manifest description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the required engine version.
    /// </summary>
    public string EngineVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the manifest dependencies.
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the package type.
    /// </summary>
    public string PackageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entry point.
    /// </summary>
    public string EntryPoint { get; set; } = string.Empty;

    /// <summary>
    /// Loads a manifest from disk.
    /// </summary>
    public static async Task<FXManifest> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var manifest = JsonSerializer.Deserialize<FXManifest>(json, SerializerOptions);
        return manifest ?? throw new InvalidOperationException($"Unable to load manifest from {path}.");
    }

    /// <summary>
    /// Saves the manifest to disk.
    /// </summary>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }
}

/// <summary>
/// Represents layout data for a widget surface.
/// </summary>
public sealed class FXLayout
{
    /// <summary>
    /// Gets or sets the layout name.
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// Gets or sets widget positions.
    /// </summary>
    public Dictionary<string, WidgetLayoutState> WidgetPositions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets anchors.
    /// </summary>
    public Dictionary<string, string> Anchors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the layout scale.
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the layout offsets.
    /// </summary>
    public Dictionary<string, double> Offsets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the animation states.
    /// </summary>
    public Dictionary<string, string> AnimationStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Represents the position and size of a widget within a layout.
/// </summary>
public sealed class WidgetLayoutState
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public double Height { get; set; }
}

/// <summary>
/// Represents a user profile for the engine.
/// </summary>
public sealed class FXProfile
{
    /// <summary>
    /// Gets or sets the profile identifier.
    /// </summary>
    public string Id { get; set; } = "default";

    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the installed theme identifier.
    /// </summary>
    public string InstalledTheme { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the installed plugin identifiers.
    /// </summary>
    public List<string> InstalledPlugins { get; set; } = new();

    /// <summary>
    /// Gets or sets the persistent settings.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the window positions.
    /// </summary>
    public Dictionary<string, string> WindowPositions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the saved cities.
    /// </summary>
    public List<string> Cities { get; set; } = new();

    /// <summary>
    /// Gets or sets the API keys.
    /// </summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the display language.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets the UI scale.
    /// </summary>
    public double Scale { get; set; } = 1.0;
}

/// <summary>
/// Defines the logger contract consumed by the engine and its extensions.
/// Implementations should remain lightweight and side-effect free apart from logging.
/// </summary>
public interface IFXLogger
{
    /// <summary>
    /// Writes a log entry.
    /// </summary>
    void Log(FXLogLevel level, string message, Exception? exception = null);

    /// <summary>
    /// Writes an informational log entry.
    /// </summary>
    void Info(string message) => Log(FXLogLevel.Information, message);

    /// <summary>
    /// Writes a warning log entry.
    /// </summary>
    void Warning(string message) => Log(FXLogLevel.Warning, message);

    /// <summary>
    /// Writes an error log entry.
    /// </summary>
    void Error(string message, Exception? exception = null) => Log(FXLogLevel.Error, message, exception);

    /// <summary>
    /// Writes a debug log entry.
    /// </summary>
    void Debug(string message) => Log(FXLogLevel.Debug, message);
}

/// <summary>
/// Defines the public event bus contract used by external components.
/// </summary>
public interface IFXEventBus
{
    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    IDisposable Subscribe(string eventName, Func<FXEvent, Task> handler);

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    Task PublishAsync(FXEvent evt, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the configuration contract exposed by the SDK.
/// </summary>
public interface IFXConfiguration
{
    /// <summary>
    /// Gets or sets the base directory used by the engine.
    /// </summary>
    string BaseDirectory { get; set; }

    /// <summary>
    /// Gets or sets the folder used for log output.
    /// </summary>
    string LogsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the settings file name.
    /// </summary>
    string SettingsFileName { get; set; }
}

/// <summary>
/// Defines the public profile contract used by applications and themes.
/// </summary>
public interface IFXProfile
{
    /// <summary>
    /// Gets or sets the profile identifier.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Gets or sets the profile name.
    /// </summary>
    string Name { get; set; }
}

/// <summary>
/// Defines the public marker interface for services that can be registered in the host service container.
/// </summary>
public interface IFXService
{
}

/// <summary>
/// Defines the contract for a lightweight asset loader.
/// </summary>
public interface IFXAssetLoader
{
    /// <summary>
    /// Loads a resource by relative path.
    /// </summary>
    Task<string?> LoadTextAsync(string relativePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a theme resource registration.
/// </summary>
public sealed class ThemeAssetRegistration
{
    public ThemeAssetRegistration(string key, string path)
    {
        Key = key;
        Path = path;
    }

    public string Key { get; }
    public string Path { get; }
}

/// <summary>
/// Represents a theme font registration.
/// </summary>
public sealed class ThemeFontRegistration
{
    public ThemeFontRegistration(string family, string path)
    {
        Family = family;
        Path = path;
    }

    public string Family { get; }
    public string Path { get; }
}

/// <summary>
/// Represents a theme animation registration.
/// </summary>
public sealed class ThemeAnimationRegistration
{
    public ThemeAnimationRegistration(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }
    public string Type { get; }
}

/// <summary>
/// Represents a theme color registration.
/// </summary>
public sealed class ThemeColorRegistration
{
    public ThemeColorRegistration(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string Value { get; }
}

/// <summary>
/// Exposes a theme palette to host consumers.
/// </summary>
public sealed class ThemePalette
{
    public string Primary { get; set; } = "#1E1E1E";
    public string Secondary { get; set; } = "#2F2F2F";
    public string Accent { get; set; } = "#4FC3F7";
    public string Success { get; set; } = "#4CAF50";
    public string Warning { get; set; } = "#FF9800";
    public string Danger { get; set; } = "#F44336";
    public string Background { get; set; } = "#111111";
    public string Foreground { get; set; } = "#F5F5F5";
    public string Glow { get; set; } = "#FFFFFF";
    public string Shadow { get; set; } = "#000000";
}

/// <summary>
/// Exposes resources registered by a theme.
/// </summary>
public sealed class ThemeResources
{
    private readonly Dictionary<string, string> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _fonts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _animations = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Assets => _assets;
    public IReadOnlyDictionary<string, string> Fonts => _fonts;
    public IReadOnlyDictionary<string, string> Animations => _animations;

    public void RegisterAsset(string key, string path) => _assets[key] = path;
    public void RegisterFont(string family, string path) => _fonts[family] = path;
    public void RegisterAnimation(string name, string type) => _animations[name] = type;
}

/// <summary>
/// Exposes metadata for a loaded theme.
/// </summary>
public sealed class ThemeMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string PackagePath { get; set; } = string.Empty;
}

/// <summary>
/// Provides the theme runtime context used by theme implementations.
/// </summary>
public sealed class ThemeContext
{
    public ThemeContext()
    {
        Resources = new ThemeResources();
        Palette = new ThemePalette();
        Metadata = new ThemeMetadata();
    }

    public ThemeResources Resources { get; }
    public ThemePalette Palette { get; }
    public ThemeMetadata Metadata { get; }
}

/// <summary>
/// Represents a service registration that can be provided by a plugin or application.
/// </summary>
public sealed class FXServiceRegistration
{
    /// <summary>
    /// Initializes a new service registration.
    /// </summary>
    public FXServiceRegistration(Type serviceType, object implementation)
    {
        ServiceType = serviceType;
        Implementation = implementation;
    }

    /// <summary>
    /// Gets the service contract type.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the implementation instance.
    /// </summary>
    public object Implementation { get; }
}

/// <summary>
/// Provides a lightweight dependency injection container for SDK consumers.
/// </summary>
public sealed class FXServiceContainer
{
    private readonly Dictionary<Type, object> _services = new();

    /// <summary>
    /// Registers a service instance for the specified contract type.
    /// </summary>
    public void Register<TService>(TService implementation) where TService : class
    {
        _services[typeof(TService)] = implementation ?? throw new ArgumentNullException(nameof(implementation));
    }

    /// <summary>
    /// Registers a service using a registration descriptor.
    /// </summary>
    public void Register(FXServiceRegistration registration)
    {
        _services[registration.ServiceType] = registration.Implementation ?? throw new ArgumentNullException(nameof(registration));
    }

    /// <summary>
    /// Resolves a service instance for the specified contract type.
    /// </summary>
    public TService? Resolve<TService>() where TService : class
    {
        return _services.TryGetValue(typeof(TService), out var instance) ? instance as TService : null;
    }
}

/// <summary>
/// Provides the public event bus implementation used by the SDK.
/// </summary>
public sealed class FXEventBus : IFXEventBus
{
    private readonly Dictionary<string, List<Func<FXEvent, Task>>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Subscribes to an event name.
    /// </summary>
    public IDisposable Subscribe(string eventName, Func<FXEvent, Task> handler)
    {
        if (string.IsNullOrWhiteSpace(eventName)) throw new ArgumentException("Event name cannot be empty.", nameof(eventName));
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers = new List<Func<FXEvent, Task>>();
            _subscribers[eventName] = handlers;
        }

        handlers.Add(handler);
        return new Subscription(this, eventName, handler);
    }

    /// <summary>
    /// Publishes an event to all current subscribers.
    /// </summary>
    public async Task PublishAsync(FXEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt is null) throw new ArgumentNullException(nameof(evt));

        if (_subscribers.TryGetValue(evt.Name, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await handler(evt);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly FXEventBus _owner;
        private readonly string _eventName;
        private readonly Func<FXEvent, Task> _handler;
        private bool _disposed;

        public Subscription(FXEventBus owner, string eventName, Func<FXEvent, Task> handler)
        {
            _owner = owner;
            _eventName = eventName;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_owner._subscribers.TryGetValue(_eventName, out var handlers))
            {
                handlers.Remove(_handler);
            }
        }
    }
}

/// <summary>
/// Provides a simple asset loader implementation that reads files from disk.
/// </summary>
public sealed class FXAssetLoader : IFXAssetLoader
{
    /// <summary>
    /// Loads text content from the configured base directory.
    /// </summary>
    public Task<string?> LoadTextAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.FromResult<string?>(null);

        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.Exists(path) ? File.ReadAllTextAsync(path, cancellationToken) : Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Represents a command exposed by a plugin.
/// </summary>
public sealed class FXPluginCommand
{
    public FXPluginCommand(string name, string description, Func<FXEngineContext, Task> execute)
    {
        Name = name;
        Description = description;
        Execute = execute;
    }

    public string Name { get; }
    public string Description { get; }
    public Func<FXEngineContext, Task> Execute { get; }
}

/// <summary>
/// Provides a plugin-focused runtime context for safe SDK access.
/// </summary>
public sealed class PluginContext
{
    public PluginContext(FXEngineContext context)
    {
        Logger = context.Logger;
        Renderer = context.Renderer;
        Services = context.Services;
        Assets = context.Assets;
        Settings = context.Settings;
        Configuration = context.Configuration;
        EventBus = context.EventBus;
        Theme = context.ThemeContext;
    }

    public IFXLogger Logger { get; }
    public IFXRenderer? Renderer { get; }
    public FXServiceContainer Services { get; }
    public IFXAssetLoader Assets { get; }
    public IDictionary<string, object> Settings { get; }
    public IFXConfiguration Configuration { get; }
    public IFXEventBus EventBus { get; }
    public ThemeContext Theme { get; }
}
