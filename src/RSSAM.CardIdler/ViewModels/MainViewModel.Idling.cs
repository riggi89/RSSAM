using RSSAM.CardIdler.Models;
using RSSAM.CardIdler.Services;

namespace RSSAM.CardIdler.ViewModels;

/// <summary>Badge scans, the idle loop and the game lists (batch + queue).</summary>
public sealed partial class MainViewModel
{
    private readonly Dictionary<int, GameInfo> _games = new();
    private List<GameInfo> _ordered = new();
    private CancellationTokenSource? _idleCts;
    private CancellationTokenSource? _metaCts;
    private DateTime _idleStart, _nextCheck;
    private bool _blocked;
    private bool _useFinalizeLogin;

    /// <summary>Reads the badges page once and shows the games - without idling anything.</summary>
    private async Task ScanAsync()
    {
        var steam = _steam;
        if (steam == null || IsIdling || Scanning) return;
        Scanning = true;
        Status = "Reading your badges page...";
        try
        {
            var rows = await ReadBadgesAsync(steam, CancellationToken.None);
            if (IsIdling) return; // Start was pressed meanwhile - the idle loop does its own scan
            ApplyScan(rows);
            Status = rows.Count == 0
                ? "No card drops left on this account"
                : $"{Plural(rows.Count, "game")} with {Plural(TotalDrops, "card drop")} - press Start to idle";
            AddLog(rows.Count == 0 ? "Scan: no card drops left" : $"Scan: {Plural(rows.Count, "game")}, {Plural(TotalDrops, "drop")} left");
        }
        catch (Exception ex) { ReportScanError(ex); }
        finally { Scanning = false; }
    }

    private void StartIdling()
    {
        if (_steam == null || !IsLoggedIn) return;
        _idleCts?.Cancel();
        var cts = _idleCts = new CancellationTokenSource();
        if (!IsIdling) _idleStart = DateTime.Now;
        State = ConnState.Idling;
        Rebatch(); // start right away with the last scan; the loop rescans immediately anyway
        _clock.Start();
        _ = IdleLoopAsync(_steam, cts.Token);
    }

    private void StopIdling(string? message)
    {
        _idleCts?.Cancel();
        _idleCts = null;
        _clock.Stop();
        try { if (_steam?.IsLoggedOn == true) _steam.SetGamesPlayed(Array.Empty<int>()); } catch { /* connection already gone */ }
        NextCheckText = "-";
        if (IsLoggedIn) State = ConnState.Online;
        Rebatch();
        if (message != null) { Status = message; AddLog(message); }
    }

    /// <summary>How long the games stay stopped at each recheck, so Steam can close the session and grant drops.</summary>
    private static readonly TimeSpan DropSettleTime = TimeSpan.FromSeconds(5);

    private async Task IdleLoopAsync(SteamService steam, CancellationToken ct)
    {
        try
        {
            for (var cycle = 0; !ct.IsCancellationRequested; cycle++)
            {
                // Steam only grants card drops (and updates the badges page) when a play session ENDS,
                // so every recheck stops the games for a moment before reading the page again.
                if (cycle > 0 && !_blocked)
                {
                    Status = "Stopping games for a moment so Steam hands out drops...";
                    steam.SetGamesPlayed(Array.Empty<int>());
                    await Task.Delay(DropSettleTime, ct);
                }

                Status = "Checking your badges for new drops...";
                List<BadgeRow>? pending = null;
                try { pending = await ReadBadgesAsync(steam, ct); }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { ReportScanError(ex); }
                if (ct.IsCancellationRequested) return;

                if (pending == null)
                {
                    Rebatch(); // scan failed - resume the same games anyway
                }
                else
                {
                    ApplyScan(pending); // also resumes the (new) batch
                    if (pending.Count == 0)
                    {
                        StopIdling("All caught up - no card drops left");
                        return;
                    }
                    Status = _blocked ? "Steam is in use elsewhere - waiting" : $"Idling {Plural(NowPlaying.Count, "game")}";
                }

                _nextCheck = DateTime.Now.AddMinutes(RecheckMinutes);
                await Task.Delay(TimeSpan.FromMinutes(RecheckMinutes), ct);
            }
        }
        catch (OperationCanceledException) { /* stopped */ }
        catch (Exception ex) { Log.Write("Idle loop crashed", ex); StopIdling("Idle loop stopped: " + ex.Message); }
    }

    /// <summary>
    /// Reads the badges page as the logged-in owner. Tries the cookie method that worked last time first,
    /// then the other one - a page that comes back as the public view is never treated as "no drops".
    /// </summary>
    private async Task<List<BadgeRow>> ReadBadgesAsync(SteamService steam, CancellationToken ct)
    {
        Exception? last = null;
        foreach (var viaFinalize in new[] { _useFinalizeLogin, !_useFinalizeLogin })
        {
            try
            {
                var cookies = await steam.GetWebCookieHeaderAsync(viaFinalize);
                var result = await BadgeScraper.GetGamesWithDropsAsync(steam.SteamId64.ToString(), cookies, ct);
                _useFinalizeLogin = viaFinalize;
                return result;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Log.Write($"Badge read ({(viaFinalize ? "finalizelogin" : "access token")}) failed: {ex.Message}");
                last = ex;
            }
        }
        throw last!;
    }

    private void ReportScanError(Exception ex)
    {
        if (ex is BadgesNotSignedInException)
        {
            AddLog("Steam would not show your private badge data (web login rejected)");
            Status = "Could not read your drops - web login rejected";
            return;
        }
        Log.Write("Badge check failed", ex);
        AddLog("Could not read badges: " + ex.Message);
        Status = "Badge check failed" + (IsIdling ? " - retrying next cycle" : " - press Rescan to try again");
    }

    /// <summary>Applies a fresh badge scan: keeps GameInfo instances (and their metadata), counts new drops.</summary>
    private void ApplyScan(List<BadgeRow> rows)
    {
        var drops = new List<(string Name, int Count, bool Finished)>();
        var ordered = new List<GameInfo>();
        foreach (var r in rows.OrderByDescending(r => r.HoursOnRecord).ThenBy(r => r.Name))
        {
            if (!_games.TryGetValue(r.AppId, out var g))
            {
                g = _games[r.AppId] = new GameInfo { AppId = r.AppId, Name = r.Name, DropsRemaining = r.DropsRemaining };
                _meta.TryApplyCached(g);
            }
            else if (r.DropsRemaining < g.DropsRemaining)
            {
                drops.Add((g.Name, g.DropsRemaining - r.DropsRemaining, false));
            }
            g.DropsRemaining = r.DropsRemaining;
            g.HoursOnRecord = r.HoursOnRecord;
            ordered.Add(g);
        }

        // The scan only lists games with drops LEFT - a game that got its last drop simply isn't there anymore.
        var keep = ordered.Select(g => g.AppId).ToHashSet();
        foreach (var gone in _games.Values.Where(g => !keep.Contains(g.AppId)).ToList())
        {
            if (gone.DropsRemaining > 0) drops.Add((gone.Name, gone.DropsRemaining, true));
            if (gone.IsSelected) Select(null);
            _games.Remove(gone.AppId);
        }
        ReportDrops(drops);

        _ordered = ordered;
        TotalDrops = ordered.Sum(g => g.DropsRemaining);
        Rebatch();
        if (Selected == null && NowPlaying.Count > 0) Select(NowPlaying[0]);
        LoadMetadata(ordered);
    }

    private void ReportDrops(List<(string Name, int Count, bool Finished)> drops)
    {
        if (drops.Count == 0) return;
        foreach (var d in drops)
            AddLog($"+{Plural(d.Count, "card")} from {d.Name}{(d.Finished ? " - all drops collected" : "")}");
        var total = drops.Sum(d => d.Count);
        CardsEarned += total;
        ShowToast(drops.Count == 1
            ? $"+{Plural(total, "card")} from {drops[0].Name}"
            : $"+{Plural(total, "card")} from {Plural(drops.Count, "game")}");
    }

    /// <summary>Splits the games into the batch (up to ConcurrentGames) and the queue, and tells Steam if idling.</summary>
    private void Rebatch()
    {
        var batch = _ordered.Take(ConcurrentGames).ToHashSet();
        NowPlaying.Clear();
        Queue.Clear();
        foreach (var g in _ordered)
        {
            var inBatch = batch.Contains(g);
            g.IsIdling = IsIdling && inBatch;
            if (!g.IsIdling) g.IdleTime = "";
            (inBatch ? NowPlaying : Queue).Add(g);
        }
        if (IsIdling && !_blocked && _steam?.IsLoggedOn == true) _steam.SetGamesPlayed(NowPlaying.Select(g => g.AppId));
    }

    private void LoadMetadata(List<GameInfo> games)
    {
        _metaCts?.Cancel();
        var cts = _metaCts = new CancellationTokenSource();
        var todo = games.Where(_meta.NeedsFetch).ToList(); // already in batch order
        if (todo.Count == 0) return;
        _ = Task.Run(async () =>
        {
            try { foreach (var g in todo) await _meta.EnsureAsync(g, cts.Token); }
            catch (OperationCanceledException) { }
        });
    }

    private void OnPlayingBlocked(bool blocked)
    {
        _blocked = blocked;
        if (!IsIdling) return;
        if (blocked)
        {
            State = ConnState.Paused;
            Status = "Steam is in use elsewhere - waiting";
            AddLog("Account is playing on another device - idling paused");
        }
        else
        {
            State = ConnState.Idling;
            Rebatch();
            Status = $"Idling {Plural(NowPlaying.Count, "game")}";
            AddLog("Other session ended - idling resumed");
        }
    }

    private void ClearGames()
    {
        _games.Clear();
        _ordered = new();
        Rebatch();
        Select(null);
        TotalDrops = 0;
        CardsEarned = 0;
        Uptime = "00:00:00";
    }
}
