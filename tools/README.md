# tools

Helper scripts for working on CastDesk. They aren't part of the app (the app project excludes this folder).

| Tool | What it does |
|---|---|
| `readme-shots/` | Renders the README images off-screen: `docs/screenshots/settings.png` (the Settings window, with default settings) and `Assets/logo-64.png` (from the app icon). Nothing appears on screen and your settings file isn't read. |

Retake the screenshots after changing the UI:

```powershell
dotnet build -c Debug                      # from the repo root: builds CastDesk.dll, which the tool loads
dotnet run --project tools\readme-shots
```
