using System.Text.Json;
using FXEngine.SDK;

namespace FXEngine.Core;

/// <summary>
/// Implements the engine-wide logger that writes entries to disk under the configured logs directory.
/// </summary>
public sealed class FXLogger : IFXLogger
{
    private readonly FXEngineConfiguration _configuration;

    public FXLogger(FXEngineConfiguration configuration)
    {
        _configuration = configuration;
        Directory.CreateDirectory(GetLogsDirectory());
    }

    /// <inheritdoc />
    public void Log(FXLogLevel level, string message, Exception? exception = null)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        var fileName = $"fxengine-{DateTimeOffset.UtcNow:yyyy-MM-dd}.log";
        var line = $"[{timestamp}] [{level}] {message}";
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        File.AppendAllText(Path.Combine(GetLogsDirectory(), fileName), line + Environment.NewLine);
    }

    private string GetLogsDirectory() => Path.Combine(_configuration.BaseDirectory, _configuration.LogsDirectory);
}

/// <summary>
/// Provides persistent, JSON-backed settings storage for the engine.
/// </summary>
public sealed class SettingsManager
{
    private readonly FXEngineConfiguration _configuration;
    private readonly IFXLogger _logger;
    private readonly Dictionary<string, Dictionary<string, object?>> _values = new(StringComparer.OrdinalIgnoreCase);

    public SettingsManager(FXEngineConfiguration configuration, IFXLogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(json);
        _values.Clear();
        if (data is not null)
        {
            foreach (var pair in data)
            {
                _values[pair.Key] = new Dictionary<string, object?>(pair.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        _logger.Log(FXLogLevel.Debug, $"Loaded settings from {path}");
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var path = GetSettingsPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(_values, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);
        _logger.Log(FXLogLevel.Debug, $"Saved settings to {path}");
    }

    public Task SetValueAsync<T>(string section, string key, T value, CancellationToken cancellationToken = default)
    {
        if (!_values.ContainsKey(section))
        {
            _values[section] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        _values[section][key] = value;
        _logger.Log(FXLogLevel.Debug, $"Updated setting {section}:{key}");
        return Task.CompletedTask;
    }

    public Task<T?> GetValueAsync<T>(string section, string key, CancellationToken cancellationToken = default)
    {
        if (_values.TryGetValue(section, out var sectionValues) && sectionValues.TryGetValue(key, out var value))
        {
            if (value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            if (value is JsonElement element)
            {
                return Task.FromResult(ConvertJsonElement<T>(element));
            }

            if (typeof(T) == typeof(string))
            {
                return Task.FromResult((T?)(object?)value?.ToString());
            }

            if (value is IConvertible)
            {
                return Task.FromResult((T?)Convert.ChangeType(value, typeof(T)));
            }
        }

        return Task.FromResult<T?>(default);
    }

    private static T? ConvertJsonElement<T>(JsonElement element)
    {
        if (typeof(T) == typeof(string))
        {
            return (T?)(object?)element.GetString();
        }

        if (typeof(T) == typeof(int))
        {
            return (T?)(object?)element.GetInt32();
        }

        if (typeof(T) == typeof(bool))
        {
            return (T?)(object?)element.GetBoolean();
        }

        if (typeof(T) == typeof(double))
        {
            return (T?)(object?)element.GetDouble();
        }

        if (typeof(T) == typeof(Guid))
        {
            return (T?)(object?)element.GetGuid();
        }

        return (T?)(object?)element.ToString();
    }

    private string GetSettingsPath() => Path.Combine(_configuration.BaseDirectory, _configuration.SettingsFileName);
}

/// <summary>
/// Implements an in-process event bus for engine-wide notifications.
/// </summary>
public sealed class EventBus
{
    private readonly IFXLogger _logger;
    private readonly Dictionary<string, List<Func<FXEvent, Task>>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    public EventBus(IFXLogger logger)
    {
        _logger = logger;
    }

    public IDisposable Subscribe(string eventName, Func<FXEvent, Task> handler)
    {
        if (!_subscribers.TryGetValue(eventName, out var handlers))
        {
            handlers = new List<Func<FXEvent, Task>>();
            _subscribers[eventName] = handlers;
        }

        handlers.Add(handler);
        return new Subscription(this, eventName, handler);
    }

    public async Task PublishAsync(FXEvent evt, CancellationToken cancellationToken = default)
    {
        if (_subscribers.TryGetValue(evt.Name, out var handlers))
        {
            foreach (var handler in handlers.ToArray())
            {
                await handler(evt);
            }
        }

        _logger.Log(FXLogLevel.Debug, $"Published event {evt.Name}");
    }

    private sealed class Subscription : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly string _eventName;
        private readonly Func<FXEvent, Task> _handler;
        private bool _disposed;

        public Subscription(EventBus eventBus, string eventName, Func<FXEvent, Task> handler)
        {
            _eventBus = eventBus;
            _eventName = eventName;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _eventBus._subscribers.TryGetValue(_eventName, out var handlers);
            if (handlers is not null)
            {
                handlers.Remove(_handler);
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Maintains the collection of applications registered with the engine.
/// </summary>
public sealed class ApplicationManager
{
    private readonly IFXLogger _logger;
    private readonly EventBus _eventBus;
    private readonly List<IFXApplication> _applications = new();

    public ApplicationManager(IFXLogger logger, EventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public void Register(IFXApplication application)
    {
        if (_applications.Any(app => app.Id.Equals(application.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Application '{application.Id}' is already registered.");
        }

        _applications.Add(application);
        _logger.Log(FXLogLevel.Information, $"Registered application {application.Id}");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var application in _applications)
        {
            await application.InitializeAsync(new FXEngineContext(new FXEngineConfiguration(), _logger), cancellationToken);
        }

        await _eventBus.PublishAsync(new FXEvent("engine.applications.initialized"), cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var application in _applications)
        {
            await application.StartAsync(new FXEngineContext(new FXEngineConfiguration(), _logger), cancellationToken);
        }

        await _eventBus.PublishAsync(new FXEvent("engine.applications.started"), cancellationToken);
    }
}
