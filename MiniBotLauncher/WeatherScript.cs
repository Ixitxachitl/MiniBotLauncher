using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class WeatherScript
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<string> GetWeather(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return "Usage: !weather cityname";

        try
        {
            string url = $"https://wttr.in/{Uri.EscapeDataString(city)}?format=3";
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MiniBotLauncher/1.0");

            string weather = await httpClient.GetStringAsync(url);

            if (!string.IsNullOrWhiteSpace(weather))
            {
                return $"Weather for {weather}";
            }
            else
            {
                return $"Could not retrieve weather for {city}.";
            }
        }
        catch (Exception ex)
        {
            return $"Error getting weather: {ex.Message}";
        }
    }
}
