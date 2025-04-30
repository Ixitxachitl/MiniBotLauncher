using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenNLP.Tools.PosTagger;
using OpenNLP.Tools.Tokenize;

public static class ButtsBotScript
{
    private static readonly Random rng = new Random();

    public static Func<string, Task> DebugLog = null;

    public static async Task<string> Process(string message, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
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
            var (candidates, tagMap) = AnalyzeTextLocal(message);

            if (candidates == null || candidates.Count == 0)
            {
                await TryLog("ButtsBot: No nouns or adjectives found.");
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

    private static (List<string>, Dictionary<string, string>) AnalyzeTextLocal(string text)
    {
        string modelPath = ExtractModelToTempFile("en-pos-maxent.bin");
        var tokenizer = new EnglishRuleBasedTokenizer(false);
        var tagger = new EnglishMaximumEntropyPosTagger(modelPath);

        var tokens = tokenizer.Tokenize(text);
        var tags = tagger.Tag(tokens);

        var candidates = new List<string>();
        var tagMap = new Dictionary<string, string>();

        for (int i = 0; i < tokens.Length; i++)
        {
            string tag = tags[i];
            if (tag.StartsWith("NN") || tag.StartsWith("JJ"))
            {
                candidates.Add(tokens[i]);
                tagMap[tokens[i]] = tag;
            }
        }

        return (candidates, tagMap);
    }

    private static string ReplaceAllOccurrences(string source, string find, string replace)
    {
        if (string.IsNullOrEmpty(find))
            return source;

        return Regex.Replace(
            source,
            @"\b" + Regex.Escape(find) + @"\b",
            replace,
            RegexOptions.IgnoreCase
        );
    }

    private static string ExtractModelToTempFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(resourceName));

        if (fullName == null)
            throw new Exception($"Embedded resource {resourceName} not found.");

        string tempPath = Path.GetTempFileName();
        using var input = assembly.GetManifestResourceStream(fullName);
        using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
        input.CopyTo(output);
        return tempPath;
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
