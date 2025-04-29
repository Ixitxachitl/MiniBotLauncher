using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public static class ButtsBotScript
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly Random rng = new Random();

    public static Func<string, Task> DebugLog = null;

    public static async Task<string> Process(string message, string nlpApiKey, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(nlpApiKey))
            return null;

        if (message.StartsWith("!", StringComparison.Ordinal))
            return null;

        if (rng.NextDouble() > 0.02)
        {
            await TryLog($"ButtsBot: Skipped message from {username} (no trigger).");
            return null;
        }

        await TryLog($"ButtsBot: Attempting to replace a word in message: \"{message}\"");

        try
        {
            var (candidates, tagMap) = await AnalyzeTextAsync(message, nlpApiKey);

            if (candidates == null || candidates.Count == 0)
            {
                await TryLog($"ButtsBot: No nouns or adjectives found.");
                return null;
            }

            string wordToReplace = candidates[rng.Next(candidates.Count)];
            string tag = tagMap[wordToReplace];

            await TryLog($"ButtsBot: Selected \"{wordToReplace}\" (tag {tag})");

            string replacement = (tag == "NNS" || tag == "NNPS") ? "butts" : "butt";

            string funnyText = ReplaceAllOccurrences(message, wordToReplace, replacement);
            await TryLog($"ButtsBot: Final funny message: \"{funnyText}\"");

            return funnyText;
        }
        catch (Exception ex)
        {
            await TryLog($"ButtsBot: Error - {ex.Message}");
            return null;
        }
    }

    private static async Task<(List<string>, Dictionary<string, string>)> AnalyzeTextAsync(string text, string nlpApiKey)
    {
        var url = "https://api.nlpcloud.io/v1/en_core_web_lg/dependencies";

        var payload = new { text = text };
        var requestContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Token {nlpApiKey}");

        var response = await client.PostAsync(url, requestContent);

        if (!response.IsSuccessStatusCode)
            return (null, null);

        string responseContent = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(responseContent);

        var candidates = new List<string>();
        var tagMap = new Dictionary<string, string>();

        var words = json["words"] as JArray;
        if (words != null)
        {
            foreach (var wordObj in words)
            {
                string word = wordObj.Value<string>("text") ?? "";
                string tag = wordObj.Value<string>("tag") ?? "";

                if (tag.StartsWith("NN") || tag.StartsWith("JJ")) // nouns and adjectives
                {
                    candidates.Add(word);
                    tagMap[word] = tag;
                }
            }
        }

        return (candidates, tagMap);
    }

    private static string ReplaceAllOccurrences(string source, string find, string replace)
    {
        if (string.IsNullOrEmpty(find))
            return source;

        return System.Text.RegularExpressions.Regex.Replace(
            source,
            @"\b" + System.Text.RegularExpressions.Regex.Escape(find) + @"\b",
            replace,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
