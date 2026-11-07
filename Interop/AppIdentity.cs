using System.Runtime.InteropServices;
using CastDesk.Diagnostics;
using Microsoft.Win32;

namespace CastDesk.Interop;

/// <summary>
/// Gives CastDesk an app identity Windows can name. Without one, Windows' media controls list CastDesk as
/// "Unknown app": an unpackaged program is identified only by its file name.
/// </summary>
/// <remarks>
/// Sets an explicit AppUserModelID for the process and registers its display name and icon under
/// HKCU\Software\Classes\AppUserModelId\{id}, the documented place for unpackaged apps. That registry entry alone
/// isn't enough for the media controls: they find the name through the Start menu shortcut whose
/// System.AppUserModel.ID is this ID, which the installer creates (installer\Package.wxs). A copy run without
/// installing has no such shortcut and still shows "Unknown app". Keep the ID in step with Package.wxs.
/// </remarks>
internal static class AppIdentity
{
    public const string AppUserModelId = "CastDesk.Receiver";

    /// <summary>Call before the first window is created.</summary>
    public static void Apply(string displayName, string iconPath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{AppUserModelId}");
            key.SetValue("DisplayName", displayName);
            key.SetValue("IconUri", iconPath);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            AppLog.Write("host", $"Could not register CastDesk's app name: {exception.Message}");
        }

        var result = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        if (result != 0) AppLog.Write("host", $"Could not set CastDesk's app ID: 0x{result:X8}");
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
