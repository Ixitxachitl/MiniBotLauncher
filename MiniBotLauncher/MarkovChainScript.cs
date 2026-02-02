using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public static class MarkovChainScript
{
    public static Func<string, Task>? DebugLog = null;
    private static Dictionary<string, List<string>> transitions = new Dictionary<string, List<string>>();
    private static int messageCounter = 0;
    private static Random rng = new Random();
    private static string? currentChannel = null;
    private static string baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MiniBot");
    private static string? saveFilePath = null;
    private static string? lastLoadedFilePath = null;
    private static int messageInterval = 35;
    private static HashSet<string> bannedWords = new(StringComparer.OrdinalIgnoreCase);

    public static void SetMessageInterval(int interval)
    {
        messageInterval = Math.Max(1, interval);
    }

    public static int GetMessageInterval() => messageInterval;

    public static void SetBannedWords(IEnumerable<string> words)
    {
        bannedWords = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
    }

    public static HashSet<string> GetBannedWords() => bannedWords;

    public static string GetBaseFolder() => baseFolder;

    public static int GetTransitionCount() => transitions.Count;

    /// <summary>
    /// Cleans the database by removing all transitions containing banned words.
    /// Returns the number of entries removed.
    /// </summary>
    public static (int keysRemoved, int valuesRemoved) CleanDatabase(string brainFilePath)
    {
        if (bannedWords.Count == 0)
            return (0, 0);

        if (!File.Exists(brainFilePath))
            return (0, 0);

        try
        {
            string json = File.ReadAllText(brainFilePath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            if (data == null)
                return (0, 0);

            int keysRemoved = 0;
            int valuesRemoved = 0;

            // Find keys to remove (keys contain "word1|word2")
            var keysToRemove = data.Keys.Where(key =>
            {
                var parts = key.Split('|');
                return parts.Any(p => bannedWords.Contains(p));
            }).ToList();

            foreach (var key in keysToRemove)
            {
                data.Remove(key);
                keysRemoved++;
            }

            // Remove banned words from remaining value lists
            foreach (var key in data.Keys.ToList())
            {
                int before = data[key].Count;
                data[key] = data[key].Where(w => !bannedWords.Contains(w)).ToList();
                valuesRemoved += before - data[key].Count;

                // Remove key if no values left
                if (data[key].Count == 0)
                {
                    data.Remove(key);
                    keysRemoved++;
                }
            }

            // Save cleaned data
            string cleanedJson = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(brainFilePath, cleanedJson);

            // Reload if this is the currently loaded brain
            if (string.Equals(brainFilePath, saveFilePath, StringComparison.OrdinalIgnoreCase))
            {
                transitions = data;
            }

            TryLog($"MarkovChainScript: Cleaned database - removed {keysRemoved} keys and {valuesRemoved} values");
            return (keysRemoved, valuesRemoved);
        }
        catch (Exception ex)
        {
            TryLog($"MarkovChainScript: Error cleaning database - {ex.Message}");
            return (0, 0);
        }
    }

    public static void SetChannel(string channelName)
    {
        currentChannel = channelName.ToLowerInvariant();
        string newPath = Path.Combine(baseFolder, $"markov_brain_{currentChannel}.json");

        // Only load and log if it's a new file (or first time)
        if (lastLoadedFilePath == null || !string.Equals(lastLoadedFilePath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            saveFilePath = newPath;
            LoadTransitions(); // This also sets 'transitions'
            lastLoadedFilePath = newPath;
            TryLog($"MarkovChainScript: Loaded Markov brain for channel '{currentChannel}' from '{saveFilePath}'.");
        }
    }


    public static void ResetCounter()
    {
        messageCounter = 0;
        transitions.Clear();
    }

    public static string? LearnAndMaybeRespond(string message, string username, string botUsername)
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

        if (!IsMostlyEnglish(message))
        {
            TryLog("MarkovChainScript: Ignored non-English message.");
            return null;
        }

        // Filter out messages containing banned words
        if (ContainsBannedWord(message))
        {
            TryLog("MarkovChainScript: Ignored message with banned word.");
            return null;
        }

        if (transitions.Count == 0)
            LoadTransitions();

        TryLog("MarkovChainScript: Learning from message.");
        LearnFromChat(message);

        messageCounter++;
        if (messageCounter >= messageInterval)
        {
            messageCounter = 0;
            // Try up to 5 times to generate a sentence without banned words
            for (int attempt = 0; attempt < 5; attempt++)
            {
                string response = GenerateSentence();
                if (!ContainsBannedWord(response))
                {
                    TryLog($"MarkovChainScript: Responding with generated sentence: {response}");
                    return response;
                }
                TryLog($"MarkovChainScript: Filtered out response with banned word (attempt {attempt + 1})");
            }
            TryLog("MarkovChainScript: Could not generate clean response after 5 attempts");
            return null;
        }

        SaveTransitions();
        return null;
    }

    private static bool ContainsBannedWord(string message)
    {
        if (bannedWords.Count == 0) return false;
        var words = message.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Any(w => bannedWords.Any(b => string.Equals(w, b, StringComparison.OrdinalIgnoreCase)));
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
            if (string.IsNullOrEmpty(saveFilePath)) return;
            
            string? folder = Path.GetDirectoryName(saveFilePath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
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
        transitions.Clear(); // Important: avoid blending old data

        try
        {
            if (!string.IsNullOrEmpty(saveFilePath) && File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                transitions = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json) 
                    ?? new Dictionary<string, List<string>>();
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
