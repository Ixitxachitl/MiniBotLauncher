using System;
using System.Drawing;
using System.Windows.Forms;

public class WeatherSettingsForm : Form
{
    private ComboBox cboFormat;
    private TextBox txtCustomFormat;
    private Label lblCustomFormat;
    private Button btnFormatHelp;
    private SettingsData settings;

    // Format options with their display text and actual values
    private readonly (string Display, string Value)[] formatOptions = new[]
    {
        ("1 - Basic: 🌦 +11⁰C", "1"),
        ("2 - With details: 🌦   🌡️+11°C 🌬️↓4km/h", "2"),
        ("3 - Location + basic: Nuremberg: 🌦 +11⁰C", "3"),
        ("4 - Location + details: Nuremberg: 🌦   🌡️+11°C 🌬️↓4km/h", "4"),
        ("Custom format (use % notation)", "custom")
    };

    private const string FormatHelpText = @"Custom Format %-Notation:

  %c    Weather condition (emoji)
  %C    Weather condition (text)
  %x    Weather condition (plain-text symbol)
  %h    Humidity
  %t    Temperature (Actual)
  %f    Temperature (Feels Like)
  %w    Wind
  %l    Location
  %m    Moon phase 🌑🌒🌓🌔🌕🌖🌗🌘
  %M    Moon day
  %p    Precipitation (mm/3 hours)
  %P    Pressure (hPa)
  %u    UV index (1-12)

  %D    Dawn*
  %S    Sunrise*
  %z    Zenith*
  %s    Sunset*
  %d    Dusk*
  %T    Current time*
  %Z    Local timezone

  * Times shown in local timezone

Example: %l: %c %t (feels like %f)";

    public WeatherSettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "Weather Settings";
        this.Size = new Size(500, 280);
        Theme.ApplyToDialog(this);

        var lbl = Theme.CreateLabel("Weather Format", 20, 20);

        cboFormat = new ComboBox
        {
            Location = new Point(20, 45),
            Size = new Size(440, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Theme.BackgroundMedium,
            ForeColor = Theme.ForegroundColor,
            FlatStyle = FlatStyle.Flat
        };

        foreach (var opt in formatOptions)
        {
            cboFormat.Items.Add(opt.Display);
        }

        // Determine current selection
        int selectedIndex = GetIndexForValue(settings.Weather_FormatString);
        cboFormat.SelectedIndex = selectedIndex;

        lblCustomFormat = Theme.CreateLabel("Custom Format String:", 20, 85);
        lblCustomFormat.Visible = selectedIndex == 4;

        txtCustomFormat = Theme.CreateTextBox(20, 110, 380);
        txtCustomFormat.Text = selectedIndex == 4 ? settings.Weather_FormatString : "";
        txtCustomFormat.Visible = selectedIndex == 4;

        btnFormatHelp = Theme.CreateButton("?", 410, 108, 50, 28);
        btnFormatHelp.Visible = selectedIndex == 4;
        btnFormatHelp.Click += (s, e) =>
        {
            MessageBox.Show(FormatHelpText, "Format Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        cboFormat.SelectedIndexChanged += (s, e) =>
        {
            bool isCustom = cboFormat.SelectedIndex == 4;
            lblCustomFormat.Visible = isCustom;
            txtCustomFormat.Visible = isCustom;
            btnFormatHelp.Visible = isCustom;
        };

        var btnOK = Theme.CreateButton("OK", this.ClientSize.Width - 180, 190);
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Click += (s, e) =>
        {
            if (cboFormat.SelectedIndex == 4)
            {
                // Custom format
                settings.Weather_FormatString = txtCustomFormat.Text.Trim();
            }
            else
            {
                // Predefined format (1-4)
                settings.Weather_FormatString = formatOptions[cboFormat.SelectedIndex].Value;
            }
            this.Close();
        };

        var btnCancel = Theme.CreateButton("Cancel", this.ClientSize.Width - 95, 190);
        btnCancel.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] { lbl, cboFormat, lblCustomFormat, txtCustomFormat, btnFormatHelp, btnOK, btnCancel });
    }

    private int GetIndexForValue(string value)
    {
        // Check if it's one of the predefined formats
        for (int i = 0; i < 4; i++)
        {
            if (formatOptions[i].Value == value)
                return i;
        }
        // Otherwise it's a custom format
        return 4;
    }
}
