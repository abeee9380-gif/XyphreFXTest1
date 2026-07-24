using FXEngine.Core;
using FXEngine.SDK;

namespace FXEngine.Host;

/// <summary>
/// Entry-point host for the FX Engine platform.
/// </summary>
public static class Host
{
    public static async Task<int> RunAsync(string[] args)
    {
        var configuration = new FXEngineConfiguration
        {
            BaseDirectory = AppContext.BaseDirectory
        };

        var logger = new FXLogger(configuration);
        var engine = new EngineManager(configuration, logger);

        try
        {
            var context = await engine.StartAsync();
            logger.Log(FXLogLevel.Information, $"Engine context ready with {context.State.Count} state entries");
            return 0;
        }
        catch (Exception ex)
        {
            logger.Log(FXLogLevel.Critical, "FX Engine failed to start", ex);
            return 1;
        }
    }
}
