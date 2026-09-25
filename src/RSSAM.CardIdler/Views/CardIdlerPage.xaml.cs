using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using QRCoder;
using RSSAM.CardIdler.Models;
using RSSAM.CardIdler.ViewModels;
using RSSAM.Presentation.Shell;
using Windows.Storage.Streams;
using Windows.System;

namespace RSSAM.CardIdler.Views;

public sealed partial class CardIdlerPage : Page, IShellContentPage
{
    private enum GameViewMode { Tile, List, Detail }

    private static readonly MainViewModel SharedViewModel = new();
    private static bool _autoLoginAttempted;

    private string _searchQuery = string.Empty;
    private GameViewMode _viewMode = GameViewMode.Tile;
    private ShellToolbarItem? _concurrentSliderItem;
    private ShellToolbarItem? _recheckSliderItem;
    private bool _isLoaded;

    public static Func<string, string>? LocalizationResolver { get; set; }

    public MainViewModel ViewModel => SharedViewModel;
    public ObservableCollection<GameInfo> FilteredGames { get; } = new();

    public CardIdlerPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += CardIdlerPage_Loaded;
        Unloaded += CardIdlerPage_Unloaded;
    }

    public string? SearchContextId => ViewModel.IsLoggedIn ? "card-idler-games" : null;
    public string? SearchPlaceholder => ViewModel.IsLoggedIn
        ? T("Search.CardIdler", "Search games with card drops …")
        : null;
    public string StatusText => ViewModel.Status;
    public bool CanGoBack => false;
    public bool IsBusy => ViewModel.IsWorking;

    public event EventHandler? ShellStateChanged;

    public void ApplySearch(string query)
    {
        _searchQuery = query?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    public void GoBack() { }

    public IReadOnlyList<ShellToolbarItem> GetToolbarItems()
    {
        if (!ViewModel.IsLoggedIn)
            return Array.Empty<ShellToolbarItem>();

        var startStop = ViewModel.IsIdling
            ? new ShellToolbarItem
            {
                Key = "card-idler-stop",
                Text = T("CardIdler.Stop", "Stop"),
                ToolTip = T("CardIdler.Stop", "Stop"),
                Glyph = "\uE71A",
                IsEnabled = ViewModel.StopCommand.CanExecute(null),
                Execute = () => ViewModel.StopCommand.Execute(null)
            }
            : new ShellToolbarItem
            {
                Key = "card-idler-start",
                Text = T("CardIdler.Start", "Start idling"),
                ToolTip = T("CardIdler.Start", "Start idling"),
                Glyph = "\uE768",
                IsEnabled = ViewModel.StartCommand.CanExecute(null),
                Execute = () => ViewModel.StartCommand.Execute(null)
            };

        var concurrentSlider = new ShellToolbarItem
        {
            Key = "card-idler-concurrent",
            Text = T("CardIdler.Concurrent", "Games at once"),
            ToolTip = T("CardIdler.Concurrent", "Games at once"),
            ItemType = ShellToolbarItemType.Slider,
            Minimum = 1,
            Maximum = 32,
            StepFrequency = 1,
            SliderWidth = 290,
            Value = ViewModel.ConcurrentGames,
            ValueText = ViewModel.ConcurrentGames.ToString(),
            ValueChanged = value => ViewModel.ConcurrentGames = (int)Math.Round(value)
        };

        var recheckSlider = new ShellToolbarItem
        {
            Key = "card-idler-recheck",
            Text = T("CardIdler.Recheck", "Recheck interval (minutes)"),
            ToolTip = T("CardIdler.Recheck", "Recheck interval (minutes)"),
            ItemType = ShellToolbarItemType.Slider,
            Minimum = 5,
            Maximum = 60,
            StepFrequency = 5,
            SliderWidth = 310,
            Value = ViewModel.RecheckMinutes,
            ValueText = ViewModel.RecheckMinutes.ToString(),
            SecondaryText = ViewModel.NextCheckText,
            ValueChanged = value => ViewModel.RecheckMinutes = (int)Math.Round(value / 5d) * 5
        };

        _concurrentSliderItem = concurrentSlider;
        _recheckSliderItem = recheckSlider;

        return new[]
        {
            startStop,
            new ShellToolbarItem
            {
                Key = "card-idler-rescan",
                Text = T("CardIdler.Rescan", "Rescan"),
                ToolTip = T("CardIdler.Rescan", "Rescan"),
                Glyph = "\uE72C",
                IsEnabled = ViewModel.RefreshCommand.CanExecute(null),
                Execute = () => ViewModel.RefreshCommand.Execute(null)
            },
            new ShellToolbarItem { Key = "card-idler-separator-options", Text = string.Empty, ItemType = ShellToolbarItemType.Separator },
            concurrentSlider,
            recheckSlider,
            CreateViewButton("card-idler-view-tile", "CardIdler.View.Tile", "Tile view", "\uECA5", GameViewMode.Tile),
            CreateViewButton("card-idler-view-list", "CardIdler.View.List", "List view", "\uEA37", GameViewMode.List),
            CreateViewButton("card-idler-view-detail", "CardIdler.View.Detail", "Detail view", "\uE8A0", GameViewMode.Detail),
            new ShellToolbarItem
            {
                Key = "card-idler-separator-account",
                Text = string.Empty,
                ItemType = ShellToolbarItemType.Separator,
                Placement = ShellToolbarItemPlacement.Right
            },
            new ShellToolbarItem
            {
                Key = "card-idler-sign-out",
                Text = T("CardIdler.SignOut", "Sign out"),
                ToolTip = T("CardIdler.SignOut", "Sign out"),
                Glyph = "\uE8AC",
                Placement = ShellToolbarItemPlacement.Right,
                IsEnabled = ViewModel.LogoutCommand.CanExecute(null),
                Execute = () => ViewModel.LogoutCommand.Execute(null)
            }
        };
    }

    public void RefreshLocalization()
    {
        PageTitleText.Text = T("CardIdler.Title", "Card Idler");
        PageSubtitleText.Text = T("CardIdler.Subtitle", "Farm remaining Steam trading-card drops without launching each game.");
        DropsLeftLabelText.Text = T("CardIdler.DropsLeft", "DROPS LEFT");
        CardsEarnedLabelText.Text = T("CardIdler.CardsEarned", "CARDS EARNED");
        SessionLabelText.Text = T("CardIdler.Session", "SESSION");
        SelectGameHintText.Text = T("CardIdler.SelectGame", "Select a game to see its details.");
        DetailDropsLabelText.Text = T("CardIdler.CardDrops", "Card drops");
        DetailPlaytimeLabelText.Text = T("CardIdler.Playtime", "Playtime");
        OpenStoreButton.Content = T("CardIdler.OpenStore", "Open store page");
        ActivityLabelText.Text = T("CardIdler.Activity", "Activity");
        DetailImageColumnText.Text = T("CardIdler.Image", "Image");
        DetailGameColumnText.Text = T("CardIdler.Game", "Game");
        DetailAppIdColumnText.Text = T("CardIdler.AppId", "App ID");
        DetailDropsColumnText.Text = T("CardIdler.Drops", "Drops");
        DetailPlaytimeColumnText.Text = T("CardIdler.Playtime", "Playtime");
        LoginTitleText.Text = T("CardIdler.LoginTitle", "Steam sign-in");
        LoginDescriptionText.Text = T("CardIdler.LoginDescription", "Sign in to scan your remaining card drops and start idling.");
        UsernameLabelText.Text = T("CardIdler.Username", "Steam account name");
        PasswordLabelText.Text = T("CardIdler.Password", "Password");
        StaySignedInCheckBox.Content = T("CardIdler.StaySignedIn", "Stay signed in (encrypted on this PC)");
        LoginButton.Content = T("CardIdler.SignIn", "Sign in");
        LoginOrText.Text = T("CardIdler.Or", "or");
        QrLoginButton.Content = T("CardIdler.QrSignIn", "Sign in with QR code");
        QrTitleText.Text = T("CardIdler.QrTitle", "Sign in with Steam Mobile");
        QrDescriptionText.Text = T("CardIdler.QrDescription", "Open Steam Mobile, scan this code and confirm the sign-in.");
        QrCancelButton.Content = T("Dialog.Cancel", "Cancel");
        GuardTitleText.Text = T("CardIdler.Guard", "Steam Guard");
        GuardWrongText.Text = T("CardIdler.GuardWrong", "The code was not accepted. Enter the newest code.");
        GuardCancelButton.Content = T("Dialog.Cancel", "Cancel");
        GuardConfirmButton.Content = T("CardIdler.Confirm", "Confirm");
        UpdateBatchLabel();
        RaiseShellStateChanged();
    }

    private ShellToolbarItem CreateViewButton(string key, string localizationKey, string fallback, string glyph, GameViewMode mode)
        => new()
        {
            Key = key,
            Text = T(localizationKey, fallback),
            ToolTip = T(localizationKey, fallback),
            Glyph = glyph,
            ItemType = ShellToolbarItemType.ToggleButton,
            Placement = ShellToolbarItemPlacement.Right,
            IsChecked = _viewMode == mode,
            Toggle = isChecked =>
            {
                if (isChecked)
                    SetViewMode(mode);
                else
                    RaiseShellStateChanged();
            }
        };

    private async void CardIdlerPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.NowPlaying.CollectionChanged -= Games_CollectionChanged;
        ViewModel.NowPlaying.CollectionChanged += Games_CollectionChanged;
        ViewModel.Queue.CollectionChanged -= Games_CollectionChanged;
        ViewModel.Queue.CollectionChanged += Games_CollectionChanged;

        RefreshLocalization();
        ApplyFilter();
        UpdateViewVisibility();

        if (!ViewModel.IsLoggedIn)
            UserBox.Focus(FocusState.Programmatic);

        if (!_autoLoginAttempted)
        {
            _autoLoginAttempted = true;
            await ViewModel.TryAutoLoginAsync();
        }
    }

    private void CardIdlerPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.NowPlaying.CollectionChanged -= Games_CollectionChanged;
        ViewModel.Queue.CollectionChanged -= Games_CollectionChanged;
    }

    private void Games_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ApplyFilter();

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isLoaded)
            return;

        switch (e.PropertyName)
        {
            case nameof(MainViewModel.NextCheckText):
                if (_recheckSliderItem is not null)
                    _recheckSliderItem.SecondaryText = ViewModel.NextCheckText;
                break;
            case nameof(MainViewModel.ConcurrentGames):
                if (_concurrentSliderItem is not null)
                {
                    _concurrentSliderItem.Value = ViewModel.ConcurrentGames;
                    _concurrentSliderItem.ValueText = ViewModel.ConcurrentGames.ToString();
                }
                break;
            case nameof(MainViewModel.RecheckMinutes):
                if (_recheckSliderItem is not null)
                {
                    _recheckSliderItem.Value = ViewModel.RecheckMinutes;
                    _recheckSliderItem.ValueText = ViewModel.RecheckMinutes.ToString();
                }
                break;
            case nameof(MainViewModel.QrChallengeUrl):
                _ = RenderQrCodeAsync(ViewModel.QrChallengeUrl);
                break;
            case nameof(MainViewModel.IsLoggedIn):
                if (!ViewModel.IsLoggedIn)
                {
                    _searchQuery = string.Empty;
                    QrCodeImage.Source = null;
                }
                ApplyFilter();
                RaiseShellStateChanged();
                break;
            case nameof(MainViewModel.IsIdling):
            case nameof(MainViewModel.Scanning):
            case nameof(MainViewModel.Busy):
            case nameof(MainViewModel.IsWorking):
            case nameof(MainViewModel.Status):
                UpdateBatchLabel();
                RaiseShellStateChanged();
                break;
        }
    }

    private void SetViewMode(GameViewMode mode)
    {
        if (_viewMode == mode)
            return;

        _viewMode = mode;
        UpdateViewVisibility();
        RaiseShellStateChanged();
    }

    private void UpdateViewVisibility()
    {
        TileGamesView.Visibility = _viewMode == GameViewMode.Tile ? Visibility.Visible : Visibility.Collapsed;
        ListGamesView.Visibility = _viewMode == GameViewMode.List ? Visibility.Visible : Visibility.Collapsed;
        DetailGamesPanel.Visibility = _viewMode == GameViewMode.Detail ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyFilter()
    {
        var selectedId = ViewModel.Selected?.AppId;
        var query = _searchQuery;
        var games = ViewModel.NowPlaying
            .Concat(ViewModel.Queue)
            .GroupBy(game => game.AppId)
            .Select(group => group.First())
            .Where(game => query.Length == 0 ||
                           game.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                           game.AppId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           game.Developers.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                           game.Genres.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(game => game.IsIdling)
            .ThenBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        FilteredGames.Clear();
        foreach (var game in games)
            FilteredGames.Add(game);

        if (selectedId is int appId && games.All(game => game.AppId != appId))
            ViewModel.SelectCommand.Execute(null);
    }

    private void UpdateBatchLabel()
        => BatchLabelText.Text = ViewModel.IsIdling
            ? T("CardIdler.NowPlaying", "NOW PLAYING")
            : T("CardIdler.Ready", "READY TO IDLE");

    private async Task RenderQrCodeAsync(string challengeUrl)
    {
        if (string.IsNullOrWhiteSpace(challengeUrl))
        {
            QrCodeImage.Source = null;
            return;
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(challengeUrl, QRCodeGenerator.ECCLevel.M);
            using var code = new PngByteQRCode(data);
            var bytes = code.GetGraphic(8);

            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                writer.DetachStream();
            }

            stream.Seek(0);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);
            if (challengeUrl == ViewModel.QrChallengeUrl)
                QrCodeImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            ViewModel.ReportQrRenderingError(ex.Message);
        }
    }

    private void GameList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GameInfo game)
            ViewModel.SelectCommand.Execute(game);
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.LoginCommand.Execute(PasswordBox.Password);

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.LoginCommand.CanExecute(null))
        {
            ViewModel.LoginCommand.Execute(PasswordBox.Password);
            e.Handled = true;
        }
    }

    private void GuardCodeBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.SubmitGuardCommand.CanExecute(null))
        {
            ViewModel.SubmitGuardCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void OpenStoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Selected is not null)
            await Launcher.LaunchUriAsync(new Uri(ViewModel.Selected.StoreUrl));
    }

    private void RaiseShellStateChanged()
        => ShellStateChanged?.Invoke(this, EventArgs.Empty);

    private static string T(string key, string fallback)
    {
        var value = LocalizationResolver?.Invoke(key);
        return string.IsNullOrWhiteSpace(value) || value == key ? fallback : value;
    }
}
