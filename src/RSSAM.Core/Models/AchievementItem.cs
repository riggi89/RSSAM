// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RSSAM.Models;

public sealed class AchievementItem : INotifyPropertyChanged
{
    private bool _isChecked;

    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string? IconNormal { get; init; }
    public string? IconLocked { get; init; }
    public bool IsHidden { get; init; }
    public int Permission { get; init; }
    public bool OriginalState { get; set; }
    public DateTime? UnlockTime { get; init; }

    public bool IsProtected => (Permission & 3) != 0;
    public bool CanEdit => !IsProtected;
    public string ProtectionText => IsProtected ? (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? "Geschützt" : "Protected") : "";
    public string? IconUrl { get; set; }
    public string UnlockText => UnlockTime?.ToString("g") ?? "";

    public bool IsChecked
    {
        get => _isChecked;
        set { if (_isChecked == value) return; _isChecked = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsModified)); }
    }

    public bool IsModified => IsChecked != OriginalState;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
