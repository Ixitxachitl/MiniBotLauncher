using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// Centralized theme and UI styling for consistent appearance across all forms.
/// </summary>
public static class Theme
{
    // Colors
    public static readonly Color BackgroundDark = Color.FromArgb(30, 30, 30);
    public static readonly Color BackgroundMedium = Color.FromArgb(40, 40, 40);
    public static readonly Color BackgroundLight = Color.FromArgb(50, 50, 50);
    public static readonly Color HoverColor = Color.FromArgb(70, 70, 70);
    public static readonly Color AccentColor = Color.FromArgb(0, 122, 204);
    public static readonly Color ForegroundColor = Color.White;
    public static readonly Color DisabledColor = Color.Gray;

    // Font
    public static readonly Font DefaultFont = new Font("Segoe UI", 10F);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateRoundRectRgn(
        int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
        int nWidthEllipse, int nHeightEllipse);

    /// <summary>
    /// Apply standard dark theme to a form.
    /// </summary>
    public static void ApplyToForm(Form form)
    {
        form.BackColor = BackgroundDark;
        form.ForeColor = ForegroundColor;
        form.Font = DefaultFont;
    }

    /// <summary>
    /// Apply standard dark theme to a dialog form.
    /// </summary>
    public static void ApplyToDialog(Form form)
    {
        ApplyToForm(form);
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
    }

    /// <summary>
    /// Create a styled button with rounded corners.
    /// </summary>
    public static Button CreateButton(string text, int left, int top, int width = 70, int height = 35)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            BackColor = BackgroundLight,
            ForeColor = ForegroundColor,
            FlatStyle = FlatStyle.Flat,
            TabStop = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = HoverColor;
        button.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, button.Width, button.Height, 10, 10));
        return button;
    }

    /// <summary>
    /// Create a styled TextBox.
    /// </summary>
    public static TextBox CreateTextBox(int left, int top, int width = 200, bool multiline = false, int height = 0)
    {
        var textBox = new TextBox
        {
            Left = left,
            Top = top,
            Width = width,
            BackColor = BackgroundLight,
            ForeColor = ForegroundColor,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = multiline
        };
        if (multiline && height > 0)
            textBox.Height = height;
        return textBox;
    }

    /// <summary>
    /// Create a styled Label.
    /// </summary>
    public static Label CreateLabel(string text, int left, int top, bool autoSize = true)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            AutoSize = autoSize,
            ForeColor = ForegroundColor,
            BackColor = Color.Transparent
        };
    }

    /// <summary>
    /// Create a styled ComboBox.
    /// </summary>
    public static ComboBox CreateComboBox(int left, int top, int width = 200, ComboBoxStyle style = ComboBoxStyle.DropDownList)
    {
        return new ComboBox
        {
            Left = left,
            Top = top,
            Width = width,
            BackColor = BackgroundLight,
            ForeColor = ForegroundColor,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = style
        };
    }

    /// <summary>
    /// Create a styled ListBox.
    /// </summary>
    public static ListBox CreateListBox(int left, int top, int width, int height)
    {
        return new ListBox
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            BackColor = BackgroundMedium,
            ForeColor = ForegroundColor,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    /// <summary>
    /// Apply rounded corners to a control.
    /// </summary>
    public static void ApplyRoundedCorners(Control control, int radius = 10)
    {
        control.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, control.Width, control.Height, radius, radius));
    }
}
