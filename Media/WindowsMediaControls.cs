using CastDesk.Diagnostics;
using CastDesk.Tv;
using Windows.Media;
using Windows.Storage.Streams;

namespace CastDesk.Media;

/// <summary>
/// CastDesk's entry in Windows' media controls (the media panel in Quick Settings, the volume flyout,
/// and the keyboard's media keys).
/// </summary>
/// <remarks>
/// WebView2 has media controls of its own, but they belong to msedgewebview2.exe and Windows can't tell
/// which app they're for, so it shows "Unknown app". CastDesk turns those off
/// (--disable-features=HardwareMediaKeyHandling) and registers these on its own window instead, so the
/// panel shows CastDesk with the video's title.
/// </remarks>
internal sealed class WindowsMediaControls
{
    private readonly SystemMediaTransportControls _controls;
    private MediaState _shown = MediaState.Nothing;

    public WindowsMediaControls(IntPtr windowHandle)
    {
        _controls = SystemMediaTransportControlsInterop.GetForWindow(windowHandle);
        _controls.IsPlayEnabled = true;
        _controls.IsPauseEnabled = true;
        _controls.IsNextEnabled = false;
        _controls.IsPreviousEnabled = false;
        _controls.IsEnabled = false;
        _controls.ButtonPressed += OnButtonPressed;
    }

    /// <summary>
    /// A media button was pressed: "play", "pause", "togglePlay", "nexttrack" or "previoustrack".
    /// Raised on a background thread.
    /// </summary>
    public event EventHandler<string>? CommandRequested;

    public void Show(MediaState state)
    {
        if (state == _shown) return;
        var metadataChanged = state.Title != _shown.Title || state.Artist != _shown.Artist || state.Artwork != _shown.Artwork;
        _shown = state;

        if (!state.HasVideo)
        {
            _controls.PlaybackStatus = MediaPlaybackStatus.Closed;
            _controls.IsEnabled = false;
            return;
        }

        _controls.IsEnabled = true;
        _controls.IsNextEnabled = state.HasNext;
        _controls.IsPreviousEnabled = state.HasPrevious;
        _controls.PlaybackStatus = state.Playing ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
        if (!metadataChanged) return;

        try
        {
            var display = _controls.DisplayUpdater;
            display.ClearAll();
            display.Type = MediaPlaybackType.Video;
            display.VideoProperties.Title = string.IsNullOrEmpty(state.Title) ? "YouTube" : state.Title;
            display.VideoProperties.Subtitle = state.Artist;
            if (Uri.TryCreate(state.Artwork, UriKind.Absolute, out var artworkUri) && artworkUri.Scheme == Uri.UriSchemeHttps)
                display.Thumbnail = RandomAccessStreamReference.CreateFromUri(artworkUri);
            display.Update();
        }
        catch (Exception exception)
        {
            AppLog.Write("host", $"Could not update the media controls: {exception.Message}");
        }
    }

    private void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        var command = args.Button switch
        {
            SystemMediaTransportControlsButton.Play => "play",
            SystemMediaTransportControlsButton.Pause => "pause",
            SystemMediaTransportControlsButton.Next => "nexttrack",
            SystemMediaTransportControlsButton.Previous => "previoustrack",
            _ => null,
        };
        if (command != null) CommandRequested?.Invoke(this, command);
    }
}
