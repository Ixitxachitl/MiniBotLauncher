using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class AskAIScript
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string gptServerUrl = "http://localhost:4891/v1/chat/completions";

    public static async Task<string> GetResponse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "You need to provide a prompt after !askai!";

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
                return $"Error contacting AI: {response.StatusCode}";
            }

            string responseString = await response.Content.ReadAsStringAsync();
            JObject parsed = JObject.Parse(responseString);

            string reply = parsed["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrWhiteSpace(reply))
            {
                return "Sorry, no reply from AI.";
            }

            // Truncate reply if too long
            if (reply.Length > 450)
                reply = reply.Substring(0, 450) + "...";

            return reply.Trim();
        }
        catch (Exception ex)
        {
            return $"Error contacting AI: {ex.Message}";
        }
    }
}
