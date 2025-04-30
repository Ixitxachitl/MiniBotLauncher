# MiniBotLauncher

MiniBotLauncher is a lightweight, C#/.NET 8 Windows Forms application that connects to Twitch chat and enables several fun, chat-interactive scripts, including:

- **AskAI**: Ask AI natural language questions
- **Weather**: Get real-time weather info
- **Translate**: Automatically Translate Non-English into English
- **ButtsBot**: Replace words with "butt" or "butts" randomly for comedic effect
- **ClapThatBot**: Generate funny "I'd clap that" responses
- **MarkovChain**: Build a chat-based Markov brain and generate random sentences

---

## ✨ Features

- Full Twitch OAuth authorization flow inside the app
- Auto-save and load settings from `Documents\MiniBot\settings.json`
- Enable or disable scripts individually
- TwitchLib-based connection management
- Self-contained publish (single .exe optional)
- Built for .NET 8 (Modern, fast, portable)

---

## 💪 Requirements

- Windows 10/11
- .NET 8 Runtime (if not using self-contained EXE)
- Twitch account

Optional:
- Local GPT4All server running with the correct model (`llama3-8b-instruct`) for AskAI
- **No longer requires NLPCloud API** — ButtsBot and ClapThatBot now use an embedded, offline POS tagger model (OpenNLP)

---

## 📚 Installation

1. Download the latest release.
2. Extract the zip.
3. Run `MiniBotLauncher.exe`.

> Settings are saved automatically to:
>
> `C:\Users\<YourName>\Documents\MiniBot\settings.json`

> The Markov brain is saved to:
>
> `C:\Users\<YourName>\Documents\MiniBot\markov_brain.json`

---

## ⚙️ Setup Instructions

1. **Enter your Twitch Bot Username** (the account you want the bot to act as).
2. **Enter your Client ID**:
   - Go to https://dev.twitch.tv/console/apps
   - Create a new Application.
   - Set Redirect URL to: `http://localhost:8750/callback/`
   - Copy your Client ID.
3. **Enter your Channel Name** (the channel to join).
4. **Click 'Get Token'**:
   - A browser window will open.
   - Approve the permissions.
   - The token will automatically populate.
5. **Click 'Connect'**.
6. **Toggle on the scripts you want active**.

> **Important:**
> - AskAI requires a **local GPT4All server** running with the `llama3-8b-instruct` model.
> - ButtsBot and ClapThatBot work offline now using OpenNLP model embedded in the EXE.

---

## 🔹 Scripts Overview

| Script | Description |
|:---|:---|
| AskAI | Answers natural language questions. Requires a local GPT4All server running the `llama3-8b-instruct` model. |
| Weather | Provides real-time weather based on city name. |
| Translate | Automatically Translates Non-English into English. |
| ButtsBot | Replaces random words with "butt"/"butts" in chat using local POS tagging. |
| ClapThatBot | Fun "I'd clap that" joke generation using local POS tagging. |
| MarkovChain | Learns from chat and generates fun random sentences every 35 messages. |

---

## 🚀 Building from Source

- Open `MiniBotLauncher.sln` in Visual Studio 2022+.
- Set Configuration to `Release`.
- Right-click project → Publish.
- Set to **Self-contained**, **Single EXE** if you want a portable app.

---

## 📄 License

MIT License. Free for any personal or commercial use.

OpenNLP is used for offline part-of-speech tagging and is licensed under the Apache License 2.0.
See `/Resources/Models/LICENSE-OpenNLP.txt` for details.

---

## 🚀 Credits

- TwitchLib (chat connection)
- Newtonsoft.Json (settings and Markov brain)
- OpenNLP (offline POS tagging)
- GPT4All (local AI serving for AskAI)
- Built by **Ixitxachitl**

