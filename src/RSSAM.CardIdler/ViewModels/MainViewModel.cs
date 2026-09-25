using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using RSSAM.CardIdler.Models;
using RSSAM.CardIdler.Services;

namespace RSSAM.CardIdler.ViewModels;

public enum ConnState { Offline, Connecting, Online, Idling, Paused }

/// <summary>
/// The whole app state. Split into partial files:
/// this one (bindable state, commands, helpers), .Session (sign-in / Steam Guard / reconnect)
/// and .Idling (badge scans, the idle loop, the game lists).
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IGuardPrompt, IDisposable
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly MetadataService _meta = new();
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread()
        ?? throw new InvalidOperationException("Card Idler must be created on the WinUI thread.");
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    private ConnState _state;
    private string _status = "Sign in to start farming cards";
    private GameInfo? _selected;
    private int _cardsEarned;
    private int _totalDrops;
    private bool _scanning;
    private string _uptime = "00:00:00";
    private string _nextCheckText = "-";
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private string _toast = "";
    private bool _toastVisible;

    public MainViewModel()
    {
        _username = _settings.Username;
        _clock.Tick += (_, _) => Tick();
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); ToastVisible = false; };
        LoginCommand = new RelayCommand(p => _ = LoginAsync(p as string), _ => !Busy && !IsLoggedIn);
        QrLoginCommand = new RelayCommand(_ => _ = LoginWithQrAsync(), _ => !Busy && !IsLoggedIn);
        CancelQrLoginCommand = new RelayCommand(_ => CancelQrLogin(), _ => QrLoginVisible);
        LogoutCommand = new RelayCommand(_ => Logout(), _ => IsLoggedIn);
        StartCommand = new RelayCommand(_ => StartIdling(), _ => IsLoggedIn && !IsIdling);
        StopCommand = new RelayCommand(_ => StopIdling("Idling stopped"), _ => IsIdling);
        RefreshCommand = new RelayCommand(_ => { if (IsIdling) StartIdling(); else _ = ScanAsync(); }, _ => IsLoggedIn && !Scanning);
        SelectCommand = new RelayCommand(p => Select(p as GameInfo));
        SubmitGuardCommand = new RelayCommand(_ => ResolveGuard(GuardCode.Trim()), _ => GuardCode.Trim().Length >= 5);
        CancelGuardCommand = new RelayCommand(_ => ResolveGuard(null));
        OpenStoreCommand = new RelayCommand(p =>
        {
            if (p is GameInfo g) Process.Start(new ProcessStartInfo(g.StoreUrl) { UseShellExecute = true });
        });
    }

    // ----------------------------------------------------------------- bindable state
    /// <summary>The batch: games being idled (or that will be, once Start is pressed).</summary>
    public ObservableCollection<GameInfo> NowPlaying { get; } = new();
    /// <summary>Games with drops that don't fit into the current batch.</summary>
    public ObservableCollection<GameInfo> Queue { get; } = new();
    public ObservableCollection<string> Activity { get; } = new();

    public RelayCommand LoginCommand { get; }
    public RelayCommand QrLoginCommand { get; }
    public RelayCommand CancelQrLoginCommand { get; }
    public RelayCommand LogoutCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand SelectCommand { get; }
    public RelayCommand SubmitGuardCommand { get; }
    public RelayCommand CancelGuardCommand { get; }
    public RelayCommand OpenStoreCommand { get; }

    public ConnState State
    {
        get => _state;
        private set
        {
            if (!Set(ref _state, value)) return;
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(IsIdling));
            OnPropertyChanged(nameof(StateLabel));
            OnPropertyChanged(nameof(BatchTitle));
            RefreshCommands();
        }
    }
    public bool IsLoggedIn => _state is ConnState.Online or ConnState.Idling or ConnState.Paused;
    public bool IsIdling => _state is ConnState.Idling or ConnState.Paused;
    public string StateLabel => _state switch
    {
        ConnState.Offline => "OFFLINE", ConnState.Connecting => "CONNECTING", ConnState.Online => "READY",
        ConnState.Idling => "IDLING", _ => "PAUSED",
    };
    public string BatchTitle => IsIdling ? "NOW PLAYING" : "READY TO IDLE";

    public string Status { get => _status; private set => Set(ref _status, value); }
    public GameInfo? Selected { get => _selected; private set => Set(ref _selected, value); }
    public bool Scanning
    {
        get => _scanning;
        private set
        {
            if (Set(ref _scanning, value))
            {
                OnPropertyChanged(nameof(IsWorking));
                RefreshCommands();
            }
        }
    }
    public bool IsWorking => Busy || Scanning;
    public int CardsEarned { get => _cardsEarned; private set => Set(ref _cardsEarned, value); }
    public int TotalDrops { get => _totalDrops; private set => Set(ref _totalDrops, value); }
    public string Uptime { get => _uptime; private set => Set(ref _uptime, value); }
    public string NextCheckText { get => _nextCheckText; private set => Set(ref _nextCheckText, value); }

    public int ConcurrentGames
    {
        get => _settings.ConcurrentGames;
        set
        {
            var v = Math.Clamp(value, 1, 32);
            if (v == _settings.ConcurrentGames) return;
            _settings.ConcurrentGames = v;
            _settings.Save();
            OnPropertyChanged();
            Rebatch();
        }
    }

    public int RecheckMinutes
    {
        get => _settings.RecheckMinutes;
        set
        {
            var v = Math.Clamp(value, 5, 60);
            if (v == _settings.RecheckMinutes) return;
            _settings.RecheckMinutes = v;
            _settings.Save();
            OnPropertyChanged();
        }
    }

    /// <summary>Fills the module with sample games without contacting Steam; intended for design-time diagnostics.</summary>
    public void LoadDemo()
    {
        AccountName = "demo";
        State = ConnState.Idling;
        Status = "Demo mode - nothing is really being idled";
        _idleStart = DateTime.Now.AddMinutes(-42);
        _clock.Start();
        var rows = new (int, string, int, double)[]
        {
            (730, "Counter-Strike 2", 4, 812.4), (570, "Dota 2", 3, 120.5), (440, "Team Fortress 2", 5, 64.2),
            (292030, "The Witcher 3: Wild Hunt", 2, 96.1), (1091500, "Cyberpunk 2077", 3, 58.8), (1245620, "ELDEN RING", 1, 143),
            (271590, "Grand Theft Auto V", 2, 77.7), (1174180, "Red Dead Redemption 2", 4, 0), (367520, "Hollow Knight", 2, 31.5),
            (413150, "Stardew Valley", 3, 210.9), (1086940, "Baldur's Gate 3", 2, 0), (105600, "Terraria", 1, 88),
        };
        var scan = rows.Select(r => new BadgeRow(r.Item1, r.Item2, r.Item3, r.Item4)).ToList();
        ApplyScan(scan);
        CardsEarned = 7;

        // A few seconds in, pretend a recheck found drops: one from CS2, and Terraria's last one.
        var later = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        later.Tick += (_, _) =>
        {
            later.Stop();
            ApplyScan(scan.Where(r => r.AppId != 105600)
                          .Select(r => r.AppId == 730 ? r with { DropsRemaining = r.DropsRemaining - 1 } : r).ToList());
        };
        later.Start();
    }

    // ----------------------------------------------------------------- small helpers
    private void Select(GameInfo? g)
    {
        if (Selected != null) Selected.IsSelected = false;
        Selected = g;
        if (g == null) return;
        g.IsSelected = true;
        if (_meta.NeedsFetch(g)) _ = Task.Run(async () => { try { await _meta.EnsureAsync(g, CancellationToken.None); } catch { } });
    }

    private void Tick()
    {
        var up = DateTime.Now - _idleStart;
        Uptime = $"{(int)up.TotalHours:00}:{up.Minutes:00}:{up.Seconds:00}";
        foreach (var g in NowPlaying) g.IdleTime = g.IsIdling ? Uptime : "";
        var left = _nextCheck - DateTime.Now;
        NextCheckText = left > TimeSpan.Zero ? $"{(int)left.TotalMinutes:00}:{left.Seconds:00}" : "now";
    }

    /// <summary>Raised when new cards dropped - the window flashes its taskbar button.</summary>
    public event Action? CardsDropped;

    public string Toast { get => _toast; private set => Set(ref _toast, value); }
    public bool ToastVisible { get => _toastVisible; private set => Set(ref _toastVisible, value); }

    private void ShowToast(string text)
    {
        Toast = text;
        ToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
        CardsDropped?.Invoke();
    }

    private void AddLog(string text)
    {
        Activity.Insert(0, $"{DateTime.Now:HH:mm}   {text}");
        while (Activity.Count > 100) Activity.RemoveAt(Activity.Count - 1);
    }

    private static string Plural(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";

    private void Dispatch(Action action)
    {
        if (_dispatcher.HasThreadAccess)
            action();
        else
            _dispatcher.TryEnqueue(() => action());
    }

    private void RefreshCommands()
    {
        LoginCommand?.RaiseCanExecuteChanged();
        QrLoginCommand?.RaiseCanExecuteChanged();
        CancelQrLoginCommand?.RaiseCanExecuteChanged();
        LogoutCommand?.RaiseCanExecuteChanged();
        StartCommand?.RaiseCanExecuteChanged();
        StopCommand?.RaiseCanExecuteChanged();
        RefreshCommand?.RaiseCanExecuteChanged();
        SubmitGuardCommand?.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        _idleCts?.Cancel();
        _qrLoginCts?.Cancel();
        try { _steam?.SetGamesPlayed(Array.Empty<int>()); } catch { }
        _steam?.Dispose();
    }
}
