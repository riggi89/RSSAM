// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RSSAM.Services;
using Windows.Graphics;

namespace RSSAM;

public sealed partial class MainWindow : Window
{
    private string? _activeSearchContext;
    private bool _initialized;
    private bool _restoringWindowPlacement;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        ApplySystemTitleBarChrome();

        ShellPageHost.SearchStateChanged += ShellPageHost_SearchStateChanged;
        ShellPageHost.NavigationStateChanged += ShellPageHost_NavigationStateChanged;
        App.LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
        Closed += MainWindow_Closed;

        _initialized = true;
        ApplyRuntimeSettings();
        ApplyLocalization();
        UpdateResponsiveLayout(RootLayout.ActualWidth > 0 ? RootLayout.ActualWidth : 1200);
        UpdateTitleBarState();
    }

    public void ApplyRuntimeSettings()
    {
        if (!_initialized)
            return;

        RootLayout.RequestedTheme = App.RuntimeSettings.Theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        SystemBackdrop = App.RuntimeSettings.Backdrop switch
        {
            "Acrylic" => new DesktopAcrylicBackdrop(),
            "Standard" => null,
            _ => new MicaBackdrop()
        };

        ShellPageHost.ApplyRuntimeSettings();
        ApplySystemTitleBarChrome();
    }

    public void RestorePersistedWindowPlacement()
    {
        _restoringWindowPlacement = true;

        try
        {
            var settings = App.RuntimeSettings;

            if (settings.RememberWindowPosition)
            {
                AppWindow.MoveAndResize(new RectInt32(
                    settings.WindowX,
                    settings.WindowY,
                    settings.WindowWidth,
                    settings.WindowHeight));
            }

            if (AppWindow.Presenter is OverlappedPresenter presenter &&
                (settings.StartMaximized || (settings.RememberWindowPosition && settings.WindowMaximized)))
            {
                presenter.Maximize();
            }
        }
        catch
        {
            // Window placement is convenience state and must never prevent startup.
        }
        finally
        {
            _restoringWindowPlacement = false;
        }
    }

    private void SaveWindowPlacement()
    {
        if (_restoringWindowPlacement || !App.RuntimeSettings.RememberWindowPosition)
            return;

        try
        {
            var settings = App.RuntimeSettings;
            var isMaximized = AppWindow.Presenter is OverlappedPresenter presenter &&
                              presenter.State == OverlappedPresenterState.Maximized;

            settings.WindowMaximized = isMaximized;

            // Preserve the last normal bounds while the window is maximized.
            if (!isMaximized)
            {
                var position = AppWindow.Position;
                var size = AppWindow.Size;
                settings.WindowX = position.X;
                settings.WindowY = position.Y;
                settings.WindowWidth = Math.Max(720, size.Width);
                settings.WindowHeight = Math.Max(520, size.Height);
            }
        }
        catch
        {
        }
    }

    private void ApplyLocalization()
    {
        ToolTipService.SetToolTip(TitleBarBackButton, App.LocalizationService.Get("Tool.Back"));
        ToolTipService.SetToolTip(TitleBarPaneButton, App.LocalizationService.Get("Tool.Navigation"));
        UpdateSearchState();
        ShellPageHost.RefreshLocalization();
    }

    private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private void ShellPageHost_SearchStateChanged(object? sender, EventArgs e)
        => UpdateSearchState();

    private void ShellPageHost_NavigationStateChanged(object? sender, EventArgs e)
        => UpdateTitleBarState();

    private void UpdateSearchState()
    {
        var nextContext = ShellPageHost.SearchContextId;
        var contextChanged = !string.Equals(
            _activeSearchContext,
            nextContext,
            StringComparison.OrdinalIgnoreCase);

        _activeSearchContext = nextContext;
        UniversalSearchBox.Visibility = ShellPageHost.IsSearchAvailable
            ? Visibility.Visible
            : Visibility.Collapsed;
        UniversalSearchBox.PlaceholderText = ShellPageHost.SearchPlaceholder;

        if (contextChanged)
        {
            UniversalSearchBox.Text = string.Empty;
            ShellPageHost.ApplySearch(string.Empty);
        }

        DispatcherQueue.TryEnqueue(UpdateTitleBarPassthroughRegion);
    }

    private void UpdateTitleBarState()
    {
        TitleBarBackButton.Visibility = ShellPageHost.CanGoBack
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UniversalSearchBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        if (!_initialized)
            return;

        var value = sender.Text?.Trim() ?? string.Empty;
        ShellPageHost.ApplySearch(value);
    }

    private void TitleBarPaneButton_Click(object sender, RoutedEventArgs e)
        => ShellPageHost.ToggleNavigationPane();

    private void TitleBarBackButton_Click(object sender, RoutedEventArgs e)
        => ShellPageHost.GoBack();

    private void RootLayout_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateResponsiveLayout(e.NewSize.Width);
        UpdateTitleBarPassthroughRegion();
    }

    private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        => UpdateTitleBarPassthroughRegion();

    private void UpdateTitleBarPassthroughRegion()
    {
        try
        {
            if (!ExtendsContentIntoTitleBar || AppTitleBar.XamlRoot is null)
                return;

            var nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
            var scale = AppTitleBar.XamlRoot.RasterizationScale;

            var interactiveElements = new FrameworkElement[]
            {
                TitleBarBackButton,
                TitleBarPaneButton,
                UniversalSearchBox
            };

            var passthroughRegions = interactiveElements
                .Where(element =>
                    element.Visibility == Visibility.Visible &&
                    element.ActualWidth > 0 &&
                    element.ActualHeight > 0)
                .Select(element => CreateScaledRect(element, scale))
                .ToArray();

            nonClientPointerSource.SetRegionRects(
                NonClientRegionKind.Passthrough,
                passthroughRegions);
        }
        catch
        {
            // Title-bar hit testing must never prevent the window from starting.
        }
    }

    private static RectInt32 CreateScaledRect(FrameworkElement element, double scale)
    {
        var transform = element.TransformToVisual(null);
        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(
            0,
            0,
            element.ActualWidth,
            element.ActualHeight));

        return new RectInt32(
            (int)Math.Round(bounds.X * scale),
            (int)Math.Round(bounds.Y * scale),
            (int)Math.Round(bounds.Width * scale),
            (int)Math.Round(bounds.Height * scale));
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0)
            return;

        var showFullProductName = width >= 1320;
        ProductNameText.Visibility = showFullProductName
            ? Visibility.Visible
            : Visibility.Collapsed;

        TitleBarLeadingColumn.Width = new GridLength(
            showFullProductName
                ? 420
                : width < 1000
                    ? 180
                    : 220);

        UniversalSearchBox.Width = width < 1000
            ? Math.Clamp(width - 400, 280, 500)
            : 540;
        UniversalSearchBox.Margin = new Thickness(20, 0, 20, 0);
        TitleBarCaptionColumn.Width = new GridLength(width < 760 ? 132 : 140);
        AppTitleBar.Padding = width < 760
            ? new Thickness(4, 0, 0, 0)
            : new Thickness(8, 0, 0, 0);
    }

    private void ApplySystemTitleBarChrome()
    {
        try
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
                return;

            var titleBar = AppWindow.TitleBar;
            titleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        }
        catch
        {
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        DialogService.ResetState();

        SaveWindowPlacement();
        App.TrySaveSettings(showError: false);
    }
}
