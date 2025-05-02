using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

public class TranslateSettingsForm : Form
{
    private ComboBox cmbLanguage;
    private SettingsData settings;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

    private readonly Dictionary<string, string> languageMap = new()
    {
        { "English", "en" }, { "Spanish", "es" }, { "French", "fr" }, { "German", "de" },
        { "Italian", "it" }, { "Portuguese", "pt" }, { "Russian", "ru" }, { "Japanese", "ja" },
        { "Korean", "ko" }, { "Chinese (Simplified)", "zh-cn" }, { "Chinese (Traditional)", "zh-tw" }
    };

    public TranslateSettingsForm(SettingsData currentSettings)
    {
        settings = currentSettings;

        this.Text = "Translate Settings";
        this.Size = new Size(400, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        var lbl = new Label
        {
            Text = "Target Language",
            Left = 20,
            Top = 20,
            AutoSize = true
        };

        cmbLanguage = new ComboBox
        {
            Left = 20,
            Top = 45,
            Width = 340,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        foreach (var kv in languageMap)
            cmbLanguage.Items.Add(kv.Key);

        var selected = languageMap.FirstOrDefault(kv => kv.Value == settings.Translate_TargetLanguage).Key ?? "English";
        cmbLanguage.SelectedItem = selected;

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
            var selectedKey = cmbLanguage.SelectedItem?.ToString();
            if (selectedKey != null && languageMap.TryGetValue(selectedKey, out string langCode))
            {
                settings.Translate_TargetLanguage = langCode;
            }
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

        this.Controls.AddRange(new Control[] { lbl, cmbLanguage, btnOK, btnCancel });
    }
}
