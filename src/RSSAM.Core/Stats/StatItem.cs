// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RSSAM.Models;

public enum StatValueKind { Integer, Float }

public sealed class StatItem : INotifyPropertyChanged
{
    private string _editableValue = "";
    private bool _editingEnabled;

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required StatValueKind Kind { get; init; }
    public int Permission { get; init; }
    public bool IsIncrementOnly { get; init; }
    public int OriginalIntValue { get; init; }
    public float OriginalFloatValue { get; init; }
    public int MinimumIntValue { get; init; } = int.MinValue;
    public int MaximumIntValue { get; init; } = int.MaxValue;
    public int MaximumIntChange { get; init; }
    public float MinimumFloatValue { get; init; } = float.MinValue;
    public float MaximumFloatValue { get; init; } = float.MaxValue;
    public float MaximumFloatChange { get; init; }

    public bool IsProtected => (Permission & 2) != 0;
    public bool IsReadOnly => IsProtected || !_editingEnabled;
    public string Flags
    {
        get
        {
            var german = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
            return string.Join(", ", new[]
            {
                IsIncrementOnly ? (german ? "Nur erhöhen" : "Increment only") : null,
                IsProtected ? (german ? "Geschützt" : "Protected") : null,
                (Permission & ~2) != 0 ? (german ? "Unbekannte Berechtigung" : "Unknown permission") : null
            }.Where(x => x is not null)) is { Length: > 0 } flags ? flags : "-";
        }
    }

    public string EditableValue
    {
        get => _editableValue;
        set { if (_editableValue == value) return; _editableValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsModified)); }
    }

    public bool IsModified
    {
        get
        {
            if (Kind == StatValueKind.Integer)
            {
                if (int.TryParse(EditableValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value))
                    return value != OriginalIntValue;
                return !string.Equals(EditableValue, OriginalIntValue.ToString(CultureInfo.CurrentCulture), StringComparison.CurrentCulture);
            }

            if (float.TryParse(EditableValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var floatValue))
                return !floatValue.Equals(OriginalFloatValue);
            return !string.Equals(EditableValue, OriginalFloatValue.ToString(CultureInfo.CurrentCulture), StringComparison.CurrentCulture);
        }
    }

    public bool TryGetInt(out int value) => int.TryParse(EditableValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    public bool TryGetFloat(out float value)
        => float.TryParse(EditableValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value) &&
           float.IsFinite(value);

    public void SetEditingEnabled(bool enabled)
    {
        _editingEnabled = enabled;
        OnPropertyChanged(nameof(IsReadOnly));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
