// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Models;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 7;

    // Language / appearance
    public string Language { get; set; } = "de-DE";
    public string Theme { get; set; } = "System";
    public string Backdrop { get; set; } = "Mica";

    // Shell / navigation
    public string NavigationMode { get; set; } = "Compact";
    public bool IsNavigationPaneOpen { get; set; }
    public bool ShowStatusBar { get; set; } = true;
    public string GameLibraryView { get; set; } = "Grid";

    // Window
    public bool StartMaximized { get; set; }
    public bool RememberWindowPosition { get; set; } = true;
    public int WindowX { get; set; } = 80;
    public int WindowY { get; set; } = 80;
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;
    public bool WindowMaximized { get; set; }

    // Startup / behavior
    public bool LoadGamesOnStartup { get; set; } = true;
    public bool ConfirmBeforeStore { get; set; } = true;
    public bool ConfirmBeforeReset { get; set; } = true;
    public bool ShowSuccessMessages { get; set; } = true;
    public bool EnableStatisticsEditingByDefault { get; set; }

    // Search state
    public Dictionary<string, string> SearchQueries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
