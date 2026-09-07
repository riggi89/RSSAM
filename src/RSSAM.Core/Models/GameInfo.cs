// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RSSAM.Models;

public sealed class GameInfo : INotifyPropertyChanged
{
    private string _name;
    private string? _imageUrl;
    private bool _isFavorite;

    public GameInfo(uint id, string type, string name)
    {
        if (id == 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Steam App IDs must be greater than zero.");
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Type = type;
        _name = name;
    }

    public uint Id { get; }
    public string Type { get; }
    public string AppIdText => $"App {Id}";

    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; OnPropertyChanged(); }
    }

    public string? ImageUrl
    {
        get => _imageUrl;
        set { if (_imageUrl == value) return; _imageUrl = value; OnPropertyChanged(); }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set { if (_isFavorite == value) return; _isFavorite = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
