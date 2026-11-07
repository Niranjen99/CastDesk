using System.Diagnostics;
using CastDesk.Configuration;

namespace CastDesk.UI;

/// <summary>
/// The Settings window (tray menu → Settings…). Edits a copy of the settings; <see cref="Result"/> holds
/// the new values after Save.
/// </summary>
internal sealed class SettingsForm : Form
{
    private const int FieldWidth = 440;

    private readonly TextBox _nameBox = new() { Width = FieldWidth, MaxLength = CastDeskOptions.MaxFriendlyNameLength };
    private readonly CheckBox _fullscreenOnCastBox = new() { Text = "Go fullscreen when a cast starts", AutoSize = true };
    private readonly CheckBox _hideWhenStoppedBox = new() { Text = "Hide the window when casting stops", AutoSize = true };
    private readonly CheckBox _startWithWindowsBox = new() { Text = "Start CastDesk when I sign in to Windows", AutoSize = true };
    private readonly NumericUpDown _portBox = new() { Minimum = CastDeskOptions.MinimumPort, Maximum = CastDeskOptions.MaximumPort, Width = 100 };
    private readonly TextBox _userAgentBox = new() { Width = FieldWidth, Height = 76, Multiline = true, ScrollBars = ScrollBars.Vertical, WordWrap = true };
    private readonly CastDeskOptions _original;

    public SettingsForm(CastDeskOptions current, bool startsWithWindows)
    {
        _original = current;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "CastDesk Settings";
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true; // the main window may be hidden in the tray
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(16, 12, 16, 12);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };

        layout.Controls.Add(Heading("Receiver"));
        layout.Controls.Add(Label("Name in your phone's Cast menu"));
        layout.Controls.Add(_nameBox);
        layout.Controls.Add(Hint("Phones may keep showing the old name for a few minutes after a change."));

        layout.Controls.Add(Heading("Window"));
        layout.Controls.Add(_fullscreenOnCastBox);
        layout.Controls.Add(_hideWhenStoppedBox);

        layout.Controls.Add(Heading("Startup"));
        layout.Controls.Add(_startWithWindowsBox);
        layout.Controls.Add(Hint("CastDesk starts quietly in the notification area, ready for casts."));

        layout.Controls.Add(Heading("Advanced"));
        layout.Controls.Add(Label("Port phones connect to"));
        layout.Controls.Add(_portBox);
        layout.Controls.Add(Hint("Default 8765. If Windows has reserved the port or another app uses it, CastDesk picks a free one."));
        layout.Controls.Add(Label("TV identity (user agent) YouTube sees"));
        layout.Controls.Add(_userAgentBox);
        var resetUserAgentButton = new Button { Text = "Use the default", AutoSize = true, Margin = new Padding(3, 4, 3, 3) };
        resetUserAgentButton.Click += (_, _) => _userAgentBox.Text = CastDeskOptions.DefaultUserAgent;
        layout.Controls.Add(resetUserAgentButton);
        layout.Controls.Add(Hint("Change this only if YouTube stops showing its TV app."));

        layout.Controls.Add(CreateButtonRow());
        Controls.Add(layout);

        _nameBox.Text = current.FriendlyName;
        _fullscreenOnCastBox.Checked = current.FullscreenOnCast;
        _hideWhenStoppedBox.Checked = current.HideWhenCastingStops;
        _startWithWindowsBox.Checked = startsWithWindows;
        _portBox.Value = Math.Clamp(current.DialPort, CastDeskOptions.MinimumPort, CastDeskOptions.MaximumPort);
        _userAgentBox.Text = current.EffectiveUserAgent;

        ResumeLayout(performLayout: true);
    }

    /// <summary>The edited settings, after Save.</summary>
    public CastDeskOptions? Result { get; private set; }

    /// <summary>Whether CastDesk should start at sign-in, after Save.</summary>
    public bool StartWithWindows => _startWithWindowsBox.Checked;

    private Control CreateButtonRow()
    {
        var row = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var logLink = new LinkLabel { Text = "Open log folder", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
        logLink.LinkClicked += (_, _) =>
        {
            Directory.CreateDirectory(CastDeskOptions.DataDirectory);
            Process.Start(new ProcessStartInfo(CastDeskOptions.DataDirectory) { UseShellExecute = true });
        };

        var saveButton = new Button { Text = "Save", AutoSize = true, MinimumSize = new Size(88, 0) };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(88, 0), DialogResult = DialogResult.Cancel };
        saveButton.Click += (_, _) => Save();
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        row.Controls.Add(logLink, 0, 0);
        row.Controls.Add(saveButton, 1, 0);
        row.Controls.Add(cancelButton, 2, 0);
        return row;
    }

    private void Save()
    {
        var name = _nameBox.Text.Trim();
        if (name.Length == 0)
        {
            MessageBox.Show(this, "Enter a name for the Cast menu.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameBox.Focus();
            return;
        }

        var result = _original.Clone();
        result.FriendlyName = name;
        result.FullscreenOnCast = _fullscreenOnCastBox.Checked;
        result.HideWhenCastingStops = _hideWhenStoppedBox.Checked;
        result.DialPort = (int)_portBox.Value;
        // Store an empty value for the default, so a future CastDesk with a newer default picks it up.
        var userAgent = _userAgentBox.Text.Trim();
        result.UserAgent = userAgent.Length == 0 || userAgent == CastDeskOptions.DefaultUserAgent ? "" : userAgent;

        Result = result;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10.5F),
        Margin = new Padding(0, 14, 0, 4),
    };

    private static Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(0, 6, 0, 2) };

    private static Label Hint(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(FieldWidth, 0),
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(0, 2, 0, 2),
    };
}
