using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OpenNLP.Tools.PosTagger;
using OpenNLP.Tools.Tokenize;

public static class ClapThatBotScript
{
    private static readonly Random rng = new Random();
    private static int replyChancePercent = 2;

    public static void SetReplyChance(int percent)
    {
        replyChancePercent = Math.Clamp(percent, 1, 100);
    }

    public static Func<string, Task> DebugLog = null;

    public static async Task<string> Process(string message, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
            return null;

        if (message.TrimStart().StartsWith("!", StringComparison.Ordinal))
            return null;

        int roll = rng.Next(100);
        if (roll >= replyChancePercent)
        {
            await TryLog($"ClapThatBot: Skipped message from {username} (roll {roll} ≥ {replyChancePercent}).");
            return null;
        }

        await TryLog($"ClapThatBot: Triggered for message: \"{message}\"");

        try
        {
            var (adjective, noun, isPlural) = FindAdjectiveNounPairLocal(message);

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

    private static (string, string, bool) FindAdjectiveNounPairLocal(string text)
    {
        string modelPath = ExtractModelToTempFile("EnglishPOS.nbin");
        DebugLog?.Invoke($"ClapThatBot: Loading POS model from: {modelPath}");

        var tokenizer = new EnglishRuleBasedTokenizer(false);
        var tagger = new EnglishMaximumEntropyPosTagger(modelPath);

        var tokens = tokenizer.Tokenize(text);
        var tags = tagger.Tag(tokens);

        for (int i = 0; i < tokens.Length - 1; i++)
        {
            string tag1 = tags[i];
            string tag2 = tags[i + 1];

            // Prevent identical consecutive tokens from being adjective-noun pairs
            if (tag1.StartsWith("JJ") && tag2.StartsWith("NN") && tokens[i] != tokens[i + 1])
            {
                bool isPlural = tag2 == "NNS" || tag2 == "NNPS";
                return (tokens[i], tokens[i + 1], isPlural);
            }
        }

        return (null, null, false);
    }

    private static string ExtractModelToTempFile(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

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
