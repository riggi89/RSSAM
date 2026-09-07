// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RSSAM.Presentation.Shell;
using RSSAM.Services;

namespace RSSAM.Views;

public sealed partial class SettingsPage : Page, IShellContentPage
{
    private bool _loading = true;

    public event EventHandler? ShellStateChanged;

    public string? SearchContextId => null;
    public string? SearchPlaceholder => null;
    public string StatusText => string.Empty;
    public bool CanGoBack => false;

    public SettingsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        Loaded += SettingsPage_Loaded;
        SizeChanged += SettingsPage_SizeChanged;
        App.LocalizationService.LanguageChanged += LocalizationService_LanguageChanged;
    }

    public IReadOnlyList<ShellToolbarItem> GetToolbarItems()
        =>
        [
            new ShellToolbarItem
            {
                Key = "settings-file",
                Text = App.LocalizationService.Get("Command.OpenSettingsFile"),
                Glyph = "\uE8A5",
                Placement = ShellToolbarItemPlacement.Left,
                Execute = OpenSettingsFile
            },
            new ShellToolbarItem
            {
                Key = "settings-defaults",
                Text = App.LocalizationService.Get("Command.Defaults"),
                Glyph = "\uE777",
                Placement = ShellToolbarItemPlacement.Left,
                Execute = () => _ = ResetSettingsAsync()
            }
        ];

    public void ApplySearch(string query) { }
    public void GoBack() { }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _loading = true;
        ApplyLocalization();
        ApplySettingsToControls();
        SettingsPathText.Text = App.SettingsService.SettingsPath;
        _loading = false;
        ApplyResponsiveLayout(ActualWidth);
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _loading = true;
            try
            {
                ApplyLocalization();
                RefreshComboBoxSelection(LanguageComboBox);
                RefreshComboBoxSelection(ThemeComboBox);
                RefreshComboBoxSelection(BackdropComboBox);
            }
            finally
            {
                _loading = false;
            }

            ShellStateChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void ApplySettingsToControls()
    {
        var settings = App.RuntimeSettings;

        SelectComboBoxItemByTag(LanguageComboBox, settings.Language, "de-DE");
        SelectComboBoxItemByTag(ThemeComboBox, settings.Theme, "System");
        SelectComboBoxItemByTag(BackdropComboBox, settings.Backdrop, "Mica");

        CompactNavigationToggle.IsOn = string.Equals(
            settings.NavigationMode,
            "Compact",
            StringComparison.OrdinalIgnoreCase);
        StatusBarToggle.IsOn = settings.ShowStatusBar;
        StartMaximizedToggle.IsOn = settings.StartMaximized;
        RememberWindowToggle.IsOn = settings.RememberWindowPosition;
        LoadGamesOnStartupSwitch.IsOn = settings.LoadGamesOnStartup;
        ConfirmBeforeStoreSwitch.IsOn = settings.ConfirmBeforeStore;
        ConfirmBeforeResetSwitch.IsOn = settings.ConfirmBeforeReset;
        ShowSuccessMessagesSwitch.IsOn = settings.ShowSuccessMessages;
        EnableStatisticsEditingByDefaultSwitch.IsOn = settings.EnableStatisticsEditingByDefault;

    }

    private void ApplyLocalization()
    {
        SettingsTitleText.Text = App.LocalizationService.Get("Settings.Title");
        SettingsSubtitleText.Text = App.LocalizationService.Get("Settings.Subtitle");
        AppearanceSectionText.Text = App.LocalizationService.Get("Settings.Section.Appearance");
        WindowSectionText.Text = App.LocalizationService.Get("Settings.Section.Window");
        StartSectionText.Text = App.LocalizationService.Get("Settings.Section.Start");
        ConfigurationSectionText.Text = App.LocalizationService.Get("Settings.Section.Configuration");

        LanguageSettingsRow.Title = App.LocalizationService.Get("Settings.Language.Title");
        LanguageSettingsRow.Description = App.LocalizationService.Get("Settings.Language.Description");
        LanguageGermanItem.Content = App.LocalizationService.Get("Settings.Language.German");
        LanguageEnglishItem.Content = App.LocalizationService.Get("Settings.Language.English");

        ThemeSettingsRow.Title = App.LocalizationService.Get("Settings.Theme.Title");
        ThemeSettingsRow.Description = App.LocalizationService.Get("Settings.Theme.Description");
        ThemeSystemItem.Content = App.LocalizationService.Get("Settings.Theme.System");
        ThemeLightItem.Content = App.LocalizationService.Get("Settings.Theme.Light");
        ThemeDarkItem.Content = App.LocalizationService.Get("Settings.Theme.Dark");

        BackdropSettingsRow.Title = App.LocalizationService.Get("Settings.Backdrop.Title");
        BackdropSettingsRow.Description = App.LocalizationService.Get("Settings.Backdrop.Description");
        BackdropStandardItem.Content = App.LocalizationService.Get("Settings.Backdrop.Standard");

        CompactNavigationSettingsRow.Title = App.LocalizationService.Get("Settings.CompactNav.Title");
        CompactNavigationSettingsRow.Description = App.LocalizationService.Get("Settings.CompactNav.Description");
        StatusBarSettingsRow.Title = App.LocalizationService.Get("Settings.StatusBar.Title");
        StatusBarSettingsRow.Description = App.LocalizationService.Get("Settings.StatusBar.Description");
        StartMaximizedSettingsRow.Title = App.LocalizationService.Get("Settings.StartMaximized.Title");
        StartMaximizedSettingsRow.Description = App.LocalizationService.Get("Settings.StartMaximized.Description");
        RememberWindowSettingsRow.Title = App.LocalizationService.Get("Settings.RememberWindow.Title");
        RememberWindowSettingsRow.Description = App.LocalizationService.Get("Settings.RememberWindow.Description");

        LoadGamesSettingsRow.Title = App.LocalizationService.Get("Settings.LoadGames.Title");
        LoadGamesSettingsRow.Description = App.LocalizationService.Get("Settings.LoadGames.Description");
        ConfirmStoreSettingsRow.Title = App.LocalizationService.Get("Settings.ConfirmStore.Title");
        ConfirmStoreSettingsRow.Description = App.LocalizationService.Get("Settings.ConfirmStore.Description");
        ConfirmResetSettingsRow.Title = App.LocalizationService.Get("Settings.ConfirmReset.Title");
        ConfirmResetSettingsRow.Description = App.LocalizationService.Get("Settings.ConfirmReset.Description");
        SuccessSettingsRow.Title = App.LocalizationService.Get("Settings.Success.Title");
        SuccessSettingsRow.Description = App.LocalizationService.Get("Settings.Success.Description");
        StatEditSettingsRow.Title = App.LocalizationService.Get("Settings.StatEdit.Title");
        StatEditSettingsRow.Description = App.LocalizationService.Get("Settings.StatEdit.Description");

        ConfigSettingsRow.Title = App.LocalizationService.Get("Settings.Config.Title");
        ConfigSettingsRow.Description = App.LocalizationService.Get("Settings.Config.Description");
        OpenSettingsFileButton.Content = App.LocalizationService.Get("Command.OpenSettingsFile");
        OpenSettingsFolderButton.Content = App.LocalizationService.Get("Command.SettingsFolder");

        SetToggleText(CompactNavigationToggle);
        SetToggleText(StatusBarToggle);
        SetToggleText(StartMaximizedToggle);
        SetToggleText(RememberWindowToggle);
        SetToggleText(LoadGamesOnStartupSwitch);
        SetToggleText(ConfirmBeforeStoreSwitch);
        SetToggleText(ConfirmBeforeResetSwitch);
        SetToggleText(ShowSuccessMessagesSwitch);
        SetToggleText(EnableStatisticsEditingByDefaultSwitch);

        AboutSectionText.Text = App.LocalizationService.Get("Settings.About.Section");
        AboutVersionText.Text = App.LocalizationService.Format("Settings.About.Version", AppVersion.Display);
        AboutDescriptionText.Text = App.LocalizationService.Get("Settings.About.Description");
        OriginalProjectText.Text = App.LocalizationService.Get("Settings.About.Original");
        LicenseTitleText.Text = App.LocalizationService.Get("Settings.About.License");
        LicenseDescriptionText.Text = App.LocalizationService.Get("Settings.About.LicenseText");
        CopyrightText.Text = App.LocalizationService.Get("Settings.About.Copyright");
        AboutNoteTitleText.Text = App.LocalizationService.Get("Settings.About.NoteTitle");
        AboutNoteMessageText.Text = App.LocalizationService.Get("Settings.About.NoteMessage");
    }

    private void SetToggleText(ToggleSwitch toggle)
    {
        toggle.OnContent = App.LocalizationService.Get("Toggle.On");
        toggle.OffContent = App.LocalizationService.Get("Toggle.Off");
    }

    private void SettingsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => SaveSettingsFromControls();

    private void SettingsControl_Toggled(object sender, RoutedEventArgs e)
        => SaveSettingsFromControls();

    private void SaveSettingsFromControls()
    {
        if (_loading)
            return;

        var settings = App.RuntimeSettings;
        var selectedLanguage = GetSelectedTag(LanguageComboBox, "de-DE");
        var languageChanged = !string.Equals(settings.Language, selectedLanguage, StringComparison.OrdinalIgnoreCase);

        settings.Language = selectedLanguage;
        settings.Theme = GetSelectedTag(ThemeComboBox, "System");
        settings.Backdrop = GetSelectedTag(BackdropComboBox, "Mica");
        settings.NavigationMode = CompactNavigationToggle.IsOn ? "Compact" : "Expanded";
        settings.IsNavigationPaneOpen = !CompactNavigationToggle.IsOn;
        settings.ShowStatusBar = StatusBarToggle.IsOn;
        settings.StartMaximized = StartMaximizedToggle.IsOn;
        settings.RememberWindowPosition = RememberWindowToggle.IsOn;
        settings.LoadGamesOnStartup = LoadGamesOnStartupSwitch.IsOn;
        settings.ConfirmBeforeStore = ConfirmBeforeStoreSwitch.IsOn;
        settings.ConfirmBeforeReset = ConfirmBeforeResetSwitch.IsOn;
        settings.ShowSuccessMessages = ShowSuccessMessagesSwitch.IsOn;
        settings.EnableStatisticsEditingByDefault = EnableStatisticsEditingByDefaultSwitch.IsOn;

        if (!App.TrySaveSettings())
            return;

        if (languageChanged)
            App.LocalizationService.SetLanguage(settings.Language);

        App.MainWindow?.ApplyRuntimeSettings();
        ShellStateChanged?.Invoke(this, EventArgs.Empty);
        App.InfoBarService.ShowSuccess(App.LocalizationService.Get("Settings.Saved"));
    }

    private void OpenSettingsFile_Click(object sender, RoutedEventArgs e)
        => OpenSettingsFile();

    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
        => OpenSettingsFolder();

    private void OpenSettingsFile()
    {
        try
        {
            App.TrySaveSettings(showError: false);
            Directory.CreateDirectory(App.SettingsService.SettingsDirectory);

            if (!File.Exists(App.SettingsService.SettingsPath))
                App.SettingsService.Save(App.RuntimeSettings);

            Process.Start(new ProcessStartInfo
            {
                FileName = App.SettingsService.SettingsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            App.InfoBarService.ShowError(
                App.LocalizationService.Format("Settings.OpenFileError", ex.Message));
        }
    }

    private void OpenSettingsFolder()
    {
        try
        {
            Directory.CreateDirectory(App.SettingsService.SettingsDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = App.SettingsService.SettingsDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            App.InfoBarService.ShowError(
                App.LocalizationService.Format("Settings.OpenFolderError", ex.Message));
        }
    }

    private async Task ResetSettingsAsync()
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            XamlRoot,
            App.LocalizationService.Get("Dialog.SettingsReset.Title"),
            App.LocalizationService.Format("Dialog.SettingsReset.Content", AppVersion.Display),
            App.LocalizationService.Get("Command.Reset"),
            App.LocalizationService.Get("Dialog.Cancel"));

        if (!confirmed)
            return;

        try
        {
            _loading = true;
            App.SettingsService.Reset(App.RuntimeSettings);
            App.LocalizationService.SetLanguage(App.RuntimeSettings.Language);
            ApplyLocalization();
            ApplySettingsToControls();
            App.MainWindow?.ApplyRuntimeSettings();
            App.InfoBarService.ShowSuccess(App.LocalizationService.Get("Settings.ResetDone"));
        }
        catch (Exception ex)
        {
            App.InfoBarService.ShowError(
                App.LocalizationService.Format("Settings.ResetError", ex.Message));
        }
        finally
        {
            _loading = false;
            ShellStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void SelectComboBoxItemByTag(ComboBox comboBox, string? tag, string fallback)
    {
        var requested = string.IsNullOrWhiteSpace(tag) ? fallback : tag;

        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), requested, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private static string GetSelectedTag(ComboBox comboBox, string fallback)
        => (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;

    private static void RefreshComboBoxSelection(ComboBox comboBox)
    {
        var selectedItem = comboBox.SelectedItem;
        if (selectedItem is null)
            return;

        comboBox.SelectedItem = null;
        comboBox.SelectedItem = selectedItem;
    }

    private void SettingsPage_SizeChanged(object sender, SizeChangedEventArgs e)
        => ApplyResponsiveLayout(e.NewSize.Width);

    private void ApplyResponsiveLayout(double width)
    {
        if (width <= 0)
            return;

        SettingsRootGrid.Padding = width < 760
            ? new Thickness(12, 12, 12, 28)
            : width < 1200
                ? new Thickness(20, 16, 20, 32)
                : new Thickness(28, 18, 28, 40);
    }

}
