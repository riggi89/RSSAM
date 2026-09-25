using System.Text;

namespace RSSAM.CardIdler.Services;

/// <summary>Minimal log file - helps with troubleshooting without the user having to see anything.</summary>
public static class Log
{
    private static readonly object Lock = new();

    public static void Write(string message, Exception? ex = null)
    {
        try
        {
            lock (Lock)
            {
                var path = Paths.File("log.txt");
                var info = new FileInfo(path);
                if (info.Exists && info.Length > 512 * 1024) System.IO.File.Move(path, path + ".old", overwrite: true);

                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("  ").AppendLine(message);
                if (ex != null) sb.AppendLine(ex.ToString());
                System.IO.File.AppendAllText(path, sb.ToString());
            }
        }
        catch
        {
            // Logging itself must never become a problem.
        }
    }
}
