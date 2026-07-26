namespace XephyreFX.App.Config;

public enum WeatherApiProvider { OpenWeatherMap, WeatherApiCom, OpenMeteo }

public sealed class WeatherApiConfig
{
    public bool Enabled { get; set; }
    public WeatherApiProvider Provider { get; set; } = WeatherApiProvider.OpenWeatherMap;
    public string ApiKey { get; set; } = "";
    public string City { get; set; } = "";
}
