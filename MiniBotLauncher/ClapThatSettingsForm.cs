using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class ClapThatSettingsForm : Form
{
    private TrackBar slider;
    private Label lblValue;
    private SettingsData settings;
    private TextBox txtReplacement;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    public ClapThatSettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "ClapThat Settings";
        this.Size = new Size(400, 250);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        var lbl = new Label
        {
            Text = "Reply Frequency",
            Left = 20,
            Top = 20,
            AutoSize = true
        };

        slider = new TrackBar
        {
            Left = 20,
            Top = 45,
            Width = 300,
            Minimum = 1,
            Maximum = 100,
            Value = settings.ClapThat_ReplyChancePercent,
            TickFrequency = 10
        };

        lblValue = new Label
        {
            Text = $"{slider.Value}%",
            Left = slider.Right + 10,
            Top = slider.Top + 5,
            AutoSize = true
        };

        slider.ValueChanged += (s, e) =>
        {
            lblValue.Text = $"{slider.Value}%";
        };

        var lblReplacement = new Label
        {
            Text = "Replacement Word",
            Left = 20,
            Top = 90,
            AutoSize = true
        };

        txtReplacement = new TextBox
        {
            Left = 20,
            Top = 115,
            Width = 150,
            Text = settings.ClapThat_ReplacementWord ?? "clap"
        };

        var btnOK = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Left = this.ClientSize.Width - 180,
            Top = 160,
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
            settings.ClapThat_ReplyChancePercent = slider.Value;
            settings.ClapThat_ReplacementWord = txtReplacement.Text;

            this.Close();
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Left = this.ClientSize.Width - 95,
            Top = 160,
            Width = 70,
            Height = 35,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btnCancel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCancel.Width, btnCancel.Height, 10, 10));

        this.Controls.AddRange(new Control[] { lbl, slider, lblValue, lblReplacement, txtReplacement, btnOK, btnCancel });
    }
}
