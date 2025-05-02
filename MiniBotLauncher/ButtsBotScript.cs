using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class ButtsBotScript
{
    private static readonly Random rng = new Random();
    private static readonly Dictionary<string, List<string>> CmuDict = new();
    private static int replyChancePercent = 2;

    public static void SetReplyChance(int percent)
    {
        replyChancePercent = Math.Clamp(percent, 1, 100);
    }



    public static Func<string, Task> DebugLog = null;

    public static async Task<string> Process(string message, string username)
    {
        if (!CanReplaceSyllables(message))
        {
            await TryLog("ButtsBot: No syllables found in message — skipping.");
            return null;
        }

        string funnyText = await ReplaceSyllablesWithButt(message, 0.05);

        return funnyText;
    }

    private static async Task<string> ReplaceSyllablesWithButt(string message, double replaceChance)
    {
        if (DebugLog != null) await DebugLog.Invoke($"[DEBUG] Using replaceChance: {replaceChance}");
        var tokens = Regex.Split(message, "(\\W+)");
        var modifiedTokens = new List<string>();
        var syllableMap = new Dictionary<int, List<string>>();
        var fallback = new List<(int tokenIndex, int syllableIndex)>();
        var toReplace = new List<(int tokenIndex, int syllableIndex)>();

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            if (string.IsNullOrWhiteSpace(token) || !char.IsLetterOrDigit(token[0]))
            {
                modifiedTokens.Add(token);
                continue;
            }

            var syllables = GetSyllables(token);
            if (syllables == null || syllables.Count == 0)
            {
                modifiedTokens.Add(token);
                continue;
            }

            var syllableList = new List<string>(syllables);
            syllableMap[i] = syllableList;
            modifiedTokens.Add(null);

            for (int j = 0; j < syllableList.Count; j++)
            {
                string original = syllableList[j];
                fallback.Add((i, j));
                if (rng.NextDouble() < replaceChance)
                {
                    toReplace.Add((i, j));
                    if (DebugLog != null) await DebugLog.Invoke($"[5% HIT] Token {i}, Syllable {j}: '{original}' → 'butt'");
                }
                else
                {
                    if (DebugLog != null) await DebugLog.Invoke($"[SKIP] Token {i}, Syllable {j}: '{original}' left unchanged");
                }
            }
        }

        if (DebugLog != null) await DebugLog.Invoke($"[REPLACE COUNT] {toReplace.Count} syllable(s) marked for replacement");
        if (toReplace.Count == 0 && fallback.Count > 0)
        {
            var forced = fallback[rng.Next(fallback.Count)];
            toReplace.Add(forced);
            if (DebugLog != null) await DebugLog.Invoke($"[FORCED] Token {forced.tokenIndex}, Syllable {forced.syllableIndex} → 'butt'");

        }

        // This is the correct and final fix: in-place mutation only
        foreach (var (tokenIndex, syllableIndex) in toReplace)
        {
            if (syllableMap.TryGetValue(tokenIndex, out var list) && syllableIndex < list.Count)
            {
                list[syllableIndex] = "butt";
            }
        }

        for (int i = 0; i < modifiedTokens.Count; i++)
        {
            if (modifiedTokens[i] == null)
                modifiedTokens[i] = string.Join("", syllableMap[i]);
        }

        var result = string.Join("", modifiedTokens);
        if (DebugLog != null) await DebugLog.Invoke($"[FINAL OUTPUT] {result}");
        return result;
    }

    private static bool CanReplaceSyllables(string message)
    {
        var tokens = Regex.Split(message, "(\\W+)");
        foreach (string token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token) || !char.IsLetterOrDigit(token[0]))
                continue;

            var syllables = GetSyllables(token.ToLowerInvariant());
            if (syllables != null && syllables.Count > 0)
                return true;
        }
        return false;
    }

    private static List<string> GetSyllables(string word)
    {
        if (CmuDict.TryGetValue(word.ToLowerInvariant(), out var syllables) && syllables.Count > 0)
            return new List<string>(syllables);

        return HeuristicSyllableSplit(word);
    }

    private static List<string> HeuristicSyllableSplit(string word)
    {
        var syllables = new List<string>();

        var matches = Regex.Matches(word.ToLowerInvariant(), @"[^aeiouy]*[aeiouy]+[^aeiouy]*");
        foreach (Match match in matches)
        {
            if (!string.IsNullOrWhiteSpace(match.Value))
                syllables.Add(match.Value);
        }

        if (syllables.Count == 0)
            syllables.Add(word);

        return syllables;
    }

    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }
}
