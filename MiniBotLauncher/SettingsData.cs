using System.Collections.Generic;

public class SettingsData
{
    public string BotUsername { get; set; }
    public string ClientID { get; set; }
    public string OAuthToken { get; set; }
    public string ChannelName { get; set; }

    public bool AskAIEnabled { get; set; }
    public bool WeatherEnabled { get; set; }
    public bool TranslateEnabled { get; set; }
    public bool ButtsbotEnabled { get; set; }
    public bool ClapThatEnabled { get; set; }
    public bool MarkovChainEnabled { get; set; }

    public List<string> IgnoredUsernames { get; set; } = new();

    // AI-specific settings
    public string AskAI_ModelName { get; set; } = "llama3-8b-instruct";
    public int AskAI_MaxTokens { get; set; } = 130;
    public string AskAI_SystemMessage { get; set; } = "";

    // Weather-specific settings
    public string Weather_FormatString { get; set; } = "2";

    // Translation-specific settings
    public string Translate_TargetLanguage { get; set; } = "en";

    // Buttsbot-specific settings
    public int ButtsBot_ReplyChancePercent { get; set; } = 2;

    // Clapthat-specific settings
    public int ClapThat_ReplyChancePercent { get; set; } = 2;
}
