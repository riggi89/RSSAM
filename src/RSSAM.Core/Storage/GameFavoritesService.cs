// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Text.Json;

namespace RSSAM.Services;

public sealed class GameFavoritesService
{
    private const int CurrentSchemaVersion = 1;
    private readonly HashSet<uint> _favoriteAppIds = [];
    private readonly object _sync = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string FavoritesDirectory { get; }

    public string FavoritesPath => Path.Combine(FavoritesDirectory, "favorites.json");

    public GameFavoritesService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RSSAM"))
    {
    }

    public GameFavoritesService(string favoritesDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(favoritesDirectory);
        FavoritesDirectory = Path.GetFullPath(favoritesDirectory);
        LoadCore();
    }

    public bool IsFavorite(uint appId)
    {
        lock (_sync)
            return _favoriteAppIds.Contains(appId);
    }

    public void SetFavorite(uint appId, bool isFavorite)
    {
        if (appId == 0)
            throw new ArgumentOutOfRangeException(nameof(appId), "Steam App IDs must be greater than zero.");

        lock (_sync)
        {
            var changed = isFavorite
                ? _favoriteAppIds.Add(appId)
                : _favoriteAppIds.Remove(appId);

            if (!changed)
                return;

            try
            {
                SaveCore();
            }
            catch
            {
                if (isFavorite)
                    _favoriteAppIds.Remove(appId);
                else
                    _favoriteAppIds.Add(appId);

                throw;
            }
        }
    }

    private void LoadCore()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(FavoritesPath))
                    return;

                var json = File.ReadAllText(FavoritesPath);
                var document = JsonSerializer.Deserialize<GameFavoritesDocument>(json, JsonOptions);
                if (document?.FavoriteAppIds is null)
                    return;

                _favoriteAppIds.Clear();
                foreach (var appId in document.FavoriteAppIds.Where(id => id > 0))
                    _favoriteAppIds.Add(appId);
            }
            catch
            {
                // A damaged favorites file must not prevent RSSAM from starting.
                _favoriteAppIds.Clear();
            }
        }
    }

    private void SaveCore()
    {
        Directory.CreateDirectory(FavoritesDirectory);
        var document = new GameFavoritesDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            FavoriteAppIds = _favoriteAppIds.OrderBy(id => id).ToList()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{FavoritesPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FavoritesPath, true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // A stale temporary file must not hide the original save result.
            }
        }
    }

    private sealed class GameFavoritesDocument
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public List<uint> FavoriteAppIds { get; set; } = [];
    }
}
