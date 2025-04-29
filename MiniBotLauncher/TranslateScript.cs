using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

public static class TranslateScript
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<string> TryTranslate(string inputText, string username)
    {
        if (string.IsNullOrWhiteSpace(inputText))
            return null;

        if (string.IsNullOrWhiteSpace(username))
            return null;

        string trimmedInput = inputText.Trim().ToLowerInvariant();
        string forcedLang = null;

        // Known short word language mapping
        var knownWords = new Dictionary<string, string>
        {
            { "si", "es" }, { "oui", "fr" }, { "no", "es" },
            { "ciao", "it" }, { "ja", "de" }, { "non", "fr" }
        };

        if (trimmedInput.Length <= 5 && knownWords.ContainsKey(trimmedInput))
            forcedLang = knownWords[trimmedInput];

        (string translatedText, string sourceLang) = await TranslateText(inputText, forcedLang);

        if (string.IsNullOrWhiteSpace(translatedText))
            return null;

        bool forced = forcedLang != null;

        string cleanedInput = CleanTextForComparison(inputText);
        string cleanedTranslation = CleanTextForComparison(translatedText);

        if (cleanedInput.Equals(cleanedTranslation, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!forced && sourceLang == "en")
            return null;

        var trustedLatinLangs = new HashSet<string> { "es", "it", "pt", "de", "fr", "nl", "ro", "pl", "sv", "no", "da" };

        if (IsLatinAlphabet(inputText) && !trustedLatinLangs.Contains(sourceLang))
            return null;

        string fullLanguageName = GetLanguageDisplayName(sourceLang);

        return $"[Translated from {fullLanguageName}] {username}: {translatedText}";
    }

    private static async Task<(string, string)> TranslateText(string text, string forcedSourceLang = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        try
        {
            string slParam = forcedSourceLang ?? "auto";
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={slParam}&tl=en&dt=t&q={Uri.EscapeDataString(text)}";

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            string response = await client.GetStringAsync(url);

            JArray json = JArray.Parse(response);
            string translatedText = "";

            if (json != null && json.Count > 0 && json[0] is JArray translationParts)
            {
                foreach (var part in translationParts)
                {
                    if (part is JArray segment && segment.Count > 0 && segment[0] != null)
                        translatedText += segment[0].ToString();
                }
            }

            string sourceLang = json.Count > 2 && json[2] != null ? json[2].ToString() : "unknown";

            return (translatedText.Trim(), sourceLang);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string GetLanguageDisplayName(string isoCode)
    {
        if (string.IsNullOrEmpty(isoCode))
            return "Unknown";

        var manualMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "iw", "Hebrew" },
            { "zh-cn", "Chinese (Simplified)" },
            { "zh-tw", "Chinese (Traditional)" },
            { "fil", "Filipino" }
        };

        if (manualMap.TryGetValue(isoCode, out string mapped))
            return mapped;

        try
        {
            var culture = new CultureInfo(isoCode);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(culture.EnglishName);
        }
        catch
        {
            return isoCode.ToUpperInvariant();
        }
    }

    private static bool IsLatinAlphabet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        int nonLatinCount = 0, letterCount = 0;

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                letterCount++;
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 0x00C0 && c <= 0x024F)))
                    nonLatinCount++;
            }
        }

        if (letterCount == 0)
            return true;

        return (double)nonLatinCount / letterCount < 0.2;
    }

    private static string CleanTextForComparison(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var cleanedChars = new List<char>();
        bool lastWasSpace = false;

        foreach (char c in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                cleanedChars.Add(c);
                lastWasSpace = false;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    cleanedChars.Add(' ');
                    lastWasSpace = true;
                }
            }
        }

        return new string(cleanedChars.ToArray()).Trim();
    }
}
