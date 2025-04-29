using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class MarkovChainScript
{
    private static Dictionary<string, List<string>> transitions = new Dictionary<string, List<string>>();
    private static int messageCounter = 0;
    private static Random rng = new Random();
    private static readonly string saveFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MiniBot",
        "markov_brain.json"
    );

    private static readonly HashSet<string> knownBots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "streamelements", "nightbot", "sery_bot", "wizebot", "kofistreambot",
        "botrixoficial", "tangiabot", "moobot", "own3d", "creatisbot",
        "frostytoolsdotcom", "streamlabs", "pokemoncommunitygame", "fossabot",
        "soundalerts", "botbandera", "overlayexpert", "trackerggbot",
        "songlistbot", "commanderroot", "instructbot", "autogpttest",
        "aerokickbot", "streamerelem", "ronniabot", "tune2livebot",
        "peepostreambot", "playwithviewersbot", "hexe_bot", "super_sweet_bot",
        "streamroutine_bot", "remasuri_bot", "milanitommasobot", "jeetbot",
        "bot584588", "lurky_dogg"
    };

    public static string LearnAndMaybeRespond(string message, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
            return null;

        if (message.StartsWith("!", StringComparison.Ordinal))
            return null;

        username = username.ToLowerInvariant();
        botUsername = botUsername.ToLowerInvariant();

        if (username == botUsername)
            return null;

        if (knownBots.Contains(username))
            return null;

        string lowerMessage = message.ToLowerInvariant();
        if (lowerMessage.Contains("http") || lowerMessage.Contains(".com") || lowerMessage.Contains(".net") || lowerMessage.Contains(".org"))
            return null;

        if (!IsMostlyEnglish(message))
            return null;

        if (transitions.Count == 0)
            LoadTransitions();

        LearnFromChat(message);

        messageCounter++;
        if (messageCounter >= 35)
        {
            messageCounter = 0;
            return GenerateSentence();
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
        catch
        {
            // Silent fail (no debug logs)
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
        catch
        {
            // Silent fail (no debug logs)
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
}
