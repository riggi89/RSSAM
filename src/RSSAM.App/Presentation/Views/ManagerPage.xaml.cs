// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using RSSAM.Models;
using RSSAM.Presentation.Shell;
using RSSAM.Services;

namespace RSSAM.Views;

public sealed partial class ManagerPage : Page, IShellContentPage
{
    private enum GameLibraryViewMode
    {
        Grid,
        List,
        Table
    }

    private readonly SteamCatalogService _catalogService = new(App.LocalizationService);
    private readonly ObservableCollection<GameInfo> _games = new();
    private readonly List<AchievementItem> _allAchievements = new();
    private readonly List<StatItem> _allStatistics = new();
    private readonly SemaphoreSlim _gameOperationGate = new(1, 1);

    private CancellationTokenSource? _catalogCts;
    private GameStatsService? _gameStatsService;
    private GameInfo? _selectedGame;
    private bool _isInitialized;
    private bool _initialLoadCompleted;
    private bool _statsEditingEnabled;
    private bool _showGamesListPending;
    private bool _isCatalogBusy;
    private bool _isGameBusy;
    private bool _showFavoritesOnly;
    private bool _suppressFavoritePersistence;
    private GameLibraryViewMode _gameLibraryViewMode;
    private string? _operationStatusText;
    private string _searchQuery = string.Empty;

    public event EventHandler? ShellStateChanged;

    public string? SearchContextId => GameDetailView.Visibility == Visibility.Visible
        ? (GameTabView.SelectedIndex == 0 ? "achievements" : "statistics")
        : "games";

    public string? SearchPlaceholder => GameDetailView.Visibility == Visibility.Visible
        ? (GameTabView.SelectedIndex == 0
            ? App.LocalizationService.Get("Search.Achievements")
            : App.LocalizationService.Get("Search.Statistics"))
        : App.LocalizationService.Get("Search.Games");

    public string StatusText => string.IsNullOrWhiteSpace(_operationStatusText)
        ? GetGamesStatusText()
        : _operationStatusText;

    public bool CanGoBack => GameDetailView.Visibility == Visibility.Visible && !_isGameBusy;

    public ManagerPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        GamesGrid.ItemsSource = _games;
        GamesList.ItemsSource = _games;
        GamesTable.ItemsSource = _games;
        _statsEditingEnabled = App.RuntimeSettings.EnableStatisticsEditingByDefault;
        SetGamesViewMode(
            App.RuntimeSettings.GameLibraryView.ToUpperInvariant() switch
            {
                "LIST" => GameLibraryViewMode.List,
                "TABLE" => GameLibraryViewMode.Table,
                _ => GameLibraryViewMode.Grid
            },
            save: false);

        ApplyLocalization();
        App.LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;

        Loaded += ManagerPage_Loaded;
        Unloaded += ManagerPage_Unloaded;
        _isInitialized = true;
    }

    public IReadOnlyList<ShellToolbarItem> GetToolbarItems()
    {
        if (GameDetailView.Visibility != Visibility.Visible)
        {
            return
            [
                new ShellToolbarItem
                {
                    Key = "reload-games",
                    Text = App.LocalizationService.Get("Command.ReloadGames"),
                    Glyph = "\uE72C",
                    Placement = ShellToolbarItemPlacement.Left,
                    IsEnabled = !_isCatalogBusy,
                    Execute = () => _ = LoadGamesAsync(showInfoBar: true)
                }
            ];
        }

        if (GameTabView.SelectedIndex == 0)
        {
            return
            [
                ToolbarButton("reload", "Command.Reload", "\uE72C", () => _ = LoadSelectedGameStatsAsync(showInfoBar: true)),
                ToolbarButton("lock-all", "Command.LockAll", "\uE72E", LockAll),
                ToolbarButton("invert", "Command.Invert", "\uE895", InvertAll),
                ToolbarButton("unlock-all", "Command.UnlockAll", "\uE785", UnlockAll),
                ToolbarButton("save", "Command.Save", "\uE74E", () => _ = StoreChangesAsync())
            ];
        }

        return
        [
            ToolbarButton("reload", "Command.Reload", "\uE72C", () => _ = LoadSelectedGameStatsAsync(showInfoBar: true)),
            new ShellToolbarItem
            {
                Key = "edit-stats",
                Text = App.LocalizationService.Get("Command.Edit"),
                Glyph = "\uE70F",
                ItemType = ShellToolbarItemType.ToggleButton,
                Placement = ShellToolbarItemPlacement.Left,
                IsChecked = _statsEditingEnabled,
                IsEnabled = !_isGameBusy,
                Toggle = SetStatisticsEditing
            },
            ToolbarButton("reset", "Command.Reset", "\uE7A7", () => _ = ResetStatsAsync()),
            ToolbarButton("save", "Command.Save", "\uE74E", () => _ = StoreChangesAsync())
        ];
    }

    public void ApplySearch(string query)
    {
        _searchQuery = query?.Trim() ?? string.Empty;

        if (GameDetailView.Visibility != Visibility.Visible)
        {
            ApplyGameFilter();
            return;
        }

        if (GameTabView.SelectedIndex == 0)
            ApplyAchievementFilter();
        else
            ApplyStatisticsFilter();
    }

    public void GoBack()
    {
        ShowGamesList();
    }

    public void ShowGamesList()
    {
        _showGamesListPending = false;

        if (GameDetailView.Visibility != Visibility.Visible)
            return;

        if (_isGameBusy)
        {
            _showGamesListPending = true;
            return;
        }

        _gameStatsService?.Dispose();
        _gameStatsService = null;
        _selectedGame = null;
        _allAchievements.Clear();
        _allStatistics.Clear();
        _searchQuery = string.Empty;
        _operationStatusText = null;

        GameDetailView.Visibility = Visibility.Collapsed;
        GamesView.Visibility = Visibility.Visible;
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private ShellToolbarItem ToolbarButton(string key, string textKey, string glyph, Action action)
        => new()
        {
            Key = key,
            Text = App.LocalizationService.Get(textKey),
            Glyph = glyph,
            Placement = ShellToolbarItemPlacement.Left,
            IsEnabled = !_isGameBusy,
            Execute = action
        };

    private async void ManagerPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateResponsiveLayout(ActualWidth);

        if (_initialLoadCompleted)
            return;

        _initialLoadCompleted = true;

        if (App.RuntimeSettings.LoadGamesOnStartup)
            await LoadGamesAsync(showInfoBar: false);
        else
            SetStatusText(App.LocalizationService.Get("Info.AutoLoadDisabled"));
    }

    private void ManagerPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // NavigationCacheMode keeps the page alive. Long running native sessions are
        // released only when leaving a selected game or when the app closes.
    }

    private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyLocalization();
            ShellStateChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void ApplyLocalization()
    {
        GamesTitleText.Text = App.LocalizationService.Get("Games.Title");
        GamesSubtitleText.Text = App.LocalizationService.Get("Games.Subtitle");
        var favoritesOnlyText = App.LocalizationService.Get("Games.Favorites.Only");
        var gridViewText = App.LocalizationService.Get("Games.View.Grid");
        var listViewText = App.LocalizationService.Get("Games.View.List");
        var tableViewText = App.LocalizationService.Get("Games.View.Table");
        ToolTipService.SetToolTip(GamesFavoritesOnlyButton, favoritesOnlyText);
        ToolTipService.SetToolTip(GamesGridViewButton, gridViewText);
        ToolTipService.SetToolTip(GamesListViewButton, listViewText);
        ToolTipService.SetToolTip(GamesTableViewButton, tableViewText);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(GamesFavoritesOnlyButton, favoritesOnlyText);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(GamesGridViewButton, gridViewText);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(GamesListViewButton, listViewText);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(GamesTableViewButton, tableViewText);
        FavoriteTableColumn.Header = App.LocalizationService.Get("Games.Column.Favorite");
        ImageTableColumn.Header = App.LocalizationService.Get("Games.Column.Image");
        NameTableColumn.Header = App.LocalizationService.Get("Games.Column.Name");
        AppIdTableColumn.Header = App.LocalizationService.Get("Games.Column.AppId");
        TypeTableColumn.Header = App.LocalizationService.Get("Games.Column.Type");
        AchievementsTab.Header = App.LocalizationService.Get("Achievements.Tab");
        AchievementFilterAllItem.Content = App.LocalizationService.Get("Achievements.All");
        AchievementFilterUnlockedItem.Content = App.LocalizationService.Get("Achievements.Unlocked");
        AchievementFilterLockedItem.Content = App.LocalizationService.Get("Achievements.Locked");
        StatisticsTab.Header = App.LocalizationService.Get("Statistics.Tab");
        StatisticsTitleText.Text = App.LocalizationService.Get("Statistics.Title");
        StatisticsHelpText.Text = App.LocalizationService.Get("Statistics.Help");
        StatisticsColumnNameText.Text = App.LocalizationService.Get("Statistics.Column.Name");
        StatisticsColumnValueText.Text = App.LocalizationService.Get("Statistics.Column.Value");
        StatisticsColumnFlagsText.Text = App.LocalizationService.Get("Statistics.Column.Flags");
        GameStatusText.Text = App.LocalizationService.Get("Status.Ready");

        if (_selectedGame is not null)
            SelectedGameSubtitle.Text = App.LocalizationService.Format("Game.AppId", _selectedGame.Id);

        ApplyGameFilter();
        ApplyAchievementFilter();
        ApplyStatisticsFilter();
    }

    private async Task LoadGamesAsync(bool showInfoBar)
    {
        var cancellation = new CancellationTokenSource();
        var previousCancellation = _catalogCts;
        _catalogCts = cancellation;
        previousCancellation?.Cancel();

        var loadingText = App.LocalizationService.Get("Core.Catalog.Loading");
        SetCatalogBusy(true, loadingText);
        if (showInfoBar)
            App.InfoBarService.ShowProgress(loadingText);

        var progress = new Progress<string>(message =>
        {
            if (ReferenceEquals(_catalogCts, cancellation) && !cancellation.IsCancellationRequested)
                ReportLoadingProgress(message, showInfoBar);
        });

        try
        {
            var games = await _catalogService.LoadOwnedGamesAsync(progress, cancellation.Token);

            if (!ReferenceEquals(_catalogCts, cancellation) || cancellation.IsCancellationRequested)
                return;

            ReplaceGames(games);

            ApplyGameFilter();
            ShellStateChanged?.Invoke(this, EventArgs.Empty);

            var completionText = App.LocalizationService.Format("Info.GamesFound", games.Count);
            SetStatusText(completionText);
            if (showInfoBar && App.RuntimeSettings.ShowSuccessMessages)
                App.InfoBarService.ShowSuccess(completionText);
            else if (showInfoBar)
                App.InfoBarService.Close();

        }
        catch (OperationCanceledException)
        {
            if (ReferenceEquals(_catalogCts, cancellation))
            {
                SetStatusText(GetGamesStatusText());
                if (showInfoBar)
                    App.InfoBarService.Close();
            }
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_catalogCts, cancellation) && !cancellation.IsCancellationRequested)
            {
                SetStatusText(ex.Message);
                if (showInfoBar)
                    App.InfoBarService.ShowError(ex.Message);
            }
        }
        finally
        {
            if (ReferenceEquals(_catalogCts, cancellation))
            {
                _catalogCts = null;
                SetCatalogBusy(false);
            }

            cancellation.Dispose();
        }
    }

    private async void Games_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GameInfo game)
            await OpenGameAsync(game);
    }

    private void GamesGridViewButton_Click(object sender, RoutedEventArgs e)
        => SetGamesViewMode(GameLibraryViewMode.Grid, save: true);

    private void GamesListViewButton_Click(object sender, RoutedEventArgs e)
        => SetGamesViewMode(GameLibraryViewMode.List, save: true);

    private void GamesTableViewButton_Click(object sender, RoutedEventArgs e)
        => SetGamesViewMode(GameLibraryViewMode.Table, save: true);

    private void GamesFavoritesOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        _showFavoritesOnly = GamesFavoritesOnlyButton.IsChecked == true;
        ApplyGameFilter();
    }

    private void FavoriteButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button)
            return;

        var text = App.LocalizationService.Get("Games.Favorite.Toggle");
        ToolTipService.SetToolTip(button, text);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, text);
    }

    private void SetGamesViewMode(GameLibraryViewMode viewMode, bool save)
    {
        _gameLibraryViewMode = viewMode;
        GamesGrid.Visibility = viewMode == GameLibraryViewMode.Grid
            ? Visibility.Visible
            : Visibility.Collapsed;
        GamesList.Visibility = viewMode == GameLibraryViewMode.List
            ? Visibility.Visible
            : Visibility.Collapsed;
        GamesTable.Visibility = viewMode == GameLibraryViewMode.Table
            ? Visibility.Visible
            : Visibility.Collapsed;
        GamesGridViewButton.IsChecked = viewMode == GameLibraryViewMode.Grid;
        GamesListViewButton.IsChecked = viewMode == GameLibraryViewMode.List;
        GamesTableViewButton.IsChecked = viewMode == GameLibraryViewMode.Table;

        if (save)
        {
            App.RuntimeSettings.GameLibraryView = viewMode.ToString();
            App.TrySaveSettings(showError: false);
        }

        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task OpenGameAsync(GameInfo game)
    {
        if (_isGameBusy)
            return;

        _selectedGame = game;
        _searchQuery = string.Empty;
        AchievementStateFilter.SelectedIndex = 0;
        GameTabView.SelectedIndex = 0;
        _statsEditingEnabled = App.RuntimeSettings.EnableStatisticsEditingByDefault;

        GamesView.Visibility = Visibility.Collapsed;
        GameDetailView.Visibility = Visibility.Visible;
        SelectedGameTitle.Text = game.Name;
        SelectedGameSubtitle.Text = App.LocalizationService.Format("Game.AppId", game.Id);

        ShellStateChanged?.Invoke(this, EventArgs.Empty);
        await LoadSelectedGameStatsAsync(showInfoBar: false);
    }

    private async Task LoadSelectedGameStatsAsync(bool showInfoBar)
    {
        if (_selectedGame is null)
            return;

        if (!await _gameOperationGate.WaitAsync(0))
            return;

        var loadingText = App.LocalizationService.Get("Busy.LoadAchievements");
        SetGameBusy(true, loadingText);
        if (showInfoBar)
            App.InfoBarService.ShowProgress(loadingText);

        try
        {
            await LoadSelectedGameStatsCoreAsync();

            var completionText = App.LocalizationService.Format(
                "Info.GameLoaded",
                _allAchievements.Count,
                _allStatistics.Count);
            SetStatusText(completionText);

            if (showInfoBar && App.RuntimeSettings.ShowSuccessMessages)
            {
                App.InfoBarService.ShowSuccess(completionText);
            }
            else if (showInfoBar)
            {
                App.InfoBarService.Close();
            }
        }
        catch (Exception ex)
        {
            SetStatusText(ex.Message);
            if (showInfoBar)
                App.InfoBarService.ShowError(ex.Message);
        }
        finally
        {
            SetGameBusy(false);
            _gameOperationGate.Release();
        }
    }

    private async Task LoadSelectedGameStatsCoreAsync()
    {
        if (_selectedGame is null)
            return;

        var appId = _selectedGame.Id;
        _gameStatsService?.Dispose();
        _gameStatsService = null;
        _allAchievements.Clear();
        _allStatistics.Clear();
        ApplyAchievementFilter();
        ApplyStatisticsFilter();

        GameStatsService? candidate = null;
        try
        {
            candidate = await Task.Run(() => new GameStatsService(appId, App.LocalizationService));
            var data = await candidate.LoadAsync();

            _allAchievements.Clear();
            _allAchievements.AddRange(data.Achievements);
            _allStatistics.Clear();
            _allStatistics.AddRange(data.Statistics);

            foreach (var stat in _allStatistics)
                stat.SetEditingEnabled(_statsEditingEnabled);

            _gameStatsService = candidate;
            candidate = null;

            ApplyAchievementFilter();
            ApplyStatisticsFilter();
            SelectedGameTitle.Text = _gameStatsService.GameName;
            ShellStateChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            candidate?.Dispose();
        }
    }

    private void GameTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
            return;

        _searchQuery = string.Empty;
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AchievementStateFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
            return;

        ApplyAchievementFilter();
    }

    private void ApplyGameFilter()
    {
        if (GamesGrid is null)
            return;

        IEnumerable<GameInfo> items = _games;

        if (_showFavoritesOnly)
            items = items.Where(x => x.IsFavorite);

        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            items = items.Where(x =>
                x.Name.Contains(_searchQuery, StringComparison.CurrentCultureIgnoreCase) ||
                x.Id.ToString().Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        IEnumerable<GameInfo> filtered = !_showFavoritesOnly && string.IsNullOrWhiteSpace(_searchQuery)
            ? _games
            : items.ToList();
        GamesGrid.ItemsSource = filtered;
        GamesList.ItemsSource = filtered;
        GamesTable.ItemsSource = filtered;

        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ReplaceGames(IEnumerable<GameInfo> games)
    {
        foreach (var game in _games)
            game.PropertyChanged -= GameInfo_PropertyChanged;

        _games.Clear();
        foreach (var game in games)
        {
            game.IsFavorite = App.GameFavoritesService.IsFavorite(game.Id);
            game.PropertyChanged += GameInfo_PropertyChanged;
            _games.Add(game);
        }
    }

    private void GameInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressFavoritePersistence ||
            e.PropertyName != nameof(GameInfo.IsFavorite) ||
            sender is not GameInfo game)
        {
            return;
        }

        try
        {
            App.GameFavoritesService.SetFavorite(game.Id, game.IsFavorite);
            ApplyGameFilter();
        }
        catch (Exception ex)
        {
            _suppressFavoritePersistence = true;
            try
            {
                game.IsFavorite = !game.IsFavorite;
            }
            finally
            {
                _suppressFavoritePersistence = false;
            }

            var message = App.LocalizationService.Format("Games.Favorites.SaveError", ex.Message);
            SetStatusText(message);
            App.InfoBarService.ShowError(message);
        }
    }

    private void ApplyAchievementFilter()
    {
        if (AchievementsList is null)
            return;

        IEnumerable<AchievementItem> items = _allAchievements;
        var state = AchievementStateFilter?.SelectedIndex ?? 0;

        if (state == 1)
            items = items.Where(x => x.IsChecked);
        else if (state == 2)
            items = items.Where(x => !x.IsChecked);

        if (!string.IsNullOrWhiteSpace(_searchQuery) && GameTabView.SelectedIndex == 0)
        {
            items = items.Where(x =>
                x.Name.Contains(_searchQuery, StringComparison.CurrentCultureIgnoreCase) ||
                x.Description.Contains(_searchQuery, StringComparison.CurrentCultureIgnoreCase) ||
                x.Id.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = items.ToList();
        AchievementsList.ItemsSource = filtered;
        var unlocked = _allAchievements.Count(x => x.IsChecked);
        AchievementSummaryText.Text = App.LocalizationService.Format(
            "Achievements.Summary",
            filtered.Count,
            _allAchievements.Count,
            unlocked);
    }

    private void ApplyStatisticsFilter()
    {
        if (StatisticsList is null)
            return;

        IEnumerable<StatItem> items = _allStatistics;
        if (!string.IsNullOrWhiteSpace(_searchQuery) && GameTabView.SelectedIndex == 1)
        {
            items = items.Where(x =>
                x.DisplayName.Contains(_searchQuery, StringComparison.CurrentCultureIgnoreCase) ||
                x.Id.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                x.Flags.Contains(_searchQuery, StringComparison.CurrentCultureIgnoreCase));
        }

        StatisticsList.ItemsSource = items.ToList();
    }

    private void LockAll()
    {
        foreach (var item in _allAchievements.Where(x => !x.IsProtected))
            item.IsChecked = false;
        ApplyAchievementFilter();
    }

    private void UnlockAll()
    {
        foreach (var item in _allAchievements.Where(x => !x.IsProtected))
            item.IsChecked = true;
        ApplyAchievementFilter();
    }

    private void InvertAll()
    {
        foreach (var item in _allAchievements.Where(x => !x.IsProtected))
            item.IsChecked = !item.IsChecked;
        ApplyAchievementFilter();
    }

    private void SetStatisticsEditing(bool enabled)
    {
        _statsEditingEnabled = enabled;
        foreach (var stat in _allStatistics)
            stat.SetEditingEnabled(enabled);
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task StoreChangesAsync()
    {
        if (_gameStatsService is null)
            return;

        if (!await _gameOperationGate.WaitAsync(0))
            return;

        SetGameBusy(true, App.LocalizationService.Get("Busy.Saving"));

        try
        {
            if (App.RuntimeSettings.ConfirmBeforeStore)
            {
                var confirmed = await DialogService.ShowConfirmationAsync(
                    XamlRoot,
                    App.LocalizationService.Get("Dialog.Store.Title"),
                    App.LocalizationService.Get("Dialog.Store.Content"),
                    App.LocalizationService.Get("Command.Save"),
                    App.LocalizationService.Get("Dialog.Cancel"));

                if (!confirmed)
                {
                    SetStatusText(App.LocalizationService.Get("Status.Ready"));
                    return;
                }
            }

            var service = _gameStatsService;
            var result = await Task.Run(() => service.Store(_allAchievements, _allStatistics));
            if (result.Achievements > 0 || result.Statistics > 0)
                await LoadSelectedGameStatsCoreAsync();

            var completionText = App.LocalizationService.Format(
                "Info.Saved",
                result.Achievements,
                result.Statistics);
            SetStatusText(completionText);
            if (App.RuntimeSettings.ShowSuccessMessages)
                App.InfoBarService.ShowSuccess(completionText);
        }
        catch (Exception ex)
        {
            SetStatusText(ex.Message);
            App.InfoBarService.ShowError(ex.Message);
        }
        finally
        {
            SetGameBusy(false);
            _gameOperationGate.Release();
        }
    }

    private async Task ResetStatsAsync()
    {
        if (_gameStatsService is null)
            return;

        if (!await _gameOperationGate.WaitAsync(0))
            return;

        SetGameBusy(true, App.LocalizationService.Get("Busy.Resetting"));

        try
        {
            var resetChoice = await AskResetScopeAsync();
            if (resetChoice is null)
            {
                SetStatusText(App.LocalizationService.Get("Status.Ready"));
                return;
            }

            if (App.RuntimeSettings.ConfirmBeforeReset)
            {
                var confirmed = await DialogService.ShowConfirmationAsync(
                    XamlRoot,
                    App.LocalizationService.Get("Dialog.ResetConfirm.Title"),
                    App.LocalizationService.Get("Dialog.ResetConfirm.Content"),
                    App.LocalizationService.Get("Command.Reset"),
                    App.LocalizationService.Get("Dialog.Cancel"));

                if (!confirmed)
                {
                    SetStatusText(App.LocalizationService.Get("Status.Ready"));
                    return;
                }
            }

            var service = _gameStatsService;
            await Task.Run(() => service.ResetAll(resetChoice.Value));
            await LoadSelectedGameStatsCoreAsync();
            SetStatusText(App.LocalizationService.Get("Status.Ready"));
        }
        catch (Exception ex)
        {
            SetStatusText(ex.Message);
            App.InfoBarService.ShowError(ex.Message);
        }
        finally
        {
            SetGameBusy(false);
            _gameOperationGate.Release();
        }
    }

    private async Task<bool?> AskResetScopeAsync()
    {
        var result = await DialogService.ShowChoiceAsync(
            XamlRoot,
            App.LocalizationService.Get("Dialog.ResetScope.Title"),
            App.LocalizationService.Get("Dialog.ResetScope.Content"),
            App.LocalizationService.Get("Dialog.ResetScope.All"),
            App.LocalizationService.Get("Dialog.ResetScope.StatsOnly"),
            App.LocalizationService.Get("Dialog.Cancel"));

        return result switch
        {
            ContentDialogResult.Primary => true,
            ContentDialogResult.Secondary => false,
            _ => null
        };
    }

    private string GetGamesStatusText()
    {
        if (_games.Count == 0)
            return App.LocalizationService.Get("Status.NoGames");

        var activeItemsSource = _gameLibraryViewMode switch
        {
            GameLibraryViewMode.List => GamesList?.ItemsSource,
            GameLibraryViewMode.Table => GamesTable?.ItemsSource,
            _ => GamesGrid?.ItemsSource
        };
        var displayed = activeItemsSource is IEnumerable<GameInfo> items
            ? items.Count()
            : _games.Count;

        return displayed == _games.Count
            ? App.LocalizationService.Format("Status.GamesLoaded", _games.Count)
            : App.LocalizationService.Format("Status.GamesFiltered", _games.Count, displayed);
    }

    private void SetGameBusy(bool busy, string? text = null)
    {
        _isGameBusy = busy;
        if (!string.IsNullOrWhiteSpace(text))
            _operationStatusText = text;
        GameProgressRing.IsActive = busy;
        GameStatusText.Text = text ?? _operationStatusText ?? App.LocalizationService.Get("Status.Ready");
        GameTabView.IsEnabled = !busy;
        ShellStateChanged?.Invoke(this, EventArgs.Empty);

        if (!busy && _showGamesListPending)
        {
            _showGamesListPending = false;
            DispatcherQueue.TryEnqueue(ShowGamesList);
        }
    }

    private void SetCatalogBusy(bool busy, string? text = null)
    {
        _isCatalogBusy = busy;
        if (!string.IsNullOrWhiteSpace(text))
            _operationStatusText = text;
        CatalogProgressRing.IsActive = busy;
        GamesGrid.IsEnabled = !busy;
        GamesList.IsEnabled = !busy;
        GamesTable.IsEnabled = !busy;
        GamesFavoritesOnlyButton.IsEnabled = !busy;
        GamesGridViewButton.IsEnabled = !busy;
        GamesListViewButton.IsEnabled = !busy;
        GamesTableViewButton.IsEnabled = !busy;
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ReportLoadingProgress(string message, bool showInfoBar)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _operationStatusText = message;
        if (showInfoBar)
            App.InfoBarService.ShowProgress(message);
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetStatusText(string message)
    {
        _operationStatusText = string.IsNullOrWhiteSpace(message) ? null : message;
        if (GameDetailView.Visibility == Visibility.Visible)
            GameStatusText.Text = _operationStatusText ?? App.LocalizationService.Get("Status.Ready");
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateResponsiveLayout(e.NewSize.Width);

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0)
            return;

        var narrow = width < 760;
        GamesHeaderGrid.Margin = narrow
            ? new Thickness(12, 14, 12, 8)
            : new Thickness(24, 20, 24, 12);
        GamesGrid.Margin = narrow
            ? new Thickness(8, 0, 8, 8)
            : new Thickness(16, 0, 16, 12);
        GamesList.Margin = GamesGrid.Margin;
        GamesTable.Margin = GamesGrid.Margin;
        GameHeaderPanel.Margin = narrow
            ? new Thickness(12, 12, 12, 8)
            : new Thickness(20, 16, 20, 10);
    }
}
