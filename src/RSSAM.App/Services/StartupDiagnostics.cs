// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Runtime.InteropServices;
using System.Text;

namespace RSSAM.Services;

/// <summary>
/// Records failures that happen before the WinUI shell and InfoBar are available.
/// </summary>
internal static class StartupDiagnostics
{
    private const long MaximumLogLength = 2 * 1024 * 1024;
    private const uint MessageBoxOk = 0x00000000;
    private const uint MessageBoxIconError = 0x00000010;

    private static readonly object Sync = new();
    private static readonly Encoding LogEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static int _fatalDialogShown;

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RSSAM",
        "Logs");

    public static string LogPath => Path.Combine(LogDirectory, "startup.log");

    public static void StartSession()
    {
        RotateLogIfNeeded();
        Write(
            $"Session started | Version={AppVersion.Display} | " +
            $"Process={RuntimeInformation.ProcessArchitecture} | " +
            $"OS={RuntimeInformation.OSDescription} | " +
            $"BaseDirectory={AppContext.BaseDirectory}");
    }

    public static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    LogPath,
                    $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {message}{Environment.NewLine}",
                    LogEncoding);
            }
        }
        catch
        {
            // Diagnostics must never become a second startup failure.
        }
    }

    public static void WriteException(string stage, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write($"{stage}{Environment.NewLine}{exception}");
    }

    public static void ReportFatal(string stage, Exception exception)
    {
        WriteException(stage, exception);

        if (Interlocked.Exchange(ref _fatalDialogShown, 1) != 0)
            return;

        try
        {
            var rootException = exception.GetBaseException();
            var message =
                "RSSAM could not be started.\n\n" +
                $"{rootException.Message}\n\n" +
                $"Startup log:\n{LogPath}";

            _ = MessageBoxW(
                IntPtr.Zero,
                message,
                "RSSAM startup error",
                MessageBoxOk | MessageBoxIconError);
        }
        catch
        {
            // The log remains available even if Windows cannot display the dialog.
        }
    }

    private static void RotateLogIfNeeded()
    {
        try
        {
            lock (Sync)
            {
                if (!File.Exists(LogPath) || new FileInfo(LogPath).Length < MaximumLogLength)
                    return;

                Directory.CreateDirectory(LogDirectory);
                File.Move(LogPath, Path.Combine(LogDirectory, "startup.previous.log"), overwrite: true);
            }
        }
        catch
        {
            // A failed rotation must not prevent app startup or further log attempts.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(
        IntPtr windowHandle,
        string text,
        string caption,
        uint type);
}
