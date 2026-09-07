// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Text.Json;
using RSSAM.Models;

namespace RSSAM.Services;

public sealed class AppSettingsService
{
    private readonly object _sync = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SettingsDirectory { get; }

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public AppSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RSSAM"))
    {
    }

    public AppSettingsService(string settingsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsDirectory);
        SettingsDirectory = Path.GetFullPath(settingsDirectory);
    }

    public AppSettings Load()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                Normalize(settings);
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_sync)
        {
            SaveCore(settings);
        }
    }

    public void Reset(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_sync)
        {
            var defaults = new AppSettings();
            Copy(defaults, settings);
            SaveCore(settings);
        }
    }

    private void SaveCore(AppSettings settings)
    {
        Normalize(settings);

        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var tempPath = $"{SettingsPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SettingsPath, true);
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

    private static void Copy(AppSettings source, AppSettings target)
    {
        target.SchemaVersion = source.SchemaVersion;
        target.Language = source.Language;
        target.Theme = source.Theme;
        target.Backdrop = source.Backdrop;
        target.NavigationMode = source.NavigationMode;
        target.IsNavigationPaneOpen = source.IsNavigationPaneOpen;
        target.ShowStatusBar = source.ShowStatusBar;
        target.GameLibraryView = source.GameLibraryView;
        target.StartMaximized = source.StartMaximized;
        target.RememberWindowPosition = source.RememberWindowPosition;
        target.WindowX = source.WindowX;
        target.WindowY = source.WindowY;
        target.WindowWidth = source.WindowWidth;
        target.WindowHeight = source.WindowHeight;
        target.WindowMaximized = source.WindowMaximized;
        target.LoadGamesOnStartup = source.LoadGamesOnStartup;
        target.ConfirmBeforeStore = source.ConfirmBeforeStore;
        target.ConfirmBeforeReset = source.ConfirmBeforeReset;
        target.ShowSuccessMessages = source.ShowSuccessMessages;
        target.EnableStatisticsEditingByDefault = source.EnableStatisticsEditingByDefault;
        target.SearchQueries = new Dictionary<string, string>(source.SearchQueries, StringComparer.OrdinalIgnoreCase);
    }

    private static void Normalize(AppSettings settings)
    {
        var previousSchemaVersion = settings.SchemaVersion;

        if (previousSchemaVersion < 4 &&
            string.Equals(settings.NavigationMode, "Expanded", StringComparison.OrdinalIgnoreCase))
        {
            settings.IsNavigationPaneOpen = true;
        }

        settings.SchemaVersion = 7;

        if (settings.Language is not ("de-DE" or "en-US"))
            settings.Language = "de-DE";

        if (settings.Theme is not ("System" or "Light" or "Dark"))
            settings.Theme = "System";

        if (settings.Backdrop is not ("Mica" or "Acrylic" or "Standard"))
            settings.Backdrop = "Mica";

        if (settings.NavigationMode is not ("Compact" or "Expanded"))
            settings.NavigationMode = "Compact";

        if (settings.GameLibraryView is not ("Grid" or "List" or "Table"))
            settings.GameLibraryView = "Grid";

        settings.WindowWidth = Math.Clamp(settings.WindowWidth, 720, 7680);
        settings.WindowHeight = Math.Clamp(settings.WindowHeight, 520, 4320);
        settings.WindowX = Math.Clamp(settings.WindowX, -32768, 32767);
        settings.WindowY = Math.Clamp(settings.WindowY, -32768, 32767);
        settings.SearchQueries ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        settings.SearchQueries = new Dictionary<string, string>(settings.SearchQueries, StringComparer.OrdinalIgnoreCase);
    }
}
