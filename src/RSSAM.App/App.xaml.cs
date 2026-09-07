// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using Microsoft.UI.Xaml;
using RSSAM.Localization;
using RSSAM.Models;
using RSSAM.Services;

namespace RSSAM;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static AppSettingsService SettingsService { get; } = new();
    public static GameFavoritesService GameFavoritesService { get; } = new();
    public static AppSettings RuntimeSettings { get; private set; } = new();
    public static LocalizationService LocalizationService { get; private set; } = new("de-DE");
    public static InfoBarService InfoBarService { get; } = new();

    public App()
    {
        if (SteamWorkerServer.TryRunFromCommandLine() is int workerExitCode)
        {
            Environment.Exit(workerExitCode);
            return;
        }

        StartupDiagnostics.StartSession();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                StartupDiagnostics.WriteException("Unhandled application-domain exception", exception);
            else
                StartupDiagnostics.Write($"Unhandled application-domain exception: {e.ExceptionObject}");
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
            StartupDiagnostics.WriteException("Unobserved task exception", e.Exception);
        UnhandledException += (_, e) =>
            StartupDiagnostics.ReportFatal("Unhandled WinUI exception", e.Exception);

        try
        {
            RuntimeSettings = SettingsService.Load();
            LocalizationService = new LocalizationService(RuntimeSettings.Language);

            InitializeComponent();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.ReportFatal("Application initialization failed", ex);
            throw;
        }
    }

    public static bool TrySaveSettings(bool showError = true)
    {
        try
        {
            SettingsService.Save(RuntimeSettings);
            return true;
        }
        catch (Exception ex)
        {
            if (showError)
            {
                InfoBarService.ShowError(
                    LocalizationService.Format("Settings.SaveError", ex.Message));
            }

            return false;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            MainWindow.RestorePersistedWindowPlacement();
            StartupDiagnostics.Write("Main window activated successfully.");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.ReportFatal("Main-window creation failed", ex);
            throw;
        }
    }
}
