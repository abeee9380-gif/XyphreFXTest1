namespace FXEngine.Core;

/// <summary>
/// Represents the summary information generated at the end of engine startup.
/// </summary>
public sealed class FXBootReport
{
    /// <summary>
    /// Gets or sets the engine version.
    /// </summary>
    public string EngineVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets startup duration.
    /// </summary>
    public TimeSpan StartupTime { get; set; }

    /// <summary>
    /// Gets or sets the count of loaded themes.
    /// </summary>
    public int LoadedThemes { get; set; }

    /// <summary>
    /// Gets or sets the count of loaded plugins.
    /// </summary>
    public int LoadedPlugins { get; set; }

    /// <summary>
    /// Gets or sets the count of loaded packages.
    /// </summary>
    public int LoadedPackages { get; set; }

    /// <summary>
    /// Gets or sets the count of loaded applications.
    /// </summary>
    public int LoadedApplications { get; set; }

    /// <summary>
    /// Gets or sets the warnings captured during startup.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the errors captured during startup.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
