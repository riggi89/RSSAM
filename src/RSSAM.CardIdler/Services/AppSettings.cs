using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RSSAM.CardIdler.Services;

/// <summary>User settings (settings.json) and the saved login (refresh token, encrypted with Windows DPAPI).</summary>
public sealed class AppSettings
{
    public string Username { get; set; } = "";
    public int ConcurrentGames { get; set; } = 32;   // 32 is Steam's own limit
    public int RecheckMinutes { get; set; } = 15;
    public string ProtectedToken { get; set; } = "";

    public static AppSettings Load()
    {
        try
        {
            var path = Paths.File("settings.json");
            if (System.IO.File.Exists(path))
                return JsonSerializer.Deserialize<AppSettings>(System.IO.File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex) { Log.Write("Could not read settings", ex); }
        return new();
    }

    public void Save()
    {
        try { System.IO.File.WriteAllText(Paths.File("settings.json"), JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { Log.Write("Could not save settings", ex); }
    }

    public string? GetToken()
    {
        if (string.IsNullOrEmpty(ProtectedToken)) return null;
        try
        {
            var raw = ProtectedData.Unprotect(Convert.FromBase64String(ProtectedToken), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(raw);
        }
        catch { return null; }
    }

    public void SetToken(string? token)
    {
        ProtectedToken = string.IsNullOrEmpty(token)
            ? ""
            : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser));
        Save();
    }
}
