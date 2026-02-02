using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

public class TranslateSettingsForm : Form
{
    private ComboBox cmbLanguage;
    private SettingsData settings;

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
        Theme.ApplyToDialog(this);

        var lbl = Theme.CreateLabel("Target Language", 20, 20);

        cmbLanguage = Theme.CreateComboBox(20, 45, 340);

        foreach (var kv in languageMap)
            cmbLanguage.Items.Add(kv.Key);

        var selected = languageMap.FirstOrDefault(kv => kv.Value == settings.Translate_TargetLanguage).Key ?? "English";
        cmbLanguage.SelectedItem = selected;

        var btnOK = Theme.CreateButton("OK", this.ClientSize.Width - 180, 100);
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Click += (s, e) =>
        {
            var selectedKey = cmbLanguage.SelectedItem?.ToString();
            if (selectedKey != null && languageMap.TryGetValue(selectedKey, out string? langCode))
            {
                settings.Translate_TargetLanguage = langCode;
            }
            this.Close();
        };

        var btnCancel = Theme.CreateButton("Cancel", this.ClientSize.Width - 95, 100);
        btnCancel.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] { lbl, cmbLanguage, btnOK, btnCancel });
    }
}
