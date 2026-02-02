using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class MarkovChainSettingsForm : Form
{
    private ListBox lstBrainFiles = null!;
    private ListBox lstBannedWords = null!;
    private TextBox txtNewWord = null!;
    private TrackBar sliderInterval = null!;
    private Label lblIntervalValue = null!;
    private SettingsData settings;
    private string baseFolder;

    public MarkovChainSettingsForm(SettingsData currentSettings)
    {
        settings = currentSettings;
        baseFolder = MarkovChainScript.GetBaseFolder();
        InitializeComponent();
        LoadBrainFiles();
        LoadBannedWords();
    }

    private void InitializeComponent()
    {
        this.Text = "Markov Chain Settings";
        this.Size = new Size(550, 520);
        Theme.ApplyToDialog(this);

        // === Brain Files Section ===
        var lblBrainFiles = Theme.CreateLabel("Channel Brain Files", 20, 15);
        lstBrainFiles = Theme.CreateListBox(20, 40, 320, 150);

        var btnViewStats = Theme.CreateButton("View Stats", 350, 40, 100, 35);
        btnViewStats.Click += BtnViewStats_Click;

        var btnDeleteBrain = Theme.CreateButton("Delete", 350, 85, 100, 35);
        btnDeleteBrain.Click += BtnDeleteBrain_Click;

        var btnRefresh = Theme.CreateButton("Refresh", 350, 130, 100, 35);
        btnRefresh.Click += (s, e) => LoadBrainFiles();

        // === Message Interval Section ===
        var lblInterval = Theme.CreateLabel("Messages before response:", 20, 205);
        sliderInterval = new TrackBar
        {
            Left = 20,
            Top = 230,
            Width = 280,
            Minimum = 5,
            Maximum = 100,
            Value = Math.Clamp(settings.Markov_MessageInterval, 5, 100),
            TickFrequency = 10
        };
        lblIntervalValue = Theme.CreateLabel(sliderInterval.Value.ToString(), sliderInterval.Right + 10, sliderInterval.Top + 5);
        sliderInterval.ValueChanged += (s, e) => lblIntervalValue.Text = sliderInterval.Value.ToString();

        // === Banned Words Section ===
        var lblBannedWords = Theme.CreateLabel("Banned Words (messages with these words are ignored)", 20, 280);
        lstBannedWords = Theme.CreateListBox(20, 305, 320, 120);

        txtNewWord = Theme.CreateTextBox(20, 435, 250);
        txtNewWord.PlaceholderText = "Enter word to ban...";

        var btnAddWord = Theme.CreateButton("+", 280, 433, 40, 30);
        btnAddWord.Click += BtnAddWord_Click;

        var btnRemoveWord = Theme.CreateButton("Remove", 350, 305, 100, 35);
        btnRemoveWord.Click += BtnRemoveWord_Click;

        var btnClearWords = Theme.CreateButton("Clear All", 350, 350, 100, 35);
        btnClearWords.Click += BtnClearWords_Click;

        var btnCleanDB = Theme.CreateButton("Clean Database", 350, 395, 100, 35);
        btnCleanDB.Click += BtnCleanDatabase_Click;

        // === OK / Cancel ===
        var btnOK = Theme.CreateButton("OK", this.ClientSize.Width - 180, 435);
        btnOK.DialogResult = DialogResult.OK;
        btnOK.Click += (s, e) =>
        {
            settings.Markov_MessageInterval = sliderInterval.Value;
            settings.Markov_BannedWords = lstBannedWords.Items.Cast<string>().ToList();
            MarkovChainScript.SetMessageInterval(sliderInterval.Value);
            MarkovChainScript.SetBannedWords(settings.Markov_BannedWords);
            this.Close();
        };

        var btnCancel = Theme.CreateButton("Cancel", this.ClientSize.Width - 95, 435);
        btnCancel.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[]
        {
            lblBrainFiles, lstBrainFiles, btnViewStats, btnDeleteBrain, btnRefresh,
            lblInterval, sliderInterval, lblIntervalValue,
            lblBannedWords, lstBannedWords, txtNewWord, btnAddWord, btnRemoveWord, btnClearWords, btnCleanDB,
            btnOK, btnCancel
        });
    }

    private void LoadBrainFiles()
    {
        lstBrainFiles.Items.Clear();

        if (!Directory.Exists(baseFolder))
            return;

        var files = Directory.GetFiles(baseFolder, "markov_brain_*.json");
        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            string channel = fileName.Replace("markov_brain_", "").Replace(".json", "");
            var fileInfo = new FileInfo(file);
            string size = FormatFileSize(fileInfo.Length);
            lstBrainFiles.Items.Add($"{channel} ({size})");
        }
    }

    private void LoadBannedWords()
    {
        lstBannedWords.Items.Clear();
        foreach (var word in settings.Markov_BannedWords)
        {
            lstBannedWords.Items.Add(word);
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private void BtnViewStats_Click(object? sender, EventArgs e)
    {
        if (lstBrainFiles.SelectedItem == null)
        {
            MessageBox.Show("Please select a brain file first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string selected = lstBrainFiles.SelectedItem.ToString()!;
        string channel = selected.Split(' ')[0];
        string filePath = Path.Combine(baseFolder, $"markov_brain_{channel}.json");

        if (!File.Exists(filePath))
        {
            MessageBox.Show("Brain file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var transitions = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
            int keyCount = transitions?.Count ?? 0;
            int totalWords = transitions?.Values.Sum(v => v.Count) ?? 0;
            var fileInfo = new FileInfo(filePath);

            string stats = $"Channel: {channel}\n" +
                          $"File Size: {FormatFileSize(fileInfo.Length)}\n" +
                          $"Unique Word Pairs: {keyCount:N0}\n" +
                          $"Total Learned Words: {totalWords:N0}\n" +
                          $"Last Modified: {fileInfo.LastWriteTime:g}";

            MessageBox.Show(stats, "Brain File Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading brain file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDeleteBrain_Click(object? sender, EventArgs e)
    {
        if (lstBrainFiles.SelectedItem == null)
        {
            MessageBox.Show("Please select a brain file first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string selected = lstBrainFiles.SelectedItem.ToString()!;
        string channel = selected.Split(' ')[0];
        string filePath = Path.Combine(baseFolder, $"markov_brain_{channel}.json");

        var result = MessageBox.Show(
            $"Delete brain file for channel '{channel}'?\n\nThis cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    LoadBrainFiles();
                    MessageBox.Show($"Brain file for '{channel}' deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnAddWord_Click(object? sender, EventArgs e)
    {
        string word = txtNewWord.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(word))
            return;

        if (!lstBannedWords.Items.Contains(word))
        {
            lstBannedWords.Items.Add(word);
            txtNewWord.Clear();
        }
    }

    private void BtnRemoveWord_Click(object? sender, EventArgs e)
    {
        if (lstBannedWords.SelectedItem != null)
        {
            lstBannedWords.Items.Remove(lstBannedWords.SelectedItem);
        }
    }

    private void BtnClearWords_Click(object? sender, EventArgs e)
    {
        if (lstBannedWords.Items.Count == 0) return;

        var result = MessageBox.Show(
            "Clear all banned words?",
            "Confirm Clear",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            lstBannedWords.Items.Clear();
        }
    }

    private void BtnCleanDatabase_Click(object? sender, EventArgs e)
    {
        if (lstBannedWords.Items.Count == 0)
        {
            MessageBox.Show("No banned words to clean. Add banned words first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Make sure the banned words are applied before cleaning
        MarkovChainScript.SetBannedWords(lstBannedWords.Items.Cast<string>());

        if (lstBrainFiles.SelectedItem == null)
        {
            // Clean all brain files
            var result = MessageBox.Show(
                "No brain file selected. Clean ALL brain files?",
                "Confirm Clean All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            int totalKeys = 0;
            int totalValues = 0;
            var files = Directory.GetFiles(baseFolder, "markov_brain_*.json");
            foreach (var file in files)
            {
                var (keys, values) = MarkovChainScript.CleanDatabase(file);
                totalKeys += keys;
                totalValues += values;
            }

            MessageBox.Show(
                $"Cleaned all databases.\nRemoved {totalKeys} transitions and {totalValues} word references.",
                "Clean Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            string selected = lstBrainFiles.SelectedItem.ToString()!;
            string channel = selected.Split(' ')[0];
            string filePath = Path.Combine(baseFolder, $"markov_brain_{channel}.json");

            var result = MessageBox.Show(
                $"Clean database for '{channel}'?\nThis will remove all transitions containing banned words.",
                "Confirm Clean",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            var (keysRemoved, valuesRemoved) = MarkovChainScript.CleanDatabase(filePath);

            MessageBox.Show(
                $"Cleaned database for '{channel}'.\nRemoved {keysRemoved} transitions and {valuesRemoved} word references.",
                "Clean Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        LoadBrainFiles(); // Refresh to show new file sizes
    }
}
