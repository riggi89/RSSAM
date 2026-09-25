using RSSAM.CardIdler.Services;

namespace RSSAM.CardIdler.ViewModels;

/// <summary>Sign-in, sign-out, Steam Guard prompt and automatic reconnect.</summary>
public sealed partial class MainViewModel
{
    private SteamService? _steam;
    private TaskCompletionSource<string?>? _guardTcs;
    private string _username;
    private string _accountName = "";
    private string _loginError = "";
    private bool _stayLoggedIn = true;
    private bool _busy;
    private bool _guardVisible;
    private string _guardHint = "";
    private string _guardCode = "";
    private bool _guardWrong;

    public string Username { get => _username; set => Set(ref _username, value); }
    public string AccountName { get => _accountName; private set => Set(ref _accountName, value); }
    public string LoginError { get => _loginError; private set { Set(ref _loginError, value); OnPropertyChanged(nameof(HasLoginError)); } }
    public bool HasLoginError => _loginError.Length > 0;
    public bool StayLoggedIn { get => _stayLoggedIn; set => Set(ref _stayLoggedIn, value); }
    public bool Busy
    {
        get => _busy;
        private set
        {
            if (!Set(ref _busy, value)) return;
            OnPropertyChanged(nameof(NotBusy));
            OnPropertyChanged(nameof(IsWorking));
            RefreshCommands();
        }
    }
    public bool NotBusy => !_busy;

    public bool GuardVisible { get => _guardVisible; private set => Set(ref _guardVisible, value); }
    public string GuardHint { get => _guardHint; private set => Set(ref _guardHint, value); }
    public bool GuardWrong { get => _guardWrong; private set => Set(ref _guardWrong, value); }
    public string GuardCode { get => _guardCode; set => Set(ref _guardCode, value); }

    /// <summary>Startup: sign in with the saved login, if there is one. Only scans - idling waits for Start.</summary>
    public async Task TryAutoLoginAsync()
    {
        if (string.IsNullOrEmpty(_settings.Username) || _settings.GetToken() == null) return;
        await LoginAsync(null);
    }

    private async Task LoginAsync(string? password)
    {
        if (Busy) return;
        Busy = true;
        LoginError = "";
        State = ConnState.Connecting;
        Status = "Connecting to Steam...";
        var user = Username.Trim();
        var token = string.IsNullOrEmpty(password) ? _settings.GetToken() : null;

        _steam?.Dispose();
        var steam = _steam = CreateSteam();
        try
        {
            if (user.Length == 0) throw new SteamLoginException("Enter your Steam account name.");
            var account = await steam.LogInAsync(user, password, token, this, CancellationToken.None);
            AccountName = account;
            _settings.Username = user;
            _settings.SetToken(StayLoggedIn ? steam.RefreshToken : null);
            OnSignedIn(steam);
            AddLog($"Signed in as {account} (store region {_meta.Country})");
            _ = ScanAsync();
        }
        catch (Exception ex)
        {
            if (ex is not SteamLoginException) Log.Write("Login failed", ex);
            if (token != null) _settings.SetToken(null); // a rejected saved token is useless from now on
            LoginError = ex is SteamLoginException ? ex.Message : "Login failed: " + ex.Message;
            steam.Dispose();
            _steam = null;
            State = ConnState.Offline;
            Status = "Sign in to start farming cards";
        }
        finally { Busy = false; }
    }

    private void OnSignedIn(SteamService steam)
    {
        if (steam.Country != null) _meta.Country = steam.Country;
        State = ConnState.Online;
    }

    private void Logout()
    {
        StopIdling(null);
        _settings.SetToken(null);
        _steam?.Dispose();
        _steam = null;
        ClearGames();
        AccountName = "";
        State = ConnState.Offline;
        Status = "Signed out";
        AddLog("Signed out");
    }

    private SteamService CreateSteam()
    {
        var steam = new SteamService();
        steam.Disconnected += reason => Dispatch(() => OnLostConnection(steam, reason));
        steam.PlayingBlocked += blocked => Dispatch(() => OnPlayingBlocked(blocked));
        return steam;
    }

    private void OnLostConnection(SteamService steam, string reason)
    {
        if (steam != _steam || !IsLoggedIn) return; // an old connection that was already replaced
        var wasIdling = IsIdling;
        StopIdling(null);
        AddLog(reason + " - reconnecting...");
        _ = ReconnectAsync(wasIdling);
    }

    /// <summary>Retries with the saved login; picks idling back up only if it was running when the connection dropped.</summary>
    private async Task ReconnectAsync(bool resumeIdling)
    {
        foreach (var delay in new[] { 5, 15, 30, 60, 120 })
        {
            State = ConnState.Connecting;
            Status = $"Connection lost - retrying in {delay}s";
            await Task.Delay(TimeSpan.FromSeconds(delay));
            var token = _settings.GetToken();
            if (string.IsNullOrEmpty(token)) break; // not saved (or the user signed out meanwhile)

            _steam?.Dispose();
            var steam = _steam = CreateSteam();
            try
            {
                await steam.LogInAsync(Username, null, token, this, CancellationToken.None);
                OnSignedIn(steam);
                AddLog("Reconnected");
                if (resumeIdling) StartIdling(); else _ = ScanAsync();
                return;
            }
            catch (Exception ex) { Log.Write("Reconnect failed: " + ex.Message); }
        }
        _steam?.Dispose();
        _steam = null;
        State = ConnState.Offline;
        Status = "Could not reconnect - sign in again";
    }

    // ----------------------------------------------------------------- Steam Guard prompt
    public Task<string?> AskCodeAsync(bool isEmail, string hint, bool previousWasWrong)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatch(() =>
        {
            _guardTcs = tcs;
            GuardCode = "";
            GuardWrong = previousWasWrong;
            GuardHint = isEmail ? $"Steam mailed a code to {hint}" : "Enter the code from your Steam mobile app";
            GuardVisible = true;
            Status = "Waiting for Steam Guard code";
        });
        return tcs.Task;
    }

    private void ResolveGuard(string? code)
    {
        GuardVisible = false;
        _guardTcs?.TrySetResult(code);
        _guardTcs = null;
        if (code != null) Status = "Verifying code...";
    }
}
