using CastDesk.Diagnostics;

namespace CastDesk.Configuration;

/// <summary>
/// The ID CastDesk announces on the network. Phones remember a receiver by this ID, so it must stay the
/// same across restarts, or each start adds another "CastDesk" to the phone's Cast menu.
/// Saved in %LOCALAPPDATA%\CastDesk\device-id.txt (the same file earlier versions used).
/// </summary>
internal static class DeviceIdentity
{
    public static string FilePath { get; } = Path.Combine(CastDeskOptions.DataDirectory, "device-id.txt");

    public static string LoadOrCreate()
    {
        try
        {
            if (File.Exists(FilePath) && Guid.TryParse(File.ReadAllText(FilePath).Trim(), out var saved))
                return saved.ToString("D");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AppLog.Write("host", $"Could not read {FilePath}: {exception.Message}");
        }

        var created = Guid.NewGuid().ToString("D");
        try
        {
            Directory.CreateDirectory(CastDeskOptions.DataDirectory);
            File.WriteAllText(FilePath, created);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            AppLog.Write("host", $"Could not save the device ID; phones may list CastDesk twice after a restart: {exception.Message}");
        }
        return created;
    }
}
