// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Presentation.Shell;

public interface IShellContentPage
{
    IReadOnlyList<ShellToolbarItem> GetToolbarItems();
    string? SearchContextId { get; }
    string? SearchPlaceholder { get; }
    string StatusText { get; }
    bool CanGoBack { get; }

    event EventHandler? ShellStateChanged;

    void ApplySearch(string query);
    void GoBack();
}
