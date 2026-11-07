using CastDesk.Configuration;
using CastDesk.Diagnostics;
using CastDesk.Interop;
using CastDesk.Media;
using CastDesk.Tv;
using CastDesk.UI;

namespace CastDesk;

/// <summary>
/// The player window (YouTube's TV app) and the tray icon. CastDesk starts in the tray; the window comes
/// to the front when a phone casts, and hides again a few seconds after the phone stops casting.
/// F11 (or Alt+Enter) toggles fullscreen. Inside the TV app, Esc is its Back button. The window's close
/// button hides it to the tray; only Exit in the tray menu quits.
/// </summary>
public sealed class MainForm : Form
{
    private const string AppName = "CastDesk";
    private const int MaxTrayTextLength = 127;
    private const int HideAfterStopMilliseconds = 3000;

    private readonly CastDeskController _controller;
    private readonly TvAppView _tvApp;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusMenuItem;
    private readonly System.Windows.Forms.Timer _hideAfterStopTimer;
    private readonly AudioSessionRenamer _audioSessionRenamer;
    private WindowsMediaControls? _mediaControls;
    private CastDeskOptions _options;
    private SettingsForm? _settingsForm;

    private bool _allowVisible;
    private bool _isExiting;
    private bool _isFullscreen;
    private Rectangle _windowedBounds;
    private FormWindowState _windowedState = FormWindowState.Normal;

    public MainForm(CastDeskOptions options)
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 720);
        MinimumSize = new Size(640, 400);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        Text = AppName;
        Icon = LoadAppIcon(SystemInformation.IconSize);
        KeyPreview = true;

        _options = options;
        _controller = new CastDeskController(options);

        _tvApp = new TvAppView { Dock = DockStyle.Fill };
        _tvApp.ScreenInfoReported += (_, details) => _controller.ReportScreenInfo(details);
        _tvApp.ExitRequested += (_, _) =>
        {
            _controller.ReportTvAppStopped();
            HideToTray();
        };
        _tvApp.MediaStateChanged += (_, state) => _mediaControls?.Show(state);
        _tvApp.FullscreenToggleRequested += (_, _) => ToggleFullscreen();
        _tvApp.PageFullscreenChanged += (_, isFullscreen) =>
        {
            if (isFullscreen) EnterFullscreen();
            else ExitFullscreen();
        };
        Controls.Add(_tvApp);

        // The volume mixer would otherwise call the TV app's sound "Microsoft Edge WebView2".
        _audioSessionRenamer = new AudioSessionRenamer(AppName, $"{Environment.ProcessPath},0", () => _tvApp.BrowserProcessId);

        _statusMenuItem = new ToolStripMenuItem("Starting receiver…") { Enabled = false };
        var showPlayerMenuItem = new ToolStripMenuItem("Show Player", null, (_, _) => ShowPlayer());
        showPlayerMenuItem.Font = new Font(showPlayerMenuItem.Font, FontStyle.Bold);
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add(_statusMenuItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(showPlayerMenuItem);
        trayMenu.Items.Add(new ToolStripMenuItem("Reload YouTube", null, (_, _) => _tvApp.Reload()));
        trayMenu.Items.Add(new ToolStripMenuItem("Restart Receiver", null, (_, _) => _controller.RestartReceiver()));
        trayMenu.Items.Add(new ToolStripMenuItem("Settings…", null, (_, _) => OpenSettings()));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication()));

        _trayIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(SystemInformation.SmallIconSize),
            Text = AppName,
            ContextMenuStrip = trayMenu,
            Visible = true,
        };
        _trayIcon.MouseClick += (_, click) => { if (click.Button == MouseButtons.Left) ShowPlayer(); };

        _hideAfterStopTimer = new System.Windows.Forms.Timer { Interval = HideAfterStopMilliseconds };
        _hideAfterStopTimer.Tick += (_, _) =>
        {
            _hideAfterStopTimer.Stop();
            HideToTray();
        };

        _controller.LaunchRequested += (_, launchData) => OnLaunchRequested(launchData);
        _controller.StopRequested += (_, _) =>
        {
            _tvApp.GoHome();
            if (_options.HideWhenCastingStops) _hideAfterStopTimer.Start();
        };
        _controller.ReceiverStatusChanged += (_, status) => SetReceiverStatus(status);

        ResumeLayout(performLayout: false);
        _controller.Start();
    }

    // ======================================================================
    //  Showing and hiding
    // ======================================================================

    protected override void SetVisibleCore(bool value)
    {
        // Application.Run makes the main form visible; CastDesk starts in the tray instead.
        // The handle is still created so the window can receive Close().
        if (!_allowVisible)
        {
            value = false;
            if (!IsHandleCreated) CreateHandle();
        }
        base.SetVisibleCore(value);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTheme.UseDarkTitleBar(Handle);
        if (_mediaControls != null) return;
        try
        {
            _mediaControls = new WindowsMediaControls(Handle);
            // Media buttons arrive on a background thread; WebView2 must be called on the UI thread.
            _mediaControls.CommandRequested += (_, command) => BeginInvoke(() => _tvApp.SendMediaCommand(command));
        }
        catch (Exception exception)
        {
            AppLog.Write("host", $"Windows media controls unavailable: {exception.Message}");
        }
    }

    private async void OnLaunchRequested(string launchData)
    {
        ShowWindow();
        if (_options.FullscreenOnCast) EnterFullscreen();
        try
        {
            await _tvApp.LaunchAsync(launchData, _controller.UserAgent, _controller.DialPort);
        }
        catch (Exception exception)
        {
            AppLog.Write("tv", $"Could not open the TV app: {exception}");
            SetReceiverStatus("Could not open YouTube. See castdesk.log.");
        }
    }

    /// <summary>Shows the window and, the first time, the TV app's home screen (Show Player, tray click).</summary>
    private async void ShowPlayer()
    {
        ShowWindow();

        // The browser is created the first time the window shows (WebView2 needs a visible window).
        try
        {
            await _tvApp.ShowHomeIfEmptyAsync(_controller.UserAgent, _controller.DialPort);
            _tvApp.Focus();
        }
        catch (Exception exception)
        {
            AppLog.Write("tv", $"Could not start the TV app browser: {exception}");
            SetReceiverStatus("Could not start the browser. See castdesk.log.");
        }
    }

    private void ShowWindow()
    {
        _hideAfterStopTimer.Stop();
        _allowVisible = true;
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized)
            WindowState = _isFullscreen ? FormWindowState.Normal : _windowedState;
        BringToForeground();
    }

    private void BringToForeground()
    {
        Activate();
        // Windows may refuse to give focus to a background app; briefly making the window
        // topmost still raises it above the other windows.
        TopMost = true;
        TopMost = false;
    }

    /// <summary>
    /// Hides the window and unloads the TV app, so nothing (videos, home-screen previews) plays while
    /// CastDesk sits in the tray. A cast or Show Player loads it again.
    /// </summary>
    private void HideToTray()
    {
        _hideAfterStopTimer.Stop();
        Hide();
        _tvApp.Unload();
        _mediaControls?.Show(MediaState.Nothing);
        _controller.ReportTvAppStopped(); // the phone's next cast launches the TV app again
    }

    private void SetReceiverStatus(string status)
    {
        _statusMenuItem.Text = status;
        var trayText = $"{AppName}: {status}";
        _trayIcon.Text = trayText.Length <= MaxTrayTextLength ? trayText : trayText[..(MaxTrayTextLength - 1)] + "…";
    }

    // ======================================================================
    //  Fullscreen (borderless, covering the taskbar)
    // ======================================================================

    private void ToggleFullscreen()
    {
        if (_isFullscreen) ExitFullscreen();
        else EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        if (_isFullscreen) return;
        _windowedState = WindowState == FormWindowState.Minimized ? FormWindowState.Normal : WindowState;
        _windowedBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _isFullscreen = true;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.FromControl(this).Bounds;
        _tvApp.ShowFullscreenState(true);
    }

    private void ExitFullscreen()
    {
        if (!_isFullscreen) return;
        _isFullscreen = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        Bounds = _windowedBounds;
        WindowState = _windowedState;
        _tvApp.ShowFullscreenState(false);
    }

    // ======================================================================
    //  Keyboard, tray menu and closing
    // ======================================================================

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        // Only reached when the TV app doesn't have focus; inside it, page-bridge.js catches these.
        if (keyData == Keys.F11 || keyData == (Keys.Alt | Keys.Enter))
        {
            ToggleFullscreen();
            return true;
        }
        return base.ProcessCmdKey(ref message, keyData);
    }

    /// <summary>Opens the Settings window (or brings it forward if it is already open) and applies Save.</summary>
    private void OpenSettings()
    {
        if (_settingsForm != null)
        {
            _settingsForm.Activate();
            return;
        }

        using var settingsForm = new SettingsForm(_options, StartupRegistration.IsEnabled);
        _settingsForm = settingsForm;
        try
        {
            if (settingsForm.ShowDialog() != DialogResult.OK || settingsForm.Result is not { } updated) return;

            var problems = new List<string>();
            if (updated.Save() is { } saveProblem) problems.Add($"Could not save the settings: {saveProblem}");
            if (settingsForm.StartWithWindows != StartupRegistration.IsEnabled &&
                StartupRegistration.SetEnabled(settingsForm.StartWithWindows) is { } startupProblem)
                problems.Add($"Could not change the startup setting: {startupProblem}");

            var userAgentChanged = updated.EffectiveUserAgent != _options.EffectiveUserAgent;
            _options = updated;
            _controller.ApplyOptions(updated);
            if (userAgentChanged) _tvApp.ChangeUserAgent(updated.EffectiveUserAgent);

            if (problems.Count > 0)
                MessageBox.Show(string.Join("\n\n", problems), "CastDesk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _settingsForm = null;
        }
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // The window's close button hides to the tray; only Exit (or Windows shutting down) quits.
        if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _trayIcon.Visible = false;
        _controller.Dispose(); // stops discovery and tells phones CastDesk is gone
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideAfterStopTimer.Dispose();
            _audioSessionRenamer.Dispose();
            _trayIcon.ContextMenuStrip?.Dispose();
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }

    private static Icon LoadAppIcon(Size size)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "CastDesk.ico");
        try
        {
            return new Icon(iconPath, size);
        }
        catch (Exception exception) when (exception is IOException or ArgumentException)
        {
            AppLog.Write("host", $"Could not load {iconPath}: {exception.Message}");
            return SystemIcons.Application;
        }
    }
}
