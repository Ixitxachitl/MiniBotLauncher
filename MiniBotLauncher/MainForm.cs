using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;

public partial class MainForm : Form
{
    private TwitchClient client;
    private TextBox txtBotUsername;
    private TextBox txtClientID;
    private TextBox txtOAuthToken;
    private TextBox txtChannelName;
    private TextBox txtNLPApiKey;
    private CheckBox toggleAskAI;
    private CheckBox toggleWeather;
    private CheckBox toggleTranslate;
    private CheckBox toggleButtsbot;
    private CheckBox toggleClapThat;
    private CheckBox toggleMarkovChain;
    private Button btnGetToken;
    private Button btnConnect;
    private TextBox txtStatusLog;
    private Label lblConnectionStatus;
    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MiniBot",
        "settings.json"
    );
    private bool isDisconnecting = false;

    public MainForm()
    {
        InitializeComponent();
        LoadSettings();
        UpdateToggleStates();
        this.FormClosing += MainForm_FormClosing;

        ButtsBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        ClapThatBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };

        client = new TwitchClient();
    }

    private void InitializeComponent()
    {
        this.Text = "MiniBotLauncher";
        this.Size = new Size(500, 650);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        Label lblBotUsername = new Label() { Text = "Bot Username", Left = 20, Top = 20, Width = 120 };
        txtBotUsername = new TextBox() { Left = 150, Top = 20, Width = 300 };
        txtBotUsername.TextChanged += TextFields_TextChanged;

        Label lblClientID = new Label() { Text = "Client ID", Left = 20, Top = 60, Width = 120 };
        txtClientID = new TextBox() { Left = 150, Top = 60, Width = 300 };
        txtClientID.TextChanged += TextFields_TextChanged;

        Label lblOAuthToken = new Label() { Text = "OAuth Token", Left = 20, Top = 100, Width = 120 };
        txtOAuthToken = new TextBox() { Left = 150, Top = 100, Width = 300, ReadOnly = true, UseSystemPasswordChar = true };
        txtOAuthToken.TextChanged += TextFields_TextChanged;

        btnGetToken = new Button() { Text = "Get Token", Left = 260, Top = 140, Width = 90 };
        btnGetToken.Click += btnGetToken_Click;

        btnConnect = new Button() { Text = "Connect", Left = 360, Top = 140, Width = 90 };
        btnConnect.Click += btnConnect_Click;

        Label lblChannel = new Label() { Text = "Channel to Join", Left = 20, Top = 180, Width = 120 };
        txtChannelName = new TextBox() { Left = 150, Top = 180, Width = 300 };
        txtChannelName.TextChanged += TextFields_TextChanged;

        Label lblNLPKey = new Label() { Text = "NLP Cloud API Key", Left = 20, Top = 220, Width = 140 };
        txtNLPApiKey = new TextBox() { Left = 150, Top = 220, Width = 300 };
        txtNLPApiKey.TextChanged += TextFields_TextChanged;

        Label lblScripts = new Label() { Text = "Toggle Scripts:", Left = 20, Top = 270, Width = 120 };

        toggleAskAI = CreateToggle("AskAI", 150, 270);
        toggleWeather = CreateToggle("Weather", 300, 270);
        toggleTranslate = CreateToggle("Translate", 150, 310);
        toggleButtsbot = CreateToggle("Buttsbot", 300, 310);
        toggleClapThat = CreateToggle("ClapThat", 150, 350);
        toggleMarkovChain = CreateToggle("MarkovChain", 300, 350);

        txtStatusLog = new TextBox() { Left = 20, Top = 400, Width = 430, Height = 150, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        lblConnectionStatus = new Label() { Text = "Disconnected", Left = 20, Top = 570, Width = 300, ForeColor = Color.Red };

        Controls.Add(lblBotUsername);
        Controls.Add(txtBotUsername);
        Controls.Add(lblClientID);
        Controls.Add(txtClientID);
        Controls.Add(lblOAuthToken);
        Controls.Add(txtOAuthToken);
        Controls.Add(btnGetToken);
        Controls.Add(btnConnect);
        Controls.Add(lblChannel);
        Controls.Add(txtChannelName);
        Controls.Add(lblNLPKey);
        Controls.Add(txtNLPApiKey);
        Controls.Add(lblScripts);
        Controls.Add(toggleAskAI);
        Controls.Add(toggleWeather);
        Controls.Add(toggleTranslate);
        Controls.Add(toggleButtsbot);
        Controls.Add(toggleClapThat);
        Controls.Add(toggleMarkovChain);
        Controls.Add(txtStatusLog);
        Controls.Add(lblConnectionStatus);

        DisableAllToggles();
    }

    private CheckBox CreateToggle(string text, int left, int top)
    {
        var toggle = new CheckBox()
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 120,
            Height = 30,
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.LightGray,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        toggle.FlatAppearance.BorderSize = 1;
        toggle.CheckedChanged += toggleScript_CheckedChanged;
        return toggle;
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        if (client != null && client.IsConnected)
        {
            Log("Disconnecting from Twitch...");
            isDisconnecting = true;
            client.Disconnect();
        }
        else if (!isDisconnecting) // ⬅ Only allow reconnect if not still cleaning up
        {
            ConnectToTwitch();
        }
    }

    private void btnGetToken_Click(object sender, EventArgs e) => StartOAuthFlow(txtClientID.Text);
    private HttpListener oauthListener;

    private void StartOAuthFlow(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            MessageBox.Show("Client ID is required to start OAuth.");
            return;
        }

        string url = $"https://id.twitch.tv/oauth2/authorize" +
                     $"?client_id={clientId}" +
                     $"&redirect_uri=http://localhost:8750/callback/" +
                     $"&response_type=token" +
                     $"&scope=chat:read+chat:edit";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"Error launching OAuth page: {ex.Message}");
        }

        if (oauthListener != null)
        {
            oauthListener.Stop();
            oauthListener.Close();
        }

        oauthListener = new HttpListener();
        oauthListener.Prefixes.Add("http://localhost:8750/");
        oauthListener.Start();
        Log("Listening for OAuth callback...");

        // Start first listener
        oauthListener.BeginGetContext(OnOAuthCallback, null);
    }

    private void OnOAuthCallback(IAsyncResult result)
    {
        try
        {
            var context = oauthListener.EndGetContext(result);

            if (context.Request.Url.AbsolutePath == "/callback/")
            {
                Log("Serving OAuth HTML page...");

                string responseHtml = @"
                <html><body>
                <script>
                const hash = window.location.hash.substr(1);
                const params = new URLSearchParams(hash);
                const token = params.get('access_token');
                if (token) {
                    fetch('/token/', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: 'access_token=' + encodeURIComponent(token)
                    }).then(() => {
                        document.body.innerHTML = '<h1>Token received! You can close this window.</h1>';
                    });
                } else {
                    document.body.innerHTML = '<h1>Error: No access token found.</h1>';
                }
                </script>
                </body></html>";

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                // 🟰 Keep listening for the POST
                oauthListener.BeginGetContext(OnOAuthCallback, null);
            }
            else if (context.Request.Url.AbsolutePath == "/token/")
            {
                Log("Receiving OAuth token POST...");

                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    var parsed = System.Web.HttpUtility.ParseQueryString(body);
                    string token = parsed["access_token"];

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        Invoke(new Action(() => txtOAuthToken.Text = token));
                        Log("OAuth token captured successfully!");
                    }
                    else
                    {
                        Log("No access token found in POST body.");
                    }
                }

                context.Response.StatusCode = 200;
                context.Response.Close();

                // 🛑 After we got the token, STOP listening
                oauthListener.Stop();
                oauthListener.Close();
                oauthListener = null;
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();

                // 🟰 Keep listening just in case more weird requests
                oauthListener.BeginGetContext(OnOAuthCallback, null);
            }
        }
        catch (Exception ex)
        {
            Log($"OAuth callback error: {ex.Message}");
        }
    }



    private void ConnectToTwitch()
    {
        if (!IsBasicAuthValid())
        {
            Log("Missing bot username, OAuth token, or channel name. Cannot connect.");
            return;
        }

        if (client != null && client.IsConnected)
            client.Disconnect();

        client = new TwitchClient();
        client.AutoReListenOnException = false;

        string finalOAuth = txtOAuthToken.Text.StartsWith("oauth:") ? txtOAuthToken.Text : "oauth:" + txtOAuthToken.Text;

        ConnectionCredentials credentials = new ConnectionCredentials(txtBotUsername.Text, finalOAuth);
        client.Initialize(credentials, txtChannelName.Text);

        client.OnConnected += Client_OnConnected;
        client.OnDisconnected += Client_OnDisconnected;
        client.OnConnectionError += (s, e) => Log($"Connection error: {e.Error.Message}");
        client.OnError += (s, e) => Log($"Client error: {e.Exception.Message}");
        client.OnLog += (s, e) => Log(e.Data);
        client.OnMessageReceived += Client_OnMessageReceived;

        Log("Connecting to Twitch IRC server at wss://irc-ws.chat.twitch.tv:443");
        client.Connect();
        Log("Attempting to connect to Twitch...");
    }

    private void Client_OnConnected(object sender, OnConnectedArgs e)
    {
        Invoke(new Action(() =>
        {
            Log("Connected to Twitch!");
            lblConnectionStatus.Text = "Connected";
            lblConnectionStatus.ForeColor = Color.Green;
            btnConnect.Text = "Disconnect";
            EnableAllToggles();

            // Disable editing fields
            txtBotUsername.Enabled = false;
            txtClientID.Enabled = false;
            txtChannelName.Enabled = false;
            txtNLPApiKey.Enabled = false;
            btnGetToken.Enabled = false;
        }));
    }

    private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
    {
        Invoke(new Action(() =>
        {
            Log("Disconnected from Twitch.");
            lblConnectionStatus.Text = "Disconnected";
            lblConnectionStatus.ForeColor = Color.Red;
            btnConnect.Text = "Connect";
            DisableAllToggles();

            // Re-enable fields
            txtBotUsername.Enabled = true;
            txtClientID.Enabled = true;
            txtChannelName.Enabled = true;
            txtNLPApiKey.Enabled = true;
            btnGetToken.Enabled = true;

            // Clean up old client properly
            if (client != null)
            {
                client.OnConnected -= Client_OnConnected;
                client.OnDisconnected -= Client_OnDisconnected;
                client.OnConnectionError -= (s, e2) => Log($"Connection error: {e2.Error.Message}");
                client.OnError -= (s, e2) => Log($"Client error: {e2.Exception.Message}");
                client.OnLog -= (s, e2) => Log(e2.Data);
                client.OnMessageReceived -= Client_OnMessageReceived;
                client = null;  // 👈 VERY IMPORTANT: completely null out the old client!
            }

            isDisconnecting = false;
        }));
    }

    private void LoadSettings()
    {
        if (File.Exists(SettingsFile))
        {
            string json = File.ReadAllText(SettingsFile);
            var settings = JsonSerializer.Deserialize<SettingsData>(json);

            txtBotUsername.Text = settings.BotUsername;
            txtClientID.Text = settings.ClientID;
            txtOAuthToken.Text = settings.OAuthToken;
            txtChannelName.Text = settings.ChannelName;
            txtNLPApiKey.Text = settings.NLPApiKey;

            toggleAskAI.Checked = settings.AskAIEnabled;
            toggleWeather.Checked = settings.WeatherEnabled;
            toggleTranslate.Checked = settings.TranslateEnabled;
            toggleButtsbot.Checked = settings.ButtsbotEnabled;
            toggleClapThat.Checked = settings.ClapThatEnabled;
            toggleMarkovChain.Checked = settings.MarkovChainEnabled;
        }
    }

    private void SaveSettings()
    {
        var settings = new SettingsData
        {
            BotUsername = txtBotUsername.Text,
            ClientID = txtClientID.Text,
            OAuthToken = txtOAuthToken.Text,
            ChannelName = txtChannelName.Text,
            NLPApiKey = txtNLPApiKey.Text,
            AskAIEnabled = toggleAskAI.Checked,
            WeatherEnabled = toggleWeather.Checked,
            TranslateEnabled = toggleTranslate.Checked,
            ButtsbotEnabled = toggleButtsbot.Checked,
            ClapThatEnabled = toggleClapThat.Checked,
            MarkovChainEnabled = toggleMarkovChain.Checked
        };
    
        try
        {
            string directory = Path.GetDirectoryName(SettingsFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
    
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            Log($"Failed to save settings: {ex.Message}");
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        SaveSettings();
        if (client != null && client.IsConnected)
            client.Disconnect();
    }

    private void DisableAllToggles()
    {
        toggleAskAI.Enabled = false;
        toggleWeather.Enabled = false;
        toggleTranslate.Enabled = false;
        toggleButtsbot.Enabled = false;
        toggleClapThat.Enabled = false;
        toggleMarkovChain.Enabled = false;
    }

    private void EnableAllToggles()
    {
        toggleAskAI.Enabled = true;
        toggleWeather.Enabled = true;
        toggleTranslate.Enabled = true;
        toggleMarkovChain.Enabled = true;

        if (!string.IsNullOrWhiteSpace(txtNLPApiKey.Text))
        {
            toggleButtsbot.Enabled = true;
            toggleClapThat.Enabled = true;
        }
    }

    private void TextFields_TextChanged(object sender, EventArgs e) => UpdateToggleStates();

    private void UpdateToggleStates()
    {
        bool basicReady = IsBasicAuthValid();
        bool nlpReady = IsNLPReady();
        bool getTokenReady = !string.IsNullOrWhiteSpace(txtBotUsername.Text) &&
                             !string.IsNullOrWhiteSpace(txtClientID.Text) &&
                             lblConnectionStatus.Text != "Connected";

        btnConnect.Enabled = basicReady;
        btnGetToken.Enabled = getTokenReady;

        if (lblConnectionStatus.Text == "Connected")
        {
            toggleAskAI.Enabled = basicReady;
            toggleWeather.Enabled = basicReady;
            toggleTranslate.Enabled = basicReady;
            toggleMarkovChain.Enabled = basicReady;

            toggleButtsbot.Enabled = nlpReady;
            toggleClapThat.Enabled = nlpReady;
        }
        else
        {
            DisableAllToggles();
        }
    }

    private bool IsBasicAuthValid() =>
        !string.IsNullOrWhiteSpace(txtBotUsername.Text) &&
        !string.IsNullOrWhiteSpace(txtClientID.Text) &&
        !string.IsNullOrWhiteSpace(txtOAuthToken.Text) &&
        !string.IsNullOrWhiteSpace(txtChannelName.Text);

    private bool IsNLPReady() => IsBasicAuthValid() && !string.IsNullOrWhiteSpace(txtNLPApiKey.Text);

    private void Log(string message)
    {
        if (txtStatusLog.InvokeRequired)
        {
            txtStatusLog.Invoke(new Action(() =>
                txtStatusLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n")
            ));
        }
        else
        {
            txtStatusLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }

    private void toggleScript_CheckedChanged(object sender, EventArgs e)
    {
        var checkbox = sender as CheckBox;
        if (checkbox == null) return;

        checkbox.BackColor = checkbox.Checked ? Color.LightGreen : Color.LightGray;
    }

    private async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        string message = e.ChatMessage.Message;
        string username = e.ChatMessage.Username;
        string channel = e.ChatMessage.Channel;
        string processedMessage = message;

        if (username.Equals(txtBotUsername.Text, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Command: !askai prompt
        if (toggleAskAI.Checked && message.StartsWith("!askai ", StringComparison.OrdinalIgnoreCase))
        {
            string prompt = message.Substring(6).Trim();
            if (!string.IsNullOrEmpty(prompt))
            {
                string response = await AskAIScript.GetResponse(prompt);
                if (!string.IsNullOrWhiteSpace(response))
                    client.SendMessage(channel, response);
            }
        }

        // Command: !weather location
        if (toggleWeather.Checked && message.StartsWith("!weather ", StringComparison.OrdinalIgnoreCase))
        {
            string city = message.Substring(9).Trim();
            if (!string.IsNullOrEmpty(city))
            {
                string weather = await WeatherScript.GetWeather(city);
                if (!string.IsNullOrWhiteSpace(weather))
                    client.SendMessage(channel, weather);
            }
            return;
        }

        // Passive Scripts (modify normal messages)

        if (toggleButtsbot.Checked)
        {
            string buttsMessage = await ButtsBotScript.Process(message, txtNLPApiKey.Text, username, txtBotUsername.Text);
            if (!string.IsNullOrWhiteSpace(buttsMessage))
            {
                client.SendMessage(channel, buttsMessage);
            }
        }

        if (toggleTranslate.Checked)
        {
            string translated = await TranslateScript.TryTranslate(message, username);
            if (!string.IsNullOrWhiteSpace(translated))
            {
                client.SendMessage(channel, translated);
            }
        }

        
        if (toggleClapThat.Checked)
        {
            string clapResponse = await ClapThatBotScript.Process(message, txtNLPApiKey.Text, username, txtBotUsername.Text);
            if (!string.IsNullOrWhiteSpace(clapResponse))
            {
                client.SendMessage(channel, clapResponse);
            }
        }

        if (toggleMarkovChain.Checked)
        {
            string markov = MarkovChainScript.LearnAndMaybeRespond(message, username, txtBotUsername.Text);
            if (!string.IsNullOrWhiteSpace(markov))
            {
                client.SendMessage(channel, markov);
            }
        }

        // If message changed by any passive script, send it
        if (processedMessage != message && !string.IsNullOrWhiteSpace(processedMessage))
        {
            client.SendMessage(channel, processedMessage);
        }
    }

    private class SettingsData
    {
        public string BotUsername { get; set; }
        public string ClientID { get; set; }
        public string OAuthToken { get; set; }
        public string ChannelName { get; set; }
        public string NLPApiKey { get; set; }
        public bool AskAIEnabled { get; set; }
        public bool WeatherEnabled { get; set; }
        public bool TranslateEnabled { get; set; }
        public bool ButtsbotEnabled { get; set; }
        public bool ClapThatEnabled { get; set; }
        public bool MarkovChainEnabled { get; set; }
    }
}

