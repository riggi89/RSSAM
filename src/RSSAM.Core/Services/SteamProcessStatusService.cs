// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Diagnostics;

namespace RSSAM.Services;

/// <summary>Provides a lightweight, exception-safe Steam process status check.</summary>
public static class SteamProcessStatusService
{
    public static bool IsSteamRunning()
    {
        Process[] processes = [];

        try
        {
            processes = Process.GetProcessesByName("steam");
            return processes.Any(process => !process.HasExited);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        finally
        {
            foreach (var process in processes)
                process.Dispose();
        }
    }
}
