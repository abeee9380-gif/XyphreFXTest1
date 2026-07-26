using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Tests;

public class RendererRuntimeTests
{
    [Fact]
    public async Task RendererManager_RendersLayersAndOperations()
    {
        var configuration = new FXEngineConfiguration { BaseDirectory = Path.GetTempPath() };
        var logger = new TestLogger();
        var context = new FXEngineContext(configuration, logger);
        var renderer = new TestRenderer();
        var rendererManager = new RendererManager(logger);
        rendererManager.Register(renderer);

        await rendererManager.InitializeAsync(context);

        var surface = new FXRenderSurface("main", 800, 600);
        surface.Layers.Add(new FXRenderLayer("hud")
        {
            Operations =
            [
                new FXRenderOperation("text", "Hello")
            ]
        });

        await rendererManager.RenderAsync(context, surface);

        Assert.True(renderer.Initialized);
        Assert.True(renderer.Rendered);
        Assert.Equal(1, surface.Layers.Count);
    }

    [Fact]
    public async Task AssetManager_CachesAndResolvesResources()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "fxengine-assets", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        await File.WriteAllTextAsync(Path.Combine(tempDirectory, "sample.json"), "{\"title\":\"ok\"}");

        var configuration = new FXEngineConfiguration { BaseDirectory = tempDirectory };
        var logger = new TestLogger();
        var assetManager = new AssetManager(configuration, logger);

        var first = await assetManager.LoadAssetAsync("sample.json");
        var second = await assetManager.LoadAssetAsync("sample.json");

        Assert.NotNull(first);
        Assert.Equal(first, second);
        Assert.True(assetManager.IsCached("sample.json"));
    }

    [Fact]
    public async Task AssetManager_LoadsFromConfiguredAssetDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "fxengine-asset-dir", Guid.NewGuid().ToString("N"));
        var assetsDirectory = Path.Combine(tempDirectory, "Assets");
        Directory.CreateDirectory(assetsDirectory);
        await File.WriteAllTextAsync(Path.Combine(assetsDirectory, "theme.json"), "{\"theme\":\"dark\"}");

        var configuration = new FXEngineConfiguration { BaseDirectory = tempDirectory, AssetDirectory = "Assets" };
        var logger = new TestLogger();
        var assetManager = new AssetManager(configuration, logger);

        var content = await assetManager.LoadAssetAsync("theme.json");

        Assert.NotNull(content);
        Assert.Contains("dark", content);
    }

    private sealed class TestLogger : IFXLogger
    {
        public void Log(FXLogLevel level, string message, Exception? exception = null) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
        public void Debug(string message) { }
    }

    private sealed class TestRenderer : IFXRenderer
    {
        public string Id => "test.renderer";
        public string Name => "Test Renderer";
        public string Version => "1.0.0";
        public bool Initialized { get; private set; }
        public bool Rendered { get; private set; }

        public Task InitializeAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return Task.CompletedTask;
        }

        public Task RenderAsync(FXEngineContext context, CancellationToken cancellationToken = default)
        {
            Rendered = true;
            return Task.CompletedTask;
        }
    }
}
