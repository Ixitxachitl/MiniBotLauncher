using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public class IgnoreListForm : Form
{
    private ListBox listBox;
    private TextBox txtNewUser;
    private Button btnAdd, btnRemove, btnClose;
    private List<string> ignoredUsers;

    public IgnoreListForm(List<string> currentList)
    {
        this.Text = "Ignored Users";
        this.Size = new Size(380, 350);
        Theme.ApplyToDialog(this);

        ignoredUsers = new List<string>(currentList);

        listBox = Theme.CreateListBox(20, 20, 320, 180);
        listBox.Items.AddRange(ignoredUsers.ToArray());

        txtNewUser = Theme.CreateTextBox(20, listBox.Bottom + 15, 260);

        btnAdd = Theme.CreateButton("+", txtNewUser.Right + 20, txtNewUser.Top - 5, 40, 35);
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

        btnRemove = Theme.CreateButton("Remove Selected", 20, txtNewUser.Bottom + 20, 150, 40);
        btnRemove.Click += (s, e) =>
        {
            if (listBox.SelectedItem is string selected)
            {
                ignoredUsers.Remove(selected);
                listBox.Items.Remove(selected);
            }
        };

        btnClose = Theme.CreateButton("OK", btnRemove.Right + 80, btnRemove.Top, 90, 40);
        btnClose.DialogResult = DialogResult.OK;
        btnClose.Click += (s, e) => Close();

        Controls.AddRange(new Control[]
        {
            listBox, txtNewUser, btnAdd, btnRemove, btnClose
        });
    }

    public List<string> GetIgnoredUsernames() => ignoredUsers;
}
