<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue" alt=".NET 8">
  <img src="https://img.shields.io/github/v/release/Ixitxachitl/MiniBotLauncher" alt="Latest Release">
  <img src="https://img.shields.io/github/license/Ixitxachitl/MiniBotLauncher" alt="License">
  <img src="https://img.shields.io/github/stars/Ixitxachitl/MiniBotLauncher?style=social" alt="GitHub stars">
</p>

# MiniBotLauncher

| MiniBotLauncher is a lightweight, C#/.NET 8 Windows Forms application that connects to Twitch chat and enables several fun, chat-interactive scripts, including:<br><br>• **AskAI** — Ask natural language questions to a local AI model<br>• **Weather** — Get real-time weather info for cities<br>• **Translate** — Automatically translate non-English messages to English<br>• **ButtsBot** — Randomly replaces syllables in messages with "butt" for comedic effect<br>• **ClapThatBot** — Generates "I'd clap that" responses from adjective+noun pairs<br>• **MarkovChain** — Builds a Markov chain brain from chat and generates random responses<br>• **Sound Alerts** — Plays sound effects on specific commands; supports .mp3/.wav, volume control, and a stop/clear queue button<br>• **WalkOnScript** — Plays personalized entrance sounds for viewers when they first speak after the stream has started | ![MiniBotLauncher Screenshot](https://github.com/user-attachments/assets/c94ef522-1b41-4317-a4b5-19020afad0a7) |
| :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------: |

---

## ✨ Features

* OAuth-based Twitch login flow with auto-closing browser tab
* Channel selector dropdown for quick channel switching
* Manual chat input field to send messages directly from the app
* Auto-saves all settings to `Documents\MiniBot\settings.json`
* Enable or disable each script individually using toggles (works even when offline)
* Each script has its own ⚙️ settings popup for configuration:

  * **AskAI**: Server address, port, model dropdown (auto-fetched), max tokens (1–255), optional system message, configurable command trigger
  * **Weather**: Format preset dropdown with 4 options plus custom format support with help reference
  * **Translate**: Choose target language from dropdown
  * **ButtsBot**: Adjustable reply frequency (syllable replacements are fixed at 5%)
  * **ClapThatBot**: Adjustable reply frequency
  * **MarkovChain**: Brain file manager, banned words list with case-insensitive filtering, clean database function
  * **Sound Alerts**: Add trigger-to-sound mappings, adjust volume, and stop/clear playback queue
  * **WalkOnScript**: Assign personalized walk-on sounds per user, plays once per stream unless reset by reconnect or stream restart
* Ignore messages from specific users using the Ignore List popup
* Stylish dark mode UI with centralized theming, rounded corners and tooltips
* Most scripts operate offline. Weather and Translate use public web APIs and require an internet connection, but no user setup is needed.

---

## 💪 Requirements

* Windows 10 or 11
* .NET 8 Runtime (unless using self-contained build)
* Twitch account

Optional:

* Local GPT4All server with compatible models such as `llama3-8b-instruct` or `phi-3-mini-instruct`

> ButtsBot and ClapThatBot use embedded offline models, no external services required.

---

## 📙 Installation

1. Download the latest release from GitHub
2. Extract the contents
3. Run `MiniBotLauncher.exe`

Settings are stored at:

```
C:\Users\<YourName>\Documents\MiniBot\settings.json
```

Markov brain data is stored at:

```
C:\Users\<YourName>\Documents\MiniBot\markov_brain_<channel>.json
```

---

## ⚙️ Setup Instructions

1. Enter your **Twitch Bot Username**
2. Enter your **Client ID** from [https://dev.twitch.tv/console/apps](https://dev.twitch.tv/console/apps)

   * Set redirect URL: `http://localhost:8750/callback/`
3. Select or enter a **Channel Name** from the dropdown (previously joined channels are remembered)
4. Click **Login** and authorize access in the browser (tab closes automatically after success)
5. Click **Connect**
6. Toggle the scripts you want active (toggles work even when not connected)
7. Click each script's ⚙️ to configure its behavior
8. Use the 📄 button in the top-right to open the Ignore List
9. Use the chat input field at the bottom to send messages directly from the app

> AskAI requires GPT4All running locally. Now includes server/port configuration, automatic model listing, and configurable command trigger (default: `!askai`).
> Responses with trailing tags like `tutor:` or `response:` are automatically cleaned unless clearly part of the content.
> ButtsBot replaces syllables at random, one syllable is always replaced if none were hit by chance.
> MarkovChain will respond every 35 messages. Features banned words filtering (input and output), brain file manager, and database cleaning.
> Sound Alerts maps chat commands to sound files and queues them for playback with a short delay. It supports `.mp3` and `.wav`, adjustable volume, and a button to stop and clear the current queue.
> WalkOnScript plays a specific sound the first time a user speaks during a stream. Triggers reset on reconnect or when stream start time changes.

---

## 🔹 Scripts Overview

| Script       | Description                                                                                   |
| ------------ | --------------------------------------------------------------------------------------------- |
| AskAI        | Answers natural language queries using a local AI model (configurable command trigger)        |
| Weather      | Provides current weather using wttr.in with 4 preset formats or custom % notation             |
| Translate    | Detects non-English and translates into your chosen target language                           |
| ButtsBot     | Replaces ∼5% of syllables in messages with "butt" — reply rate adjustable in settings         |
| ClapThatBot  | Detects adjective+noun phrases and responds with "I'd clap that" — reply rate adjustable      |
| MarkovChain  | Learns from chat to generate phrases — includes banned words, brain manager, and DB cleaning  |
| Sound Alerts | Plays sound files on command triggers with volume and queue control                           |
| WalkOnScript | Plays a unique walk-on sound per user the first time they speak after the stream starts       |

---

## 🚀 Building from Source

* Open `MiniBotLauncher.sln` in Visual Studio 2022+
* Build in `Release` mode
* Right-click project → Publish
* Use self-contained, single-file publish option for portability

---

## 📄 License

MIT License — free for personal or commercial use

Third-party libraries:

* **OpenNLP** — POS tagging (Apache 2.0)
* **CMUdict** — Syllable parsing (BSD-style license)
* **TwitchLib** — Twitch connection
* **GPT4All** — Local AI inference engine
* **Newtonsoft.Json** — JSON parsing for settings and Markov brain
* **NAudio** — Audio playback for .wav and .mp3 support

---

## 🚀 Credits

Thanks to:

* TwitchLib
* GPT4All
* OpenNLP
* CMUdict
* Newtonsoft.Json
* NAudio

Built with ❤️ by **Ixitxachitl**
