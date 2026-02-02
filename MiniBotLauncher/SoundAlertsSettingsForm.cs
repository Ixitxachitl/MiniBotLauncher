using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class SoundAlertsForm : Form
{
    private ListBox listBox = null!;
    private TextBox txtCommandInput = null!;
    private Button btnAdd = null!, btnRemove = null!, btnClose = null!;
    private Dictionary<string, string> soundMappings;
    private SettingsData settings;

    public SoundAlertsForm(SettingsData settingsData)
    {
        InitializeComponent();
        settings = settingsData;
        soundMappings = new Dictionary<string, string>(settings.SoundAlertMappings);

        foreach (var kvp in soundMappings)
            listBox.Items.Add($"{kvp.Key} → {kvp.Value}");
    }

    private void InitializeComponent()
    {
        this.Text = "Sound Alerts";
        this.Size = new Size(500, 380);
        Theme.ApplyToDialog(this);

        listBox = Theme.CreateListBox(20, 20, 440, 200);

        txtCommandInput = Theme.CreateTextBox(20, listBox.Bottom + 15, 390);

        btnAdd = Theme.CreateButton("+", txtCommandInput.Right + 10, txtCommandInput.Top - 5, 40, 40);
        btnAdd.Click += (s, e) =>
        {
            string command = txtCommandInput.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(command))
            {
                MessageBox.Show("Please enter a command before selecting a sound file.", "Missing Command");
                return;
            }

            if (soundMappings.ContainsKey(command))
            {
                MessageBox.Show("That command is already assigned to a sound file.", "Duplicate Command");
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3",
                Title = "Select Sound File"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string path = ofd.FileName;
                soundMappings[command] = path;
                listBox.Items.Add($"{command} → {path}");
                txtCommandInput.Clear();
            }
        };

        btnRemove = Theme.CreateButton("Remove Selected", 20, btnAdd.Bottom + 15, 150, 40);
        btnRemove.Click += (s, e) =>
        {
            if (listBox.SelectedItem is string selected)
            {
                int idx = selected.IndexOf(" → ");
                if (idx != -1)
                {
                    string key = selected.Substring(0, idx);
                    soundMappings.Remove(key);
                    listBox.Items.Remove(selected);
                }
            }
        };

        Button btnCancel = Theme.CreateButton("Cancel", listBox.Right - 90, btnRemove.Top, 90, 40);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Click += (s, e) => Close();

        btnClose = Theme.CreateButton("OK", btnCancel.Left - 100, btnRemove.Top, 90, 40);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Click += (s, e) =>
        {
            settings.SoundAlertMappings = soundMappings;
            Close();
        };

        Controls.AddRange(new Control[] { listBox, txtCommandInput, btnAdd, btnRemove, btnClose, btnCancel });
    }
}
