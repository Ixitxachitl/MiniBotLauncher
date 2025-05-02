using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class WeatherScript
{
    private static readonly HttpClient httpClient = new HttpClient();

    public static Func<string, Task> DebugLog = null;

    private static string formatString = "2";

    public static void SetFormat(string format)
    {
        formatString = string.IsNullOrWhiteSpace(format) ? "2" : format;
    }

    public static async Task<string> GetWeather(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            await TryLog("WeatherScript: No city provided.");
            return "Usage: !weather cityname";
        }

        try
        {
            string url = $"https://wttr.in/{Uri.EscapeDataString(city)}?format={Uri.EscapeDataString(formatString)}";
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MiniBotLauncher/1.0");

            await TryLog($"WeatherScript: Requesting weather for {city}");

            string weather = await httpClient.GetStringAsync(url);

            if (!string.IsNullOrWhiteSpace(weather))
            {
                await TryLog($"WeatherScript: Received response: {weather}");
                return $"Weather for {city}: {weather}";
            }
            else
            {
                await TryLog("WeatherScript: Empty response from server.");
                return $"Could not retrieve weather for {city}.";
            }
        }
        catch (Exception ex)
        {
            await TryLog($"WeatherScript: Error getting weather - {ex.Message}");
            return $"Error getting weather: {ex.Message}";
        }
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
