// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Presentation.Shell;

public enum ShellToolbarItemType
{
    Button,
    ToggleButton
}

public enum ShellToolbarItemPlacement
{
    Left,
    Right
}

public sealed class ShellToolbarItem
{
    public required string Key { get; init; }
    public required string Text { get; init; }
    public string Glyph { get; init; } = "";
    public string? ToolTip { get; init; }
    public ShellToolbarItemType ItemType { get; init; } = ShellToolbarItemType.Button;
    public ShellToolbarItemPlacement Placement { get; init; } = ShellToolbarItemPlacement.Left;
    public bool IsEnabled { get; set; } = true;
    public bool IsChecked { get; set; }
    public Action? Execute { get; init; }
    public Action<bool>? Toggle { get; init; }
}
