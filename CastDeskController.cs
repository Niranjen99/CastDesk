using System.Net.Sockets;
using CastDesk.Configuration;
using CastDesk.Diagnostics;
using CastDesk.Dial;

namespace CastDesk;

/// <summary>
/// The receiver: makes CastDesk discoverable as a TV (<see cref="SsdpAdvertiser"/>) and accepts the
/// phone's request to launch YouTube (<see cref="DialHttpServer"/>). The player window then opens
/// YouTube's TV app, which handles the casting session itself.
/// Public events are raised on the UI thread (the thread that created the controller).
/// </summary>
public sealed class CastDeskController : IDisposable
{
    private CastDeskOptions _options;
    private readonly SynchronizationContext _uiContext;
    private readonly DialHttpServer _dialServer;
    private readonly SsdpAdvertiser _ssdp;
    private bool _disposed;

    public CastDeskController(CastDeskOptions options)
    {
        _options = options;
        _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        var deviceId = DeviceIdentity.LoadOrCreate();
        AppLog.Write("host", $"Device ID {deviceId}.");

        _dialServer = new DialHttpServer(options.FriendlyName, deviceId);
        _dialServer.LaunchRequested += (_, launchData) => PostToUi(() => LaunchRequested?.Invoke(this, launchData));
        _dialServer.StopRequested += (_, _) => PostToUi(() => StopRequested?.Invoke(this, EventArgs.Empty));
        _ssdp = new SsdpAdvertiser(deviceId, () => _dialServer.Port);
    }

    /// <summary>A phone asked to launch YouTube; the argument is its launch data (pairing code, video).</summary>
    public event EventHandler<string>? LaunchRequested;

    /// <summary>A phone asked to stop YouTube.</summary>
    public event EventHandler? StopRequested;

    /// <summary>One-line receiver status for the tray icon.</summary>
    public event EventHandler<string>? ReceiverStatusChanged;

    public string FriendlyName => _options.FriendlyName;

    /// <summary>The DIAL server's port (valid after <see cref="Start"/>).</summary>
    public int DialPort => _dialServer.Port;

    public string UserAgent => _options.EffectiveUserAgent;

    public void Start()
    {
        try
        {
            _dialServer.Start(_options.DialPort);
            _ssdp.Start();
            RaiseStatus($"Ready. Cast to \"{_options.FriendlyName}\" from the YouTube app.");
        }
        catch (SocketException exception)
        {
            AppLog.Write("host", $"Receiver failed to start: {exception}");
            RaiseStatus($"Receiver not started: {exception.Message}");
        }
    }

    /// <summary>Stops and restarts discovery. Phones may need to reopen their Cast menu.</summary>
    public void RestartReceiver()
    {
        AppLog.Write("host", "Restarting receiver.");
        _ssdp.Stop();
        _dialServer.Stop();
        Start();
    }

    /// <summary>
    /// Uses new settings. Discovery restarts only if the name or port changed; phones may need to
    /// reopen their Cast menu to see the change.
    /// </summary>
    public void ApplyOptions(CastDeskOptions options)
    {
        var receiverChanged = options.FriendlyName != _options.FriendlyName || options.DialPort != _options.DialPort;
        _options = options;
        _dialServer.FriendlyName = options.FriendlyName;
        if (receiverChanged) RestartReceiver();
    }

    /// <summary>Passes the TV app's screen details to the DIAL server, where the phone reads them.</summary>
    public void ReportScreenInfo(IReadOnlyDictionary<string, string> details) => _dialServer.SetAdditionalData(details);

    /// <summary>The TV app is closed or no longer casting.</summary>
    public void ReportTvAppStopped() => _dialServer.MarkStopped();

    private void RaiseStatus(string text)
    {
        AppLog.Write("host", text);
        PostToUi(() => ReceiverStatusChanged?.Invoke(this, text));
    }

    private void PostToUi(Action action) => _uiContext.Post(_ => { if (!_disposed) action(); }, null);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ssdp.Dispose(); // sends ssdp:byebye so phones drop CastDesk straight away
        _dialServer.Dispose();
    }
}
