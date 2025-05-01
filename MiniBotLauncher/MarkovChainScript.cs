using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class MarkovChainScript
{
    public static Func<string, Task> DebugLog = null;
    private static Dictionary<string, List<string>> transitions = new Dictionary<string, List<string>>();
    private static int messageCounter = 0;
    private static Random rng = new Random();
    private static readonly string saveFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MiniBot",
        "markov_brain.json"
    );

    public static string LearnAndMaybeRespond(string message, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
            return null;

        if (message.StartsWith("!", StringComparison.Ordinal))
        {
            TryLog("MarkovChainScript: Ignored command message.");
            return null;
        }

        if (username.Equals(botUsername, StringComparison.OrdinalIgnoreCase))
        {
            TryLog("MarkovChainScript: Ignored message from self.");
            return null;
        }

        string lowerMessage = message.ToLowerInvariant();
        if (lowerMessage.Contains("http") || lowerMessage.Contains(".com") || lowerMessage.Contains(".net") || lowerMessage.Contains(".org"))
        {
            TryLog("MarkovChainScript: Ignored link-containing message.");
            return null;
        }

        if (!IsMostlyEnglish(message))
        {
            TryLog("MarkovChainScript: Ignored non-English message.");
            return null;
        }

        if (transitions.Count == 0)
            LoadTransitions();

        TryLog("MarkovChainScript: Learning from message.");
        LearnFromChat(message);

        messageCounter++;
        if (messageCounter >= 35)
        {
            messageCounter = 0;
            string response = GenerateSentence();
            TryLog($"MarkovChainScript: Responding with generated sentence: {response}");
            return response;
        }

        SaveTransitions();
        return null;
    }

    private static void LearnFromChat(string message)
    {
        var words = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 3)
            return;

        for (int i = 0; i < words.Length - 2; i++)
        {
            var key = $"{words[i]}|{words[i + 1]}";
            var nextWord = words[i + 2];

            if (!transitions.ContainsKey(key))
            {
                transitions[key] = new List<string>();
            }

            transitions[key].Add(nextWord);
        }
    }

    private static string GenerateSentence(int maxWords = 20)
    {
        if (transitions.Count == 0)
            return "";

        var keys = new List<string>(transitions.Keys);
        string currentKey = keys[rng.Next(keys.Count)];
        var parts = currentKey.Split('|');
        string result = $"{parts[0]} {parts[1]}";

        for (int i = 0; i < maxWords; i++)
        {
            if (!transitions.ContainsKey(currentKey) || transitions[currentKey].Count == 0)
                break;

            string nextWord = transitions[currentKey][rng.Next(transitions[currentKey].Count)];
            result += " " + nextWord;

            currentKey = $"{parts[1]}|{nextWord}";
            parts = currentKey.Split('|');
        }

        return result;
    }

    private static void SaveTransitions()
    {
        try
        {
            string folder = Path.GetDirectoryName(saveFilePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string json = JsonConvert.SerializeObject(transitions, Formatting.Indented);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception ex)
        {
            TryLog($"MarkovChainScript: Error saving transitions - {ex.Message}");
        }
    }

    private static void LoadTransitions()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                transitions = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            }
        }
        catch (Exception ex)
        {
            TryLog($"MarkovChainScript: Error loading transitions - {ex.Message}");
        }
    }

    private static bool IsMostlyEnglish(string text)
    {
        int englishCharCount = 0, totalCharCount = 0;

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                totalCharCount++;
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    englishCharCount++;
            }
        }

        if (totalCharCount == 0)
            return false;

        return (double)englishCharCount / totalCharCount >= 0.7;
    }

    private static void TryLog(string message)
    {
        if (DebugLog != null)
            DebugLog.Invoke(message);
    }
}
