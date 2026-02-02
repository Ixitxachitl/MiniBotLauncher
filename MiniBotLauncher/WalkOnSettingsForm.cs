using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class WalkOnSettingsForm : Form
{
    private ListBox listBox = null!;
    private TextBox txtUsernameInput = null!;
    private Button btnAdd = null!, btnRemove = null!, btnPreview = null!, btnClose = null!, btnCancel = null!;
    private Dictionary<string, string> walkOnMappings;
    private SettingsData settings;

    public WalkOnSettingsForm(SettingsData settingsData)
    {
        InitializeComponent();
        settings = settingsData;
        walkOnMappings = new Dictionary<string, string>(settings.WalkOnSoundMappings);

        foreach (var kvp in walkOnMappings)
            listBox.Items.Add($"{kvp.Key} → {kvp.Value}");
    }

    private void InitializeComponent()
    {
        this.Text = "Walk-On Sounds";
        this.Size = new Size(500, 420);
        Theme.ApplyToDialog(this);

        listBox = Theme.CreateListBox(20, 20, 440, 200);

        txtUsernameInput = Theme.CreateTextBox(20, listBox.Bottom + 15, 390);

        btnAdd = Theme.CreateButton("+", txtUsernameInput.Right + 10, txtUsernameInput.Top - 5, 40, 40);
        btnAdd.Click += (s, e) =>
        {
            string username = txtUsernameInput.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Please enter a username before selecting a sound file.", "Missing Username");
                return;
            }

            if (walkOnMappings.ContainsKey(username))
            {
                MessageBox.Show("That username already has a sound assigned.", "Duplicate Username");
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Audio Files (*.wav;*.mp3)|*.wav;*.mp3",
                Title = "Select Walk-On Sound"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string path = ofd.FileName;
                walkOnMappings[username] = path;
                listBox.Items.Add($"{username} → {path}");
                txtUsernameInput.Clear();
            }
        };

        btnRemove = Theme.CreateButton("Remove", 20, btnAdd.Bottom + 15, 100, 40);
        btnRemove.Click += (s, e) =>
        {
            if (listBox.SelectedItem is string selected)
            {
                int idx = selected.IndexOf(" → ");
                if (idx != -1)
                {
                    string key = selected.Substring(0, idx);
                    walkOnMappings.Remove(key);
                    listBox.Items.Remove(selected);
                }
            }
        };

        btnPreview = Theme.CreateButton("▶ Preview", btnRemove.Right + 10, btnRemove.Top, 100, 40);
        btnPreview.Click += (s, e) =>
        {
            if (listBox.SelectedItem is string selected)
            {
                int idx = selected.IndexOf(" → ");
                if (idx != -1)
                {
                    string path = selected.Substring(idx + 3);
                    if (System.IO.File.Exists(path))
                    {
                        AudioQueue.Enqueue(path);
                    }
                    else
                    {
                        MessageBox.Show("Sound file not found.", "Error");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a sound to preview.", "No Selection");
            }
        };

        btnCancel = Theme.CreateButton("Cancel", listBox.Right - 90, btnRemove.Top, 90, 40);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Click += (s, e) => Close();

        btnClose = Theme.CreateButton("OK", btnCancel.Left - 100, btnRemove.Top, 90, 40);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Click += (s, e) =>
        {
            settings.WalkOnSoundMappings = walkOnMappings;
            WalkOnScript.SetSoundMappings(walkOnMappings);
            Close();
        };

        Controls.AddRange(new Control[] { listBox, txtUsernameInput, btnAdd, btnRemove, btnPreview, btnClose, btnCancel });
    }
}