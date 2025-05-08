using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public static class WalkOnScript
{
    private static HashSet<string> walkedOnUsers = new();
    private static string lastStreamStart = "";
    private static Dictionary<string, string> userSoundMappings = new();

    public static bool Enabled = false;
    public static Func<string, Task> DebugLog = null;

    public static void SetSoundMappings(Dictionary<string, string> mappings)
    {
        userSoundMappings = mappings ?? new();
    }

    public static Dictionary<string, string> GetSoundMappings()
    {
        return userSoundMappings;
    }

    public static void SetLastKnownStreamStart(string isoTimestamp)
    {
        lastStreamStart = isoTimestamp ?? "";
    }

    public static string GetLastKnownStreamStart()
    {
        return lastStreamStart;
    }

    public static async Task<string> TryPlayWalkOn(string username, string channel, string clientId, string oauthToken)
    {
        if (!Enabled)
        {
            await TryLog("Walk-on is disabled.");
            return null;
        }

        if (userSoundMappings == null || !userSoundMappings.ContainsKey(username))
        {
            await TryLog($"❌ Walk-on ignored — no mapping for {username}");
            return null;
        }

        string currentStart = await GetStreamStartTime(channel, clientId, oauthToken);

        if (string.IsNullOrEmpty(currentStart))
        {
            await TryLog($"Stream is not live on {channel} — skipping walk-on.");
            return null;
        }

        if (currentStart != lastStreamStart)
        {
            walkedOnUsers.Clear();
            lastStreamStart = currentStart;
            await TryLog($"New stream detected on {channel}. Walk-on data reset.");
        }

        if (!walkedOnUsers.Contains(username))
        {
            walkedOnUsers.Add(username);
            string soundPath = userSoundMappings[username];
            AudioQueue.Enqueue(soundPath);
            await TryLog($"✅ Walk-on triggered for {username}: {soundPath}");
        }
        else
        {
            await TryLog($"⏭️ Walk-on skipped — {username} already triggered.");
        }

        return lastStreamStart;
    }

    private static async Task<string> GetStreamStartTime(string channel, string clientId, string oauth)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", clientId);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {oauth}");

            string url = $"https://api.twitch.tv/helix/streams?user_login={channel}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return "";

            string json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            if (data.GetArrayLength() == 0)
                return ""; // not live

            return data[0].GetProperty("started_at").GetString();
        }
        catch (Exception ex)
        {
            await TryLog($"Error fetching stream start: {ex.Message}");
            return "";
        }
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
