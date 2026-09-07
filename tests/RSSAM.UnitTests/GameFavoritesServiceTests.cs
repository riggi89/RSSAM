// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using RSSAM.Services;

namespace RSSAM.UnitTests;

public sealed class GameFavoritesServiceTests
{
    [Fact]
    public void SetFavorite_PersistsAddAndRemove()
    {
        using var directory = new TemporaryDirectory();
        var service = new GameFavoritesService(directory.Path);

        service.SetFavorite(620, true);
        Assert.True(new GameFavoritesService(directory.Path).IsFavorite(620));

        service.SetFavorite(620, false);
        Assert.False(new GameFavoritesService(directory.Path).IsFavorite(620));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void Load_IgnoresInvalidZeroAndDuplicateIds()
    {
        using var directory = new TemporaryDirectory();
        var path = System.IO.Path.Combine(directory.Path, "favorites.json");
        File.WriteAllText(path, """
            { "SchemaVersion": 1, "FavoriteAppIds": [0, 10, 10, 20] }
            """);

        var service = new GameFavoritesService(directory.Path);

        Assert.False(service.IsFavorite(0));
        Assert.True(service.IsFavorite(10));
        Assert.True(service.IsFavorite(20));
    }

    [Fact]
    public void Load_WhenJsonIsDamaged_StartsWithEmptyFavorites()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "favorites.json"), "not-json");

        var service = new GameFavoritesService(directory.Path);

        Assert.False(service.IsFavorite(10));
    }

    [Fact]
    public void SetFavorite_RejectsInvalidAppId()
    {
        using var directory = new TemporaryDirectory();
        var service = new GameFavoritesService(directory.Path);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.SetFavorite(0, true));
    }
}
