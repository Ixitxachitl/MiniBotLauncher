using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public static class ClapThatBotScript
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly Random rng = new Random();

    public static Func<string, Task> DebugLog = null; // Optional external debug logger

    public static async Task<string> Process(string message, string nlpApiKey, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nlpApiKey))
            return null;

        if (username.Equals(botUsername, StringComparison.OrdinalIgnoreCase))
            return null; // Don't respond to own messages

        // 2% random trigger chance
        if (rng.NextDouble() > 0.02)
        {
            await TryLog($"ClapThatBot: Skipped message from {username} (no trigger).");
            return null;
        }

        await TryLog($"ClapThatBot: Triggered for message: \"{message}\"");

        try
        {
            var (adjective, noun, isPlural) = await FindAdjectiveNounPairAsync(message, nlpApiKey);

            if (string.IsNullOrEmpty(adjective) || string.IsNullOrEmpty(noun))
            {
                await TryLog("ClapThatBot: No valid adjective+noun pair found.");
                return null;
            }

            string article = isPlural ? "those" : "that";
            string response = $"I'd clap {article} {adjective.ToLower()} {noun.ToLower()}!";
            await TryLog($"ClapThatBot: Responding with: \"{response}\"");

            return response;
        }
        catch (Exception ex)
        {
            await TryLog($"ClapThatBot: Error: {ex.Message}");
            return null;
        }
    }

    private static async Task<(string, string, bool)> FindAdjectiveNounPairAsync(string text, string nlpApiKey)
    {
        try
        {
            var url = "https://api.nlpcloud.io/v1/en_core_web_lg/dependencies";

            var payload = new { text = text };
            var requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Token {nlpApiKey}");

            var response = await client.PostAsync(url, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                await TryLog($"ClapThatBot: NLP request failed: {response.StatusCode}");
                return (null, null, false);
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseContent);

            var words = json["words"] as JArray;
            if (words == null || words.Count < 2)
                return (null, null, false);

            for (int i = 0; i < words.Count - 1; i++)
            {
                var currentWord = words[i];
                var nextWord = words[i + 1];

                string currentTag = currentWord.Value<string>("tag") ?? "";
                string nextTag = nextWord.Value<string>("tag") ?? "";

                if (currentTag.StartsWith("JJ") && nextTag.StartsWith("NN"))
                {
                    string adjective = currentWord.Value<string>("text") ?? "";
                    string noun = nextWord.Value<string>("text") ?? "";
                    bool isPlural = nextTag == "NNS" || nextTag == "NNPS";
                    return (adjective, noun, isPlural);
                }
            }

            return (null, null, false);
        }
        catch (Exception ex)
        {
            await TryLog($"ClapThatBot: FindAdjectiveNounPair error: {ex.Message}");
            return (null, null, false);
        }
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
