// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace RSSAM.Services;

public sealed class InfoBarService
{
    private FrameworkElement? _root;
    private Popup? _popup;
    private InfoBar? _infoBar;
    private DispatcherTimer? _autoCloseTimer;

    public void Attach(FrameworkElement root)
    {
        if (ReferenceEquals(_root, root))
            return;

        Detach();
        _root = root;
        _root.SizeChanged += Root_SizeChanged;
    }

    public void Detach()
    {
        StopTimer();

        if (_root is not null)
            _root.SizeChanged -= Root_SizeChanged;

        if (_popup is not null)
            _popup.IsOpen = false;

        _root = null;
        _popup = null;
        _infoBar = null;
    }

    public void ShowInformation(string message, string? title = null)
        => Show(InfoBarSeverity.Informational, message, title, autoClose: true);

    public void ShowProgress(string message, string? title = null)
        => Show(
            InfoBarSeverity.Informational,
            message,
            title,
            autoClose: false,
            isClosable: false);

    public void ShowSuccess(string message, string? title = null)
        => Show(InfoBarSeverity.Success, message, title, autoClose: true);

    public void ShowWarning(string message, string? title = null)
        => Show(InfoBarSeverity.Warning, message, title, autoClose: false);

    public void ShowError(string message, string? title = null)
        => Show(InfoBarSeverity.Error, message, title, autoClose: false);

    public void Close()
    {
        StopTimer();
        if (_infoBar is not null)
            _infoBar.IsOpen = false;
        if (_popup is not null)
            _popup.IsOpen = false;
    }

    private void Show(
        InfoBarSeverity severity,
        string message,
        string? title,
        bool autoClose,
        bool isClosable = true)
    {
        if (_root?.XamlRoot is null)
            return;

        EnsurePopup();
        if (_popup is null || _infoBar is null)
            return;

        StopTimer();

        _infoBar.RequestedTheme = _root.ActualTheme;
        _infoBar.Severity = severity;
        _infoBar.Title = title ?? string.Empty;
        _infoBar.Message = message;
        _infoBar.IsClosable = isClosable;
        _infoBar.Style = ResolveSeverityStyle(severity);
        _infoBar.IsOpen = true;

        UpdatePlacement();
        _popup.IsOpen = true;

        if (autoClose)
        {
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Start();
        }
    }

    private void EnsurePopup()
    {
        if (_root?.XamlRoot is null || _popup is not null)
            return;

        _infoBar = new InfoBar
        {
            IsOpen = false,
            IsClosable = true,
            MaxWidth = 640,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _infoBar.Closed += (_, _) =>
        {
            StopTimer();
            if (_popup is not null)
                _popup.IsOpen = false;
        };

        _popup = new Popup
        {
            XamlRoot = _root.XamlRoot,
            IsLightDismissEnabled = false,
            Child = _infoBar
        };
    }

    private Style? ResolveSeverityStyle(InfoBarSeverity severity)
    {
        var key = severity switch
        {
            InfoBarSeverity.Success => "FloatingInfoBarSuccessStyle",
            InfoBarSeverity.Warning => "FloatingInfoBarWarningStyle",
            InfoBarSeverity.Error => "FloatingInfoBarErrorStyle",
            _ => "FloatingInfoBarInfoStyle"
        };

        return Application.Current.Resources[key] as Style
            ?? Application.Current.Resources["FloatingInfoBarStyle"] as Style;
    }

    private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePlacement();

    private void UpdatePlacement()
    {
        if (_root is null || _popup is null || _infoBar is null)
            return;

        var availableWidth = Math.Max(280, _root.ActualWidth - 24);
        var width = Math.Min(640, availableWidth);

        _infoBar.Width = width;
        _popup.HorizontalOffset = Math.Max(12, (_root.ActualWidth - width) / 2);
        // MainWindow uses a fixed 40 px TitleBar. Keep the floating notification
        // 12 px below it so it overlays ShellPage/toolbar content instead of the caption area.
        _popup.VerticalOffset = 52;
    }

    private void AutoCloseTimer_Tick(object? sender, object e)
        => Close();

    private void StopTimer()
    {
        if (_autoCloseTimer is null)
            return;

        _autoCloseTimer.Stop();
        _autoCloseTimer.Tick -= AutoCloseTimer_Tick;
        _autoCloseTimer = null;
    }
}
