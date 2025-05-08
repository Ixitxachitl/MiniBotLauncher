using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
public class SoundAlertsForm : Form
{
    private ListBox listBox;
    private TextBox txtCommandInput;
    private Button btnAdd, btnRemove, btnClose;
    private Dictionary<string, string> soundMappings;
    private SettingsData settings;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

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
        this.Size = new Size(500, 420);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        listBox = new ListBox
        {
            Left = 20,
            Top = 20,
            Width = 440,
            Height = 200,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        txtCommandInput = new TextBox
        {
            Left = 20,
            Top = listBox.Bottom + 15,
            Width = 150,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnAdd = CreateStyledButton("+", txtCommandInput.Right + 10, txtCommandInput.Top - 5, 40);
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

        btnRemove = CreateStyledButton("Remove Selected", 20, btnAdd.Bottom + 15, 150);
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

        btnClose = CreateStyledButton("OK", listBox.Right - 90, btnRemove.Top, 90);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Click += (s, e) =>
        {
            settings.SoundAlertMappings = soundMappings;
            Close();
        };

        Controls.AddRange(new Control[] { listBox, txtCommandInput, btnAdd, btnRemove, btnClose });
    }

    private Button CreateStyledButton(string text, int left, int top, int width)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 40,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        button.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, button.Width, button.Height, 10, 10));
        return button;
    }
}
