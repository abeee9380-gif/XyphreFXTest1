using System.Text.Json;

namespace XephyreFX.App.Config;

/// <summary>
/// Reads/writes <c>config.json</c> next to the app executable. If it's missing, a default one
/// gets created so there's always something to look at and edit. Malformed JSON never crashes
/// the app -- it just falls back to defaults and keeps running.
/// </summary>
public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public XephyreFXConfig Current { get; private set; } = new();

    public ConfigService(string? path = null)
    {
        _path = path ?? Path.Combine(AppContext.BaseDirectory, "config.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<XephyreFXConfig>(json, JsonOptions);
                if (loaded is not null)
                {
                    Current = loaded;
                    return;
                }
            }
        }
        catch
        {
            // Malformed config.json shouldn't crash the app -- fall back to defaults below.
        }

        Current = new XephyreFXConfig();
        Save();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(_path, json);
        }
        catch
        {
            // Best-effort -- a failed save (e.g. read-only folder) shouldn't crash the app.
        }
    }

    /// <summary>Wipes everything back to factory defaults and saves. This is the actual "start over" button -- Load() only re-reads whatever's currently on disk, which does nothing useful if disk already has your (unwanted) changes on it.</summary>
    public void ResetToDefaults()
    {
        Current = new XephyreFXConfig();
        Save();
    }
}
