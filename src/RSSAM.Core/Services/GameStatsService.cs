/* Portions derived from Steam Achievement Manager.
 * Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Modified for RSSAM by Daniel Riggi (riggi89), Copyright (c) 2026.
 * See LICENSE.md and NOTICE.md.
 */
using System.Globalization;
using RSSAM.API;
using RSSAM.Infrastructure.SteamSchema;
using RSSAM.Interfaces;
using RSSAM.Models;
using APITypes = RSSAM.API.Types;

namespace RSSAM.Services;

internal sealed class NativeGameStatsService : IDisposable
{
    private readonly long _appId;
    private readonly ILocalizationService _localization;
    private readonly Client _client = new();
    private readonly CancellationTokenSource _pumpCts = new();
    private readonly object _nativeSync = new();
    private readonly List<AchievementDefinition> _achievementDefinitions = new();
    private readonly List<StatDefinition> _statDefinitions = new();
    private readonly RSSAM.API.Callbacks.UserStatsReceived _callback;
    private volatile TaskCompletionSource<APITypes.UserStatsReceived>? _statsReceived;
    private Task? _pumpTask;
    private int _disposeState;
    private int _loadInProgress;

    public NativeGameStatsService(uint appId, ILocalizationService localization)
    {
        _appId = appId;
        _localization = localization;
        try
        {
            _client.Initialize(appId);
            _callback = _client.CreateAndRegisterCallback<RSSAM.API.Callbacks.UserStatsReceived>();
            _callback.OnRun += OnUserStatsReceived;
            _pumpTask = Task.Run(PumpCallbacksAsync);
        }
        catch (ClientInitializeException ex)
        {
            _client.Dispose();
            throw new InvalidOperationException(SteamClientErrorFormatter.GetMessage(ex, _localization), ex);
        }
        catch
        {
            _client.Dispose();
            throw;
        }
    }

    public string GameName
    {
        get
        {
            ThrowIfDisposed();
            lock (_nativeSync)
            {
                return _client.SteamApps001?.GetAppData((uint)_appId, "name") ?? $"App {_appId}";
            }
        }
    }

    public async Task<(List<AchievementItem> Achievements, List<StatItem> Statistics)> LoadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (Interlocked.Exchange(ref _loadInProgress, 1) != 0)
            throw new InvalidOperationException(_localization.Get("Core.Stats.LoadInProgress"));

        var completion = new TaskCompletionSource<APITypes.UserStatsReceived>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _statsReceived = completion;

        try
        {
            CallHandle call;
            lock (_nativeSync)
            {
                var steamId = _client.SteamUser.GetSteamId();
                call = _client.SteamUserStats.RequestUserStats(steamId);
            }

            if (call == CallHandle.Invalid)
                throw new InvalidOperationException(_localization.Get("Core.Stats.RequestRejected"));

            APITypes.UserStatsReceived response;
            try
            {
                response = await completion.Task
                    .WaitAsync(TimeSpan.FromSeconds(20), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(_localization.Get("Core.Stats.Timeout"));
            }

            if (response.Result != 1)
                throw new InvalidOperationException(_localization.Format("Core.Stats.LoadFailed", response.Result));

            if (!LoadUserGameStatsSchema())
                throw new InvalidOperationException(_localization.Get("Core.Stats.SchemaFailed"));

            lock (_nativeSync)
            {
                return (GetAchievements(), GetStatistics());
            }
        }
        finally
        {
            if (ReferenceEquals(_statsReceived, completion))
                _statsReceived = null;

            Interlocked.Exchange(ref _loadInProgress, 0);
        }
    }

    public (int Achievements, int Statistics) Store(IEnumerable<AchievementItem> achievements, IEnumerable<StatItem> statistics)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(achievements);
        ArgumentNullException.ThrowIfNull(statistics);

        var modifiedAchievements = achievements.Where(x => x.IsModified).ToList();
        var modifiedStatistics = statistics.Where(x => x.IsModified).ToList();

        foreach (var item in modifiedAchievements)
        {
            if (item.IsProtected)
                throw new InvalidOperationException(_localization.Format("Core.Stats.AchievementProtected", item.Name));
        }

        foreach (var item in modifiedStatistics)
        {
            ValidateStatistic(item);
        }

        if (modifiedAchievements.Count == 0 && modifiedStatistics.Count == 0)
            return (0, 0);

        lock (_nativeSync)
        {
            foreach (var item in modifiedAchievements)
            {
                if (!_client.SteamUserStats.SetAchievement(item.Id, item.IsChecked))
                    throw new InvalidOperationException(_localization.Format("Core.Stats.AchievementSaveFailed", item.Name));
            }

            foreach (var item in modifiedStatistics)
            {
                var ok = item.Kind == StatValueKind.Integer
                    ? _client.SteamUserStats.SetStatValue(item.Id, GetIntValue(item))
                    : _client.SteamUserStats.SetStatValue(item.Id, GetFloatValue(item));

                if (!ok)
                    throw new InvalidOperationException(_localization.Format("Core.Stats.StatSaveFailed", item.DisplayName));
            }

            if (!_client.SteamUserStats.StoreStats())
                throw new InvalidOperationException(_localization.Get("Core.Stats.StoreFailed"));
        }

        return (modifiedAchievements.Count, modifiedStatistics.Count);
    }

    public void ResetAll(bool achievementsToo)
    {
        ThrowIfDisposed();
        lock (_nativeSync)
        {
            if (!_client.SteamUserStats.ResetAllStats(achievementsToo))
                throw new InvalidOperationException(_localization.Get("Core.Stats.ResetFailed"));
        }
    }

    private void OnUserStatsReceived(APITypes.UserStatsReceived data)
    {
        if ((uint)(data.GameId & 0xFFFFFFFF) != (uint)_appId && data.GameId != (ulong)_appId) return;
        _statsReceived?.TrySetResult(data);
    }

    private async Task PumpCallbacksAsync()
    {
        try
        {
            while (!_pumpCts.IsCancellationRequested)
            {
                lock (_nativeSync)
                {
                    _client.RunCallbacks(false);
                }

                await Task.Delay(100, _pumpCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_pumpCts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _statsReceived?.TrySetException(ex);
        }
    }

    private static string GetLocalizedString(KeyValue kv, string language, string defaultValue)
    {
        var name = kv[language].AsString("");
        if (!string.IsNullOrEmpty(name)) return name;
        if (!language.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            name = kv["english"].AsString("");
            if (!string.IsNullOrEmpty(name)) return name;
        }
        name = kv.AsString("");
        return string.IsNullOrEmpty(name) ? defaultValue : name;
    }

    private bool LoadUserGameStatsSchema()
    {
        var installPath = Steam.GetInstallPath();
        if (string.IsNullOrWhiteSpace(installPath)) return false;
        var path = Path.Combine(installPath, "appcache", "stats", $"UserGameStatsSchema_{_appId}.bin");
        if (!File.Exists(path)) return false;

        var kv = KeyValue.LoadAsBinary(path);
        if (kv is null) return false;
        var language = _localization.SteamLanguage;

        _achievementDefinitions.Clear();
        _statDefinitions.Clear();
        var stats = kv[_appId.ToString(CultureInfo.InvariantCulture)]["stats"];
        if (!stats.Valid || stats.Children is null) return false;

        foreach (var stat in stats.Children)
        {
            if (!stat.Valid) continue;
            APITypes.UserStatType type;
            var typeNode = stat["type"];
            if (!typeNode.Valid ||
                typeNode.Type != KeyValueType.String ||
                !Enum.TryParse(typeNode.AsString(string.Empty), true, out type))
                type = APITypes.UserStatType.Invalid;

            if (type == APITypes.UserStatType.Invalid)
            {
                var typeIntNode = stat["type_int"];
                var rawType = typeIntNode.Valid ? typeIntNode.AsInteger(0) : typeNode.AsInteger(0);
                type = (APITypes.UserStatType)rawType;
            }

            switch (type)
            {
                case APITypes.UserStatType.Invalid:
                    break;
                case APITypes.UserStatType.Integer:
                {
                    var id = stat["name"].AsString("");
                    _statDefinitions.Add(new IntegerStatDefinition
                    {
                        Id = id,
                        DisplayName = GetLocalizedString(stat["display"]["name"], language, id),
                        MinValue = stat["min"].AsInteger(int.MinValue),
                        MaxValue = stat["max"].AsInteger(int.MaxValue),
                        MaxChange = stat["maxchange"].AsInteger(0),
                        IncrementOnly = stat["incrementonly"].AsBoolean(false),
                        SetByTrustedGameServer = stat["bSetByTrustedGS"].AsBoolean(false),
                        DefaultValue = stat["default"].AsInteger(0),
                        Permission = stat["permission"].AsInteger(0)
                    });
                    break;
                }
                case APITypes.UserStatType.Float:
                case APITypes.UserStatType.AverageRate:
                {
                    var id = stat["name"].AsString("");
                    _statDefinitions.Add(new FloatStatDefinition
                    {
                        Id = id,
                        DisplayName = GetLocalizedString(stat["display"]["name"], language, id),
                        MinValue = stat["min"].AsFloat(float.MinValue),
                        MaxValue = stat["max"].AsFloat(float.MaxValue),
                        MaxChange = stat["maxchange"].AsFloat(0),
                        IncrementOnly = stat["incrementonly"].AsBoolean(false),
                        DefaultValue = stat["default"].AsFloat(0),
                        Permission = stat["permission"].AsInteger(0)
                    });
                    break;
                }
                case APITypes.UserStatType.Achievements:
                case APITypes.UserStatType.GroupAchievements:
                    if (stat.Children is not null)
                    {
                        foreach (var bits in stat.Children.Where(b => string.Equals(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            if (!bits.Valid || bits.Children is null) continue;
                            foreach (var bit in bits.Children)
                            {
                                var id = bit["name"].AsString("");
                                _achievementDefinitions.Add(new AchievementDefinition
                                {
                                    Id = id,
                                    Name = GetLocalizedString(bit["display"]["name"], language, id),
                                    Description = GetLocalizedString(bit["display"]["desc"], language, ""),
                                    IconNormal = bit["display"]["icon"].AsString(""),
                                    IconLocked = bit["display"]["icon_gray"].AsString(""),
                                    IsHidden = bit["display"]["hidden"].AsBoolean(false),
                                    Permission = bit["permission"].AsInteger(0)
                                });
                            }
                        }
                    }
                    break;
            }
        }
        return true;
    }

    private List<AchievementItem> GetAchievements()
    {
        var result = new List<AchievementItem>();
        foreach (var def in _achievementDefinitions)
        {
            if (string.IsNullOrWhiteSpace(def.Id)) continue;
            if (!_client.SteamUserStats.GetAchievementAndUnlockTime(def.Id, out var achieved, out var unlockTime)) continue;
            var item = new AchievementItem
            {
                Id = def.Id,
                Name = def.Name.StartsWith('#') ? def.Id : def.Name,
                Description = def.Description,
                IconNormal = def.IconNormal,
                IconLocked = string.IsNullOrWhiteSpace(def.IconLocked) ? def.IconNormal : def.IconLocked,
                Permission = def.Permission,
                IsHidden = def.IsHidden,
                OriginalState = achieved,
                UnlockTime = achieved && unlockTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime : null,
                IsChecked = achieved
            };
            var hash = achieved ? item.IconNormal : item.IconLocked;
            item.IconUrl = string.IsNullOrWhiteSpace(hash) ? null : $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{_appId}/{hash}";
            result.Add(item);
        }
        return result.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private List<StatItem> GetStatistics()
    {
        var result = new List<StatItem>();
        foreach (var stat in _statDefinitions)
        {
            if (string.IsNullOrWhiteSpace(stat.Id)) continue;
            if (stat is IntegerStatDefinition i)
            {
                if (!_client.SteamUserStats.GetStatValue(i.Id, out int value)) continue;
                result.Add(new StatItem
                {
                    Id = i.Id,
                    DisplayName = i.DisplayName,
                    Kind = StatValueKind.Integer,
                    Permission = i.Permission,
                    IsIncrementOnly = i.IncrementOnly,
                    OriginalIntValue = value,
                    MinimumIntValue = i.MinValue,
                    MaximumIntValue = i.MaxValue,
                    MaximumIntChange = i.MaxChange,
                    EditableValue = value.ToString(CultureInfo.CurrentCulture)
                });
            }
            else if (stat is FloatStatDefinition f)
            {
                if (!_client.SteamUserStats.GetStatValue(f.Id, out float value)) continue;
                result.Add(new StatItem
                {
                    Id = f.Id,
                    DisplayName = f.DisplayName,
                    Kind = StatValueKind.Float,
                    Permission = f.Permission,
                    IsIncrementOnly = f.IncrementOnly,
                    OriginalFloatValue = value,
                    MinimumFloatValue = f.MinValue,
                    MaximumFloatValue = f.MaxValue,
                    MaximumFloatChange = f.MaxChange,
                    EditableValue = value.ToString(CultureInfo.CurrentCulture)
                });
            }
        }
        return result.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            return;

        _pumpCts.Cancel();
        try
        {
            _pumpTask?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // Callback pump errors are already forwarded to the active load.
        }

        lock (_nativeSync)
        {
            _callback.OnRun -= OnUserStatsReceived;
            _client.Dispose();
        }

        _statsReceived?.TrySetCanceled();
        _statsReceived = null;
        _pumpCts.Dispose();
    }

    private void ValidateStatistic(StatItem item)
    {
        if (item.IsProtected)
            throw new InvalidOperationException(_localization.Format("Core.Stats.StatProtected", item.DisplayName));

        if (item.Kind == StatValueKind.Integer)
        {
            var value = GetIntValue(item);
            if (value < item.MinimumIntValue || value > item.MaximumIntValue)
            {
                throw new InvalidOperationException(
                    _localization.Format(
                        "Core.Stats.ValueOutOfRange",
                        item.DisplayName,
                        item.MinimumIntValue,
                        item.MaximumIntValue));
            }

            if (item.IsIncrementOnly && value < item.OriginalIntValue)
                throw new InvalidOperationException(_localization.Format("Core.Stats.IncrementOnly", item.DisplayName));

            if (item.MaximumIntChange > 0 &&
                Math.Abs((long)value - item.OriginalIntValue) > item.MaximumIntChange)
            {
                throw new InvalidOperationException(
                    _localization.Format("Core.Stats.MaxChange", item.DisplayName, item.MaximumIntChange));
            }

            return;
        }

        var floatValue = GetFloatValue(item);
        if (floatValue < item.MinimumFloatValue || floatValue > item.MaximumFloatValue)
        {
            throw new InvalidOperationException(
                _localization.Format(
                    "Core.Stats.ValueOutOfRange",
                    item.DisplayName,
                    item.MinimumFloatValue,
                    item.MaximumFloatValue));
        }

        if (item.IsIncrementOnly && floatValue < item.OriginalFloatValue)
            throw new InvalidOperationException(_localization.Format("Core.Stats.IncrementOnly", item.DisplayName));

        if (item.MaximumFloatChange > 0 &&
            Math.Abs(floatValue - item.OriginalFloatValue) > item.MaximumFloatChange)
        {
            throw new InvalidOperationException(
                _localization.Format("Core.Stats.MaxChange", item.DisplayName, item.MaximumFloatChange));
        }
    }

    private int GetIntValue(StatItem item)
        => item.TryGetInt(out var value)
            ? value
            : throw new FormatException(_localization.Format("Core.Stats.InvalidNumber", item.DisplayName));

    private float GetFloatValue(StatItem item)
        => item.TryGetFloat(out var value)
            ? value
            : throw new FormatException(_localization.Format("Core.Stats.InvalidNumber", item.DisplayName));

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
}
