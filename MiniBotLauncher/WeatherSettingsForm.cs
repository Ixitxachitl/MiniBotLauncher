using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class WeatherSettingsForm : Form
{
    private TextBox txtFormat;
    private SettingsData settings;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    public WeatherSettingsForm(SettingsData currentSettings)
    {
        this.settings = currentSettings;

        this.Text = "Weather Settings";
        this.Size = new Size(400, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        var lbl = new Label
        {
            Text = "Weather Format String",
            Left = 20,
            Top = 20,
            AutoSize = true
        };

        txtFormat = new TextBox
        {
            Left = 20,
            Top = 45,
            Width = 340,
            Text = settings.Weather_FormatString,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOK = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Left = this.ClientSize.Width - 180,
            Top = 100,
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
            settings.Weather_FormatString = txtFormat.Text.Trim();
            this.Close();
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Left = this.ClientSize.Width - 95,
            Top = 100,
            Width = 70,
            Height = 35,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btnCancel.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btnCancel.Width, btnCancel.Height, 10, 10));

        this.Controls.AddRange(new Control[] { lbl, txtFormat, btnOK, btnCancel });
    }
}
