// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Composition.Navigation;

/// <summary>
/// Stable identifiers used by the RSSAM shell. New pages/modules should add an id here
/// and register their search/toolbar behavior through the shell composition layer.
/// </summary>
public static class NavigationIds
{
    public const string Games = "games";
    public const string Game = "game";
    public const string Changelog = "changelog";
    public const string Settings = "settings";
}
