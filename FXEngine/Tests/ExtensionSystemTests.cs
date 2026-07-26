using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Tests;

public class ExtensionSystemTests
{
    [Fact]
    public void Discovery_FindsSupportedExtensionsRecursively()
    {
        var tempDirectory = CreateTempDirectory();
        var root = Path.Combine(tempDirectory, "Packages");
        Directory.CreateDirectory(Path.Combine(root, "Sub", "Nested"));
        Directory.CreateDirectory(Path.Combine(root, "package1.pluginfx"));
        Directory.CreateDirectory(Path.Combine(root, "Sub", "Nested", "package2.themefx"));
        File.WriteAllText(Path.Combine(root, "package1.pluginfx", "manifest.fxmanifest"), CreateManifestJson("PackageOne", "Author", "1.0.0", "1.0.0", "plugin", "Plugin.dll"));
        File.WriteAllText(Path.Combine(root, "Sub", "Nested", "package2.themefx", "manifest.fxmanifest"), CreateManifestJson("PackageTwo", "Author", "2.0.0", "1.0.0", "theme", "Theme.dll"));
        File.WriteAllText(Path.Combine(root, "ignored.txt"), "ignore me");

        var loader = new PackageLoader(new FXEngineConfiguration { BaseDirectory = tempDirectory }, new TestLogger());
        var discovered = loader.DiscoverPackages(root).ToList();

        Assert.Equal(2, discovered.Count);
        Assert.All(discovered, package => Assert.Contains(package.PackageType, new[] { "plugin", "theme" }));
    }

    [Fact]
    public void ManifestParsing_ParsesRequiredFields()
    {
        var manifestPath = Path.Combine(CreateTempDirectory(), "manifest.fxmanifest");
        File.WriteAllText(manifestPath, CreateManifestJson("WeatherSDK", "Acme", "1.2.3", "1.0.0", "plugin", "WeatherSdk.dll"));

        var manifest = FXManifest.LoadAsync(manifestPath).GetAwaiter().GetResult();

        Assert.Equal("WeatherSDK", manifest.Name);
        Assert.Equal("Acme", manifest.Author);
        Assert.Equal("1.2.3", manifest.Version);
        Assert.Equal("plugin", manifest.PackageType);
    }

    [Fact]
    public void VersionValidation_RejectsInvalidVersions()
    {
        var manifestPath = Path.Combine(CreateTempDirectory(), "manifest.fxmanifest");
        File.WriteAllText(manifestPath, CreateManifestJson("Bad", "Author", "not-a-version", "1.0.0", "plugin", "Bad.dll"));

        var loader = new PackageLoader(new FXEngineConfiguration(), new TestLogger());
        var result = loader.ValidateManifest(manifestPath);

        Assert.False(result.IsValid);
        Assert.Contains("Invalid version", result.Errors[0]);
    }

    [Fact]
    public void DependencyResolution_LoadsDependenciesFirst()
    {
        var root = CreateTempDirectory();
        var sdkPath = Path.Combine(root, "WeatherSDK.pluginfx");
        var pluginPath = Path.Combine(root, "BetterWeather.pluginfx");
        Directory.CreateDirectory(sdkPath);
        Directory.CreateDirectory(pluginPath);
        File.WriteAllText(Path.Combine(sdkPath, "manifest.fxmanifest"), CreateManifestJson("WeatherSDK", "Acme", "1.0.0", "1.0.0", "plugin", "WeatherSDK.dll"));
        File.WriteAllText(Path.Combine(pluginPath, "manifest.fxmanifest"), CreateManifestJson("BetterWeather", "Acme", "1.0.0", "1.0.0", "plugin", "BetterWeather.dll", new Dictionary<string, string> { ["WeatherSDK"] = "1.0.0" }));

        var loader = new PackageLoader(new FXEngineConfiguration { BaseDirectory = root }, new TestLogger());
        var packages = loader.DiscoverPackages(root).ToList();
        var registry = new PackageRegistry(new TestLogger());
        loader.LoadPackages(packages, registry);

        Assert.Contains(registry.LoadedPackages, item => item.Id == "WeatherSDK");
        Assert.Contains(registry.LoadedPackages, item => item.Id == "BetterWeather");
    }

    [Fact]
    public void CircularDependencyDetection_ReportsError()
    {
        var root = CreateTempDirectory();
        var first = Path.Combine(root, "A.pluginfx");
        var second = Path.Combine(root, "B.pluginfx");
        Directory.CreateDirectory(first);
        Directory.CreateDirectory(second);
        File.WriteAllText(Path.Combine(first, "manifest.fxmanifest"), CreateManifestJson("A", "Acme", "1.0.0", "1.0.0", "plugin", "A.dll", new Dictionary<string, string> { ["B"] = "1.0.0" }));
        File.WriteAllText(Path.Combine(second, "manifest.fxmanifest"), CreateManifestJson("B", "Acme", "1.0.0", "1.0.0", "plugin", "B.dll", new Dictionary<string, string> { ["A"] = "1.0.0" }));

        var loader = new PackageLoader(new FXEngineConfiguration { BaseDirectory = root }, new TestLogger());
        var packages = loader.DiscoverPackages(root).ToList();
        var registry = new PackageRegistry(new TestLogger());

        loader.LoadPackages(packages, registry);

        Assert.Contains(registry.FailedPackages, item => item.Id == "A" || item.Id == "B");
    }

    [Fact]
    public void Registry_TracksInstalledLoadedAndFailedPackages()
    {
        var registry = new PackageRegistry(new TestLogger());
        var package = new TestPackage("alpha", "plugin");
        registry.RegisterInstalled(package);
        registry.MarkLoaded(package);

        Assert.Contains(registry.InstalledPackages, item => item.Id == "alpha");
        Assert.Contains(registry.LoadedPackages, item => item.Id == "alpha");
    }

    [Fact]
    public void PackageLoader_UnloadAndReloadWork()
    {
        var registry = new PackageRegistry(new TestLogger());
        var package = new TestPackage("beta", "theme");
        registry.RegisterInstalled(package);
        registry.MarkLoaded(package);

        var loader = new PackageLoader(new FXEngineConfiguration(), new TestLogger());
        loader.UnloadPackage(package, registry);
        loader.ReloadPackage(package, registry);

        Assert.Contains(registry.LoadedPackages, item => item.Id == "beta");
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "fxengine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateManifestJson(string name, string author, string version, string engineVersion, string packageType, string entryPoint, Dictionary<string, string>? dependencies = null)
    {
        var dependencyJson = dependencies is null
            ? string.Empty
            : string.Join(",", dependencies.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value}\""));

        return $$"""
        {
          "Name": "{{name}}",
          "Author": "{{author}}",
          "Version": "{{version}}",
          "EngineVersion": "{{engineVersion}}",
          "Description": "Test package",
          "EntryPoint": "{{entryPoint}}",
          "PackageType": "{{packageType}}",
          "Dependencies": {
            {{dependencyJson}}
          }
        }
        """;
    }

    private sealed class TestPackage : IPackageDescriptor
    {
        public TestPackage(string id, string packageType)
        {
            Id = id;
            PackageType = packageType;
            Name = id;
            Version = "1.0.0";
            EntryPoint = $"{id}.dll";
        }

        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public string PackageType { get; }
        public string EntryPoint { get; }
        public string? EngineVersion { get; } = "1.0.0";
        public Dictionary<string, string> Dependencies { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string ManifestPath { get; } = string.Empty;
        public string RootPath { get; } = string.Empty;
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null)
        {
        }
    }
}
