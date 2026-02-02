using System;
using System.Drawing;
using System.Windows.Forms;

public class ButtsBotSettingsForm : Form
{
    private TrackBar slider;
    private Label lblValue;
    private SettingsData settings;
    private TextBox txtReplacement;

    public ButtsBotSettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "ButtsBot Settings";
        this.Size = new Size(400, 250);
        Theme.ApplyToDialog(this);

        var lbl = Theme.CreateLabel("Reply Frequency", 20, 20);

        slider = new TrackBar
        {
            Left = 20,
            Top = 45,
            Width = 300,
            Minimum = 1,
            Maximum = 100,
            Value = settings.ButtsBot_ReplyChancePercent,
            TickFrequency = 10
        };

        lblValue = Theme.CreateLabel($"{slider.Value}%", slider.Right + 10, slider.Top + 5);

        slider.ValueChanged += (s, e) =>
        {
            lblValue.Text = $"{slider.Value}%";
        };

        var lblReplacement = Theme.CreateLabel("Replacement Word", 20, 90);
        txtReplacement = Theme.CreateTextBox(20, 115, 150);
        txtReplacement.Text = settings.ButtsBot_ReplacementWord ?? "butt";

        var btnOK = Theme.CreateButton("OK", this.ClientSize.Width - 180, 160);
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Click += (s, e) =>
        {
            settings.ButtsBot_ReplyChancePercent = slider.Value;
            settings.ButtsBot_ReplacementWord = txtReplacement.Text;
            this.Close();
        };

        var btnCancel = Theme.CreateButton("Cancel", this.ClientSize.Width - 95, 160);
        btnCancel.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] { lbl, slider, lblValue, lblReplacement, txtReplacement, btnOK, btnCancel });
    }
}
