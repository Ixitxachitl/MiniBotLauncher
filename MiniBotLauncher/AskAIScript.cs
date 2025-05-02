using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class AskAIScript
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string gptServerUrl = "http://localhost:4891/v1/chat/completions";

    public static Func<string, Task> DebugLog = null;

    private static string modelName = "llama3-8b-instruct";
    private static int maxTokens = 130;
    private static string systemMessage = "";

    public static void SetConfig(string model, int tokens, string systemMsg)
    {
        modelName = string.IsNullOrWhiteSpace(model) ? "llama3-8b-instruct" : model;
        maxTokens = Math.Max(1, Math.Min(255, tokens));
        systemMessage = systemMsg ?? "";
    }

    public static async Task<string> GetResponse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            await TryLog("AskAIScript: Empty prompt received.");
            return "You need to provide a prompt after !askai!";
        }

        await TryLog($"AskAIScript: Sending prompt to GPT server: \"{prompt}\"");

        try
        {
            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(systemMessage))
            {
                messages.Add(new { role = "system", content = systemMessage });
            }

            messages.Add(new { role = "user", content = prompt });

            var payload = new
            {
                model = modelName,
                max_tokens = maxTokens,
                messages = messages
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(gptServerUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                await TryLog($"AskAIScript: GPT server returned status {response.StatusCode}.");
                return $"Error contacting AI: {response.StatusCode}";
            }

            string responseString = await response.Content.ReadAsStringAsync();
            await TryLog("AskAIScript: Received raw response from server.");

            JObject parsed = JObject.Parse(responseString);
            string reply = parsed["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(reply))
            {
                await TryLog("AskAIScript: Empty reply from GPT server.");
                return "Sorry, no reply from AI.";
            }

            const int twitchLimit = 490; // give buffer room for username/prefix
            if (reply.Length > twitchLimit)
            {
                await TryLog($"AskAIScript: Reply exceeds Twitch limit ({reply.Length} chars), truncating.");
                reply = reply.Substring(0, twitchLimit).TrimEnd() + "...";
            }

            await TryLog($"AskAIScript: Final reply: \"{reply.Trim()}\"");

            return reply.Trim();
        }
        catch (Exception ex)
        {
            await TryLog($"AskAIScript: Exception occurred - {ex.Message}");
            return $"Error contacting AI: {ex.Message}";
        }
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
