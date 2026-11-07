using CastDesk.Diagnostics;
using Microsoft.Win32;

namespace CastDesk.Configuration;

/// <summary>
/// "Start CastDesk when I sign in": an entry under HKCU\Software\Microsoft\Windows\CurrentVersion\Run,
/// so it applies to the current user only and shows in Task Manager's Startup apps list.
/// </summary>
internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CastDesk";

    /// <summary>True when CastDesk starts at sign-in from this exe's current location.</summary>
    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string command &&
                   command.Contains(Environment.ProcessPath ?? "\0", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Turns starting at sign-in on or off. Returns null on success, or what went wrong.</summary>
    public static string? SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
            AppLog.Write("host", enabled ? "CastDesk will start at sign-in." : "CastDesk will not start at sign-in.");
            return null;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            AppLog.Write("host", $"Could not change the startup setting: {exception.Message}");
            return exception.Message;
        }
    }
}
