using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
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
    private TwitchClient client = null!;
    private TextBox txtClientID = null!;
    private ComboBox cboChannelName = null!;
    private Button btnAddChannel = null!;
    private Button btnRemoveChannel = null!;
    private CheckBox toggleAskAI = null!;
    private CheckBox toggleWeather = null!;
    private CheckBox toggleTranslate = null!;
    private CheckBox toggleButtsbot = null!;
    private CheckBox toggleClapThat = null!;
    private CheckBox toggleMarkovChain = null!;
    private CheckBox toggleSoundAlerts = null!;
    private CheckBox toggleWalkOn = null!;
    private Button btnLogin = null!;
    private Button btnLogout = null!;
    private Button btnConnect = null!;
    private Label lblLoggedInAs = null!;
    private TextBox txtStatusLog = null!;
    private Label lblConnectionStatus = null!;
    private TextBox txtChatInput = null!;
    private Button btnSendChat = null!;
    
    // Store credentials (not shown in UI)
    private string storedUsername = "";
    private string storedOAuthToken = "";
    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "MiniBot",
        "settings.json"
    );
    private bool isDisconnecting = false;

    private NotifyIcon trayIcon = null!;
    private ContextMenuStrip trayMenu = null!;
    private List<string> ignoredUsernames = new List<string>();
    private TrackBar trackVolume = null!;

    // Stored event handlers for clean unsubscription
    private EventHandler<OnConnectedArgs>? onConnected;
    private EventHandler<OnDisconnectedEventArgs>? onDisconnected;
    private EventHandler<OnConnectionErrorArgs>? onConnectionError;
    private EventHandler<OnErrorEventArgs>? onError;
    private EventHandler<OnLogArgs>? onLog;
    private EventHandler<OnMessageReceivedArgs>? onMessageReceived;

    private Button btnPinTop = null!;
    private Button btnMinimizeTray = null!;
    private Button btnInfo = null!;

    private SettingsData settings = new SettingsData();
    private HttpListener? oauthListener;
    public MainForm()
    {
        InitializeComponent();
        AddTopRightButtons();
        SetupTrayIcon();
        using (var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("MiniBotLauncher.MiniBotLauncher.ico"))
        {
            if (stream != null)
                this.Icon = new Icon(stream);
        }
        LoadSettings();

        AskAIScript.SetConfig(
            settings.AskAI_ModelName,
            settings.AskAI_MaxTokens,
            settings.AskAI_SystemMessage,
            settings.AskAI_ServerAddress,
            settings.AskAI_ServerPort
        );

        WeatherScript.SetFormat(settings.Weather_FormatString);
        TranslateScript.SetTargetLanguage(settings.Translate_TargetLanguage);
        ButtsBotScript.SetReplyChance(settings.ButtsBot_ReplyChancePercent);
        ClapThatBotScript.SetReplyChance(settings.ClapThat_ReplyChancePercent);
        ButtsBotScript.SetReplacementWord(settings.ButtsBot_ReplacementWord);

        UpdateLoginUI();
        UpdateToggleStates();
        this.FormClosing += MainForm_FormClosing!;

        ButtsBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        ClapThatBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        TranslateScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        WeatherScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        AskAIScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        MarkovChainScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        AudioQueue.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
    }

    private void AddTopRightButtons()
    {   
        Button btnIgnoreList = new Button
        {
            Text = "📄",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 160, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Emoji", 11, FontStyle.Regular)
        };
        btnIgnoreList.FlatAppearance.BorderSize = 0;
        btnIgnoreList.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack(); // 💡 refresh Z-order

            var form = new IgnoreListForm(ignoredUsernames);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ignoredUsernames = form.GetIgnoredUsernames();
                SaveSettings();
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        btnPinTop = new Button
        {
            Text = "📌",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 125, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Emoji", 11, FontStyle.Regular)
        };
        btnPinTop.FlatAppearance.BorderSize = 0;
        btnPinTop.Click += (s, e) =>
        {
            this.TopMost = !this.TopMost;
            btnPinTop.BackColor = this.TopMost ? Color.SteelBlue : Color.Transparent;
        };

        btnMinimizeTray = new Button
        {
            Text = "_",
            Size = new Size(30, 30),
            Location = new Point(this.ClientSize.Width - 90, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Emoji", 11, FontStyle.Regular)
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
            ForeColor = Color.White,
            Font = new Font("Segoe UI Emoji", 11, FontStyle.Regular)
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
                Size = new Size(440, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };

            var label = new Label
            {
                Text = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"} ©2026 Ixitxachitl",
                AutoSize = true,
                Location = new Point(20, 20),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var attribution = new Label
            {
                Text = "Includes Apache OpenNLP (Apache License 2.0)",
                AutoSize = true,
                Location = new Point(20, 40),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var attribution2 = new Label
            {
                Text = "Includes CMUdict (BSD-licensed) for offline syllable detection",
                AutoSize = true,
                Location = new Point(20, 60),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            var link = new LinkLabel
            {
                Text = "https://github.com/Ixitxachitl/MiniBotLauncher",
                AutoSize = true,
                Location = new Point(20, 80),
                LinkColor = Color.SteelBlue
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
                Location = new Point((infoForm.ClientSize.Width - 80) / 2, 110),
                Width = 80,
                Height = 40,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            okButton.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, okButton.Width, okButton.Height, 10, 10));

            infoForm.Controls.Add(label);
            infoForm.Controls.Add(attribution);
            infoForm.Controls.Add(attribution2);
            infoForm.Controls.Add(link);
            infoForm.Controls.Add(okButton);
            infoForm.AcceptButton = okButton;

            infoForm.ShowDialog();

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        this.Controls.Add(btnIgnoreList);
        this.Controls.Add(btnPinTop);
        this.Controls.Add(btnMinimizeTray);
        this.Controls.Add(btnInfo);

        var tooltip = new ToolTip();
        tooltip.SetToolTip(btnIgnoreList, "Manage Ignored Users");
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
            icon = stream != null ? new Icon(stream) : SystemIcons.Application;
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
        this.Size = new Size(515, 660);
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
                FlatStyle = FlatStyle.Flat,
                TabStop = false
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
                Top = currentTop  - 5,
                Width = toggleWidth,
                Height = 36,
                Appearance = Appearance.Button,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = buttonColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat
            };
            toggle.FlatAppearance.BorderSize = 0;

            toggle.CheckedChanged += (s, e) =>
            {
                toggle.BackColor = toggle.Checked ? toggleActiveColor : buttonColor;
            };

            toggle.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, toggle.Width, toggle.Height, 10, 10));
            return toggle;
        }

        // Client ID field
        Label lblClientID = CreateLabel("Client ID");
        txtClientID = CreateTextBox(true);
        txtClientID.TextChanged += TextFields_TextChanged!;

        // Channel to Join field
        Label lblChannel = CreateLabel("Channel to Join");
        cboChannelName = new ComboBox
        {
            Left = inputLeft,
            Top = currentTop - spacing,
            Width = 250,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cboChannelName.TextChanged += TextFields_TextChanged!;
        cboChannelName.SelectedIndexChanged += TextFields_TextChanged!;

        btnAddChannel = new Button
        {
            Text = "+",
            Left = cboChannelName.Right + 5,
            Top = currentTop - spacing - 2,
            Width = 30,
            Height = 26,
            BackColor = buttonColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat
        };
        btnAddChannel.FlatAppearance.BorderSize = 0;
        btnAddChannel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnAddChannel.Width, btnAddChannel.Height, 8, 8));
        btnAddChannel.Click += (s, e) =>
        {
            string channel = cboChannelName.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(channel) && !cboChannelName.Items.Contains(channel))
            {
                cboChannelName.Items.Add(channel);
                cboChannelName.SelectedItem = channel;
                SaveSettings();
                Log($"Added channel: {channel}");
            }
        };

        btnRemoveChannel = new Button
        {
            Text = "-",
            Left = btnAddChannel.Right + 5,
            Top = currentTop - spacing - 2,
            Width = 30,
            Height = 26,
            BackColor = buttonColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat
        };
        btnRemoveChannel.FlatAppearance.BorderSize = 0;
        btnRemoveChannel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnRemoveChannel.Width, btnRemoveChannel.Height, 8, 8));
        btnRemoveChannel.Click += (s, e) =>
        {
            if (cboChannelName.SelectedItem != null)
            {
                string? removed = cboChannelName.SelectedItem.ToString();
                cboChannelName.Items.Remove(cboChannelName.SelectedItem);
                if (cboChannelName.Items.Count > 0)
                    cboChannelName.SelectedIndex = 0;
                else
                    cboChannelName.Text = "";
                SaveSettings();
                Log($"Removed channel: {removed}");
            }
        };

        this.Controls.Add(btnAddChannel);
        this.Controls.Add(btnRemoveChannel);

        // Login status label
        Label lblLoginStatus = CreateLabel("Account");
        lblLoggedInAs = new Label
        {
            Text = "Not logged in",
            Left = inputLeft,
            Top = currentTop - spacing,
            Width = 320,
            ForeColor = Color.Gray,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };
        this.Controls.Add(lblLoggedInAs);

        // Login button
        btnLogin = CreateButton("Login");
        btnLogin.Top = currentTop;
        btnLogin.Left = marginLeft;
        btnLogin.Width = 100;
        btnLogin.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, btnLogin.Width, btnLogin.Height, 10, 10));
        btnLogin.Click += btnLogin_Click!;

        // Logout button
        btnLogout = CreateButton("Logout");
        btnLogout.Top = currentTop;
        btnLogout.Left = btnLogin.Right + 10;
        btnLogout.Width = 100;
        btnLogout.Enabled = false;
        btnLogout.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, btnLogout.Width, btnLogout.Height, 10, 10));
        btnLogout.Click += btnLogout_Click!;

        // Connect button
        btnConnect = CreateButton("Connect");
        btnConnect.Top = currentTop;
        btnConnect.Left = 315;
        btnConnect.Click += btnConnect_Click!;

        lblConnectionStatus = new Label
        {
            Text = "🔌", 
            Top = btnConnect.Top + 8,
            Left = btnConnect.Left - 32,
            AutoSize = true,
            ForeColor = Color.Red,
            BackColor = Color.Transparent
        };
        this.Controls.Add(lblConnectionStatus);
        lblConnectionStatus.BringToFront();

        currentTop += 55;
        Label lblScripts = CreateLabel("Toggle Scripts");

        toggleAskAI = CreateToggle("AskAI", marginLeft);
        toggleAskAI.Width -= 35; // Make it narrower to fit the new button

        // Restore rounded corners (both sides) on narrower toggle
        toggleAskAI.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleAskAI.Width, toggleAskAI.Height, 10, 10));

        Button btnSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleAskAI.Right + 5, toggleAskAI.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnSettings.FlatAppearance.BorderSize = 0;
        btnSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new AskAISettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
            }

            AskAIScript.SetConfig(
                settings.AskAI_ModelName,
                settings.AskAI_MaxTokens,
                settings.AskAI_SystemMessage,
                settings.AskAI_ServerAddress,
                settings.AskAI_ServerPort
            );

            this.TopMost = wasTopMost;
            this.BringToFront();
        };
        this.Controls.Add(btnSettings);

        toggleWeather = CreateToggle("Weather", marginLeft + toggleWidth + toggleGap);
        toggleWeather.Width -= 35; // Make it narrower to fit the new button

        // Restore rounded corners (both sides) on narrower toggle
        toggleWeather.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleWeather.Width, toggleWeather.Height, 10, 10));

        Button btnWeatherSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleWeather.Right + 5, toggleWeather.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnWeatherSettings.FlatAppearance.BorderSize = 0;
        btnWeatherSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new WeatherSettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                WeatherScript.SetFormat(settings.Weather_FormatString);
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        this.Controls.Add(btnWeatherSettings);

        currentTop += 40;
        toggleTranslate = CreateToggle("Translate", marginLeft);
        toggleTranslate.Width -= 35; // Make it narrower to fit the new button

        // Restore rounded corners (both sides) on narrower toggle
        toggleTranslate.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleTranslate.Width, toggleTranslate.Height, 10, 10));

        Button btnTranslateSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleTranslate.Right + 5, toggleTranslate.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnTranslateSettings.FlatAppearance.BorderSize = 0;
        btnTranslateSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new TranslateSettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                TranslateScript.SetTargetLanguage(settings.Translate_TargetLanguage);
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };
        this.Controls.Add(btnTranslateSettings);

        toggleButtsbot = CreateToggle("Buttsbot", marginLeft + toggleWidth + toggleGap);
        toggleButtsbot.Width -= 35; // Make it narrower to fit the new button

        // Restore rounded corners (both sides) on narrower toggle
        toggleButtsbot.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleButtsbot.Width, toggleButtsbot.Height, 10, 10));

        Button btnButtsbotSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleButtsbot.Right + 5, toggleButtsbot.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnButtsbotSettings.FlatAppearance.BorderSize = 0;
        btnButtsbotSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new ButtsBotSettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                ButtsBotScript.SetReplyChance(settings.ButtsBot_ReplyChancePercent);
                ButtsBotScript.SetReplacementWord(settings.ButtsBot_ReplacementWord);
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };
        this.Controls.Add(btnButtsbotSettings);

        currentTop += 40;
        toggleClapThat = CreateToggle("ClapThat", marginLeft);
        toggleClapThat.Width -= 35; // Make it narrower to fit the new button

        // Restore rounded corners (both sides) on narrower toggle
        toggleClapThat.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleClapThat.Width, toggleClapThat.Height, 10, 10));
        Button btnClapthatSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleClapThat.Right + 5, toggleClapThat.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnClapthatSettings.FlatAppearance.BorderSize = 0;
        btnClapthatSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new ClapThatSettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                ClapThatBotScript.SetReplyChance(settings.ClapThat_ReplyChancePercent);
                ClapThatBotScript.SetReplacementWord(settings.ClapThat_ReplacementWord);
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };
        this.Controls.Add(btnClapthatSettings);

        toggleMarkovChain = CreateToggle("MarkovChain", marginLeft + toggleWidth + toggleGap);
        toggleMarkovChain.Width -= 35; // Adjust for settings icon
        toggleMarkovChain.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleMarkovChain.Width, toggleMarkovChain.Height, 10, 10));

        Button btnMarkovSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleMarkovChain.Right + 5, toggleMarkovChain.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnMarkovSettings.FlatAppearance.BorderSize = 0;

        btnMarkovSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new MarkovChainSettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        this.Controls.Add(btnMarkovSettings);

        currentTop += 40;

        // Add Sound Alerts toggle
        toggleSoundAlerts = CreateToggle("Sound Alerts", marginLeft);
        toggleSoundAlerts.Width -= 35;
        toggleSoundAlerts.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleSoundAlerts.Width, toggleSoundAlerts.Height, 10, 10));

        Button btnSoundAlertsSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleSoundAlerts.Right + 5, toggleSoundAlerts.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnSoundAlertsSettings.FlatAppearance.BorderSize = 0;
        btnSoundAlertsSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new SoundAlertsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                SoundAlerts.SetSoundMappings(settings.SoundAlertMappings);
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        // Add Walk-On toggle next to Sound Alerts
        toggleWalkOn = CreateToggle("Walk-On", toggleSoundAlerts.Right + 45);
        toggleWalkOn.Width -= 35;
        toggleWalkOn.Region = Region.FromHrgn(CreateRoundRectRgn(
            0, 0, toggleWalkOn.Width, toggleWalkOn.Height, 10, 10));

        Button btnWalkOnSettings = new Button
        {
            Text = "⚙️",
            Size = new Size(30, 30),
            Location = new Point(toggleWalkOn.Right + 5, toggleWalkOn.Top + 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White
        };
        btnWalkOnSettings.FlatAppearance.BorderSize = 0;
        btnWalkOnSettings.Click += (s, e) =>
        {
            bool wasTopMost = this.TopMost;
            this.TopMost = false;
            this.SendToBack();

            var form = new WalkOnSettingsForm(settings);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveSettings();
                WalkOnScript.SetSoundMappings(settings.WalkOnSoundMappings);
            }

            this.TopMost = wasTopMost;
            this.BringToFront();
        };

        // Volume slider
        trackVolume = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            TickStyle = TickStyle.None,
            Width = 410,
            Left = marginLeft,
            Top = currentTop + 40,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        trackVolume.Scroll += (s, e) =>
        {
            AudioQueue.SetVolume(trackVolume.Value / 100f);
            SaveSettings();
        };

        // Stop button
        Button btnStopAlerts = new Button
        {
            Text = "⏹️",
            Size = new Size(30, 30),
            Location = new Point(trackVolume.Right, trackVolume.Top - 5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Enabled = false
        };
        AudioQueue.OnPlaybackStateChanged += (isPlaying) =>
        {
            btnStopAlerts.Invoke(() => btnStopAlerts.Enabled = isPlaying);
        };
        btnStopAlerts.FlatAppearance.BorderSize = 0;
        btnStopAlerts.Click += (s, e) =>
        {
            AudioQueue.StopAll();
            btnStopAlerts.Enabled = false;
        };

        // Enable/disable stop button during playback
        SoundAlerts.OnPlaybackStateChanged = (isPlaying) =>
        {
            btnStopAlerts.Invoke(() => btnStopAlerts.Enabled = isPlaying);
        };

        currentTop = trackVolume.Bottom + 10;

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

        currentTop = txtStatusLog.Bottom + 5;

        // Chat input field
        txtChatInput = new TextBox
        {
            Left = marginLeft,
            Top = currentTop,
            Width = 355,
            Height = 25,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Type a message...",
            Enabled = false
        };
        txtChatInput.KeyDown += TxtChatInput_KeyDown;

        btnSendChat = new Button
        {
            Text = "Send",
            Left = txtChatInput.Right + 5,
            Top = currentTop,
            Width = 80,
            Height = 25,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        btnSendChat.FlatAppearance.BorderSize = 0;
        btnSendChat.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnSendChat.Width, btnSendChat.Height, 8, 8));
        btnSendChat.Click += BtnSendChat_Click;

        Controls.AddRange(new Control[]
        {
            lblClientID, txtClientID,
            lblChannel, cboChannelName,
            lblLoginStatus, lblLoggedInAs,
            btnLogin, btnLogout, btnConnect,
            lblScripts,
            toggleAskAI, toggleWeather,
            toggleTranslate, toggleButtsbot,
            toggleClapThat, toggleMarkovChain,
            toggleSoundAlerts, btnSoundAlertsSettings,
            toggleWalkOn, btnWalkOnSettings,
            trackVolume, btnStopAlerts,
            txtStatusLog, lblConnectionStatus,
            txtChatInput, btnSendChat
        });
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
            FlatStyle = FlatStyle.Flat
        };
        toggle.FlatAppearance.BorderSize = 1;
        toggle.CheckedChanged += toggleScript_CheckedChanged!;
        return toggle;
    }

    private void btnConnect_Click(object? sender, EventArgs e)
    {
        if (client != null && client.IsConnected)
        {
            Log("Disconnecting from Twitch...");
            isDisconnecting = true;
            CleanupClient();
            // Trigger UI update after disconnect
            Client_OnDisconnected(this, new OnDisconnectedEventArgs()); // optional fallback
        }
        else if (!isDisconnecting)
        {
            ConnectToTwitch();
        }
    }

    private void btnLogin_Click(object? sender, EventArgs e) => StartOAuthFlow(txtClientID.Text);
    
    private void btnLogout_Click(object? sender, EventArgs e)
    {
        storedUsername = "";
        storedOAuthToken = "";
        UpdateLoginUI();
        SaveSettings();
        Log("Logged out from Twitch.");
    }

    private void UpdateLoginUI()
    {
        bool isLoggedIn = !string.IsNullOrWhiteSpace(storedOAuthToken) && !string.IsNullOrWhiteSpace(storedUsername);
        
        if (isLoggedIn)
        {
            lblLoggedInAs.Text = $"Logged in as: {storedUsername}";
            lblLoggedInAs.ForeColor = Color.LightGreen;
            btnLogin.Enabled = false;
            btnLogout.Enabled = true;
        }
        else
        {
            lblLoggedInAs.Text = "Not logged in";
            lblLoggedInAs.ForeColor = Color.Gray;
            btnLogin.Enabled = true;
            btnLogout.Enabled = false;
        }
        
        UpdateToggleStates();
    }

    private async Task<string?> ValidateTokenAndGetUsername(string token)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            
            var response = await httpClient.GetAsync("https://api.twitch.tv/helix/users");
            
            if (!response.IsSuccessStatusCode)
            {
                // Try the validate endpoint for more info
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
                var validateResponse = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
                
                if (validateResponse.IsSuccessStatusCode)
                {
                    string validateJson = await validateResponse.Content.ReadAsStringAsync();
                    using var validateDoc = JsonDocument.Parse(validateJson);
                    if (validateDoc.RootElement.TryGetProperty("login", out var loginProp))
                    {
                        return loginProp.GetString();
                    }
                }
                return null;
            }
            
            // Need Client-ID header for Helix API
            httpClient.DefaultRequestHeaders.Add("Client-ID", txtClientID.Text);
            response = await httpClient.GetAsync("https://api.twitch.tv/helix/users");
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");
                if (data.GetArrayLength() > 0)
                {
                    return data[0].GetProperty("login").GetString();
                }
            }
            
            // Fallback to validate endpoint
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
            var fallbackResponse = await httpClient.GetAsync("https://id.twitch.tv/oauth2/validate");
            if (fallbackResponse.IsSuccessStatusCode)
            {
                string fallbackJson = await fallbackResponse.Content.ReadAsStringAsync();
                using var fallbackDoc = JsonDocument.Parse(fallbackJson);
                if (fallbackDoc.RootElement.TryGetProperty("login", out var loginProp))
                {
                    return loginProp.GetString();
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error validating token: {ex.Message}");
            return null;
        }
    }

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
                     $"&scope=chat:read+chat:edit" +
                     $"&force_verify=true";

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"Error launching OAuth page: {ex.Message}");
        }

        try
        {
            if (oauthListener != null)
            {
                try
                {
                    oauthListener.Abort(); // Force-close any pending connections
                }
                catch { }
                oauthListener = null;
                Thread.Sleep(100); // Give the OS time to release the port
            }

            oauthListener = new HttpListener();
            oauthListener.Prefixes.Add("http://localhost:8750/");
            oauthListener.Start();
            Log("Listening for OAuth callback...");

            // Start first listener
            oauthListener.BeginGetContext(OnOAuthCallback, null);
        }
        catch (System.Net.HttpListenerException ex)
        {
            Log($"Error starting OAuth listener: {ex.Message}");
            MessageBox.Show(
                "Port 8750 is already in use. Please close any other instances of MiniBotLauncher and try again.",
                "OAuth Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OnOAuthCallback(IAsyncResult result)
    {
        try
        {
            if (oauthListener == null) return;
            var context = oauthListener.EndGetContext(result);

            if (context.Request.Url?.AbsolutePath == "/callback/")
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
                        document.body.innerHTML = '<h1>Token received! Closing...</h1>';
                        setTimeout(() => window.close(), 500);
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
                oauthListener?.BeginGetContext(OnOAuthCallback, null);
            }
            else if (context.Request.Url?.AbsolutePath == "/token/")
            {
                Log("Receiving OAuth token POST...");

                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    var parsed = System.Web.HttpUtility.ParseQueryString(body);
                    string? token = parsed["access_token"];

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        Log("OAuth token captured, fetching username...");
                        
                        // Fetch username from Twitch API
                        _ = Task.Run(async () =>
                        {
                            string? username = await ValidateTokenAndGetUsername(token);
                            
                            Invoke(new Action(() =>
                            {
                                if (!string.IsNullOrWhiteSpace(username))
                                {
                                    storedOAuthToken = token;
                                    storedUsername = username;
                                    UpdateLoginUI();
                                    SaveSettings();
                                    Log($"Logged in as: {username}");
                                }
                                else
                                {
                                    Log("Failed to get username from Twitch API. Token may be invalid.");
                                    MessageBox.Show("Failed to validate token with Twitch. Please try again.", 
                                        "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }));
                        });
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
            Log("Missing credentials or channel name. Cannot connect.");
            return;
        }

        CleanupClient();

        client = new TwitchClient();
        client.AutoReListenOnException = false;

        string finalOAuth = storedOAuthToken.StartsWith("oauth:") ? storedOAuthToken : "oauth:" + storedOAuthToken;
        ConnectionCredentials credentials = new ConnectionCredentials(storedUsername, finalOAuth);
        client.Initialize(credentials, cboChannelName.Text);

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
            client = null!;
        }
    }


    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        Invoke(new Action(() =>
        {
            Log("Connected to Twitch!");
            lblConnectionStatus.Text = "🔌";
            lblConnectionStatus.ForeColor = Color.Green;
            btnConnect.Text = "Disconnect";

            MarkovChainScript.SetChannel(cboChannelName.Text);
            SaveSettings();

            // Disable editing fields while connected
            txtClientID.Enabled = false;
            cboChannelName.Enabled = false;
            btnAddChannel.Enabled = false;
            btnRemoveChannel.Enabled = false;
            btnLogin.Enabled = false;
            btnLogout.Enabled = false;

            // Enable chat input
            txtChatInput.Enabled = true;
            btnSendChat.Enabled = true;
        }));
    }

    private void Client_OnDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        Invoke(new Action(() =>
        {
            Log("Disconnected from Twitch.");
            lblConnectionStatus.Text = "🔌";
            lblConnectionStatus.ForeColor = Color.Red;
            btnConnect.Text = "Connect";

            // Re-enable fields
            txtClientID.Enabled = true;
            cboChannelName.Enabled = true;
            btnAddChannel.Enabled = true;
            btnRemoveChannel.Enabled = true;
            btnLogin.Enabled = true;
            btnLogout.Enabled = true;

            // Disable chat input
            txtChatInput.Enabled = false;
            btnSendChat.Enabled = false;

            // 🛑 Prevent reconnect: force cleanup and null out the client
            if (client != null)
            {
                client.OnConnected -= Client_OnConnected!;
                client.OnDisconnected -= Client_OnDisconnected!;
                client.OnConnectionError -= onConnectionError;
                client.OnError -= onError;
                client.OnLog -= onLog;
                client.OnMessageReceived -= onMessageReceived;

                client.Disconnect(); // Just to be safe — fully close it
                client = null!;
            }

            isDisconnecting = false;
        }));
    }

    private void LoadSettings()
    {
        if (File.Exists(SettingsFile))
        {
            try
            {
                string json = File.ReadAllText(SettingsFile);
                settings = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
            }
            catch (Exception ex)
            {
                Log($"Failed to load settings: {ex.Message}");
                settings = new SettingsData(); // fallback to default
            }
        }

        // Ensure no nulls for collections
        settings ??= new SettingsData();
        settings.IgnoredUsernames ??= new List<string>();
        settings.SoundAlertMappings ??= new Dictionary<string, string>();
        settings.WalkOnSoundMappings ??= new Dictionary<string, string>();
        settings.ChannelList ??= new List<string>();

        // Update UI and stored credentials
        txtClientID.Text = settings.ClientID ?? "";
        
        // Load channel list into ComboBox
        cboChannelName.Items.Clear();
        foreach (var channel in settings.ChannelList)
        {
            cboChannelName.Items.Add(channel);
        }
        // Select the last used channel, or first in list
        if (!string.IsNullOrWhiteSpace(settings.ChannelName) && cboChannelName.Items.Contains(settings.ChannelName))
            cboChannelName.SelectedItem = settings.ChannelName;
        else if (cboChannelName.Items.Count > 0)
            cboChannelName.SelectedIndex = 0;
        else
            cboChannelName.Text = settings.ChannelName ?? "";
        storedUsername = settings.BotUsername ?? "";
        storedOAuthToken = settings.OAuthToken ?? "";

        // Sync toggles
        toggleAskAI.Checked = settings.AskAIEnabled;
        toggleWeather.Checked = settings.WeatherEnabled;
        toggleTranslate.Checked = settings.TranslateEnabled;
        toggleButtsbot.Checked = settings.ButtsbotEnabled;
        toggleClapThat.Checked = settings.ClapThatEnabled;
        toggleMarkovChain.Checked = settings.MarkovChainEnabled;
        toggleSoundAlerts.Checked = settings.SoundAlertsEnabled;
        toggleWalkOn.Checked = settings.WalkOnEnabled;

        // Sync script settings
        AskAIScript.SetConfig(
            settings.AskAI_ModelName,
            settings.AskAI_MaxTokens,
            settings.AskAI_SystemMessage,
            settings.AskAI_ServerAddress,
            settings.AskAI_ServerPort
        );
        AskAIScript.SetCommandTrigger(settings.AskAI_CommandTrigger);

        WeatherScript.SetFormat(settings.Weather_FormatString);
        TranslateScript.SetTargetLanguage(settings.Translate_TargetLanguage);
        ButtsBotScript.SetReplyChance(settings.ButtsBot_ReplyChancePercent);
        ButtsBotScript.SetReplacementWord(settings.ButtsBot_ReplacementWord);
        ClapThatBotScript.SetReplyChance(settings.ClapThat_ReplyChancePercent);
        ClapThatBotScript.SetReplacementWord(settings.ClapThat_ReplacementWord);
        SoundAlerts.SetSoundMappings(settings.SoundAlertMappings);
        WalkOnScript.SetSoundMappings(settings.WalkOnSoundMappings);
        WalkOnScript.SetLastKnownStreamStart(settings.WalkOnLastStreamStart);
        MarkovChainScript.SetMessageInterval(settings.Markov_MessageInterval);
        MarkovChainScript.SetBannedWords(settings.Markov_BannedWords);

        // Sync debug logging
        AskAIScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        WeatherScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        TranslateScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        ButtsBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        ClapThatBotScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        MarkovChainScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        SoundAlerts.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };
        WalkOnScript.DebugLog = async (msg) => { Log(msg); await Task.CompletedTask; };

        // Sync ignored list
        ignoredUsernames = new List<string>(settings.IgnoredUsernames);

        // Update login UI
        UpdateLoginUI();

        trackVolume.Value = Math.Clamp(settings.SoundAlertsVolume, trackVolume.Minimum, trackVolume.Maximum);
        AudioQueue.SetVolume(trackVolume.Value / 100f);
    }

    private void SaveSettings()
    {
        // Just update the existing instance
        settings.BotUsername = storedUsername;
        settings.ClientID = txtClientID.Text;
        settings.OAuthToken = storedOAuthToken;
        settings.ChannelName = cboChannelName.Text;
        settings.ChannelList = cboChannelName.Items.Cast<string>().ToList();
        settings.AskAIEnabled = toggleAskAI.Checked;
        settings.WeatherEnabled = toggleWeather.Checked;
        settings.TranslateEnabled = toggleTranslate.Checked;
        settings.ButtsbotEnabled = toggleButtsbot.Checked;
        settings.ClapThatEnabled = toggleClapThat.Checked;
        settings.MarkovChainEnabled = toggleMarkovChain.Checked;
        settings.IgnoredUsernames = ignoredUsernames;
        settings.SoundAlertsEnabled = toggleSoundAlerts.Checked;
        settings.WalkOnEnabled = toggleWalkOn.Checked;
        settings.SoundAlertsVolume = (int)(AudioQueue.GetVolume() * 100);
        settings.WalkOnSoundMappings = WalkOnScript.GetSoundMappings();
        settings.WalkOnLastStreamStart = WalkOnScript.GetLastKnownStreamStart();

        try
        {
            string? directory = Path.GetDirectoryName(SettingsFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
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

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveSettings();
        CleanupClient();
    }

    private void TextFields_TextChanged(object? sender, EventArgs e) => UpdateToggleStates();

    private void UpdateToggleStates()
    {
        bool isLoggedIn = !string.IsNullOrWhiteSpace(storedOAuthToken) && !string.IsNullOrWhiteSpace(storedUsername);
        bool basicReady = IsBasicAuthValid();
        bool loginReady = !string.IsNullOrWhiteSpace(txtClientID.Text) && !isLoggedIn;

        btnConnect.Enabled = basicReady;
        btnLogin.Enabled = loginReady;

        // Toggles are always enabled so users can configure before connecting
    }

    private void BtnSendChat_Click(object? sender, EventArgs e) => SendChatMessage();

    private void TxtChatInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true; // Prevent the "ding" sound
            SendChatMessage();
        }
    }

    private void SendChatMessage()
    {
        if (client == null || !client.IsConnected)
        {
            Log("Cannot send message: Not connected to Twitch.");
            return;
        }

        string message = txtChatInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return;

        try
        {
            client.SendMessage(cboChannelName.Text, message);
            Log($"[You]: {message}");
            txtChatInput.Clear();
        }
        catch (Exception ex)
        {
            Log($"Error sending message: {ex.Message}");
        }
    }

    private bool IsBasicAuthValid() =>
        !string.IsNullOrWhiteSpace(storedUsername) &&
        !string.IsNullOrWhiteSpace(txtClientID.Text) &&
        !string.IsNullOrWhiteSpace(storedOAuthToken) &&
        !string.IsNullOrWhiteSpace(cboChannelName.Text);

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

    private void toggleScript_CheckedChanged(object? sender, EventArgs e)
    {
        var checkbox = sender as CheckBox;
        if (checkbox == null) return;

        checkbox.BackColor = checkbox.Checked ? Color.LightGreen : Color.LightGray;
    }

    private async void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        string message = e.ChatMessage.Message;
        string username = e.ChatMessage.Username.ToLowerInvariant();
        if (ignoredUsernames.Contains(username))
        {
            Log($"Ignored message from {username}");
            return;
        }
        string channel = e.ChatMessage.Channel;
        string processedMessage = message;

        if (username.Equals(storedUsername, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string lowerMessage = message.ToLowerInvariant();
        if (lowerMessage.Contains("http") || lowerMessage.Contains(".com") || lowerMessage.Contains(".net") || lowerMessage.Contains(".org"))
        {
            Log("Ignored link-containing message.");
            return;
        }

        if (toggleSoundAlerts.Checked)
        {
            SoundAlerts.Enabled = true;
            await SoundAlerts.TryHandleMessage(message);
        }
        else
        {
            SoundAlerts.Enabled = false;
        }

        if (toggleWalkOn.Checked)
        {
            WalkOnScript.Enabled = true;
            string? newStart = await WalkOnScript.TryPlayWalkOn(
                username,
                settings.ChannelName.ToLowerInvariant(),
                settings.ClientID,
                settings.OAuthToken.Replace("oauth:", "")
            );

            if (!string.IsNullOrEmpty(newStart))
            {
                settings.WalkOnLastStreamStart = newStart;
                WalkOnScript.SetLastKnownStreamStart(newStart);
                SaveSettings();
            }
        }
        else
        {
            WalkOnScript.Enabled = false;
        }

        // Command: AskAI (configurable trigger)
        string askAiTrigger = AskAIScript.GetCommandTrigger();
        if (toggleAskAI.Checked && message.StartsWith(askAiTrigger + " ", StringComparison.OrdinalIgnoreCase))
        {
            string prompt = message.Substring(askAiTrigger.Length).Trim();
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
            string? buttsMessage = await ButtsBotScript.Process(message, username);
            if (!string.IsNullOrWhiteSpace(buttsMessage))
            {
                client.SendMessage(channel, buttsMessage);
            }
        }

        if (toggleTranslate.Checked)
        {
            string? translated = await TranslateScript.TryTranslate(message, username);
            if (!string.IsNullOrWhiteSpace(translated))
            {
                client.SendMessage(channel, translated);
            }
        }

        
        if (toggleClapThat.Checked)
        {
            string? clapResponse = await ClapThatBotScript.Process(message, username, storedUsername);
            if (!string.IsNullOrWhiteSpace(clapResponse))
            {
                client.SendMessage(channel, clapResponse);
            }
        }

        if (toggleMarkovChain.Checked)
        {
            MarkovChainScript.SetChannel(channel);
            string? markov = MarkovChainScript.LearnAndMaybeRespond(message, username, storedUsername);
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
}

