# CastDesk

A WinForms tray app (.NET 10, C#) that makes a Windows PC a **YouTube cast target**: the phone's YouTube app finds
"CastDesk-<PC name>" in its Cast menu like a smart TV, and CastDesk plays the video in YouTube's own TV app
(youtube.com/tv) hosted in WebView2. YouTube only: Spotify, Netflix and phone screen mirroring were tried and dropped
(see Rules). The README is the user documentation; keep it in step with the app.

## Commands

```powershell
dotnet build -c Debug       # bin\Debug\net10.0-windows10.0.19041.0\win-x64\CastDesk.exe
dotnet build -c Release     # bin\Release\net10.0-windows10.0.19041.0\win-x64\CastDesk.exe
dotnet publish -c Release -o publish
```

`dotnet run --project tools/readme-shots` retakes the README images off-screen (see `tools/README.md`); new helper
scripts go in `tools/`, which the app project excludes.

There are no unit tests: check changes with a build, the log (`%LOCALAPPDATA%\CastDesk\castdesk.log`, `[dial]` lines
show each phone request) and the user's phone. A running CastDesk locks its exe, so stop it before building
(`taskkill /IM CastDesk.exe /F`); it starts hidden in the tray.

To inspect the TV app, start CastDesk once with
`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS="--remote-debugging-port=9333 --remote-debugging-address=127.0.0.1"` and
drive it over CDP. `POST http://localhost:8765/apps/YouTube` opens the TV app as a phone would.

## Layout

- `CastDeskController.cs`: the receiver. Owns `Dial/SsdpAdvertiser` (discovery on UDP 1900) and
  `Dial/DialHttpServer` (device description, `GET/POST /apps/YouTube`, `DELETE /apps/YouTube/run`); events are posted
  to the UI thread.
- `Tv/TvAppView.cs`: WebView2 with the TV user agent and its own profile (`%LOCALAPPDATA%\CastDesk\WebView2`, holds the
  YouTube sign-in). Opens `youtube.com/tv?<phone's launch data>&additionalDataUrl=...`, and answers the TV app's
  screen-details POST inside WebView2 (`WebResourceRequested`) so the phone can read it from `GET /apps/YouTube`.
- `Tv/page-bridge.js` (embedded resource): media state for Windows' media controls, F11/Alt+Enter, the Back and
  Fullscreen overlay buttons.
- `MainForm.cs`: window, tray menu, fullscreen, hide-to-tray (which unloads the TV app so nothing plays hidden).
- `Media/WindowsMediaControls.cs`, `Interop/` (`AudioSessionRenamer`, `AppIdentity`, `ProcessTree`, `WindowTheme`):
  make Windows call the sound and media controls "CastDesk" instead of WebView2 / Unknown app.
- `Configuration/` (`CastDeskOptions` = `%LOCALAPPDATA%\CastDesk\settings.json`, `DeviceIdentity`,
  `StartupRegistration`), `UI/SettingsForm.cs`, `Diagnostics/AppLog.cs`.

## Rules

- **Keep the device ID** (`%LOCALAPPDATA%\CastDesk\device-id.txt`). A new one makes phones list CastDesk twice.
- **YouTube's page enforces Trusted Types and blocks inline `<style>`**: build injected UI with `createElement` and
  `element.style`, never `innerHTML`. Key presses the TV app must obey (Back = Esc) go through CDP
  `Input.dispatchKeyEvent`; script-made key events are untrusted and ignored.
- **YouTube only.** No stream extraction (v1 used YoutubeExplode + LibVLC and hit YouTube's bot check). Netflix can't
  be cast to uncertified devices; Miracast receive needs a UWP `CoreApplicationView` (`StartAsync` gives E_INVALIDARG
  from Win32); AirPlay and Google Cast mirroring would need reverse-engineering. Don't re-propose these.
- **Settings stay loadable:** older `settings.json` files must still load. Add new settings with defaults.

## Testing on the user's PC

- **Don't play audio:** opening the TV app plays previews and videos out loud. Mute through CDP or don't open it, and
  never cast test videos without asking.
- **Don't steal focus without need:** the user may be gaming. Put CastDesk back the way it was (window or tray).
- **Back up `%LOCALAPPDATA%\CastDesk\settings.json` before tests that change settings**, and restore it after.
- Don't delete the WebView2 profile: it signs the user out of YouTube.

## Docs

- README screenshots are in `docs/screenshots`: retake them with `tools/readme-shots` when the UI changes. The player
  window isn't screenshotted (opening the TV app plays sound).
- Write user-facing text and docs in plain, specific English.
- In this environment, write commit messages to a file and use `git commit -F` (Windows PowerShell adds a BOM
  otherwise). Commit and publish only when the user asks.
