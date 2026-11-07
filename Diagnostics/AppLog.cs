using System.Diagnostics;
using System.Text;
using CastDesk.Configuration;

namespace CastDesk.Diagnostics;

/// <summary>
/// Appends timestamped lines from the app and the bridge to %LOCALAPPDATA%\CastDesk\castdesk.log.
/// </summary>
internal static class AppLog
{
    private const long MaxLogBytes = 1_000_000;

    private static readonly object WriterLock = new();
    private static StreamWriter? _writer;

    public static string FilePath { get; } = Path.Combine(CastDeskOptions.DataDirectory, "castdesk.log");

    /// <param name="source">Who wrote the line, such as "host" or "bridge".</param>
    public static void Write(string source, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{source}] {message}";
        Debug.WriteLine(line);
        lock (WriterLock)
        {
            try
            {
                _writer ??= OpenWriter();
                _writer.WriteLine(line);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Logging must never take the app down.
            }
        }
    }

    private static StreamWriter OpenWriter()
    {
        Directory.CreateDirectory(CastDeskOptions.DataDirectory);

        // Once the log passes 1 MB, start a new one and keep the previous one as castdesk.log.old.
        var existingLog = new FileInfo(FilePath);
        if (existingLog.Exists && existingLog.Length > MaxLogBytes)
            File.Move(FilePath, FilePath + ".old", overwrite: true);

        return new StreamWriter(FilePath, append: true, new UTF8Encoding(false)) { AutoFlush = true };
    }
}
