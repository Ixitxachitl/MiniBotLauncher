using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class WalkOnSettingsForm : Form
{
    private ListBox listBox;
    private TextBox txtUsernameInput;
    private Button btnAdd, btnRemove, btnClose, btnCancel;
    private Dictionary<string, string> walkOnMappings;
    private SettingsData settings;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

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
        this.Size = new Size(500, 380);
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

        txtUsernameInput = new TextBox
        {
            Left = 20,
            Top = listBox.Bottom + 15,
            Width = 390,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnAdd = CreateStyledButton("+", txtUsernameInput.Right + 10, txtUsernameInput.Top - 5, 40);
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

        btnRemove = CreateStyledButton("Remove Selected", 20, btnAdd.Bottom + 15, 150);
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

        btnCancel = CreateStyledButton("Cancel", listBox.Right - 90, btnRemove.Top, 90);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Click += (s, e) => Close();

        btnClose = CreateStyledButton("OK", btnCancel.Left - 100, btnRemove.Top, 90);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Click += (s, e) =>
        {
            settings.WalkOnSoundMappings = walkOnMappings;
            WalkOnScript.SetSoundMappings(walkOnMappings);
            Close();
        };

        Controls.AddRange(new Control[] { listBox, txtUsernameInput, btnAdd, btnRemove, btnClose, btnCancel });
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