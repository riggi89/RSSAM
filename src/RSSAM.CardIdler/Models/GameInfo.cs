using RSSAM.CardIdler.ViewModels;

namespace RSSAM.CardIdler.Models;

/// <summary>One owned game with card drops, plus the store metadata shown on its card.</summary>
public sealed class GameInfo : ObservableObject
{
    private string _name = "";
    private int _dropsRemaining;
    private double _hoursOnRecord;
    private bool _isIdling;
    private bool _isSelected;
    private string _idleTime = "";
    private string _description = "";
    private string _developers = "";
    private string _genres = "";
    private string _releaseDate = "";
    private string _metacritic = "";
    private string _price = "";
    private bool _metaLoaded;

    public int AppId { get; init; }

    public string Name { get => _name; set => Set(ref _name, value); }
    public int DropsRemaining { get => _dropsRemaining; set => Set(ref _dropsRemaining, value); }
    public double HoursOnRecord { get => _hoursOnRecord; set { if (Set(ref _hoursOnRecord, value)) OnPropertyChanged(nameof(HoursText)); } }
    public bool IsIdling { get => _isIdling; set => Set(ref _isIdling, value); }
    public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
    public string IdleTime { get => _idleTime; set => Set(ref _idleTime, value); }

    public string Description { get => _description; set => Set(ref _description, value); }
    public string Developers { get => _developers; set => Set(ref _developers, value); }
    public string Genres { get => _genres; set => Set(ref _genres, value); }
    public string ReleaseDate { get => _releaseDate; set => Set(ref _releaseDate, value); }
    public string Metacritic { get => _metacritic; set { if (Set(ref _metacritic, value)) OnPropertyChanged(nameof(HasMetacritic)); } }
    public string Price { get => _price; set => Set(ref _price, value); }

    /// <summary>Something is there to show (maybe from an older cache entry).</summary>
    public bool MetaLoaded { get => _metaLoaded; set => Set(ref _metaLoaded, value); }

    /// <summary>Store region the current data is up to date for ("" = still needs a fetch).</summary>
    public string MetaCountry { get; set; } = "";

    public bool HasMetacritic => !string.IsNullOrEmpty(_metacritic);
    public string HoursText => _hoursOnRecord > 0 ? $"{_hoursOnRecord:0.#} h played" : "not played yet";

    // Steam's own CDN artwork - needs no API call, so it shows up instantly.
    public string HeaderUrl => $"https://cdn.cloudflare.steamstatic.com/steam/apps/{AppId}/header.jpg";
    public string StoreUrl => $"https://store.steampowered.com/app/{AppId}";
}

/// <summary>Store metadata as cached on disk (metadata-cache.json).</summary>
public sealed class GameMeta
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Developers { get; set; } = "";
    public string Genres { get; set; } = "";
    public string ReleaseDate { get; set; } = "";
    public string Metacritic { get; set; } = "";
    public string Price { get; set; } = "";
    public string Country { get; set; } = "";
    public DateTime FetchedUtc { get; set; }
}
