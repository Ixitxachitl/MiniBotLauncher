using System;
using System.Drawing;
using System.Windows.Forms;

public class WeatherSettingsForm : Form
{
    private TextBox txtFormat;
    private SettingsData settings;

    public WeatherSettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "Weather Settings";
        this.Size = new Size(400, 200);
        Theme.ApplyToDialog(this);

        var lbl = Theme.CreateLabel("Weather Format String", 20, 20);

        txtFormat = Theme.CreateTextBox(20, 45, 340);
        txtFormat.Text = settings.Weather_FormatString;

        var btnOK = Theme.CreateButton("OK", this.ClientSize.Width - 180, 100);
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Click += (s, e) =>
        {
            settings.Weather_FormatString = txtFormat.Text.Trim();
            this.Close();
        };

        var btnCancel = Theme.CreateButton("Cancel", this.ClientSize.Width - 95, 100);
        btnCancel.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] { lbl, txtFormat, btnOK, btnCancel });
    }
}
