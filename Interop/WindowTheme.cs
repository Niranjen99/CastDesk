using System.Runtime.InteropServices;

namespace CastDesk.Interop;

/// <summary>Window styling through DWM.</summary>
internal static class WindowTheme
{
    private const int DwmUseImmersiveDarkMode = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE, Windows 10 20H1 and later

    /// <summary>Gives the window a dark title bar to match the video area. Does nothing on older Windows.</summary>
    public static void UseDarkTitleBar(IntPtr windowHandle)
    {
        var enabled = 1;
        _ = DwmSetWindowAttribute(windowHandle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr windowHandle, int attribute, ref int value, int valueSize);
}
