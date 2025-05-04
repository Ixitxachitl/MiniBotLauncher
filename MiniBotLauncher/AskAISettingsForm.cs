using System;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class AskAISettingsForm : Form
{
    private ComboBox cmbModel;
    private TrackBar sliderTokens;
    private Label lblTokens;
    private TextBox txtSystemMessage;
    private TextBox txtServerAddress;
    private TextBox txtServerPort;
    private Button btnConnect;
    private SettingsData settings;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    public AskAISettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "AI Settings";
        this.Size = new Size(460, 440);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        int left = 20;

        var lblServer = new Label { Text = "Server Address", Left = left, Top = 20, AutoSize = true };
        txtServerAddress = new TextBox { Left = left, Top = 45, Width = 240, Text = settings.AskAI_ServerAddress, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };

        var lblPort = new Label { Text = "Port", Left = 270, Top = 20, AutoSize = true };
        txtServerPort = new TextBox { Left = 270, Top = 45, Width = 70, Text = settings.AskAI_ServerPort.ToString(), BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };

        btnConnect = new Button
        {
            Text = "Connect",
            Left = 350,
            Top = 44,
            Width = 80,
            Height = 27,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnConnect.FlatAppearance.BorderSize = 0;
        btnConnect.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btnConnect.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnConnect.Width, btnConnect.Height, 10, 10));
        btnConnect.Click += BtnConnect_Click;

        var lblModel = new Label { Text = "Model Name", Left = left, Top = 85, AutoSize = true };
        cmbModel = new ComboBox { Left = left, Top = 110, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbModel.Items.Add(settings.AskAI_ModelName); // default
        cmbModel.SelectedItem = settings.AskAI_ModelName;

        var lblToken = new Label { Text = "Max Tokens (1–255)", Left = left, Top = 150, AutoSize = true };
        sliderTokens = new TrackBar { Left = left, Top = 175, Width = 300, Minimum = 1, Maximum = 255, Value = settings.AskAI_MaxTokens, TickFrequency = 10 };
        lblTokens = new Label { Text = settings.AskAI_MaxTokens.ToString(), Left = sliderTokens.Right + 10, Top = sliderTokens.Top + 5, AutoSize = true };
        sliderTokens.ValueChanged += (s, e) => lblTokens.Text = sliderTokens.Value.ToString();

        var lblSystem = new Label { Text = "System Message (optional)", Left = left, Top = 230, AutoSize = true };
        txtSystemMessage = new TextBox { Left = left, Top = 255, Width = 400, Height = 60, Multiline = true, Text = settings.AskAI_SystemMessage, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, ScrollBars = ScrollBars.Vertical };

        var btnOK = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Left = this.ClientSize.Width - 170,
            Top = 340,
            Width = 70,
            Height = 35,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOK.FlatAppearance.BorderSize = 0;
        btnOK.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btnOK.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnOK.Width, btnOK.Height, 10, 10));
        btnOK.Click += (s, e) =>
        {
            settings.AskAI_ModelName = cmbModel.SelectedItem?.ToString() ?? settings.AskAI_ModelName;
            settings.AskAI_MaxTokens = sliderTokens.Value;
            settings.AskAI_SystemMessage = txtSystemMessage.Text.Trim();
            settings.AskAI_ServerAddress = txtServerAddress.Text.Trim();
            settings.AskAI_ServerPort = int.TryParse(txtServerPort.Text, out int p) ? p : settings.AskAI_ServerPort;
            this.Close();
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Left = this.ClientSize.Width - 90,
            Top = 340,
            Width = 70,
            Height = 35,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btnCancel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCancel.Width, btnCancel.Height, 10, 10));

        Controls.AddRange(new Control[]
        {
            lblServer, txtServerAddress,
            lblPort, txtServerPort, btnConnect,
            lblModel, cmbModel,
            lblToken, sliderTokens, lblTokens,
            lblSystem, txtSystemMessage,
            btnOK, btnCancel
        });
    }

    private async void BtnConnect_Click(object sender, EventArgs e)
    {
        string address = txtServerAddress.Text.Trim();
        string port = txtServerPort.Text.Trim();
        if (!int.TryParse(port, out int p)) return;

        using var client = new HttpClient();
        try
        {
            string url = $"{address}:{p}/v1/models";
            string response = await client.GetStringAsync(url);
            JObject parsed = JObject.Parse(response);

            var models = new List<string>();
            foreach (var model in parsed["data"])
            {
                string id = model["id"]?.ToString();
                if (!string.IsNullOrWhiteSpace(id)) models.Add(id);
            }

            cmbModel.Items.Clear();
            cmbModel.Items.AddRange(models.ToArray());
            if (models.Count > 0) cmbModel.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error connecting to server: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}