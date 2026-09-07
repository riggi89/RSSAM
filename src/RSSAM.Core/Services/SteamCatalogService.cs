/* Portions derived from Steam Achievement Manager.
 * Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Modified for RSSAM by Daniel Riggi (riggi89), Copyright (c) 2026.
 * See LICENSE.md and NOTICE.md.
 */

using System.Net.Http;
using System.Xml.Linq;
using RSSAM.API;
using RSSAM.Models;
using RSSAM.Interfaces;

namespace RSSAM.Services;

internal sealed class NativeSteamCatalogService : IDisposable
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private readonly ILocalizationService _localization;

    public NativeSteamCatalogService(ILocalizationService localization)
    {
        _localization = localization;
    }

    public async Task<IReadOnlyList<GameInfo>> LoadOwnedGamesAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        string? xml = null;
        try
        {
            progress?.Report(_localization.Get("Core.Catalog.Loading"));
            xml = await _http.GetStringAsync("https://gib.me/sam/games.xml", cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            // Fallback below: installed games only.
        }
        catch (OperationCanceledException)
        {
            // HttpClient timeout: fallback below to installed games.
        }

        try
        {
            return await Task.Run(() =>
            {
                Client? client = null;
                try
                {
                    client = new Client();
                    client.Initialize(0);

                    if (xml is null)
                    {
                        return LoadInstalledGamesFallback(client, progress, cancellationToken);
                    }

                    XDocument doc;
                    try
                    {
                        doc = XDocument.Parse(xml, LoadOptions.None);
                    }
                    catch (System.Xml.XmlException)
                    {
                        return LoadInstalledGamesFallback(client, progress, cancellationToken);
                    }

                    var nodes = doc.Root?.Elements("game") ?? Enumerable.Empty<XElement>();
                    var all = nodes.Select(x => new
                    {
                        Id = uint.TryParse(x.Value, out var id) ? id : 0u,
                        Type = string.IsNullOrWhiteSpace((string?)x.Attribute("type")) ? "normal" : (string)x.Attribute("type")!
                    }).Where(x => x.Id != 0).ToArray();

                    progress?.Report(_localization.Format("Core.Catalog.Checking", all.Length));
                    var games = new List<GameInfo>();
                    foreach (var entry in all)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!client.SteamApps008.IsSubscribedApp(entry.Id)) continue;
                        games.Add(CreateGame(client, entry.Id, entry.Type));
                    }

                    return (IReadOnlyList<GameInfo>)games
                        .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();
                }
                finally
                {
                    client?.Dispose();
                }
            }, cancellationToken);
        }
        catch (ClientInitializeException ex)
        {
            throw new InvalidOperationException(SteamClientErrorFormatter.GetMessage(ex, _localization), ex);
        }
    }

    public async Task<GameInfo> GetOwnedGameAsync(uint appId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                Client? client = null;
                try
                {
                    client = new Client();
                    client.Initialize(0);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!client.SteamApps008.IsSubscribedApp(appId))
                        throw new InvalidOperationException(_localization.Get("Core.Catalog.NotOwned"));
                    return CreateGame(client, appId, "normal");
                }
                finally
                {
                    client?.Dispose();
                }
            }, cancellationToken);
        }
        catch (ClientInitializeException ex)
        {
            throw new InvalidOperationException(SteamClientErrorFormatter.GetMessage(ex, _localization), ex);
        }
    }

    private static GameInfo CreateGame(Client client, uint id, string type)
    {
        var name = client.SteamApps001?.GetAppData(id, "name");
        if (string.IsNullOrWhiteSpace(name)) name = $"App {id}";
        return new GameInfo(id, type, name)
        {
            ImageUrl = GetGameImageUrl(client, id)
        };
    }

    private static string? GetGameImageUrl(Client client, uint id)
    {
        var language = client.SteamApps008.GetCurrentGameLanguage();
        if (string.IsNullOrWhiteSpace(language)) language = "english";

        var candidate = client.SteamApps001?.GetAppData(id, $"small_capsule/{language}");
        if (!string.IsNullOrWhiteSpace(candidate))
            return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{candidate}";

        if (!language.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            candidate = client.SteamApps001?.GetAppData(id, "small_capsule/english");
            if (!string.IsNullOrWhiteSpace(candidate))
                return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{candidate}";
        }

        candidate = client.SteamApps001?.GetAppData(id, "logo");
        return string.IsNullOrWhiteSpace(candidate)
            ? $"https://cdn.cloudflare.steamstatic.com/steam/apps/{id}/capsule_184x69.jpg"
            : $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{id}/{candidate}.jpg";
    }

    private IReadOnlyList<GameInfo> LoadInstalledGamesFallback(Client client, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(_localization.Get("Core.Catalog.Fallback"));
        var result = new Dictionary<uint, GameInfo>();
        var installPath = Steam.GetInstallPath();
        if (string.IsNullOrWhiteSpace(installPath)) return Array.Empty<GameInfo>();

        foreach (var steamApps in EnumerateSteamAppsFolders(installPath)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(steamApps)) continue;

            IEnumerable<string> manifests;
            try
            {
                manifests = Directory.EnumerateFiles(steamApps, "appmanifest_*.acf").ToArray();
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var manifest in manifests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = Path.GetFileNameWithoutExtension(manifest);
                if (!uint.TryParse(file.AsSpan("appmanifest_".Length), out var id)) continue;
                if (!client.SteamApps008.IsSubscribedApp(id)) continue;
                result[id] = CreateGame(client, id, "normal");
            }
        }

        return result.Values.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static IEnumerable<string> EnumerateSteamAppsFolders(string installPath)
    {
        yield return Path.Combine(installPath, "steamapps");

        var file = Path.Combine(installPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(file)) yield break;

        foreach (var line in File.ReadLines(file))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('"') || !trimmed.Contains("\"path\"", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var index = Array.FindIndex(parts, x => x.Equals("path", StringComparison.OrdinalIgnoreCase));
            if (index < 0 || index + 1 >= parts.Length) continue;
            var library = parts[index + 1].Replace("\\\\", "\\");
            if (!string.IsNullOrWhiteSpace(library)) yield return Path.Combine(library, "steamapps");
        }
    }

    public void Dispose() => _http.Dispose();
}
