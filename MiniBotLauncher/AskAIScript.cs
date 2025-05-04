using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class AskAIScript
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static string gptServerUrl = "http://localhost:4891/v1/chat/completions";
    public static void SetServer(string address, int port)
    {
        gptServerUrl = $"{address}:{port}/v1/chat/completions";
    }

    public static Func<string, Task> DebugLog = null;

    private static string modelName = "Llama 3 8B Instruct";
    private static int maxTokens = 50;
    private static string systemMessage = "";

    public static void SetConfig(string model, int tokens, string systemMsg, string serverAddress, int port)
    {
        modelName = string.IsNullOrWhiteSpace(model) ? "Llama 3 8B Instruct" : model;
        maxTokens = Math.Max(1, Math.Min(255, tokens));
        systemMessage = systemMsg ?? "";
        gptServerUrl = $"{serverAddress}:{port}/v1/chat/completions";
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

            return CleanModelOutput(reply.Trim());
        }
        catch (Exception ex)
        {
            await TryLog($"AskAIScript: Exception occurred - {ex.Message}");
            return $"Error contacting AI: {ex.Message}";
        }
    }

    private static string CleanModelOutput(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return response;

        // Trim known role-like labels if they appear after punctuation at the end
        return System.Text.RegularExpressions.Regex.Replace(
            response,
            @"[-–—]\s*(tutor|assistant|user|response|system|bot|ai|helper|model|guide|coach|mentor|responder)[:\s]*$",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        ).Trim();
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
