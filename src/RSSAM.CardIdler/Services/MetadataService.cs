using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
using RSSAM.CardIdler.Models;

namespace RSSAM.CardIdler.Services;

/// <summary>
/// Fetches store metadata (description, developer, genres, release date, Metacritic, price) from Steam's
/// public store API and caches it on disk. Prices are always requested for <see cref="Country"/> - without
/// an explicit country Steam guesses the region from whichever server answers, which is how yen prices
/// ended up in a German account. The API is rate limited, so requests are spaced out.
/// </summary>
public sealed class MetadataService
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread()
        ?? throw new InvalidOperationException("MetadataService must be created on the WinUI thread.");
    private static readonly HttpClient Http = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
    {
        Timeout = TimeSpan.FromSeconds(20),
    };
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(2); // prices change with sales
    private readonly Dictionary<int, GameMeta> _cache = new();
    private readonly string _cachePath = Paths.File("metadata-cache.json");
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MetadataService()
    {
        try
        {
            if (File.Exists(_cachePath))
                _cache = JsonSerializer.Deserialize<Dictionary<int, GameMeta>>(File.ReadAllText(_cachePath)) ?? new();
        }
        catch (Exception ex) { Log.Write("Could not read metadata cache", ex); }
    }

    /// <summary>Store region for prices, two letters ("DE"). Set from the Steam login; Windows region until then.</summary>
    public string Country { get; set; } = RegionInfo.CurrentRegion.TwoLetterISORegionName;

    /// <summary>Shows cached data right away. Returns false if it still needs a fresh fetch (missing, old, other region).</summary>
    public bool TryApplyCached(GameInfo g)
    {
        if (!_cache.TryGetValue(g.AppId, out var m)) return false;
        var sameRegion = string.Equals(m.Country, Country, StringComparison.OrdinalIgnoreCase);
        var fresh = sameRegion && DateTime.UtcNow - m.FetchedUtc < MaxAge;
        Apply(g, m, sameRegion ? m.Price : "", fresh);
        return fresh;
    }

    /// <summary>True until the game has up-to-date data for the current region.</summary>
    public bool NeedsFetch(GameInfo g) => g.MetaCountry != Country;

    /// <summary>Loads metadata for the game (cache first). Returns quietly on failure.</summary>
    public async Task EnsureAsync(GameInfo g, CancellationToken ct)
    {
        if (!NeedsFetch(g) || TryApplyCached(g)) return;

        await _gate.WaitAsync(ct);
        try
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={g.AppId}&cc={Country}&l=english";
            using var res = await Http.GetAsync(url, ct);
            if (res.StatusCode == HttpStatusCode.TooManyRequests) { await Task.Delay(TimeSpan.FromSeconds(30), ct); return; }
            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            // Steam sometimes answers under a different app id (e.g. 730 -> its current base app), so take the only entry.
            var root = doc.RootElement.EnumerateObject().First().Value;
            var meta = root.GetProperty("success").GetBoolean() && root.TryGetProperty("data", out var d)
                ? Parse(d)
                : new GameMeta { Name = g.Name, Description = "No store page available for this game.", Price = "Not on sale" }; // delisted
            meta.Country = Country;
            meta.FetchedUtc = DateTime.UtcNow;

            _cache[g.AppId] = meta;
            Apply(g, meta, meta.Price, fresh: true);
            Save();
            await Task.Delay(1500, ct); // stay well below the store API rate limit
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Log.Write($"Metadata for {g.AppId} failed: {ex.Message}"); }
        finally { _gate.Release(); }
    }

    private static GameMeta Parse(JsonElement d) => new()
    {
        Name = Str(d, "name"),
        Description = WebUtility.HtmlDecode(Regex.Replace(Str(d, "short_description"), "<.*?>", "")).Trim(),
        Developers = Join(d, "developers"),
        ReleaseDate = d.TryGetProperty("release_date", out var rd) ? Str(rd, "date") : "",
        Genres = d.TryGetProperty("genres", out var gs)
            ? string.Join(", ", gs.EnumerateArray().Select(x => Str(x, "description")).Take(4)) : "",
        Metacritic = d.TryGetProperty("metacritic", out var mc) && mc.TryGetProperty("score", out var sc) ? sc.GetInt32().ToString() : "",
        Price = FormatPrice(d),
    };

    /// <summary>"19,99€", "4,99€ (-75%)", "Free" or "Not on sale" - in the store's own formatting for the region.</summary>
    private static string FormatPrice(JsonElement d)
    {
        if (d.TryGetProperty("is_free", out var free) && free.ValueKind == JsonValueKind.True) return "Free";
        if (!d.TryGetProperty("price_overview", out var po)) return "Not on sale";
        var price = Str(po, "final_formatted");
        return po.TryGetProperty("discount_percent", out var dp) && dp.TryGetInt32(out var pct) && pct > 0 ? $"{price} (-{pct}%)" : price;
    }

    private void Save()
    {
        try { File.WriteAllText(_cachePath, JsonSerializer.Serialize(_cache)); }
        catch (Exception ex) { Log.Write("Could not save metadata cache", ex); }
    }

    private void Apply(GameInfo g, GameMeta m, string price, bool fresh)
    {
        var country = Country;
        // Property setters raise PropertyChanged, so this must run on the UI thread.
        void Do()
        {
            g.Description = m.Description; g.Developers = m.Developers; g.Genres = m.Genres;
            g.ReleaseDate = m.ReleaseDate; g.Metacritic = m.Metacritic; g.Price = price;
            g.MetaCountry = fresh ? country : ""; g.MetaLoaded = true;
        }
        if (_dispatcher.HasThreadAccess)
            Do();
        else
            _dispatcher.TryEnqueue(Do);
    }

    private static string Str(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static string Join(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Array
            ? string.Join(", ", v.EnumerateArray().Select(x => x.GetString()).Where(s => !string.IsNullOrEmpty(s))) : "";
}
