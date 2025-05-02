using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class IgnoreListForm : Form
{
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    private ListBox listBox;
    private TextBox txtNewUser;
    private Button btnAdd, btnRemove, btnClose;
    private List<string> ignoredUsers;

    private Color bgColor = Color.FromArgb(30, 30, 30);
    private Color foreColor = Color.White;
    private Color buttonColor = Color.FromArgb(50, 50, 50);
    private Color hoverColor = Color.FromArgb(70, 70, 70);

    public IgnoreListForm(List<string> currentList)
    {
        this.Text = "Ignored Users";
        this.Size = new Size(380, 350);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = bgColor;
        this.ForeColor = foreColor;
        this.Font = new Font("Segoe UI", 10F);

        ignoredUsers = new List<string>(currentList);

        listBox = new ListBox
        {
            Left = 20,
            Top = 20,
            Width = 320,
            Height = 180,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        listBox.Items.AddRange(ignoredUsers.ToArray());

        txtNewUser = new TextBox
        {
            Left = 20,
            Top = listBox.Bottom + 15,
            Width = 260,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = foreColor,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnAdd = CreateStyledButton("+", txtNewUser.Right + 20, txtNewUser.Top - 5, 40);
        btnAdd.Click += (s, e) =>
        {
            string name = txtNewUser.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(name) && !ignoredUsers.Contains(name))
            {
                ignoredUsers.Add(name);
                listBox.Items.Add(name);
                txtNewUser.Clear();
            }
        };

        btnRemove = CreateStyledButton("Remove Selected", 20, txtNewUser.Bottom + 20, 150);
        btnRemove.Click += (s, e) =>
        {
            if (listBox.SelectedItem is string selected)
            {
                ignoredUsers.Remove(selected);
                listBox.Items.Remove(selected);
            }
        };

        btnClose = CreateStyledButton("OK", btnRemove.Right + 80, btnRemove.Top, 90);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            listBox, txtNewUser, btnAdd, btnRemove, btnClose
        });
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
            BackColor = buttonColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hoverColor;
        button.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, button.Width, button.Height, 10, 10));
        return button;
    }

    public List<string> GetIgnoredUsernames() => ignoredUsers;
}
