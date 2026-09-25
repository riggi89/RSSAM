using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace RSSAM.CardIdler.Services;

public sealed record BadgeRow(int AppId, string Name, int DropsRemaining, double HoursOnRecord);

/// <summary>
/// The badges page came back as the public view. Steam only shows "card drops remaining" (and games
/// whose badge isn't crafted yet) to the logged-in owner, so this page can't be trusted.
/// </summary>
public sealed class BadgesNotSignedInException : Exception
{
    public BadgesNotSignedInException() : base("Steam served the public badges page (web login not accepted).") { }
}

/// <summary>
/// Reads the account's badges pages and returns every game that still has card drops left.
/// (Same approach as the original Node script - there is no official API for "drops remaining".)
/// Parsing goes by the page structure, not by English wording, so it works whatever language Steam answers in.
/// </summary>
public static class BadgeScraper
{
    private static readonly HttpClient Http = new(new HttpClientHandler { UseCookies = false, AutomaticDecompression = DecompressionMethods.All })
    {
        Timeout = TimeSpan.FromSeconds(30),
    };
    private static readonly Regex RowStartRe = new("class=\"badge_row[\\s\"]");
    private static readonly Regex AppRe =new(@"gamecards/(\d+)");
    private static readonly Regex TitleRe = new("class=\"badge_title\">\\s*(.*?)\\s*(?:&nbsp;|<)", RegexOptions.Singleline);
    private static readonly Regex DropsBlockRe = new("class=\"badge_title_stats_drops\">(.*?)</div>", RegexOptions.Singleline);
    private static readonly Regex PlaytimeBlockRe = new("class=\"badge_title_stats_playtime\">(.*?)</div>", RegexOptions.Singleline);
    private static readonly Regex BoldRe = new("progress_info_bold\"?>([^<]*)<");
    private static readonly Regex IntRe = new(@"\d+");
    private static readonly Regex NumberRe = new(@"\d[\d.,]*");
    private const int MaxPages = 60;

    public static async Task<List<BadgeRow>> GetGamesWithDropsAsync(string steamId64, string cookieHeader, CancellationToken ct)
    {
        var results = new Dictionary<int, BadgeRow>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var html = await FetchAsync(steamId64, page, cookieHeader, ct);
            if (page == 1)
            {
                // Kept for troubleshooting: the last page Steam actually served.
                try { System.IO.File.WriteAllText(Paths.File("last-badges-page.html"), html); } catch { }
                if (!html.Contains($"g_steamID = \"{steamId64}\"")) throw new BadgesNotSignedInException();
            }

            var rows = ParseRows(html);
            foreach (var r in rows) results[r.AppId] = r;

            if (rows.Count == 0 || !html.Contains($"?p={page + 1}")) break; // no further page
        }

        Log.Write($"Badge scan: {results.Count} game badge(s), {results.Values.Count(r => r.DropsRemaining > 0)} with drops left");
        return results.Values.Where(r => r.DropsRemaining > 0).ToList();
    }

    /// <summary>
    /// Splits the page into one chunk per badge. Only the row element itself counts - "badge_row_overlay" and
    /// "badge_row_inner" sit inside every row and must not start a new chunk (that would separate the app id
    /// from its drop count).
    /// </summary>
    internal static List<BadgeRow> ParseRows(string html)
    {
        var rows = new List<BadgeRow>();
        foreach (var chunk in RowStartRe.Split(html).Skip(1))
        {
            var app = AppRe.Match(chunk);
            if (!app.Success) continue; // sale / event badges have no gamecards link
            var id = int.Parse(app.Groups[1].Value);
            var title = TitleRe.Match(chunk);
            var name = title.Success ? WebUtility.HtmlDecode(title.Groups[1].Value).Trim() : "";
            rows.Add(new BadgeRow(id, name.Length > 0 ? name : $"App {id}", ParseDrops(chunk), ParseHours(chunk)));
        }
        return rows;
    }

    /// <summary>"3 card drops remaining" / "Noch 3 Karten..." -> 3; "No card drops remaining" -> 0.</summary>
    private static int ParseDrops(string row)
    {
        var block = DropsBlockRe.Match(row);
        if (!block.Success) return 0;
        var bold = BoldRe.Match(block.Groups[1].Value);
        var n = IntRe.Match(bold.Success ? bold.Groups[1].Value : block.Groups[1].Value);
        return n.Success ? int.Parse(n.Value) : 0;
    }

    /// <summary>"1,234.5 hrs on record" / "12,3 Std. insgesamt" -> hours.</summary>
    private static double ParseHours(string row)
    {
        var block = PlaytimeBlockRe.Match(row);
        if (!block.Success) return 0;
        var m = NumberRe.Match(WebUtility.HtmlDecode(block.Groups[1].Value));
        if (!m.Success) return 0;
        var s = m.Value.TrimEnd('.', ',');
        // The last separator is the decimal point if at most 2 digits follow it; everything else is grouping.
        var sep = s.LastIndexOfAny(new[] { '.', ',' });
        var intPart = sep >= 0 && s.Length - sep - 1 <= 2 ? s[..sep] : s;
        var frac = sep >= 0 && s.Length - sep - 1 <= 2 ? s[(sep + 1)..] : "";
        var text = new string(intPart.Where(char.IsDigit).ToArray()) + (frac.Length > 0 ? "." + frac : "");
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) ? h : 0;
    }

    private static async Task<string> FetchAsync(string steamId64, int page, string cookieHeader, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://steamcommunity.com/profiles/{steamId64}/badges/?p={page}&l=english");
        req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Card Idler");
        req.Headers.TryAddWithoutValidation("Cookie", cookieHeader + "; Steam_Language=english");
        using var res = await Http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) throw new HttpRequestException($"Badges page returned HTTP {(int)res.StatusCode}");
        return await res.Content.ReadAsStringAsync(ct);
    }
}
