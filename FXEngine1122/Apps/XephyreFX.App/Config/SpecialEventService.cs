using System.Text.Json;

namespace XephyreFX.App.Config;

/// <summary>
/// Loads every *.json file in the Events/ folder next to the app. Ships with a built-in
/// Valentine's Day entry so the feature keeps working even before that folder exists; any
/// file the user drops in with the same Id overrides the built-in one, and any other Id adds
/// a brand new event.
/// </summary>
public sealed class SpecialEventService
{
    private static readonly SpecialEventConfig BuiltInValentines = new()
    {
        Id = "valentines",
        Name = "Valentine's Day",
        Month = 2,
        Day = 14,
        TintColorHex = "#FF5A82",
        ParticleColorHex = "#FF5C82"
    };

    private readonly string _folder;

    public List<SpecialEventConfig> Events { get; private set; } = new();

    public SpecialEventService(string? folder = null)
    {
        _folder = folder ?? Path.Combine(AppContext.BaseDirectory, "Events");
        Load();
    }

    public void Load()
    {
        var byId = new Dictionary<string, SpecialEventConfig> { [BuiltInValentines.Id] = BuiltInValentines };

        try
        {
            Directory.CreateDirectory(_folder);
            foreach (var file in Directory.GetFiles(_folder, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var evt = JsonSerializer.Deserialize<SpecialEventConfig>(json);
                    if (evt is not null && !string.IsNullOrWhiteSpace(evt.Id))
                    {
                        byId[evt.Id] = evt;
                    }
                }
                catch
                {
                    // Skip a malformed event file rather than crashing the whole app.
                }
            }
        }
        catch
        {
            // Couldn't read/create the Events folder -- just run with the built-in default.
        }

        Events = byId.Values.ToList();
    }

    public SpecialEventConfig? GetActiveEvent(int month, int day) =>
        Events.FirstOrDefault(e => e.Month == month && e.Day == day);

    public SpecialEventConfig GetById(string id) =>
        Events.FirstOrDefault(e => e.Id == id) ?? BuiltInValentines;
}
