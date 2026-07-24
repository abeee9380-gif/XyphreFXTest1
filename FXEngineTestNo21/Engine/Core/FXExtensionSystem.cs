using System.Collections.ObjectModel;
using System.Text.Json;
using FXEngine.SDK;

namespace FXEngine.Core;

/// <summary>
/// Represents a package discovered from disk and validated against the engine manifest format.
/// </summary>
public sealed class PackageDescriptor : IPackageDescriptor
{
    public PackageDescriptor(string manifestPath, FXManifest manifest)
    {
        ManifestPath = manifestPath;
        RootPath = Path.GetDirectoryName(manifestPath) ?? string.Empty;
        Id = manifest.Name;
        Name = manifest.Name;
        Version = manifest.Version;
        PackageType = manifest.PackageType;
        EntryPoint = manifest.EntryPoint;
        EngineVersion = manifest.EngineVersion;
        Dependencies = manifest.Dependencies ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string PackageType { get; }
    public string EntryPoint { get; }
    public string? EngineVersion { get; }
    public Dictionary<string, string> Dependencies { get; }
    public string ManifestPath { get; }
    public string RootPath { get; }
}

/// <summary>
/// Provides a registry for tracking package lifecycle state and dependencies.
/// </summary>
public sealed class PackageRegistry
{
    private readonly IFXLogger _logger;
    private readonly List<IPackageDescriptor> _installed = new();
    private readonly List<IPackageDescriptor> _loaded = new();
    private readonly List<IPackageDescriptor> _disabled = new();
    private readonly List<IPackageDescriptor> _failed = new();

    public PackageRegistry(IFXLogger logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<IPackageDescriptor> InstalledPackages => _installed.AsReadOnly();
    public IReadOnlyList<IPackageDescriptor> LoadedPackages => _loaded.AsReadOnly();
    public IReadOnlyList<IPackageDescriptor> DisabledPackages => _disabled.AsReadOnly();
    public IReadOnlyList<IPackageDescriptor> FailedPackages => _failed.AsReadOnly();

    public void RegisterInstalled(IPackageDescriptor package)
    {
        if (_installed.Any(item => item.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _installed.Add(package);
        _logger.Log(FXLogLevel.Information, $"Registered installed package {package.Id}");
    }

    public void MarkLoaded(IPackageDescriptor package)
    {
        if (!_loaded.Contains(package))
        {
            _loaded.Add(package);
        }

        _logger.Log(FXLogLevel.Information, $"Loaded package {package.Id}");
    }

    public void MarkDisabled(IPackageDescriptor package)
    {
        if (!_disabled.Contains(package))
        {
            _disabled.Add(package);
        }
    }

    public void MarkFailed(IPackageDescriptor package)
    {
        if (!_failed.Contains(package))
        {
            _failed.Add(package);
        }
    }

    public IEnumerable<IPackageDescriptor> Search(string? query = null, string? type = null, string? version = null, string? id = null)
    {
        return _installed.Where(package =>
            (string.IsNullOrWhiteSpace(query) || package.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || package.Id.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(type) || package.PackageType.Equals(type, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(version) || package.Version.Equals(version, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(id) || package.Id.Equals(id, StringComparison.OrdinalIgnoreCase)));
    }
}

/// <summary>
/// Discovers, validates, loads, reloads, unloads, and manages extension packages.
/// </summary>
public sealed class PackageLoader
{
    private static readonly string[] SupportedExtensions = { ".themefx", ".pluginfx", ".widgetfx", ".assetpackfx", ".soundpackfx", ".fontpackfx", ".animationfx" };
    private readonly FXEngineConfiguration _configuration;
    private readonly IFXLogger _logger;
    private readonly EventManager? _eventManager;

    public PackageLoader(FXEngineConfiguration configuration, IFXLogger logger, EventManager? eventManager = null)
    {
        _configuration = configuration;
        _logger = logger;
        _eventManager = eventManager;
    }

    public IEnumerable<IPackageDescriptor> DiscoverPackages(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            yield break;
        }

        foreach (var directory in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories).Where(static directory => !Path.GetFileName(directory).StartsWith('.')))
        {
            var directoryName = Path.GetFileName(directory);
            if (!SupportedExtensions.Any(extension => directoryName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var manifestPath = Path.Combine(directory, "manifest.fxmanifest");
            if (!File.Exists(manifestPath))
            {
                _logger.Log(FXLogLevel.Warning, $"Package directory missing manifest: {directory}");
                continue;
            }

            var validation = ValidateManifest(manifestPath);
            if (!validation.IsValid)
            {
                _logger.Log(FXLogLevel.Warning, $"Package manifest invalid: {manifestPath} ({string.Join(", ", validation.Errors)})");
                PublishEvent("PackageFailed", null, manifestPath);
                continue;
            }

            var descriptor = new PackageDescriptor(manifestPath, validation.Manifest!);
            PublishEvent("PackageDiscovered", descriptor);
            yield return descriptor;
        }
    }

    public PackageValidationResult ValidateManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return PackageValidationResult.Failure("Manifest file missing.");
        }

        try
        {
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<FXManifest>(manifestJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest is null)
            {
                return PackageValidationResult.Failure("Corrupted manifest.");
            }

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(manifest.Name))
            {
                errors.Add("Missing required field: Name.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Author))
            {
                errors.Add("Missing required field: Author.");
            }

            if (string.IsNullOrWhiteSpace(manifest.Version) || !IsValidVersion(manifest.Version))
            {
                errors.Add("Invalid version.");
            }

            if (string.IsNullOrWhiteSpace(manifest.EngineVersion) || !IsValidVersion(manifest.EngineVersion))
            {
                errors.Add("Invalid engine version.");
            }

            if (string.IsNullOrWhiteSpace(manifest.EntryPoint))
            {
                errors.Add("Missing entry point.");
            }

            if (string.IsNullOrWhiteSpace(manifest.PackageType))
            {
                errors.Add("Missing package type.");
            }

            if (errors.Count > 0)
            {
                return PackageValidationResult.Failure(errors);
            }

            return PackageValidationResult.Success(manifest);
        }
        catch (Exception ex)
        {
            _logger.Log(FXLogLevel.Error, $"Failed to parse manifest {manifestPath}", ex);
            return PackageValidationResult.Failure("Corrupted manifest.");
        }
    }

    public void LoadPackages(IEnumerable<IPackageDescriptor> packages, PackageRegistry registry)
    {
        var sorted = TopologicalSort(packages, registry).ToList();
        foreach (var package in sorted)
        {
            if (registry.LoadedPackages.Any(item => item.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            registry.RegisterInstalled(package);
            registry.MarkLoaded(package);
            PublishEvent("PackageLoaded", package);
            _logger.Log(FXLogLevel.Information, $"Loaded package {package.Id}");
        }
    }

    public void UnloadPackage(IPackageDescriptor package, PackageRegistry registry)
    {
        if (registry.LoadedPackages.Any(item => item.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase)))
        {
            registry.MarkDisabled(package);
            PublishEvent("PackageUnloaded", package);
            _logger.Log(FXLogLevel.Information, $"Unloaded package {package.Id}");
        }
    }

    public void ReloadPackage(IPackageDescriptor package, PackageRegistry registry)
    {
        UnloadPackage(package, registry);
        registry.MarkLoaded(package);
        PublishEvent("PackageReloaded", package);
        _logger.Log(FXLogLevel.Information, $"Reloaded package {package.Id}");
    }

    private IEnumerable<IPackageDescriptor> TopologicalSort(IEnumerable<IPackageDescriptor> packages, PackageRegistry registry)
    {
        var packageList = packages.ToList();
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<IPackageDescriptor>();

        foreach (var package in packageList)
        {
            Visit(package, packageList, registry, resolved, visiting, stack);
        }

        while (stack.Count > 0)
        {
            yield return stack.Pop();
        }
    }

    private void Visit(IPackageDescriptor package, List<IPackageDescriptor> packageList, PackageRegistry registry, HashSet<string> resolved, HashSet<string> visiting, Stack<IPackageDescriptor> stack)
    {
        if (resolved.Contains(package.Id))
        {
            return;
        }

        if (visiting.Contains(package.Id))
        {
            registry.MarkFailed(package);
            PublishEvent("PackageFailed", package, "Circular dependency detected");
            _logger.Log(FXLogLevel.Error, $"Circular dependency detected for {package.Id}");
            return;
        }

        visiting.Add(package.Id);
        foreach (var dependencyName in package.Dependencies.Keys)
        {
            var dependency = packageList.FirstOrDefault(item => item.Id.Equals(dependencyName, StringComparison.OrdinalIgnoreCase));
            if (dependency is null)
            {
                _logger.Log(FXLogLevel.Warning, $"Missing dependency {dependencyName} for package {package.Id}");
                continue;
            }

            Visit(dependency, packageList, registry, resolved, visiting, stack);
        }

        visiting.Remove(package.Id);
        resolved.Add(package.Id);
        stack.Push(package);
    }

    private static bool IsValidVersion(string version)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+(\.\d+){1,2}$");
    }

    private void PublishEvent(string eventName, IPackageDescriptor? package, string? message = null)
    {
        if (_eventManager is null)
        {
            return;
        }

        var payload = new Dictionary<string, object?>
        {
            ["Package"] = package,
            ["Message"] = message
        };

        _eventManager.PublishAsync(new FXEvent(eventName, payload)).GetAwaiter().GetResult();
    }
}

/// <summary>
/// Represents the result of validating a package manifest.
/// </summary>
public sealed class PackageValidationResult
{
    private PackageValidationResult(bool isValid, FXManifest? manifest, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Manifest = manifest;
        Errors = errors;
    }

    public bool IsValid { get; }
    public FXManifest? Manifest { get; }
    public IReadOnlyList<string> Errors { get; }

    public static PackageValidationResult Success(FXManifest manifest) => new(true, manifest, Array.Empty<string>());
    public static PackageValidationResult Failure(string error) => new(false, null, new[] { error });
    public static PackageValidationResult Failure(IEnumerable<string> errors) => new(false, null, errors.ToArray());
}
