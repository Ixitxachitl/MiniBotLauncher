using System;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
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

    public AskAISettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "AI Settings";
        this.Size = new Size(460, 440);
        Theme.ApplyToDialog(this);

        int left = 20;

        var lblServer = Theme.CreateLabel("Server Address", left, 20);
        txtServerAddress = Theme.CreateTextBox(left, 45, 240);
        txtServerAddress.Text = settings.AskAI_ServerAddress;

        var lblPort = Theme.CreateLabel("Port", 270, 20);
        txtServerPort = Theme.CreateTextBox(270, 45, 70);
        txtServerPort.Text = settings.AskAI_ServerPort.ToString();

        btnConnect = Theme.CreateButton("Connect", 350, 44, 80, 27);
        btnConnect.Click += BtnConnect_Click;

        var lblModel = Theme.CreateLabel("Model Name", left, 85);
        cmbModel = Theme.CreateComboBox(left, 110, 360);
        cmbModel.Items.Add(settings.AskAI_ModelName);
        cmbModel.SelectedItem = settings.AskAI_ModelName;

        var lblToken = Theme.CreateLabel("Max Tokens (1–255)", left, 150);
        sliderTokens = new TrackBar { Left = left, Top = 175, Width = 300, Minimum = 1, Maximum = 255, Value = settings.AskAI_MaxTokens, TickFrequency = 10 };
        lblTokens = Theme.CreateLabel(settings.AskAI_MaxTokens.ToString(), sliderTokens.Right + 10, sliderTokens.Top + 5);
        sliderTokens.ValueChanged += (s, e) => lblTokens.Text = sliderTokens.Value.ToString();

        var lblSystem = Theme.CreateLabel("System Message (optional)", left, 230);
        txtSystemMessage = Theme.CreateTextBox(left, 255, 400, true, 60);
        txtSystemMessage.Text = settings.AskAI_SystemMessage;
        txtSystemMessage.ScrollBars = ScrollBars.Vertical;

        var btnOK = Theme.CreateButton("OK", this.ClientSize.Width - 170, 340);
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Click += (s, e) =>
        {
            settings.AskAI_ModelName = cmbModel.SelectedItem?.ToString() ?? settings.AskAI_ModelName;
            settings.AskAI_MaxTokens = sliderTokens.Value;
            settings.AskAI_SystemMessage = txtSystemMessage.Text.Trim();
            settings.AskAI_ServerAddress = txtServerAddress.Text.Trim();
            settings.AskAI_ServerPort = int.TryParse(txtServerPort.Text, out int p) ? p : settings.AskAI_ServerPort;
            this.Close();
        };

        var btnCancel = Theme.CreateButton("Cancel", this.ClientSize.Width - 90, 340);
        btnCancel.DialogResult = DialogResult.Cancel;

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

    private async void BtnConnect_Click(object? sender, EventArgs e)
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
            var data = parsed["data"];
            if (data != null)
            {
                foreach (var model in data)
                {
                    string? id = model["id"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(id)) models.Add(id);
                }
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