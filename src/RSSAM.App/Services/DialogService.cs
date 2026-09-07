// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RSSAM.Services;

/// <summary>
/// Centralizes WinUI content dialogs and serializes them because a XamlRoot can
/// display only one ContentDialog at a time.
/// </summary>
public static class DialogService
{
    private static readonly SemaphoreSlim DialogSemaphore = new(1, 1);
    private static int _openDialogCount;

    /// <summary>Gets whether a dialog is currently open.</summary>
    public static bool HasOpenDialog => Volatile.Read(ref _openDialogCount) > 0;

    /// <summary>Defensively clears the observable dialog state during shutdown.</summary>
    public static void ResetState()
        => Volatile.Write(ref _openDialogCount, 0);

    public static Task<ContentDialogResult> ShowInfoAsync(
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowInfoAsync(null, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowInfoAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowMessageAsync(xamlRoot, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowSuccessAsync(
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowSuccessAsync(null, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowSuccessAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowMessageAsync(xamlRoot, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowWarningAsync(
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowWarningAsync(null, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowWarningAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowMessageAsync(xamlRoot, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowErrorAsync(
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowErrorAsync(null, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowErrorAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string closeButtonText = "OK")
        => ShowMessageAsync(xamlRoot, title, message, closeButtonText);

    public static Task<ContentDialogResult> ShowErrorAsync(
        string title,
        Exception exception,
        string closeButtonText = "OK")
        => ShowErrorAsync(null, title, exception, closeButtonText);

    public static Task<ContentDialogResult> ShowErrorAsync(
        XamlRoot? xamlRoot,
        string title,
        Exception exception,
        string closeButtonText = "OK")
    {
        ArgumentNullException.ThrowIfNull(exception);
        return ShowErrorAsync(xamlRoot, title, exception.Message, closeButtonText);
    }

    public static Task<ContentDialogResult> ShowConfirmAsync(
        string title,
        string message,
        string primaryButtonText = "Yes",
        string closeButtonText = "No")
        => ShowConfirmAsync(null, title, message, primaryButtonText, closeButtonText);

    public static Task<ContentDialogResult> ShowConfirmAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string primaryButtonText = "Yes",
        string closeButtonText = "No")
    {
        var dialog = CreateDialog(title, message);
        dialog.PrimaryButtonText = primaryButtonText;
        dialog.CloseButtonText = closeButtonText;
        dialog.DefaultButton = ContentDialogButton.Close;
        return ShowAsync(dialog, xamlRoot);
    }

    public static async Task<bool> ShowConfirmationAsync(
        string title,
        string message,
        string primaryButtonText = "Yes",
        string closeButtonText = "No")
        => await ShowConfirmationAsync(
            null,
            title,
            message,
            primaryButtonText,
            closeButtonText);

    public static async Task<bool> ShowConfirmationAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string primaryButtonText = "Yes",
        string closeButtonText = "No")
        => await ShowConfirmAsync(
            xamlRoot,
            title,
            message,
            primaryButtonText,
            closeButtonText) == ContentDialogResult.Primary;

    public static Task<bool> ShowYesNoAsync(
        string title,
        string message,
        string yesButtonText = "Yes",
        string noButtonText = "No")
        => ShowConfirmationAsync(title, message, yesButtonText, noButtonText);

    public static Task<bool> ShowYesNoAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string yesButtonText = "Yes",
        string noButtonText = "No")
        => ShowConfirmationAsync(xamlRoot, title, message, yesButtonText, noButtonText);

    public static Task<bool> ShowDeleteConfirmAsync(
        string title,
        string message,
        string deleteButtonText = "Delete",
        string cancelButtonText = "Cancel")
        => ShowDeleteConfirmAsync(
            null,
            title,
            message,
            deleteButtonText,
            cancelButtonText);

    public static async Task<bool> ShowDeleteConfirmAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string deleteButtonText = "Delete",
        string cancelButtonText = "Cancel")
        => await ShowConfirmAsync(
            xamlRoot,
            title,
            message,
            deleteButtonText,
            cancelButtonText) == ContentDialogResult.Primary;

    public static Task<ContentDialogResult> ShowChoiceAsync(
        string title,
        string message,
        string primaryButtonText,
        string secondaryButtonText,
        string closeButtonText)
        => ShowChoiceAsync(
            null,
            title,
            message,
            primaryButtonText,
            secondaryButtonText,
            closeButtonText);

    public static Task<ContentDialogResult> ShowChoiceAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string primaryButtonText,
        string secondaryButtonText,
        string closeButtonText)
    {
        var dialog = CreateDialog(title, message);
        dialog.PrimaryButtonText = primaryButtonText;
        dialog.SecondaryButtonText = secondaryButtonText;
        dialog.CloseButtonText = closeButtonText;
        dialog.DefaultButton = ContentDialogButton.Close;
        return ShowAsync(dialog, xamlRoot);
    }

    public static Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
        => ShowAsync(dialog, null);

    public static async Task<ContentDialogResult> ShowAsync(
        ContentDialog dialog,
        XamlRoot? xamlRoot)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        await DialogSemaphore.WaitAsync();
        var countIncremented = false;

        try
        {
            dialog.XamlRoot = ResolveXamlRoot(xamlRoot);
            dialog.RequestedTheme = ResolveRequestedTheme();

            if (Application.Current.Resources["AppContentDialogStyle"] is Style style)
                dialog.Style = style;

            Interlocked.Increment(ref _openDialogCount);
            countIncremented = true;
            return await dialog.ShowAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            App.InfoBarService.ShowError(exception.Message, "RSSAM");
            return ContentDialogResult.None;
        }
        finally
        {
            if (countIncremented)
                Interlocked.Decrement(ref _openDialogCount);

            DialogSemaphore.Release();
        }
    }

    private static Task<ContentDialogResult> ShowMessageAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string closeButtonText)
    {
        var dialog = CreateDialog(title, message);
        dialog.CloseButtonText = closeButtonText;
        dialog.DefaultButton = ContentDialogButton.Close;
        return ShowAsync(dialog, xamlRoot);
    }

    private static ContentDialog CreateDialog(string title, string message)
    {
        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.WrapWholeWords
        };

        if (Application.Current.Resources["AppDialogMessageTextStyle"] is Style messageStyle)
            messageText.Style = messageStyle;

        return new ContentDialog
        {
            Title = title,
            Content = messageText
        };
    }

    private static XamlRoot ResolveXamlRoot(XamlRoot? xamlRoot)
    {
        if (xamlRoot is not null)
            return xamlRoot;

        if (App.MainWindow?.Content is FrameworkElement rootElement &&
            rootElement.XamlRoot is not null)
        {
            return rootElement.XamlRoot;
        }

        throw new InvalidOperationException(
            "No active XamlRoot is available for the content dialog.");
    }

    private static ElementTheme ResolveRequestedTheme()
    {
        if (App.MainWindow?.Content is FrameworkElement rootElement)
            return rootElement.ActualTheme;

        return ElementTheme.Default;
    }
}
