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
using System.Reflection;

public partial class MainForm : Form
{
    private TwitchClient client;
    private TextBox txtBotUsername;
    private TextBox txtClientID;
    private TextBox txtOAuthToken;
    private TextBox txtChannelName;
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

    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    // Stored event handlers for clean unsubscription
    private EventHandler<OnConnectedArgs> onConnected;
    private EventHandler<OnDisconnectedEventArgs> onDisconnected;
    private EventHandler<OnConnectionErrorArgs> onConnectionError;
    private EventHandler<OnErrorEventArgs> onError;
    private EventHandler<OnLogArgs> onLog;
    private EventHandler<OnMessageReceivedArgs> onMessageReceived;

    private Button btnPinTop;
    private Button btnMinimizeTray;
    private Button btnInfo;

    public MainForm()
    {
        InitializeComponent();
        AddTopRightButtons();
        SetupTrayIcon();
        using (var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("MiniBotLauncher.MiniBotLauncher.ico"))
        {
            this.Icon = new Icon(stream);
        }
        LoadSettings();
        UpdateToggleStates();
        this.FormClosing += MainForm_FormClosing;

        ButtsBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        ClapThatBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        TranslateScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        WeatherScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        AskAIScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        MarkovChainScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };

        client = new TwitchClient();
    }

    private void AddTopRightButtons()
    {
        btnPinTop = new Button
        {
            Text = "📌",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 125, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnPinTop.FlatAppearance.BorderSize = 0;
        btnPinTop.Click += (s, e) =>
        {
            this.TopMost = !this.TopMost;
            btnPinTop.BackColor = this.TopMost ? Color.SteelBlue : Color.Transparent;
        };

        btnMinimizeTray = new Button
        {
            Text = "🗕",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 90, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnMinimizeTray.FlatAppearance.BorderSize = 0;
        btnMinimizeTray.Click += (s, e) => { this.Hide(); trayIcon.Visible = true; };

        btnInfo = new Button
        {
            Text = "ℹ️",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 55, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnInfo.FlatAppearance.BorderSize = 0;
        btnInfo.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack(); // 💡 force z-order refresh

            Form infoForm = new Form
            {
                Text = "About MiniBotLauncher",
                Size = new Size(400, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true
            };

            var label = new Label
            {
                Text = "v1.7 ©2025 Ixitxachitl",
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var attrabution = new Label
            {
                Text = "Includes Apache OpenNLP (Apache License 2.0)",
                AutoSize = true,
                Location = new Point(20, 35)
            };

            var link = new LinkLabel
            {
                Text = "https://github.com/Ixitxachitl/MiniBotLauncher",
                AutoSize = true,
                Location = new Point(20, 50)
            };
            link.LinkClicked += (ls, le) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = link.Text,
                    UseShellExecute = true
                });
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(150, 100),
                Width = 80
            };

            infoForm.Controls.Add(label);
            infoForm.Controls.Add(attrabution);
            infoForm.Controls.Add(link);
            infoForm.Controls.Add(okButton);
            infoForm.AcceptButton = okButton;

            infoForm.ShowDialog();

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        this.Controls.Add(btnPinTop);
        this.Controls.Add(btnMinimizeTray);
        this.Controls.Add(btnInfo);
        var tooltip = new ToolTip();
        tooltip.SetToolTip(btnPinTop, "Pin on Top");
        tooltip.SetToolTip(btnMinimizeTray, "Minimize to Tray");
        tooltip.SetToolTip(btnInfo, "About");
    }

    private void SetupTrayIcon()
    {
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Restore", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); trayIcon.Visible = false; });
        trayMenu.Items.Add("Exit", null, (s, e) => { trayIcon.Visible = false; Application.Exit(); });

        Icon icon;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MiniBotLauncher.MiniBotLauncher.ico"))
        {
            icon = new Icon(stream);
        }

        trayIcon = new NotifyIcon()
        {
            Text = "MiniBotLauncher",
            Icon = icon,
            ContextMenuStrip = trayMenu,
            Visible = false
        };
        trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); trayIcon.Visible = false; };
    }

    private void InitializeComponent()
    {
        this.Text = "MiniBotLauncher";
        this.Size = new Size(515, 625);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.Font = new Font("Segoe UI", 10F);

        Color foreColor = Color.White;
        Color buttonColor = Color.FromArgb(50, 50, 50);
        Color activeButtonColor = Color.FromArgb(70, 70, 70);
        Color toggleActiveColor = Color.FromArgb(0, 122, 204);

        int marginLeft = 30;
        int toggleGap = 10;
        int inputLeft = 150;
        int currentTop = 50;
        int spacing = 40;
        int toggleWidth = (500 - marginLeft * 2 - toggleGap) / 2;

        Label CreateLabel(string text)
        {
            var label = new Label
            {
                Text = text,
                Left = marginLeft,
                Top = currentTop,
                Width = 120,
                ForeColor = foreColor,
                BackColor = Color.Transparent
            };
            currentTop += spacing;
            return label;
        }

        TextBox CreateTextBox(bool passwordChar = false)
        {
            var textbox = new TextBox
            {
                Left = inputLeft,
                Top = currentTop - spacing,
                Width = 320,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = foreColor,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = passwordChar
            };
            return textbox;
        }

        Button CreateButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Left = inputLeft,
                Top = currentTop,
                Width = 155,
                Height = 40,
                BackColor = buttonColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = activeButtonColor;
            button.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, button.Width, button.Height, 10, 10));
            return button;
        }

        CheckBox CreateToggle(string text, int left)
        {
            var toggle = new CheckBox
            {
                Text = text,
                Left = left,
                Top = currentTop,
                Width = toggleWidth,
                Height = 36,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = buttonColor,
                ForeColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            toggle.FlatAppearance.BorderSize = 0;

            toggle.EnabledChanged += (s, e) =>
            {
                toggle.ForeColor = toggle.Enabled ? foreColor : Color.Gray;
            };

            toggle.CheckedChanged += (s, e) =>
            {
                toggle.BackColor = toggle.Checked ? toggleActiveColor : buttonColor;
            };

            toggle.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, toggle.Width, toggle.Height, 10, 10));
            return toggle;
        }

        Label lblBotUsername = CreateLabel("Bot Username");
        txtBotUsername = CreateTextBox();
        txtBotUsername.TextChanged += TextFields_TextChanged;

        Label lblClientID = CreateLabel("Client ID");
        txtClientID = CreateTextBox(true);
        txtClientID.TextChanged += TextFields_TextChanged;

        Label lblChannel = CreateLabel("Channel to Join");
        txtChannelName = CreateTextBox();
        txtChannelName.TextChanged += TextFields_TextChanged;

        Label lblOAuthToken = CreateLabel("OAuth Token");
        txtOAuthToken = CreateTextBox(true);
        txtOAuthToken.ReadOnly = true;
        txtOAuthToken.TabStop = false;
        txtOAuthToken.TextChanged += TextFields_TextChanged;

        btnGetToken = CreateButton("Get Token");
        btnGetToken.Top = currentTop;
        btnGetToken.Left = marginLeft + 120; // << place left button at left margin
        btnGetToken.Click += btnGetToken_Click;

        btnConnect = CreateButton("Connect");
        btnConnect.Top = currentTop;
        btnConnect.Left = marginLeft + btnGetToken.Width + toggleGap + 120; // << right next to GetToken + a small gap
        btnConnect.Click += btnConnect_Click;

        currentTop += 45;
        Label lblScripts = CreateLabel("Toggle Scripts");

        toggleAskAI = CreateToggle("AskAI", marginLeft);
        toggleWeather = CreateToggle("Weather", marginLeft + toggleWidth + toggleGap);
        currentTop += 40;
        toggleTranslate = CreateToggle("Translate", marginLeft);
        toggleButtsbot = CreateToggle("Buttsbot", marginLeft + toggleWidth + toggleGap);
        currentTop += 40;
        toggleClapThat = CreateToggle("ClapThat", marginLeft);
        toggleMarkovChain = CreateToggle("MarkovChain", marginLeft + toggleWidth + toggleGap);

        currentTop += 55;
        txtStatusLog = new TextBox
        {
            Left = marginLeft,
            Top = currentTop,
            Width = 440,
            Height = 100,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        currentTop += 110;
        lblConnectionStatus = new Label
        {
            Text = "Disconnected",
            Left = marginLeft,
            Top = currentTop,
            Width = 440,
            Height = 30,
            ForeColor = Color.Red,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.AddRange(new Control[]
        {
        lblBotUsername, txtBotUsername,
        lblClientID, txtClientID,
        lblOAuthToken, txtOAuthToken,
        btnGetToken, btnConnect,
        lblChannel, txtChannelName,
        lblScripts,
        toggleAskAI, toggleWeather,
        toggleTranslate, toggleButtsbot,
        toggleClapThat, toggleMarkovChain,
        txtStatusLog, lblConnectionStatus
        });

        DisableAllToggles();
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

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
            CleanupClient();
            // Trigger UI update after disconnect
            Client_OnDisconnected(this, null); // optional fallback
        }
        else if (!isDisconnecting)
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

        CleanupClient();

        client = new TwitchClient();
        client.AutoReListenOnException = false;

        string finalOAuth = txtOAuthToken.Text.StartsWith("oauth:") ? txtOAuthToken.Text : "oauth:" + txtOAuthToken.Text;
        ConnectionCredentials credentials = new ConnectionCredentials(txtBotUsername.Text, finalOAuth);
        client.Initialize(credentials, txtChannelName.Text);

        // Register handlers
        onConnected = Client_OnConnected;
        onDisconnected = Client_OnDisconnected;
        onConnectionError = (s, e) => Log($"Connection error: {e.Error.Message}");
        onError = (s, e) => Log($"Client error: {e.Exception.Message}");
        onLog = (s, e) => Log(e.Data);
        onMessageReceived = Client_OnMessageReceived;

        client.OnConnected += onConnected;
        client.OnDisconnected += onDisconnected;
        client.OnConnectionError += onConnectionError;
        client.OnError += onError;
        client.OnLog += onLog;
        client.OnMessageReceived += onMessageReceived;

        Log("Connecting to Twitch IRC server at wss://irc-ws.chat.twitch.tv:443");
        client.Connect();
        Log("Attempting to connect to Twitch...");
    }

    private void RegisterClientEvents()
    {
        onConnected = Client_OnConnected;
        onDisconnected = Client_OnDisconnected;
        onConnectionError = (s, e) => Log($"Connection error: {e.Error.Message}");
        onError = (s, e) => Log($"Client error: {e.Exception.Message}");
        onLog = (s, e) => Log(e.Data);
        onMessageReceived = Client_OnMessageReceived;

        client.OnConnected += onConnected;
        client.OnDisconnected += onDisconnected;
        client.OnConnectionError += onConnectionError;
        client.OnError += onError;
        client.OnLog += onLog;
        client.OnMessageReceived += onMessageReceived;
    }

    private void CleanupClient()
    {
        if (client == null)
            return;

        try
        {
            client.OnConnected -= onConnected;
            client.OnDisconnected -= onDisconnected;
            client.OnConnectionError -= onConnectionError;
            client.OnError -= onError;
            client.OnLog -= onLog;
            client.OnMessageReceived -= onMessageReceived;

            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            Log($"Error during cleanup: {ex.Message}");
        }
        finally
        {
            client = null;
        }
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
        CleanupClient();
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
        toggleButtsbot.Enabled = true;
        toggleClapThat.Enabled = true;
    }

    private void TextFields_TextChanged(object sender, EventArgs e) => UpdateToggleStates();

    private void UpdateToggleStates()
    {
        bool basicReady = IsBasicAuthValid();
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
            toggleButtsbot.Enabled = basicReady;
            toggleClapThat.Enabled = basicReady;
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

    private void Log(string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (txtStatusLog.InvokeRequired)
        {
            txtStatusLog.Invoke(new Action(() =>
            {
                if (string.IsNullOrEmpty(txtStatusLog.Text))
                    txtStatusLog.AppendText(logEntry);
                else
                    txtStatusLog.AppendText(Environment.NewLine + logEntry);
            }));
        }
        else
        {
            if (string.IsNullOrEmpty(txtStatusLog.Text))
                txtStatusLog.AppendText(logEntry);
            else
                txtStatusLog.AppendText(Environment.NewLine + logEntry);
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
            string buttsMessage = await ButtsBotScript.Process(message, username, txtBotUsername.Text);
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
            string clapResponse = await ClapThatBotScript.Process(message, username, txtBotUsername.Text);
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
        public bool AskAIEnabled { get; set; }
        public bool WeatherEnabled { get; set; }
        public bool TranslateEnabled { get; set; }
        public bool ButtsbotEnabled { get; set; }
        public bool ClapThatEnabled { get; set; }
        public bool MarkovChainEnabled { get; set; }
    }
}

