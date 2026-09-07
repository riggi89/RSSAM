// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Text.Json;
using RSSAM.Models;
using RSSAM.Services;

namespace RSSAM.UnitTests;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaults()
    {
        using var directory = new TemporaryDirectory();
        var settings = new AppSettingsService(directory.Path).Load();

        Assert.Equal(7, settings.SchemaVersion);
        Assert.Equal("de-DE", settings.Language);
        Assert.Equal("Grid", settings.GameLibraryView);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsSettingsAndSearchQueries()
    {
        using var directory = new TemporaryDirectory();
        var service = new AppSettingsService(directory.Path);
        var original = new AppSettings
        {
            Language = "en-US",
            Theme = "Dark",
            WindowWidth = 1600,
            SearchQueries = new Dictionary<string, string>
            {
                ["Games"] = "portal"
            }
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal("en-US", loaded.Language);
        Assert.Equal("Dark", loaded.Theme);
        Assert.Equal(1600, loaded.WindowWidth);
        Assert.Equal("portal", loaded.SearchQueries["games"]);
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public void Load_NormalizesInvalidAndOutOfRangeValues()
    {
        using var directory = new TemporaryDirectory();
        var service = new AppSettingsService(directory.Path);
        File.WriteAllText(service.SettingsPath, """
            {
              "SchemaVersion": 1,
              "Language": "fr-FR",
              "Theme": "Neon",
              "Backdrop": "Glass",
              "NavigationMode": "Expanded",
              "GameLibraryView": "Cards",
              "WindowWidth": 1,
              "WindowHeight": 99999,
              "WindowX": -99999,
              "WindowY": 99999,
              "SearchQueries": null
            }
            """);

        var loaded = service.Load();

        Assert.Equal(7, loaded.SchemaVersion);
        Assert.Equal("de-DE", loaded.Language);
        Assert.Equal("System", loaded.Theme);
        Assert.Equal("Mica", loaded.Backdrop);
        Assert.Equal("Expanded", loaded.NavigationMode);
        Assert.True(loaded.IsNavigationPaneOpen);
        Assert.Equal("Grid", loaded.GameLibraryView);
        Assert.Equal(720, loaded.WindowWidth);
        Assert.Equal(4320, loaded.WindowHeight);
        Assert.Equal(-32768, loaded.WindowX);
        Assert.Equal(32767, loaded.WindowY);
        Assert.Empty(loaded.SearchQueries);
    }

    [Fact]
    public void Load_WhenJsonIsDamaged_ReturnsDefaults()
    {
        using var directory = new TemporaryDirectory();
        var service = new AppSettingsService(directory.Path);
        File.WriteAllText(service.SettingsPath, "{ invalid json");

        var loaded = service.Load();

        Assert.Equal(new AppSettings().Language, loaded.Language);
        Assert.Equal(new AppSettings().WindowWidth, loaded.WindowWidth);
    }

    [Fact]
    public void Reset_RestoresDefaultsAndPersistsThem()
    {
        using var directory = new TemporaryDirectory();
        var service = new AppSettingsService(directory.Path);
        var settings = new AppSettings { Theme = "Dark", WindowWidth = 2000 };

        service.Reset(settings);

        Assert.Equal("System", settings.Theme);
        Assert.Equal(1280, settings.WindowWidth);
        Assert.Equal("System", service.Load().Theme);
        Assert.NotNull(JsonDocument.Parse(File.ReadAllText(service.SettingsPath)));
    }
}
