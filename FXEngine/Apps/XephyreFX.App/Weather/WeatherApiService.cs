using System.Net.Http;
using System.Text.Json;
using XephyreFX.App.Config;
using XephyreFX.App.Sim;

namespace XephyreFX.App.Weather;

public sealed class WeatherApiService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public async Task<(WeatherCondition condition, double tempC)?> FetchAsync(WeatherApiProvider provider, string apiKey, string city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        if (provider != WeatherApiProvider.OpenMeteo && string.IsNullOrWhiteSpace(apiKey)) return null;

        try
        {
            return provider switch
            {
                WeatherApiProvider.WeatherApiCom => await FetchWeatherApiComAsync(apiKey, city),
                WeatherApiProvider.OpenMeteo => await FetchOpenMeteoAsync(city),
                _ => await FetchOpenWeatherMapAsync(apiKey, city)
            };
        }
        catch
        {
            return null; // bad key/city/network -- caller just keeps whatever it had
        }
    }

    private static async Task<(WeatherCondition, double)?> FetchOpenWeatherMapAsync(string apiKey, string city)
    {
        string url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={apiKey}&units=metric";
        using var resp = await Http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        double temp = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
        string main = doc.RootElement.GetProperty("weather")[0].GetProperty("main").GetString() ?? "";

        var condition = main switch
        {
            "Thunderstorm" => WeatherCondition.Thunderstorm,
            "Rain" or "Drizzle" => WeatherCondition.Rain,
            "Clouds" => WeatherCondition.Cloudy,
            _ => WeatherCondition.Clear
        };
        return (condition, temp);
    }

    private static async Task<(WeatherCondition, double)?> FetchWeatherApiComAsync(string apiKey, string city)
    {
        string url = $"https://api.weatherapi.com/v1/current.json?key={apiKey}&q={Uri.EscapeDataString(city)}";
        using var resp = await Http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var current = doc.RootElement.GetProperty("current");
        double temp = current.GetProperty("temp_c").GetDouble();
        int code = current.GetProperty("condition").GetProperty("code").GetInt32();

        // WeatherAPI.com condition codes: 1087/1273-1282 = thunder, 1063-1201/1240-1252 = rain-ish, 1003-1030 = cloud-ish, else clear.
        var condition = code switch
        {
            1087 or (>= 1273 and <= 1282) => WeatherCondition.Thunderstorm,
            (>= 1063 and <= 1201) or (>= 1240 and <= 1252) => WeatherCondition.Rain,
            >= 1003 and <= 1030 => WeatherCondition.Cloudy,
            _ => WeatherCondition.Clear
        };
        return (condition, temp);
    }

    private static async Task<(WeatherCondition, double)?> FetchOpenMeteoAsync(string city)
    {
        // Free, no API key -- geocode the city name to lat/lon first, then fetch current weather.
        string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1";
        using var geoResp = await Http.GetAsync(geoUrl);
        if (!geoResp.IsSuccessStatusCode) return null;

        using var geoDoc = JsonDocument.Parse(await geoResp.Content.ReadAsStringAsync());
        if (!geoDoc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return null;

        double lat = results[0].GetProperty("latitude").GetDouble();
        double lon = results[0].GetProperty("longitude").GetDouble();

        string wxUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";
        using var wxResp = await Http.GetAsync(wxUrl);
        if (!wxResp.IsSuccessStatusCode) return null;

        using var wxDoc = JsonDocument.Parse(await wxResp.Content.ReadAsStringAsync());
        var cw = wxDoc.RootElement.GetProperty("current_weather");
        double temp = cw.GetProperty("temperature").GetDouble();
        int code = cw.GetProperty("weathercode").GetInt32();

        // WMO weather codes.
        var condition = code switch
        {
            >= 95 => WeatherCondition.Thunderstorm,
            (>= 51 and <= 67) or (>= 80 and <= 82) => WeatherCondition.Rain,
            >= 1 and <= 48 => WeatherCondition.Cloudy,
            _ => WeatherCondition.Clear
        };
        return (condition, temp);
    }
}
