// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using RSSAM.Presentation.Shell;
using RSSAM.Services;

namespace RSSAM.Views;

public sealed partial class ShellPage : Page
{
    private const double NarrowBreakpoint = 840;

    private readonly DispatcherTimer _steamStatusTimer;
    private IShellContentPage? _activePage;
    private bool _isCompactToolbarMode;
    private bool _isLoaded;
    private bool _selectingNavigationItem;
    private bool _steamStatusCheckInProgress;
    private bool? _isSteamRunning;
    private string _lastStatusText = string.Empty;

    public event EventHandler? SearchStateChanged;
    public event EventHandler? NavigationStateChanged;

    public bool CanGoBack => _activePage?.CanGoBack == true;
    public bool IsSearchAvailable => !string.IsNullOrWhiteSpace(_activePage?.SearchPlaceholder);
    public string? SearchContextId => _activePage?.SearchContextId;
    public string SearchPlaceholder => _activePage?.SearchPlaceholder ?? string.Empty;

    public ShellPage()
    {
        InitializeComponent();

        _steamStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _steamStatusTimer.Tick += SteamStatusTimer_Tick;

        Loaded += ShellPage_Loaded;
        Unloaded += ShellPage_Unloaded;
        App.LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
    }

    public void ToggleNavigationPane()
    {
        if (!_isLoaded)
            return;

        RootNavigationView.IsPaneOpen = !RootNavigationView.IsPaneOpen;
        App.RuntimeSettings.IsNavigationPaneOpen = RootNavigationView.IsPaneOpen;
        App.TrySaveSettings(showError: false);
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void GoBack()
    {
        if (_activePage?.CanGoBack == true)
            _activePage.GoBack();
    }

    public void ApplySearch(string query)
        => _activePage?.ApplySearch(query ?? string.Empty);

    public void ApplyRuntimeSettings()
    {
        if (!_isLoaded)
            return;

        ApplyResponsiveLayout();
    }

    public void RefreshLocalization()
    {
        ApplyLocalization();
        SearchStateChanged?.Invoke(this, EventArgs.Empty);
        UpdateToolbarForCurrentPage();
    }

    private void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        VersionTextBlock.Text = AppVersion.Display;
        _lastStatusText = App.LocalizationService.Get("Status.NoGames");
        LeftStatusTextBlock.Text = _lastStatusText;

        App.InfoBarService.Attach(ShellRootGrid);
        ApplyLocalization();
        _steamStatusTimer.Start();
        _ = RefreshSteamStatusAsync();

        NavigateTo("games", updateSelection: true);
        ApplyResponsiveLayout();
    }

    private void ShellPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        _steamStatusTimer.Stop();
        DetachActivePage();
        App.InfoBarService.Detach();
    }

    private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
    {
        if (!_isLoaded)
            return;

        DispatcherQueue.TryEnqueue(RefreshLocalization);
    }

    private void ApplyLocalization()
    {
        GamesNavigationItem.Content = App.LocalizationService.Get("Nav.Games");
        SettingsNavigationItem.Content = App.LocalizationService.Get("Nav.Settings");
        RenderSteamStatus();
    }

    private void SteamStatusTimer_Tick(object? sender, object e)
        => _ = RefreshSteamStatusAsync();

    private async Task RefreshSteamStatusAsync()
    {
        if (_steamStatusCheckInProgress)
            return;

        _steamStatusCheckInProgress = true;
        try
        {
            var isRunning = await Task.Run(SteamProcessStatusService.IsSteamRunning);
            if (!_isLoaded)
                return;

            _isSteamRunning = isRunning;
            RenderSteamStatus();
        }
        finally
        {
            _steamStatusCheckInProgress = false;
        }
    }

    private void RenderSteamStatus()
    {
        var key = _isSteamRunning switch
        {
            true => "Status.Steam.Running",
            false => "Status.Steam.NotRunning",
            null => "Status.Steam.Checking"
        };

        SteamStatusTextBlock.Text = App.LocalizationService.Get(key);

        var brushKey = _isSteamRunning switch
        {
            true => "AppSteamRunningBrush",
            false => "AppSteamNotRunningBrush",
            null => "AppSecondaryTextBrush"
        };

        if (Application.Current.Resources[brushKey] is Brush brush)
            SteamStatusIndicator.Fill = brush;
    }

    private void RootNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (!_isLoaded || _selectingNavigationItem || args.SelectedItemContainer?.Tag is not string key)
            return;

        NavigateTo(key, updateSelection: false);
    }

    private void RootNavigationView_ItemInvoked(
        NavigationView sender,
        NavigationViewItemInvokedEventArgs args)
    {
        if (!_isLoaded || args.InvokedItemContainer?.Tag is not string key)
            return;

        if (NormalizeNavigationKey(key) == "games" &&
            ContentFrame.Content is ManagerPage managerPage)
        {
            managerPage.ShowGamesList();
        }
    }

    private void NavigateTo(string key, bool updateSelection)
    {
        key = NormalizeNavigationKey(key);

        Type pageType = key switch
        {
            "settings" => typeof(SettingsPage),
            "changelog" => typeof(ChangelogPage),
            _ => typeof(ManagerPage)
        };

        if (updateSelection)
            SelectNavigationItem(key);

        if (ContentFrame.CurrentSourcePageType == pageType)
        {
            if (key == "games" && ContentFrame.Content is ManagerPage currentManagerPage)
                currentManagerPage.ShowGamesList();

            return;
        }

        ContentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
    }

    private void SelectNavigationItem(string key)
    {
        NavigationViewItem? target = key switch
        {
            "settings" => SettingsNavigationItem,
            "changelog" => ChangelogNavigationItem,
            _ => GamesNavigationItem
        };

        _selectingNavigationItem = true;
        try
        {
            RootNavigationView.SelectedItem = target;
        }
        finally
        {
            _selectingNavigationItem = false;
        }
    }

    private static string NormalizeNavigationKey(string? key)
        => key is "settings" or "changelog" ? key : "games";

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(ManagerPage) &&
            ContentFrame.Content is ManagerPage managerPage)
        {
            managerPage.ShowGamesList();
        }

        AttachActivePage(ContentFrame.Content as IShellContentPage);
    }

    private void AttachActivePage(IShellContentPage? page)
    {
        DetachActivePage();
        _activePage = page;

        if (_activePage is not null)
        {
            _activePage.ShellStateChanged += ActivePage_ShellStateChanged;
            if (!string.IsNullOrWhiteSpace(_activePage.StatusText))
                _lastStatusText = _activePage.StatusText;
        }

        LeftStatusTextBlock.Text = _lastStatusText;
        UpdateToolbarForCurrentPage();
        SearchStateChanged?.Invoke(this, EventArgs.Empty);
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DetachActivePage()
    {
        if (_activePage is not null)
            _activePage.ShellStateChanged -= ActivePage_ShellStateChanged;
        _activePage = null;
    }

    private void ActivePage_ShellStateChanged(object? sender, EventArgs e)
    {
        if (_activePage is not null && !string.IsNullOrWhiteSpace(_activePage.StatusText))
            _lastStatusText = _activePage.StatusText;

        LeftStatusTextBlock.Text = _lastStatusText;
        UpdateToolbarForCurrentPage();
        SearchStateChanged?.Invoke(this, EventArgs.Empty);
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateToolbarForCurrentPage()
    {
        ToolbarItemsHost.Children.Clear();

        if (_activePage is not null)
        {
            foreach (var item in _activePage.GetToolbarItems())
                ToolbarItemsHost.Children.Add(CreateToolbarElement(item));
        }

        UpdateToolbarVisibility();
    }

    private FrameworkElement CreateToolbarElement(ShellToolbarItem item)
    {
        if (item.ItemType == ShellToolbarItemType.ToggleButton)
        {
            var toggle = new ToggleButton
            {
                Style = (Style)Application.Current.Resources["ToolbarToggleButtonStyle"],
                IsChecked = item.IsChecked,
                IsEnabled = item.IsEnabled,
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = CreateToolbarButtonContent(item)
            };

            ToolTipService.SetToolTip(toggle, item.ToolTip ?? item.Text);
            toggle.Click += (_, _) => item.Toggle?.Invoke(toggle.IsChecked == true);
            return toggle;
        }

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["ToolbarCommandButtonStyle"],
            IsEnabled = item.IsEnabled,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = CreateToolbarButtonContent(item)
        };

        ToolTipService.SetToolTip(button, item.ToolTip ?? item.Text);
        button.Click += (_, _) => item.Execute?.Invoke();
        return button;
    }

    private UIElement CreateToolbarButtonContent(ShellToolbarItem item)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = _isCompactToolbarMode ? 0 : 6,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (!string.IsNullOrWhiteSpace(item.Glyph))
        {
            panel.Children.Add(new FontIcon
            {
                Glyph = item.Glyph,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        if (!_isCompactToolbarMode && !string.IsNullOrWhiteSpace(item.Text))
        {
            panel.Children.Add(new TextBlock
            {
                Text = item.Text,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        return panel;
    }

    private void UpdateToolbarVisibility()
    {
        var visible = ToolbarItemsHost.Children.Count > 0;
        var height = _isCompactToolbarMode ? 58 : 64;

        ToolbarHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ToolbarHost.MinHeight = height;
        ToolbarRow.Height = visible ? new GridLength(height) : new GridLength(0);
        ToolbarScrollViewer.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShellRootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isLoaded)
            ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        var width = ActualWidth;
        if (width <= 0)
            return;

        var compactToolbar = width < NarrowBreakpoint;
        var toolbarModeChanged = compactToolbar != _isCompactToolbarMode;
        _isCompactToolbarMode = compactToolbar;

        var wantsCompact = string.Equals(
            App.RuntimeSettings.NavigationMode,
            "Compact",
            StringComparison.OrdinalIgnoreCase);

        if (width < NarrowBreakpoint)
        {
            RootNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
            RootNavigationView.OpenPaneLength = 220;
        }
        else if (wantsCompact)
        {
            RootNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            RootNavigationView.OpenPaneLength = 240;
        }
        else
        {
            RootNavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            RootNavigationView.OpenPaneLength = 240;
        }

        RootNavigationView.IsPaneOpen = App.RuntimeSettings.IsNavigationPaneOpen;

        var showStatus = App.RuntimeSettings.ShowStatusBar;
        StatusBarHost.Visibility = showStatus ? Visibility.Visible : Visibility.Collapsed;
        StatusBarRow.Height = showStatus ? new GridLength(32) : new GridLength(0);

        if (toolbarModeChanged)
            UpdateToolbarForCurrentPage();
        else
            UpdateToolbarVisibility();

        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

}
