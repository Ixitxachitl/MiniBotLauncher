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
    public bool SoundAlertsEnabled { get; set; }
    public bool WalkOnEnabled { get; set; }

    public List<string> IgnoredUsernames { get; set; } = new();

    // AI-specific settings
    public string AskAI_ServerAddress { get; set; } = "http://localhost";
    public int AskAI_ServerPort { get; set; } = 4891;
    public string AskAI_ModelName { get; set; } = "Llama 3 8B Instruct";
    public int AskAI_MaxTokens { get; set; } = 50;
    public string AskAI_SystemMessage { get; set; } = "";


    // Weather-specific settings
    public string Weather_FormatString { get; set; } = "2";

    // Translation-specific settings
    public string Translate_TargetLanguage { get; set; } = "en";

    // Buttsbot-specific settings
    public int ButtsBot_ReplyChancePercent { get; set; } = 2;
    public string ButtsBot_ReplacementWord { get; set; } = "butt";

    // Clapthat-specific settings
    public int ClapThat_ReplyChancePercent { get; set; } = 2;
    public string ClapThat_ReplacementWord { get; set; } = "clap";

    // Sound Alerts-specifc settings
    public Dictionary<string, string> SoundAlertMappings { get; set; } = new();
    public int SoundAlertsVolume { get; set; } = 100; // percent (0–100)

    // Walk-On-specific settings
    public Dictionary<string, string> WalkOnSoundMappings { get; set; } = new();
    public string WalkOnLastStreamStart { get; set; } = "";


}
