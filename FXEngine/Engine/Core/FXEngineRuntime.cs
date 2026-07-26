using FXEngine.SDK;

namespace FXEngine.Core;

/// <summary>
/// Coordinates engine startup, initialization, and shutdown for the host runtime.
/// </summary>
public enum EngineState
{
    Starting,
    DiscoveringPackages,
    LoadingPackages,
    Initializing,
    Running,
    Stopping,
    Stopped,
    Error
}

/// <summary>
/// Coordinates engine startup, initialization, and shutdown for the host runtime.
/// </summary>
public sealed class EngineManager
{
    private readonly FXEngineConfiguration _configuration;
    private readonly IFXLogger _logger;
    private readonly ServiceManager _serviceManager;
    private readonly ThemeManager _themeManager;
    private readonly PluginManager _pluginManager;
    private readonly PackageManager _packageManager;
    private readonly RendererManager _rendererManager;
    private readonly ApplicationManager _applicationManager;
    private readonly SettingsManager _settingsManager;
    private readonly EventManager _eventManager;
    private readonly ProfileManager _profileManager;
    private readonly LayoutManager _layoutManager;
    private readonly AssetManager _assetManager;
    private readonly List<IFXApplication> _applications = new();
    private readonly List<string> _warnings = new();
    private readonly List<string> _errors = new();
    private readonly EventBus _eventBus;

    public EngineManager(FXEngineConfiguration configuration, IFXLogger? logger = null)
    {
        _configuration = configuration;
        _logger = logger ?? new FXLogger(configuration);
        _serviceManager = new ServiceManager(_logger);
        _themeManager = new ThemeManager(_logger);
        _pluginManager = new PluginManager(_logger);
        _packageManager = new PackageManager(_logger);
        _rendererManager = new RendererManager(_logger);
        _eventBus = new EventBus(_logger);
        _applicationManager = new ApplicationManager(_logger, _eventBus);
        _settingsManager = new SettingsManager(configuration, _logger);
        _eventManager = new EventManager(_eventBus);
        _profileManager = new ProfileManager(configuration, _logger);
        _layoutManager = new LayoutManager(configuration, _logger);
        _assetManager = new AssetManager(configuration, _logger);
    }

    /// <summary>
    /// Gets the logger used by the engine.
    /// </summary>
    public IFXLogger Logger => _logger;

    /// <summary>
    /// Gets the current engine state.
    /// </summary>
    public EngineState CurrentState { get; private set; } = EngineState.Stopped;

    /// <summary>
    /// Gets the boot report generated during startup.
    /// </summary>
    public FXBootReport BootReport { get; private set; } = new();

    /// <summary>
    /// Gets the settings manager used by the engine.
    /// </summary>
    public SettingsManager SettingsManager => _settingsManager;

    /// <summary>
    /// Gets the application manager used by the engine.
    /// </summary>
    public ApplicationManager ApplicationManager => _applicationManager;

    /// <summary>
    /// Gets the event manager used by the engine.
    /// </summary>
    public EventManager EventManager => _eventManager;

    /// <summary>
    /// Gets the theme manager used by the engine.
    /// </summary>
    public ThemeManager ThemeManager => _themeManager;

    /// <summary>
    /// Gets the plugin manager used by the engine.
    /// </summary>
    public PluginManager PluginManager => _pluginManager;

    /// <summary>
    /// Gets the package manager used by the engine.
    /// </summary>
    public PackageManager PackageManager => _packageManager;

    /// <summary>
    /// Gets the renderer manager used by the engine.
    /// </summary>
    public RendererManager RendererManager => _rendererManager;

    /// <summary>
    /// Gets the profile manager used by the engine.
    /// </summary>
    public ProfileManager ProfileManager => _profileManager;

    /// <summary>
    /// Gets the layout manager used by the engine.
    /// </summary>
    public LayoutManager LayoutManager => _layoutManager;

    /// <summary>
    /// Gets the asset manager used by the engine.
    /// </summary>
    public AssetManager AssetManager => _assetManager;

    /// <summary>
    /// Gets the service manager used by the engine.
    /// </summary>
    public ServiceManager ServiceManager => _serviceManager;

    /// <summary>
    /// Registers an application with the engine.
    /// </summary>
    public void RegisterApplication(IFXApplication application)
    {
        _applications.Add(application);
        _applicationManager.Register(application);
    }

    /// <summary>
    /// Starts the engine using the documented startup order.
    /// </summary>
    public async Task<FXEngineContext> StartAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState == EngineState.Running)
        {
            return new FXEngineContext(_configuration, _logger);
        }

        CurrentState = EngineState.Starting;
        var context = new FXEngineContext(_configuration, _logger);
        var startTime = DateTimeOffset.UtcNow;
        _logger.Log(FXLogLevel.Information, "Starting FX Engine");

        try
        {
            await LoadConfigurationAsync(context, cancellationToken);
            await InitializeEventBusAsync(context, cancellationToken);
            await InitializeSettingsAsync(context, cancellationToken);
            await InitializePackageRegistryAsync(context, cancellationToken);
            await DiscoverPackagesAsync(context, cancellationToken);
            await ValidatePackagesAsync(context, cancellationToken);
            await ResolveDependenciesAsync(context, cancellationToken);
            await LoadPackagesAsync(context, cancellationToken);
            await InitializeServicesAsync(context, cancellationToken);
            await InitializeThemeManagerAsync(context, cancellationToken);
            await InitializePluginManagerAsync(context, cancellationToken);
            await InitializeRendererAsync(context, cancellationToken);
            await InitializeApplicationsAsync(context, cancellationToken);
            await LoadUserProfileAsync(context, cancellationToken);

            CurrentState = EngineState.Running;
            BootReport = new FXBootReport
            {
                EngineVersion = typeof(EngineManager).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                StartupTime = DateTimeOffset.UtcNow - startTime,
                LoadedThemes = _themeManager.ThemesCount,
                LoadedPlugins = _pluginManager.PluginsCount,
                LoadedPackages = 0,
                LoadedApplications = _applications.Count,
                Warnings = _warnings.ToList(),
                Errors = _errors.ToList()
            };

            await _eventBus.PublishAsync(new FXEvent("EngineStarted", BootReport), cancellationToken);
            _logger.Log(FXLogLevel.Information, "FX Engine started successfully");
            return context;
        }
        catch (Exception ex)
        {
            CurrentState = EngineState.Error;
            _errors.Add(ex.Message);
            _logger.Log(FXLogLevel.Critical, "FX Engine failed to start", ex);
            throw;
        }
    }

    /// <summary>
    /// Stops the engine gracefully.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState == EngineState.Stopped || CurrentState == EngineState.Stopping)
        {
            return;
        }

        CurrentState = EngineState.Stopping;
        await _eventBus.PublishAsync(new FXEvent("EngineStopping"), cancellationToken);

        foreach (var application in _applications)
        {
            await application.StopAsync(new FXEngineContext(_configuration, _logger), cancellationToken);
        }

        CurrentState = EngineState.Stopped;
        await _eventBus.PublishAsync(new FXEvent("EngineStopped"), cancellationToken);
        _logger.Log(FXLogLevel.Information, "FX Engine stopped");
    }

    private async Task LoadConfigurationAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_configuration.BaseDirectory);
        await _settingsManager.LoadAsync(cancellationToken);
        context.State["configuration.loaded"] = true;
        await _eventBus.PublishAsync(new FXEvent("ConfigurationLoaded"), cancellationToken);
    }

    private Task InitializeEventBusAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        _eventManager.Subscribe("ConfigurationLoaded", evt => { context.State["configuration.loaded"] = true; return Task.CompletedTask; });
        _eventManager.Subscribe("PackagesDiscovered", evt => { context.State["packages.discovered"] = true; return Task.CompletedTask; });
        _eventManager.Subscribe("PackagesValidated", evt => { context.State["packages.validated"] = true; return Task.CompletedTask; });
        _eventManager.Subscribe("PackagesLoaded", evt => { context.State["packages.loaded"] = true; return Task.CompletedTask; });
        _eventManager.Subscribe("ApplicationsLoaded", evt => { context.State["applications.loaded"] = true; return Task.CompletedTask; });
        _eventManager.Subscribe("EngineStarted", evt => { context.State["engine.started"] = true; return Task.CompletedTask; });
        context.State["eventbus.initialized"] = true;
        return Task.CompletedTask;
    }

    private async Task InitializeSettingsAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        await _settingsManager.LoadAsync(cancellationToken);
        context.State["settings.initialized"] = true;
    }

    private async Task InitializePackageRegistryAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        context.State["registry.initialized"] = true;
        await _eventBus.PublishAsync(new FXEvent("PackagesDiscovered"), cancellationToken);
    }

    private async Task DiscoverPackagesAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        CurrentState = EngineState.DiscoveringPackages;
        var packagesRoot = Path.Combine(_configuration.BaseDirectory, _configuration.PackageDirectory);
        Directory.CreateDirectory(packagesRoot);
        var loader = new PackageLoader(_configuration, _logger, _eventManager);
        var discovered = loader.DiscoverPackages(packagesRoot).ToList();
        foreach (var packageDirectory in Directory.EnumerateDirectories(packagesRoot, "*", SearchOption.AllDirectories).Where(directory => !Path.GetFileName(directory).StartsWith('.')))
        {
            var manifestPath = Path.Combine(packageDirectory, "manifest.fxmanifest");
            if (!File.Exists(manifestPath))
            {
                _warnings.Add($"Missing manifest in package directory {packageDirectory}");
                continue;
            }

            var validation = loader.ValidateManifest(manifestPath);
            if (!validation.IsValid)
            {
                _errors.Add($"Invalid package manifest in {packageDirectory}: {string.Join(", ", validation.Errors)}");
            }
        }
        context.State["packages.discovered"] = discovered.Count;
        await _eventBus.PublishAsync(new FXEvent("PackagesDiscovered", discovered), cancellationToken);
    }

    private async Task ValidatePackagesAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        var packagesRoot = Path.Combine(_configuration.BaseDirectory, _configuration.PackageDirectory);
        var loader = new PackageLoader(_configuration, _logger, _eventManager);
        var discovered = loader.DiscoverPackages(packagesRoot).ToList();
        context.State["packages.validated"] = discovered.Count;
        await _eventBus.PublishAsync(new FXEvent("PackagesValidated", discovered), cancellationToken);
    }

    private async Task ResolveDependenciesAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        var packagesRoot = Path.Combine(_configuration.BaseDirectory, _configuration.PackageDirectory);
        var loader = new PackageLoader(_configuration, _logger, _eventManager);
        var discovered = loader.DiscoverPackages(packagesRoot).ToList();
        var registry = new PackageRegistry(_logger);
        loader.LoadPackages(discovered, registry);
        context.State["packages.resolved"] = discovered.Count;
        await _eventBus.PublishAsync(new FXEvent("PackagesLoaded", discovered), cancellationToken);
    }

    private async Task LoadPackagesAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        CurrentState = EngineState.LoadingPackages;
        var packagesRoot = Path.Combine(_configuration.BaseDirectory, _configuration.PackageDirectory);
        var loader = new PackageLoader(_configuration, _logger, _eventManager);
        var discovered = loader.DiscoverPackages(packagesRoot).ToList();
        var registry = new PackageRegistry(_logger);
        loader.LoadPackages(discovered, registry);
        context.State["packages.loaded"] = registry.LoadedPackages.Count;
        await _eventBus.PublishAsync(new FXEvent("PackagesLoaded", registry.LoadedPackages), cancellationToken);
    }

    private Task InitializeServicesAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        _serviceManager.Register(typeof(IFXLogger), _logger);
        _serviceManager.Register(typeof(FXEngineConfiguration), _configuration);
        _serviceManager.Register(typeof(FXEngineContext), context);
        _logger.Log(FXLogLevel.Debug, "Initialized services");
        return Task.CompletedTask;
    }

    private async Task InitializeThemeManagerAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        CurrentState = EngineState.Initializing;
        await _themeManager.InitializeAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Debug, "Initialized theme manager");
    }

    private async Task InitializePluginManagerAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        await _pluginManager.InitializeAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Debug, "Initialized plugin manager");
    }

    private async Task InitializePackageManagerAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        await _packageManager.InitializeAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Debug, "Initialized package manager");
    }

    private async Task InitializeRendererAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        await _rendererManager.InitializeAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Debug, "Initialized renderer");
    }

    private async Task InitializeApplicationsAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        await _applicationManager.InitializeAsync(cancellationToken);
        await _applicationManager.StartAsync(cancellationToken);
        await _eventBus.PublishAsync(new FXEvent("ApplicationsLoaded"), cancellationToken);
        _logger.Log(FXLogLevel.Debug, "Initialized applications");
    }

    private async Task LoadUserProfileAsync(FXEngineContext context, CancellationToken cancellationToken)
    {
        var profile = await _profileManager.LoadAsync(cancellationToken);
        context.State["profile.loaded"] = profile.Id;
    }
}

/// <summary>
/// Tracks and exposes services registered with the engine.
/// </summary>
public sealed class ServiceManager
{
    private readonly IFXLogger _logger;
    private readonly Dictionary<Type, object> _services = new();

    public ServiceManager(IFXLogger logger)
    {
        _logger = logger;
    }

    public void Register<T>(T instance) where T : class
    {
        Register(typeof(T), instance);
    }

    public void Register(Type type, object instance)
    {
        _services[type] = instance;
        _logger.Log(FXLogLevel.Debug, $"Registered service {type.Name}");
    }

    public T? Resolve<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? service as T : default;
    }
}

/// <summary>
/// Maintains available themes and applies a selected theme.
/// </summary>
public sealed class ThemeManager
{
    private readonly IFXLogger _logger;
    private readonly List<IFXTheme> _themes = new();
    private readonly Dictionary<string, IFXTheme> _activeThemes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ThemeValidationResult> _themeCache = new(StringComparer.OrdinalIgnoreCase);

    public ThemeManager(IFXLogger logger)
    {
        _logger = logger;
    }

    public int ThemesCount => _themes.Count;
    public IReadOnlyList<IFXTheme> Themes => _themes.AsReadOnly();
    public string? ActiveThemeId => _activeThemes.Keys.FirstOrDefault();

    public void Register(IFXTheme theme)
    {
        if (_themes.Any(existing => existing.Id.Equals(theme.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _themes.Add(theme);
    }

    public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        foreach (var theme in _themes)
        {
            _logger.Log(FXLogLevel.Debug, $"Initialized theme {theme.Id}");
        }

        return Task.CompletedTask;
    }

    public async Task<IFXTheme> LoadThemeAsync(string themeId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var theme = ResolveTheme(themeId);
        var validation = ValidateTheme(theme, context);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Theme '{themeId}' failed validation: {string.Join(", ", validation.Errors)}");
        }

        await theme.OnLoadAsync(context, cancellationToken);
        await theme.InitializeAsync(context, cancellationToken);
        context.ThemeContext.Metadata.Id = theme.Id;
        context.ThemeContext.Metadata.Name = theme.Name;
        context.ThemeContext.Metadata.Version = theme.Version;
        context.ThemeContext.Metadata.Author = theme.Author;
        foreach (var asset in theme.RegisterAssets())
        {
            context.ThemeContext.Resources.RegisterAsset(asset.Key, asset.Path);
        }

        foreach (var font in theme.RegisterFonts())
        {
            context.ThemeContext.Resources.RegisterFont(font.Family, font.Path);
        }

        foreach (var animation in theme.RegisterAnimations())
        {
            context.ThemeContext.Resources.RegisterAnimation(animation.Name, animation.Type);
        }

        foreach (var color in theme.RegisterColors())
        {
            ApplyThemeColor(color.Name, color.Value, context);
        }

        _logger.Log(FXLogLevel.Information, $"Loaded theme {theme.Id}");
        _activeThemes[theme.Id] = theme;
        return theme;
    }

    public async Task ApplyThemeAsync(string themeId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var theme = ResolveTheme(themeId);
        if (!_activeThemes.ContainsKey(theme.Id))
        {
            await LoadThemeAsync(theme.Id, context, cancellationToken);
        }

        await theme.ApplyAsync(context, cancellationToken);
        await theme.OnApplyAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Information, $"Applied theme {theme.Id}");
    }

    public async Task SwitchThemeAsync(string themeId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var previousTheme = _activeThemes.Values.FirstOrDefault();
        if (previousTheme is not null && !previousTheme.Id.Equals(themeId, StringComparison.OrdinalIgnoreCase))
        {
            await previousTheme.RemoveAsync(context, cancellationToken);
            await previousTheme.OnRemoveAsync(context, cancellationToken);
            await previousTheme.OnUnloadAsync(context, cancellationToken);
        }

        var nextTheme = await LoadThemeAsync(themeId, context, cancellationToken);
        await ApplyThemeAsync(nextTheme.Id, context, cancellationToken);
        _logger.Log(FXLogLevel.Information, $"Switched theme to {nextTheme.Id}");
    }

    public async Task UnloadThemeAsync(string themeId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var theme = ResolveTheme(themeId);
        if (_activeThemes.ContainsKey(theme.Id))
        {
            await theme.RemoveAsync(context, cancellationToken);
            await theme.OnRemoveAsync(context, cancellationToken);
            await theme.OnUnloadAsync(context, cancellationToken);
            _activeThemes.Remove(theme.Id);
        }

        _logger.Log(FXLogLevel.Information, $"Unloaded theme {theme.Id}");
    }

    public ThemeValidationResult ValidateThemePackage(string packagePath, FXEngineContext context)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            return ThemeValidationResult.Failure("Theme package manifest is missing.");
        }

        return ThemeValidationResult.Success();
    }

    public ThemeValidationResult ValidateTheme(IFXTheme theme, FXEngineContext context)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(theme.Id))
        {
            errors.Add("Theme id is missing.");
        }

        if (string.IsNullOrWhiteSpace(theme.Name))
        {
            errors.Add("Theme name is missing.");
        }

        if (string.IsNullOrWhiteSpace(theme.Version))
        {
            errors.Add("Theme version is missing.");
        }

        if (errors.Count > 0)
        {
            return ThemeValidationResult.Failure(errors);
        }

        return ThemeValidationResult.Success();
    }

    private IFXTheme ResolveTheme(string themeId)
    {
        var theme = _themes.FirstOrDefault(item => item.Id.Equals(themeId, StringComparison.OrdinalIgnoreCase));
        if (theme is null)
        {
            throw new InvalidOperationException($"Unknown theme '{themeId}'.");
        }

        return theme;
    }

    private void ApplyThemeColor(string name, string value, FXEngineContext context)
    {
        switch (name)
        {
            case "Primary":
                context.ThemeContext.Palette.Primary = value;
                break;
            case "Secondary":
                context.ThemeContext.Palette.Secondary = value;
                break;
            case "Accent":
                context.ThemeContext.Palette.Accent = value;
                break;
            case "Success":
                context.ThemeContext.Palette.Success = value;
                break;
            case "Warning":
                context.ThemeContext.Palette.Warning = value;
                break;
            case "Danger":
                context.ThemeContext.Palette.Danger = value;
                break;
            case "Background":
                context.ThemeContext.Palette.Background = value;
                break;
            case "Foreground":
                context.ThemeContext.Palette.Foreground = value;
                break;
            case "Glow":
                context.ThemeContext.Palette.Glow = value;
                break;
            case "Shadow":
                context.ThemeContext.Palette.Shadow = value;
                break;
        }
    }
}

public sealed class ThemeValidationResult
{
    private ThemeValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    public static ThemeValidationResult Success() => new(true, Array.Empty<string>());
    public static ThemeValidationResult Failure(string error) => new(false, new[] { error });
    public static ThemeValidationResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}

/// <summary>
/// Maintains plugin registration and activation state.
/// </summary>
public sealed class PluginManager
{
    private readonly IFXLogger _logger;
    private readonly List<IFXPlugin> _plugins = new();
    private readonly Dictionary<string, IFXPlugin> _loadedPlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FXPluginCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _services = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _enabledPlugins = new(StringComparer.OrdinalIgnoreCase);

    public PluginManager(IFXLogger logger)
    {
        _logger = logger;
    }

    public int PluginsCount => _plugins.Count;

    public IReadOnlyList<IFXPlugin> Plugins => _plugins.AsReadOnly();

    public void Register(IFXPlugin plugin)
    {
        if (_plugins.Any(existing => existing.Id.Equals(plugin.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _plugins.Add(plugin);
    }

    public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        foreach (var plugin in _plugins)
        {
            _logger.Log(FXLogLevel.Debug, $"Initialized plugin {plugin.Id}");
        }

        return Task.CompletedTask;
    }

    public async Task<IFXPlugin> LoadPluginAsync(string pluginId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var plugin = ResolvePlugin(pluginId);
        try
        {
            await plugin.OnLoadAsync(context, cancellationToken);
            await plugin.OnInitializeAsync(context, cancellationToken);
            RegisterPluginServices(plugin, context);
            RegisterPluginCommands(plugin, context);
            RegisterPluginEvents(plugin, context);
            _loadedPlugins[plugin.Id] = plugin;
            _logger.Log(FXLogLevel.Information, $"Loaded plugin {plugin.Id}");
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.Log(FXLogLevel.Error, $"Plugin {plugin.Id} failed to load", ex);
            throw;
        }
    }

    public async Task EnablePluginAsync(string pluginId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var plugin = ResolvePlugin(pluginId);
        if (!_loadedPlugins.ContainsKey(plugin.Id))
        {
            await LoadPluginAsync(plugin.Id, context, cancellationToken);
        }

        try
        {
            await plugin.OnEnableAsync(context, cancellationToken);
            _enabledPlugins.Add(plugin.Id);
            await context.EventBus.PublishAsync(new FXEvent("PluginEnabled", plugin), cancellationToken);
            _logger.Log(FXLogLevel.Information, $"Enabled plugin {plugin.Id}");
        }
        catch (Exception ex)
        {
            _enabledPlugins.Remove(plugin.Id);
            _logger.Log(FXLogLevel.Error, $"Plugin {plugin.Id} failed to enable", ex);
            await context.EventBus.PublishAsync(new FXEvent("PluginFailed", new { Plugin = plugin.Id, Exception = ex.Message }), cancellationToken);
        }
    }

    public async Task DisablePluginAsync(string pluginId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var plugin = ResolvePlugin(pluginId);
        try
        {
            await plugin.OnDisableAsync(context, cancellationToken);
            _enabledPlugins.Remove(plugin.Id);
            await context.EventBus.PublishAsync(new FXEvent("PluginDisabled", plugin), cancellationToken);
            _logger.Log(FXLogLevel.Information, $"Disabled plugin {plugin.Id}");
        }
        catch (Exception ex)
        {
            _logger.Log(FXLogLevel.Error, $"Plugin {plugin.Id} failed to disable", ex);
        }
    }

    public async Task UnloadPluginAsync(string pluginId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var plugin = ResolvePlugin(pluginId);
        try
        {
            if (_enabledPlugins.Contains(plugin.Id))
            {
                await plugin.OnDisableAsync(context, cancellationToken);
                _enabledPlugins.Remove(plugin.Id);
            }

            await plugin.OnUnloadAsync(context, cancellationToken);
            _loadedPlugins.Remove(plugin.Id);
            await context.EventBus.PublishAsync(new FXEvent("PluginUnloaded", plugin), cancellationToken);
            _logger.Log(FXLogLevel.Information, $"Unloaded plugin {plugin.Id}");
        }
        catch (Exception ex)
        {
            _logger.Log(FXLogLevel.Error, $"Plugin {plugin.Id} failed to unload", ex);
        }
    }

    public async Task ReloadPluginAsync(string pluginId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        await UnloadPluginAsync(pluginId, context, cancellationToken);
        await LoadPluginAsync(pluginId, context, cancellationToken);
        await EnablePluginAsync(pluginId, context, cancellationToken);
        await context.EventBus.PublishAsync(new FXEvent("PluginReloaded", pluginId), cancellationToken);
    }

    public FXPluginCommand? ResolveCommand(string name)
    {
        return _commands.TryGetValue(name, out var command) ? command : null;
    }

    public TService? ResolveService<TService>() where TService : class
    {
        return _services.TryGetValue(typeof(TService).Name, out var service) ? service as TService : null;
    }

    private void RegisterPluginServices(IFXPlugin plugin, FXEngineContext context)
    {
        foreach (var registration in plugin.RegisterServices())
        {
            _services[registration.ServiceType.Name] = registration.Implementation;
            context.Services.Register(registration);
        }
    }

    private void RegisterPluginCommands(IFXPlugin plugin, FXEngineContext context)
    {
        foreach (var command in plugin.RegisterCommands())
        {
            _commands[command.Name] = command;
        }
    }

    private void RegisterPluginEvents(IFXPlugin plugin, FXEngineContext context)
    {
        foreach (var eventName in plugin.RegisterEvents())
        {
            context.EventBus.Subscribe(eventName, _ => Task.CompletedTask);
        }
    }

    private IFXPlugin ResolvePlugin(string pluginId)
    {
        var plugin = _plugins.FirstOrDefault(item => item.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        if (plugin is null)
        {
            throw new InvalidOperationException($"Unknown plugin '{pluginId}'.");
        }

        return plugin;
    }
}

/// <summary>
/// Maintains installed packages and their state.
/// </summary>
public sealed class PackageManager
{
    private readonly IFXLogger _logger;
    private readonly List<IFXPackage> _packages = new();

    public PackageManager(IFXLogger logger)
    {
        _logger = logger;
    }

    public void Register(IFXPackage package)
    {
        _packages.Add(package);
    }

    public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        foreach (var package in _packages)
        {
            _logger.Log(FXLogLevel.Debug, $"Initialized package {package.Id}");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Maintains renderer implementations and dispatches render operations.
/// </summary>
public sealed class RendererManager
{
    private readonly IFXLogger _logger;
    private readonly List<IFXRenderer> _renderers = new();

    public RendererManager(IFXLogger logger)
    {
        _logger = logger;
    }

    public void Register(IFXRenderer renderer)
    {
        if (_renderers.Any(existing => existing.Id.Equals(renderer.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _renderers.Add(renderer);
    }

    public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
    {
        foreach (var renderer in _renderers)
        {
            _logger.Log(FXLogLevel.Debug, $"Initialized renderer {renderer.Id}");
            renderer.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
        }

        return Task.CompletedTask;
    }

    public async Task RenderAsync(FXEngineContext context, FXRenderSurface surface, CancellationToken cancellationToken = default)
    {
        foreach (var renderer in _renderers)
        {
            await renderer.RenderAsync(context, cancellationToken);
            _logger.Log(FXLogLevel.Debug, $"Rendered surface {surface.Id} with {renderer.Id}");
        }
    }
}

/// <summary>
/// Manages registered animation effects and their lifecycle.
/// </summary>
public sealed class AnimationManager
{
    private readonly IFXLogger _logger;
    private readonly List<IFXAnimation> _animations = new();

    public AnimationManager(IFXLogger logger)
    {
        _logger = logger;
    }

    public void Register(IFXAnimation animation)
    {
        if (_animations.Any(existing => existing.Id.Equals(animation.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _animations.Add(animation);
    }

    public async Task PlayAsync(string animationId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var animation = Resolve(animationId);
        await animation.PlayAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Debug, $"Played animation {animation.Id}");
    }

    public async Task StopAsync(string animationId, FXEngineContext context, CancellationToken cancellationToken = default)
    {
        var animation = Resolve(animationId);
        await animation.StopAsync(context, cancellationToken);
        _logger.Log(FXLogLevel.Debug, $"Stopped animation {animation.Id}");
    }

    private IFXAnimation Resolve(string animationId)
    {
        var animation = _animations.FirstOrDefault(item => item.Id.Equals(animationId, StringComparison.OrdinalIgnoreCase));
        return animation ?? throw new InvalidOperationException($"Unknown animation '{animationId}'.");
    }
}

/// <summary>
/// Manages the persistent user profile and its settings.
/// </summary>
public sealed class ProfileManager
{
    private readonly FXEngineConfiguration _configuration;
    private readonly IFXLogger _logger;

    public ProfileManager(FXEngineConfiguration configuration, IFXLogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FXProfile> LoadAsync(CancellationToken cancellationToken = default)
    {
        var profilePath = GetProfilePath();
        if (!File.Exists(profilePath))
        {
            var profile = new FXProfile();
            await SaveAsync(profile, cancellationToken);
            return profile;
        }

        var json = await File.ReadAllTextAsync(profilePath, cancellationToken);
        var profileModel = System.Text.Json.JsonSerializer.Deserialize<FXProfile>(json);
        _logger.Log(FXLogLevel.Debug, $"Loaded profile from {profilePath}");
        return profileModel ?? new FXProfile();
    }

    public async Task SaveAsync(FXProfile profile, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(GetProfilePath())!);
        var json = System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(GetProfilePath(), json, cancellationToken);
    }

    private string GetProfilePath() => Path.Combine(_configuration.BaseDirectory, _configuration.ProfileDirectory, $"default{_configuration.ProfileFileExtension}");
}

/// <summary>
/// Creates and stores layouts for applications and widgets.
/// </summary>
public sealed class LayoutManager
{
    private readonly FXEngineConfiguration _configuration;
    private readonly IFXLogger _logger;

    public LayoutManager(FXEngineConfiguration configuration, IFXLogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FXLayout> CreateDefaultLayoutAsync(string name, CancellationToken cancellationToken = default)
    {
        var layout = new FXLayout { Name = name };
        await SaveAsync(layout, cancellationToken);
        return layout;
    }

    public async Task SaveAsync(FXLayout layout, CancellationToken cancellationToken = default)
    {
        var path = GetLayoutPath(layout.Name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(layout, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);
        _logger.Log(FXLogLevel.Debug, $"Saved layout {layout.Name}");
    }

    public async Task<FXLayout> LoadAsync(string name, CancellationToken cancellationToken = default)
    {
        var path = GetLayoutPath(name);
        if (!File.Exists(path))
        {
            return await CreateDefaultLayoutAsync(name, cancellationToken);
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<FXLayout>(json) ?? new FXLayout { Name = name };
    }

    private string GetLayoutPath(string name) => Path.Combine(_configuration.BaseDirectory, _configuration.LayoutDirectory, $"{name}.fxl");
}

/// <summary>
/// Manages application assets such as themes, icons, and fonts.
/// </summary>
public sealed class AssetManager
{
    private readonly FXEngineConfiguration _configuration;
    private readonly IFXLogger _logger;
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public AssetManager(FXEngineConfiguration configuration, IFXLogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GetAssetPath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        var basePath = Path.Combine(_configuration.BaseDirectory, relativePath);
        var assetPath = Path.Combine(_configuration.BaseDirectory, _configuration.AssetDirectory, relativePath);

        if (File.Exists(basePath))
        {
            return basePath;
        }

        if (File.Exists(assetPath))
        {
            return assetPath;
        }

        return assetPath;
    }

    public void EnsureAssetDirectory(string relativePath)
    {
        var fullPath = GetAssetPath(relativePath);
        Directory.CreateDirectory(fullPath);
        _logger.Log(FXLogLevel.Debug, $"Ensured asset directory {fullPath}");
    }

    public async Task<string?> LoadAssetAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(relativePath, out var cached))
        {
            return cached;
        }

        var fullPath = GetAssetPath(relativePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
        _cache[relativePath] = content;
        return content;
    }

    public bool IsCached(string relativePath) => _cache.ContainsKey(relativePath);
}

/// <summary>
/// Exposes the event bus through an engine-managed interface.
/// </summary>
public sealed class EventManager
{
    private readonly EventBus _eventBus;

    public EventManager(IFXLogger logger)
    {
        _eventBus = new EventBus(logger);
    }

    public EventManager(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public IDisposable Subscribe(string eventName, Func<FXEvent, Task> handler) => _eventBus.Subscribe(eventName, handler);

    public Task PublishAsync(FXEvent evt, CancellationToken cancellationToken = default) => _eventBus.PublishAsync(evt, cancellationToken);
}
