using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class AskAIScript
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string gptServerUrl = "http://localhost:4891/v1/chat/completions";

    public static Func<string, Task> DebugLog = null;

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
            var payload = new
            {
                model = "llama3-8b-instruct",
                max_tokens = 130,
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful Twitch Chatbot. Keep responses short and concise." },
                    new { role = "user", content = prompt }
                }
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

            if (reply.Length > 450)
            {
                await TryLog("AskAIScript: Truncating long reply.");
                reply = reply.Substring(0, 450) + "...";
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
