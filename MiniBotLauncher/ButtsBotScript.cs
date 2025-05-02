using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class ButtsBotScript
{
    private static readonly Random rng = new Random();
    private static readonly CMUDict cmu = new CMUDict();

    public static Func<string, Task> DebugLog = null;

    public static async Task<string> Process(string message, string username, string botUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
            return null;

        if (message.TrimStart().StartsWith("!", StringComparison.Ordinal))
            return null;

        if (rng.NextDouble() > 0.02)
        {
            await TryLog($"ButtsBot: Skipped message from {username} (no trigger).");
            return null;
        }

        await TryLog($"ButtsBot: Attempting syllable-based replacement on message: \"{message}\"");

        try
        {
            string funnyText = await ReplaceSyllablesWithButt(message, 0.05);
            await TryLog($"ButtsBot: Final funny message: \"{funnyText}\"");
            return funnyText;
        }
        catch (Exception ex)
        {
            await TryLog($"ButtsBot: Error - {ex.Message}");
            return null;
        }
    }

    private static async Task<string> ReplaceSyllablesWithButt(string input, double replacementChance)
    {
        bool anyReplaced = false;
        var tokens = TokenizePreservingFormat(input);
        var modifiedTokens = new List<string>();

        foreach (var token in tokens)
        {
            if (token.Any(char.IsLetter))
            {
                var syllables = cmu.GetSyllables(token);

                if (syllables.Count == 1 && syllables[0] == token)
                {
                    await TryLog($"ButtsBot: Using heuristic split for unknown word '{token}'.");
                    syllables = HeuristicSyllableSplit(token);
                }

                for (int i = 0; i < syllables.Count; i++)
                {
                    if (rng.NextDouble() < replacementChance)
                    {
                        syllables[i] = "butt";
                        anyReplaced = true;
                    }
                }

                string rebuilt = RebuildWordFromOriginal(token, syllables);
                modifiedTokens.Add(rebuilt);
            }
            else
            {
                modifiedTokens.Add(token);
            }
        }

        return anyReplaced ? string.Join("", modifiedTokens) : null;
    }
    private static List<string> HeuristicSyllableSplit(string word)
    {
        var syllables = new List<string>();
        if (word.Length <= 3)
        {
            syllables.Add(word);
            return syllables;
        }

        string vowels = "aeiouy";
        var current = new StringBuilder();
        word = word.ToLower();

        for (int i = 0; i < word.Length; i++)
        {
            current.Append(word[i]);

            // Look ahead: V-C-V or vowel before and after a consonant
            if (i >= 1 && i < word.Length - 1)
            {
                bool isPrevVowel = vowels.Contains(word[i - 1]);
                bool isCurrConsonant = !vowels.Contains(word[i]);
                bool isNextVowel = vowels.Contains(word[i + 1]);

                if (isPrevVowel && isCurrConsonant && isNextVowel)
                {
                    syllables.Add(current.ToString());
                    current.Clear();
                }
            }
        }

        if (current.Length > 0)
            syllables.Add(current.ToString());

        return syllables;
    }


    private static async Task TryLog(string message)
    {
        if (DebugLog != null)
            await DebugLog.Invoke(message);
    }

    private static List<string> TokenizePreservingFormat(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        bool? isWord = null;

        foreach (char c in input)
        {
            bool isCurrentLetterOrDigit = char.IsLetterOrDigit(c);

            if (isWord == null)
            {
                isWord = isCurrentLetterOrDigit;
                current.Append(c);
            }
            else if (isCurrentLetterOrDigit == isWord)
            {
                current.Append(c);
            }
            else
            {
                tokens.Add(current.ToString());
                current.Clear();
                current.Append(c);
                isWord = isCurrentLetterOrDigit;
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }

    private static string RebuildWordFromOriginal(string original, List<string> newSyllables)
    {
        if (string.IsNullOrEmpty(original) || newSyllables.Count == 0)
            return original;

        var rebuilt = new StringBuilder();
        int originalIndex = 0;

        foreach (var syllable in newSyllables)
        {
            int copyLen = Math.Min(syllable.Length, original.Length - originalIndex);
            string replacement = syllable;

            if (copyLen > 0)
            {
                var originalSegment = original.Substring(originalIndex, copyLen);
                var formatted = new StringBuilder();

                for (int i = 0; i < replacement.Length; i++)
                {
                    if (i < originalSegment.Length)
                    {
                        formatted.Append(char.IsUpper(originalSegment[i]) ? char.ToUpper(replacement[i]) : char.ToLower(replacement[i]));
                    }
                    else
                    {
                        formatted.Append(replacement[i]);
                    }
                }

                replacement = formatted.ToString();
            }

            rebuilt.Append(replacement);
            originalIndex += copyLen;
        }

        return rebuilt.ToString();
    }
}

public class CMUDict
{
    // CMU Pronouncing Dictionary (cmudict.0.7a) © Carnegie Mellon University.
    // Included under a BSD-style license — see cmudict.0.7a for details.

    private readonly string ResourceName;
    private Dictionary<string, List<string>> wordToPhonemes = new();

    private static readonly HashSet<string> VowelPhonemes = new()
    {
        "AA", "AE", "AH", "AO", "AW", "AY", "EH", "ER", "EY",
        "IH", "IY", "OW", "OY", "UH", "UW"
    };

    public CMUDict()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        ResourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("cmudict.0.7a", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Embedded CMUdict resource not found.");

        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{ResourceName}' could not be loaded.");

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";;;")) continue;

            var split = line.Split("  ");
            if (split.Length != 2) continue;

            var word = split[0].ToLower().Trim();
            var phonemes = split[1].Trim().Split(' ').ToList();

            word = word.Contains('(') ? word[..word.IndexOf('(')] : word;

            if (!wordToPhonemes.ContainsKey(word))
                wordToPhonemes[word] = phonemes;
        }
    }

    public List<string> GetSyllables(string word)
    {
        int count = GetSyllableCount(word);
        return SplitTextIntoSyllables(word, count);
    }

    public int GetSyllableCount(string word)
    {
        word = word.ToLower();
        if (!wordToPhonemes.TryGetValue(word, out var phonemes))
            return 1;

        return phonemes.Count(p => IsVowel(p));
    }

    private List<string> SplitTextIntoSyllables(string word, int syllableCount)
    {
        if (syllableCount <= 1 || word.Length <= syllableCount)
            return new List<string> { word };

        var syllables = new List<string>();
        int baseLen = word.Length / syllableCount;
        int remainder = word.Length % syllableCount;
        int index = 0;

        for (int i = 0; i < syllableCount; i++)
        {
            int len = baseLen + (i < remainder ? 1 : 0);
            syllables.Add(word.Substring(index, len));
            index += len;
        }

        return syllables;
    }

    private bool IsVowel(string phoneme)
    {
        return VowelPhonemes.Any(v => phoneme.StartsWith(v));
    }
}
