<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue" alt=".NET 8">
  <img src="https://img.shields.io/github/v/release/Ixitxachitl/MiniBotLauncher" alt="Latest Release">
  <img src="https://img.shields.io/github/license/Ixitxachitl/MiniBotLauncher" alt="License">
  <img src="https://img.shields.io/github/stars/Ixitxachitl/MiniBotLauncher?style=social" alt="GitHub stars">
</p>

# MiniBotLauncher

MiniBotLauncher is a lightweight, C#/.NET 8 Windows Forms application that connects to Twitch chat and enables several fun, chat-interactive scripts, including:

* **AskAI**: Ask AI natural language questions
* **Weather**: Get real-time weather info
* **Translate**: Automatically Translate Non-English into English
* **ButtsBot**: Randomly replaces syllables in chat messages with "butt" for comedic effect
* **ClapThatBot**: Generate funny "I'd clap that" responses
* **MarkovChain**: Build a chat-based Markov brain and generate random sentences

---

## ✨ Features

* Full Twitch OAuth authorization flow inside the app
* Auto-save and load settings from `Documents\MiniBot\settings.json`
* Enable or disable scripts individually
* TwitchLib-based connection management
* Self-contained publish (single .exe optional)
* Built for .NET 8 (Modern, fast, portable)
* Ignore messages from specific usernames using the new Ignore List popup
* Dark theme UI with styled popup windows and modern tooltips

---

## 💪 Requirements

* Windows 10/11
* .NET 8 Runtime (if not using self-contained EXE)
* Twitch account

Optional:

* Local GPT4All server running with the correct model (`llama3-8b-instruct`) for AskAI
* No longer uses NLPCloud API — **ButtsBot** and **ClapThatBot** now use embedded offline models only.

> If you're using POS tagging, make sure `System.Runtime.Caching` is available (install via NuGet if needed).

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

   * Go to [https://dev.twitch.tv/console/apps](https://dev.twitch.tv/console/apps)
   * Create a new Application.
   * Set Redirect URL to: `http://localhost:8750/callback/`
   * Copy your Client ID.
3. **Enter your Channel Name** (the channel to join).
4. **Click 'Get Token'**:

   * A browser window will open.
   * Approve the permissions.
   * The token will automatically populate.
5. **Click 'Connect'**.
6. **Toggle on the scripts you want active**.
7. **Manage Ignored Users** with the 📄 icon next to the pin (top-right of the window).

> **Important Notes:**
>
> * **AskAI** requires a local GPT4All server running with the correct model (`llama3-8b-instruct`).
> * **ButtsBot** uses the CMU Pronouncing Dictionary (CMUdict) to identify syllables and replace them with "butt" with a small random chance.
> * CMUdict is embedded as a resource and used offline with no external dependencies.

---

## 🔹 Scripts Overview

| Script      | Description                                                                                                 |
| :---------- | :---------------------------------------------------------------------------------------------------------- |
| AskAI       | Answers natural language questions. Requires a local GPT4All server running the `llama3-8b-instruct` model. |
| Weather     | Provides real-time weather based on city name.                                                              |
| Translate   | Automatically translates non-English messages to English.                                                   |
| ButtsBot    | Replaces random syllables in user messages with "butt" using offline CMUdict-based syllable parsing.        |
| ClapThatBot | Generates "I'd clap that" jokes from adjective+noun pairs using local POS tagging.                          |
| MarkovChain | Learns from chat and generates fun, random sentences every 35 messages.                                     |

---

## 🚀 Building from Source

* Open `MiniBotLauncher.sln` in Visual Studio 2022+
* Set Configuration to `Release`
* Right-click project → Publish
* Use **Self-contained**, **Single EXE** if you want a portable build

---

## 📄 License

MIT License. Free for any personal or commercial use.

OpenNLP is used for offline part-of-speech tagging and is licensed under the Apache License 2.0.

CMUdict is used for syllable parsing and distributed under a BSD-style license (included in the file header).

---

## 🚀 Credits

* TwitchLib (chat connection)
* Newtonsoft.Json (settings and Markov brain)
* OpenNLP (offline POS tagging with legacy `.nbin` model)
* CMUdict (syllable parsing)
* GPT4All (local AI serving for AskAI)
* Built by **Ixitxachitl**
