using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public enum ThemeType
{
    Dark,
    Light,
    Classic
}

/// <summary>
/// Centralized theme and UI styling for consistent appearance across all forms.
/// </summary>
public static class Theme
{
    private static ThemeType _currentTheme = ThemeType.Dark;

    // Current theme colors (set dynamically)
    public static Color BackgroundDark { get; private set; }
    public static Color BackgroundMedium { get; private set; }
    public static Color BackgroundLight { get; private set; }
    public static Color HoverColor { get; private set; }
    public static Color AccentColor { get; private set; }
    public static Color ForegroundColor { get; private set; }
    public static Color DisabledColor { get; private set; }
    public static Color LinkColor { get; private set; }
    public static FlatStyle ButtonStyle { get; private set; }
    public static BorderStyle TextBoxBorder { get; private set; }

    // Font
    public static readonly Font DefaultFont = new Font("Segoe UI", 10F);

    public static ThemeType CurrentTheme => _currentTheme;

    static Theme()
    {
        SetTheme(ThemeType.Dark);
    }

    public static void SetTheme(ThemeType theme)
    {
        _currentTheme = theme;
        switch (theme)
        {
            case ThemeType.Dark:
                BackgroundDark = Color.FromArgb(30, 30, 30);
                BackgroundMedium = Color.FromArgb(40, 40, 40);
                BackgroundLight = Color.FromArgb(50, 50, 50);
                HoverColor = Color.FromArgb(70, 70, 70);
                AccentColor = Color.FromArgb(0, 122, 204);
                ForegroundColor = Color.White;
                DisabledColor = Color.Gray;
                LinkColor = Color.SteelBlue;
                ButtonStyle = FlatStyle.Flat;
                TextBoxBorder = BorderStyle.FixedSingle;
                break;

            case ThemeType.Light:
                // Modern flat light theme - white background, light gray controls
                BackgroundDark = Color.FromArgb(245, 245, 245);
                BackgroundMedium = Color.White;
                BackgroundLight = Color.FromArgb(225, 225, 225);
                HoverColor = Color.FromArgb(200, 200, 200);
                AccentColor = Color.FromArgb(0, 120, 215);
                ForegroundColor = Color.FromArgb(30, 30, 30);
                DisabledColor = Color.FromArgb(160, 160, 160);
                LinkColor = Color.FromArgb(0, 102, 204);
                ButtonStyle = FlatStyle.Flat;
                TextBoxBorder = BorderStyle.FixedSingle;
                break;

            case ThemeType.Classic:
                // Windows 95/2000 style - 3D borders, system colors
                BackgroundDark = SystemColors.Control;
                BackgroundMedium = SystemColors.Control;
                BackgroundLight = SystemColors.ControlLightLight;
                HoverColor = SystemColors.ControlLight;
                AccentColor = SystemColors.Highlight;
                ForegroundColor = SystemColors.ControlText;
                DisabledColor = SystemColors.GrayText;
                LinkColor = Color.Navy;
                ButtonStyle = FlatStyle.Standard;
                TextBoxBorder = BorderStyle.Fixed3D;
                break;
        }
    }

    public static void SetTheme(string themeName)
    {
        if (Enum.TryParse<ThemeType>(themeName, true, out var theme))
            SetTheme(theme);
    }

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
    /// Recursively apply the current theme to all controls in a form.
    /// Call this after SetTheme() to refresh UI without restart.
    /// </summary>
    public static void ApplyThemeToControl(Control control)
    {
        // Apply to the control itself
        if (control is Form form)
        {
            form.BackColor = BackgroundDark;
            form.ForeColor = ForegroundColor;
        }
        else if (control is Button btn)
        {
            // Skip transparent icon buttons (they use emoji)
            if (btn.BackColor != Color.Transparent)
            {
                btn.BackColor = BackgroundLight;
                btn.ForeColor = ForegroundColor;
                btn.FlatStyle = ButtonStyle;
                if (ButtonStyle == FlatStyle.Flat)
                {
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = HoverColor;
                    // Apply rounded corners for flat themes
                    btn.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, btn.Width, btn.Height, 10, 10));
                }
                else
                {
                    btn.FlatAppearance.BorderSize = 1;
                    // Remove rounded corners for classic theme - use full rectangle
                    btn.Region = null;
                }
            }
            else
            {
                // Icon buttons - just update foreground color
                btn.ForeColor = ForegroundColor;
            }
        }
        else if (control is TextBox txt)
        {
            txt.BackColor = _currentTheme == ThemeType.Classic ? SystemColors.Window : 
                           (_currentTheme == ThemeType.Light ? Color.White : BackgroundLight);
            txt.ForeColor = ForegroundColor;
            txt.BorderStyle = TextBoxBorder;
        }
        else if (control is ComboBox cmb)
        {
            cmb.BackColor = _currentTheme == ThemeType.Classic ? SystemColors.Window : 
                           (_currentTheme == ThemeType.Light ? Color.White : BackgroundLight);
            cmb.ForeColor = ForegroundColor;
            cmb.FlatStyle = _currentTheme == ThemeType.Classic ? FlatStyle.Standard : FlatStyle.Flat;
        }
        else if (control is CheckBox chk)
        {
            if (chk.Appearance == Appearance.Button)
            {
                // Toggle buttons
                if (!chk.Checked)
                    chk.BackColor = BackgroundLight;
                chk.ForeColor = ForegroundColor;
                chk.FlatStyle = ButtonStyle;
                if (ButtonStyle == FlatStyle.Flat)
                {
                    chk.FlatAppearance.BorderSize = 0;
                    // Apply rounded corners for flat themes
                    chk.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, chk.Width, chk.Height, 10, 10));
                }
                else
                {
                    chk.FlatAppearance.BorderSize = 1;
                    // Remove rounded corners for classic theme
                    chk.Region = null;
                }
            }
            else
            {
                chk.ForeColor = ForegroundColor;
                chk.BackColor = Color.Transparent;
            }
        }
        else if (control is Label lbl)
        {
            // Skip labels tagged with "KeepColor" - they have dynamic colors (status indicators)
            if (lbl.Tag?.ToString() == "KeepColor")
            {
                // Only update background, not foreground
                if (lbl.BackColor != Color.Transparent)
                    lbl.BackColor = BackgroundDark;
            }
            else if (lbl is LinkLabel link)
            {
                link.LinkColor = LinkColor;
            }
            else
            {
                lbl.ForeColor = ForegroundColor;
                if (lbl.BackColor != Color.Transparent)
                    lbl.BackColor = BackgroundDark;
            }
        }
        else if (control is TrackBar track)
        {
            track.BackColor = BackgroundDark;
        }
        else if (control is ListBox lst)
        {
            lst.BackColor = _currentTheme == ThemeType.Classic ? SystemColors.Window : 
                           (_currentTheme == ThemeType.Light ? Color.White : BackgroundMedium);
            lst.ForeColor = ForegroundColor;
            lst.BorderStyle = _currentTheme == ThemeType.Classic ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
        }
        else
        {
            // Generic control
            if (control.BackColor != Color.Transparent)
            {
                control.BackColor = BackgroundDark;
            }
            control.ForeColor = ForegroundColor;
        }

        // Recursively apply to children
        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
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
            FlatStyle = ButtonStyle,
            TabStop = false
        };
        if (ButtonStyle == FlatStyle.Flat)
        {
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = HoverColor;
            button.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, button.Width, button.Height, 10, 10));
        }
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
            BackColor = _currentTheme == ThemeType.Classic ? SystemColors.Window : 
                        (_currentTheme == ThemeType.Light ? Color.White : BackgroundLight),
            ForeColor = ForegroundColor,
            BorderStyle = TextBoxBorder,
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
            BackColor = _currentTheme == ThemeType.Classic ? SystemColors.Window : 
                        (_currentTheme == ThemeType.Light ? Color.White : BackgroundLight),
            ForeColor = ForegroundColor,
            FlatStyle = _currentTheme == ThemeType.Classic ? FlatStyle.Standard : FlatStyle.Flat,
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
            BackColor = _currentTheme == ThemeType.Classic ? SystemColors.Window : 
                        (_currentTheme == ThemeType.Light ? Color.White : BackgroundMedium),
            ForeColor = ForegroundColor,
            BorderStyle = _currentTheme == ThemeType.Classic ? BorderStyle.Fixed3D : BorderStyle.FixedSingle
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
