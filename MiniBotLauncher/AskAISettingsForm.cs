using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public class AskAISettingsForm : Form
{
    private TextBox txtModel;
    private TrackBar sliderTokens;
    private Label lblTokens;
    private TextBox txtSystemMessage;
    private SettingsData settings;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    public AskAISettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "AI Settings";
        this.Size = new Size(420, 360);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        int left = 20;

        var lblModel = new Label
        {
            Text = "Model Name",
            Left = left,
            Top = 20,
            AutoSize = true
        };

        txtModel = new TextBox
        {
            Left = left,
            Top = 45,
            Width = 360,
            Text = settings.AskAI_ModelName,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblToken = new Label
        {
            Text = "Max Tokens (1–255)",
            Left = left,
            Top = 85,
            AutoSize = true
        };

        sliderTokens = new TrackBar
        {
            Left = left,
            Top = 110,
            Width = 300,
            Minimum = 1,
            Maximum = 255,
            Value = settings.AskAI_MaxTokens,
            TickFrequency = 10,
            LargeChange = 10,
            SmallChange = 1
        };

        lblTokens = new Label
        {
            Text = $"{sliderTokens.Value}",
            Left = sliderTokens.Right + 10,
            Top = sliderTokens.Top + 5,
            AutoSize = true
        };

        sliderTokens.ValueChanged += (s, e) =>
        {
            lblTokens.Text = $"{sliderTokens.Value}";
        };


        var lblSystem = new Label
        {
            Text = "System Message (optional)",
            Left = left,
            Top = 170,
            AutoSize = true
        };

        txtSystemMessage = new TextBox
        {
            Left = left,
            Top = 195,
            Width = 360,
            Height = 60,
            Multiline = true,
            Text = settings.AskAI_SystemMessage,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical
        };

        var btnOK = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Left = this.ClientSize.Width - 170,
            Top = 275,
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
            settings.AskAI_ModelName = txtModel.Text.Trim();
            settings.AskAI_MaxTokens = sliderTokens.Value;
            settings.AskAI_SystemMessage = txtSystemMessage.Text.Trim();
            this.Close();
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Left = this.ClientSize.Width - 90,
            Top = 275,
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
            lblModel, txtModel,
            lblToken, sliderTokens, lblTokens,
            lblSystem, txtSystemMessage,
            btnOK, btnCancel
        });
    }
}
