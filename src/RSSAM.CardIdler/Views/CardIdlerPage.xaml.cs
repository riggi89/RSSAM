using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RSSAM.CardIdler.Models;
using RSSAM.CardIdler.ViewModels;
using Windows.System;

namespace RSSAM.CardIdler.Views;

public sealed partial class CardIdlerPage : Page
{
    private static readonly MainViewModel SharedViewModel = new();
    private static bool _autoLoginStarted;

    public static Func<string, string>? LocalizationResolver { get; set; }

    public MainViewModel ViewModel => SharedViewModel;

    public CardIdlerPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += CardIdlerPage_Loaded;
        Unloaded += CardIdlerPage_Unloaded;
    }

    public void RefreshLocalization()
    {
        PageTitleText.Text = T("CardIdler.Title", "Card Idler");
        PageSubtitleText.Text = T("CardIdler.Subtitle", "Farm remaining Steam trading-card drops without launching each game.");
        UpdateBatchLabels();
        DropsLeftLabelText.Text = T("CardIdler.DropsLeft", "DROPS LEFT");
        CardsEarnedLabelText.Text = T("CardIdler.CardsEarned", "CARDS EARNED");
        SessionLabelText.Text = T("CardIdler.Session", "SESSION");
        StartButton.Content = T("CardIdler.Start", "Start idling");
        StopButton.Content = T("CardIdler.Stop", "Stop");
        RescanButton.Content = T("CardIdler.Rescan", "Rescan");
        ConcurrentLabelText.Text = T("CardIdler.Concurrent", "Games at once");
        RecheckLabelText.Text = T("CardIdler.Recheck", "Recheck interval (minutes)");
        SignOutButton.Content = T("CardIdler.SignOut", "Sign out");
        QueueLabelText.Text = T("CardIdler.UpNext", "Up next");
        SelectGameHintText.Text = T("CardIdler.SelectGame", "Select a game to see its details.");
        DetailDropsLabelText.Text = T("CardIdler.CardDrops", "Card drops");
        DetailPlaytimeLabelText.Text = T("CardIdler.Playtime", "Playtime");
        OpenStoreButton.Content = T("CardIdler.OpenStore", "Open store page");
        ActivityLabelText.Text = T("CardIdler.Activity", "Activity");
        LoginTitleText.Text = T("CardIdler.LoginTitle", "Steam sign-in");
        LoginDescriptionText.Text = T("CardIdler.LoginDescription", "Sign in to scan your remaining card drops and start idling.");
        UsernameLabelText.Text = T("CardIdler.Username", "Steam account name");
        PasswordLabelText.Text = T("CardIdler.Password", "Password");
        StaySignedInCheckBox.Content = T("CardIdler.StaySignedIn", "Stay signed in (encrypted on this PC)");
        LoginButton.Content = T("CardIdler.SignIn", "Sign in");
        GuardTitleText.Text = T("CardIdler.Guard", "Steam Guard");
        GuardWrongText.Text = T("CardIdler.GuardWrong", "The code was not accepted. Enter the newest code.");
        GuardCancelButton.Content = T("Dialog.Cancel", "Cancel");
        GuardConfirmButton.Content = T("CardIdler.Confirm", "Confirm");
    }

    private async void CardIdlerPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RefreshLocalization();
        UserBox.Focus(FocusState.Programmatic);

        if (_autoLoginStarted)
            return;

        _autoLoginStarted = true;
        await ViewModel.TryAutoLoginAsync();
    }

    private void CardIdlerPage_Unloaded(object sender, RoutedEventArgs e)
        => ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.GuardVisible) && ViewModel.GuardVisible)
            DispatcherQueue.TryEnqueue(() => GuardCodeBox.Focus(FocusState.Programmatic));

        if (e.PropertyName is nameof(MainViewModel.IsIdling) or nameof(MainViewModel.BatchTitle))
            UpdateBatchLabels();
    }

    private void UpdateBatchLabels()
    {
        var text = ViewModel.IsIdling
            ? T("CardIdler.NowPlaying", "NOW PLAYING")
            : T("CardIdler.Ready", "READY TO IDLE");
        BatchLabelText.Text = text;
        NowPlayingLabelText.Text = text;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoginCommand.CanExecute(PasswordBox.Password))
            ViewModel.LoginCommand.Execute(PasswordBox.Password);
        PasswordBox.Password = string.Empty;
    }

    private void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
            LoginButton_Click(sender, e);
    }

    private void GuardCodeBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.SubmitGuardCommand.CanExecute(null))
            ViewModel.SubmitGuardCommand.Execute(null);
    }

    private void GameList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is GameInfo game)
            ViewModel.SelectCommand.Execute(game);
    }

    private void OpenStoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Selected is GameInfo game)
            ViewModel.OpenStoreCommand.Execute(game);
    }

    private static string T(string key, string fallback)
    {
        var value = LocalizationResolver?.Invoke(key);
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, key, StringComparison.Ordinal)
            ? fallback
            : value;
    }
}
