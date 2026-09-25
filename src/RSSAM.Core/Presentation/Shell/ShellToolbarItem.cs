// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RSSAM.Presentation.Shell;

public enum ShellToolbarItemType
{
    Button,
    ToggleButton,
    Separator,
    Slider
}

public enum ShellToolbarItemPlacement
{
    Left,
    Right
}

public sealed class ShellToolbarItem : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private bool _isChecked;
    private double _value;
    private string _valueText = string.Empty;
    private string _secondaryText = string.Empty;

    public required string Key { get; init; }
    public required string Text { get; init; }
    public string Glyph { get; init; } = string.Empty;
    public string? ToolTip { get; init; }
    public ShellToolbarItemType ItemType { get; init; } = ShellToolbarItemType.Button;
    public ShellToolbarItemPlacement Placement { get; init; } = ShellToolbarItemPlacement.Left;
    public bool IsEnabled { get => _isEnabled; set => Set(ref _isEnabled, value); }
    public bool IsChecked { get => _isChecked; set => Set(ref _isChecked, value); }
    public Action? Execute { get; init; }
    public Action<bool>? Toggle { get; init; }

    public double Minimum { get; init; }
    public double Maximum { get; init; } = 100;
    public double StepFrequency { get; init; } = 1;
    public double SliderWidth { get; init; } = 280;
    public double Value { get => _value; set => Set(ref _value, value); }
    public string ValueText { get => _valueText; set => Set(ref _valueText, value); }
    public string SecondaryText { get => _secondaryText; set => Set(ref _secondaryText, value); }
    public Action<double>? ValueChanged { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
