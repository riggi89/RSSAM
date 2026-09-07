// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using RSSAM.Interfaces;
using RSSAM.Localization;
using RSSAM.Models;

namespace RSSAM.Services;

public sealed class SteamCatalogService : IDisposable
{
    private readonly ILocalizationService _localization;
    private readonly SteamWorkerClient _worker;

    public SteamCatalogService(ILocalizationService localization)
    {
        _localization = localization;
        _worker = new SteamWorkerClient(localization);
    }

    public async Task<IReadOnlyList<GameInfo>> LoadOwnedGamesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(_localization.Get("Core.Catalog.Loading"));
        var response = await _worker.ExecuteAsync(
                new SteamWorkerRequest
                {
                    Operation = SteamWorkerOperation.LoadCatalog,
                    Language = _localization.Language
                },
                cancellationToken)
            .ConfigureAwait(false);

        return response.Games;
    }

    public async Task<GameInfo> GetOwnedGameAsync(
        uint appId,
        CancellationToken cancellationToken = default)
    {
        var response = await _worker.ExecuteAsync(
                new SteamWorkerRequest
                {
                    Operation = SteamWorkerOperation.GetOwnedGame,
                    Language = _localization.Language,
                    AppId = appId
                },
                cancellationToken)
            .ConfigureAwait(false);

        return response.Game
            ?? throw new InvalidOperationException(_localization.Get("Core.Steam.UnknownFailure"));
    }

    public void Dispose()
    {
    }
}

public sealed class GameStatsService : IDisposable
{
    private readonly uint _appId;
    private readonly ILocalizationService _localization;
    private readonly SteamWorkerClient _worker;
    private int _disposeState;

    public GameStatsService(uint appId, ILocalizationService localization)
    {
        _appId = appId;
        _localization = localization;
        _worker = new SteamWorkerClient(localization);
        GameName = $"App {appId}";
    }

    public string GameName { get; private set; }

    public async Task<(List<AchievementItem> Achievements, List<StatItem> Statistics)> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var response = await _worker.ExecuteAsync(
                CreateRequest(SteamWorkerOperation.LoadGame),
                cancellationToken)
            .ConfigureAwait(false);

        GameName = string.IsNullOrWhiteSpace(response.GameName)
            ? GameName
            : response.GameName;

        return (response.Achievements, response.Statistics);
    }

    public (int Achievements, int Statistics) Store(
        IEnumerable<AchievementItem> achievements,
        IEnumerable<StatItem> statistics)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(achievements);
        ArgumentNullException.ThrowIfNull(statistics);

        var request = CreateRequest(SteamWorkerOperation.Store);
        request.AchievementUpdates = achievements
            .Where(item => item.IsModified)
            .Select(item => new SteamAchievementUpdate
            {
                Id = item.Id,
                IsChecked = item.IsChecked
            })
            .ToList();
        request.StatisticUpdates = statistics
            .Where(item => item.IsModified)
            .Select(item => new SteamStatisticUpdate
            {
                Id = item.Id,
                EditableValue = item.EditableValue
            })
            .ToList();

        if (request.AchievementUpdates.Count == 0 && request.StatisticUpdates.Count == 0)
            return (0, 0);

        var response = _worker.ExecuteAsync(request, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return (response.ChangedAchievements, response.ChangedStatistics);
    }

    public void ResetAll(bool achievementsToo)
    {
        ThrowIfDisposed();
        var request = CreateRequest(SteamWorkerOperation.Reset);
        request.ResetAchievements = achievementsToo;
        _worker.ExecuteAsync(request, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public void Dispose()
        => Interlocked.Exchange(ref _disposeState, 1);

    private SteamWorkerRequest CreateRequest(SteamWorkerOperation operation)
        => new()
        {
            Operation = operation,
            Language = _localization.Language,
            AppId = _appId
        };

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeState) != 0, this);
}

public static class SteamWorkerServer
{
    internal const string RequestArgumentPrefix = "--RSSAM-steam-worker-request=";
    internal const string ResponseArgumentPrefix = "--RSSAM-steam-worker-response=";

    public static int? TryRunFromCommandLine()
    {
        var arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var requestPath = GetArgumentValue(arguments, RequestArgumentPrefix);
        if (string.IsNullOrWhiteSpace(requestPath))
            return null;

        var responsePath = GetArgumentValue(arguments, ResponseArgumentPrefix);
        if (string.IsNullOrWhiteSpace(responsePath))
            return 2;

        SteamWorkerResponse response;
        try
        {
            var requestJson = File.ReadAllText(requestPath);
            var request = JsonSerializer.Deserialize<SteamWorkerRequest>(requestJson, SteamWorkerJson.Options)
                ?? throw new InvalidDataException("The Steam worker request is empty.");

            response = Task.Run(() => ExecuteAsync(request))
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            response = new SteamWorkerResponse
            {
                Success = false,
                Error = ex.GetBaseException().Message
            };
        }

        try
        {
            File.WriteAllText(
                responsePath,
                JsonSerializer.Serialize(response, SteamWorkerJson.Options));
            return response.Success ? 0 : 1;
        }
        catch
        {
            return 3;
        }
    }

    private static async Task<SteamWorkerResponse> ExecuteAsync(SteamWorkerRequest request)
    {
        var localization = new LocalizationService(request.Language);
        var isGameOperation = request.Operation is
            SteamWorkerOperation.LoadGame or
            SteamWorkerOperation.Store or
            SteamWorkerOperation.Reset;

        if (isGameOperation && request.AppId == 0)
            throw new InvalidDataException("A Steam App ID is required for this operation.");

        Environment.SetEnvironmentVariable(
            "SteamAppId",
            isGameOperation && request.AppId > 0
                ? request.AppId.ToString(CultureInfo.InvariantCulture)
                : null);

        switch (request.Operation)
        {
            case SteamWorkerOperation.LoadCatalog:
            {
                using var service = new NativeSteamCatalogService(localization);
                var games = await service.LoadOwnedGamesAsync().ConfigureAwait(false);
                return new SteamWorkerResponse
                {
                    Success = true,
                    Games = games.ToList()
                };
            }

            case SteamWorkerOperation.GetOwnedGame:
            {
                using var service = new NativeSteamCatalogService(localization);
                var game = await service.GetOwnedGameAsync(request.AppId).ConfigureAwait(false);
                return new SteamWorkerResponse
                {
                    Success = true,
                    Game = game
                };
            }

            case SteamWorkerOperation.LoadGame:
            {
                using var service = new NativeGameStatsService(request.AppId, localization);
                var result = await service.LoadAsync().ConfigureAwait(false);
                return new SteamWorkerResponse
                {
                    Success = true,
                    GameName = service.GameName,
                    Achievements = result.Achievements,
                    Statistics = result.Statistics
                };
            }

            case SteamWorkerOperation.Store:
            {
                using var service = new NativeGameStatsService(request.AppId, localization);
                var current = await service.LoadAsync().ConfigureAwait(false);
                ApplyUpdates(current, request);
                var changed = service.Store(current.Achievements, current.Statistics);
                return new SteamWorkerResponse
                {
                    Success = true,
                    ChangedAchievements = changed.Achievements,
                    ChangedStatistics = changed.Statistics
                };
            }

            case SteamWorkerOperation.Reset:
            {
                using var service = new NativeGameStatsService(request.AppId, localization);
                service.ResetAll(request.ResetAchievements);
                return new SteamWorkerResponse { Success = true };
            }

            default:
                throw new InvalidDataException("Unknown Steam worker operation.");
        }
    }

    private static void ApplyUpdates(
        (List<AchievementItem> Achievements, List<StatItem> Statistics) current,
        SteamWorkerRequest request)
    {
        var achievements = current.Achievements.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var update in request.AchievementUpdates)
        {
            if (achievements.TryGetValue(update.Id, out var item))
                item.IsChecked = update.IsChecked;
        }

        var statistics = current.Statistics.ToDictionary(item => item.Id, StringComparer.Ordinal);
        foreach (var update in request.StatisticUpdates)
        {
            if (statistics.TryGetValue(update.Id, out var item))
                item.EditableValue = update.EditableValue;
        }
    }

    private static string? GetArgumentValue(IEnumerable<string> arguments, string prefix)
    {
        var argument = arguments.FirstOrDefault(
            value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return argument is null ? null : argument[prefix.Length..];
    }
}

internal sealed class SteamWorkerClient
{
    private readonly ILocalizationService _localization;

    public SteamWorkerClient(ILocalizationService localization)
    {
        _localization = localization;
    }

    public async Task<SteamWorkerResponse> ExecuteAsync(
        SteamWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException(
                _localization.Format("Core.Steam.WorkerUnavailable", "Application path unavailable"));
        }

        var workDirectory = Path.Combine(
            Path.GetTempPath(),
            "RSSAM",
            $"steam-worker-{Guid.NewGuid():N}");
        var requestPath = Path.Combine(workDirectory, "request.json");
        var responsePath = Path.Combine(workDirectory, "response.json");

        Directory.CreateDirectory(workDirectory);
        try
        {
            await File.WriteAllTextAsync(
                    requestPath,
                    JsonSerializer.Serialize(request, SteamWorkerJson.Options),
                    cancellationToken)
                .ConfigureAwait(false);

            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            startInfo.ArgumentList.Add($"{SteamWorkerServer.RequestArgumentPrefix}{requestPath}");
            startInfo.ArgumentList.Add($"{SteamWorkerServer.ResponseArgumentPrefix}{responsePath}");

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Steam worker process did not start.");

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            if (!File.Exists(responsePath))
            {
                throw new InvalidOperationException(
                    _localization.Format(
                        "Core.Steam.WorkerUnavailable",
                        $"exit code {process.ExitCode}"));
            }

            var responseJson = await File.ReadAllTextAsync(responsePath, cancellationToken)
                .ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<SteamWorkerResponse>(responseJson, SteamWorkerJson.Options)
                ?? throw new InvalidDataException("The Steam worker response is empty.");

            if (!response.Success)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(response.Error)
                        ? _localization.Get("Core.Steam.UnknownFailure")
                        : response.Error);
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                _localization.Format("Core.Steam.WorkerUnavailable", ex.Message),
                ex);
        }
        finally
        {
            TryDeleteDirectory(workDirectory);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}

internal enum SteamWorkerOperation
{
    LoadCatalog,
    GetOwnedGame,
    LoadGame,
    Store,
    Reset
}

internal sealed class SteamWorkerRequest
{
    public SteamWorkerOperation Operation { get; set; }
    public string Language { get; set; } = "de-DE";
    public uint AppId { get; set; }
    public bool ResetAchievements { get; set; }
    public List<SteamAchievementUpdate> AchievementUpdates { get; set; } = [];
    public List<SteamStatisticUpdate> StatisticUpdates { get; set; } = [];
}

internal sealed class SteamAchievementUpdate
{
    public string Id { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}

internal sealed class SteamStatisticUpdate
{
    public string Id { get; set; } = string.Empty;
    public string EditableValue { get; set; } = string.Empty;
}

internal sealed class SteamWorkerResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<GameInfo> Games { get; set; } = [];
    public GameInfo? Game { get; set; }
    public string? GameName { get; set; }
    public List<AchievementItem> Achievements { get; set; } = [];
    public List<StatItem> Statistics { get; set; } = [];
    public int ChangedAchievements { get; set; }
    public int ChangedStatistics { get; set; }
}

internal static class SteamWorkerJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
