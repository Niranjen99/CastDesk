using CastDesk.Configuration;
using CastDesk.Diagnostics;
using Microsoft.Web.WebView2.Core;

namespace CastDesk;

internal static class Program
{
    // Two copies would fight over the DIAL port and the phone's session, so only one may run.
    private const string SingleInstanceMutexName = @"Local\CastDesk.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "CastDesk is already running. Look for its icon in the notification area of the taskbar.",
                "CastDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => AppLog.Write("host", $"Unhandled UI exception: {e.Exception}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => AppLog.Write("host", $"Unhandled exception: {e.ExceptionObject}");

        // YouTube's TV app runs in Microsoft Edge WebView2, which Windows 11 includes.
        try
        {
            AppLog.Write("host", $"CastDesk starting (WebView2 {CoreWebView2Environment.GetAvailableBrowserVersionString()}).");
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                "CastDesk needs the Microsoft Edge WebView2 Runtime, which isn't installed.\n\n" +
                "Install it from https://developer.microsoft.com/microsoft-edge/webview2/ and start CastDesk again.",
                "CastDesk", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // So Windows' media controls show "CastDesk" instead of "Unknown app".
        CastDesk.Interop.AppIdentity.Apply("CastDesk", Path.Combine(AppContext.BaseDirectory, "Assets", "CastDesk.ico"));

        Application.Run(new MainForm(CastDeskOptions.Load()));
    }
}
