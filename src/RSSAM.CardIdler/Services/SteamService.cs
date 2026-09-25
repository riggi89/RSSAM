using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace RSSAM.CardIdler.Services;

/// <summary>Asks the UI for Steam Guard codes when Steam needs one.</summary>
public interface IGuardPrompt
{
    /// <param name="isEmail">true = code was mailed to the account address, false = mobile authenticator.</param>
    Task<string?> AskCodeAsync(bool isEmail, string hint, bool previousWasWrong);
}

public sealed class SteamLoginException : Exception
{
    public SteamLoginException(string message) : base(message) { }
}

/// <summary>
/// Thin wrapper around SteamKit2: logs in (password + Steam Guard, or a saved refresh token),
/// reports which games are "being played", and hands out web cookies for the badges page.
/// </summary>
public sealed class SteamService : IDisposable
{
    private readonly SteamClient _client = new();
    private readonly CallbackManager _callbacks;
    private readonly SteamUser _user;
    private CancellationTokenSource? _pumpCts;
    private TaskCompletionSource<SteamUser.LoggedOnCallback>? _logOn;
    private TaskCompletionSource? _connected;
    private string? _refreshToken;

    public SteamService()
    {
        _callbacks = new CallbackManager(_client);
        _user = _client.GetHandler<SteamUser>()!;
        _callbacks.Subscribe<SteamClient.ConnectedCallback>(_ => _connected?.TrySetResult());
        _callbacks.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        _callbacks.Subscribe<SteamUser.LoggedOnCallback>(c => _logOn?.TrySetResult(c));
        _callbacks.Subscribe<SteamUser.LoggedOffCallback>(c => Disconnected?.Invoke($"Logged off by Steam ({c.Result})"));
        _callbacks.Subscribe<SteamUser.PlayingSessionStateCallback>(c => PlayingBlocked?.Invoke(c.PlayingBlocked));
    }

    public ulong SteamId64 => _client.SteamID?.ConvertToUInt64() ?? 0;

    /// <summary>Two-letter country Steam sees this login from (e.g. "DE") - decides the store region for prices.</summary>
    public string? Country { get; private set; }
    public string? RefreshToken => _refreshToken;
    public bool IsLoggedOn { get; private set; }

    /// <summary>Raised when the connection is lost after a successful login (reason as text).</summary>
    public event Action<string>? Disconnected;

    /// <summary>Steam kicked the idle session because the account started a game somewhere else.</summary>
    public event Action<bool>? PlayingBlocked;

    /// <summary>Logs in. Pass either a saved refresh token, or username + password.</summary>
    public async Task<string> LogInAsync(string username, string? password, string? savedToken, IGuardPrompt guard, CancellationToken ct)
    {
        StartPump();

        _connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Connect();
        await WaitAsync(_connected.Task, TimeSpan.FromSeconds(20), "Could not reach Steam (connection timed out).", ct);

        string accountName = username;
        if (!string.IsNullOrEmpty(savedToken))
        {
            _refreshToken = savedToken;
        }
        else
        {
            if (string.IsNullOrEmpty(password)) throw new SteamLoginException("Enter your password.");
            try
            {
                var session = await _client.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                {
                    Username = username,
                    Password = password,
                    IsPersistentSession = true,
                    Authenticator = new GuardAuthenticator(guard),
                });
                var result = await session.PollingWaitForResultAsync(ct);
                accountName = result.AccountName;
                _refreshToken = result.RefreshToken;
            }
            catch (AuthenticationException ex)
            {
                throw new SteamLoginException(ex.Result switch
                {
                    EResult.InvalidPassword => "Wrong username or password.",
                    EResult.RateLimitExceeded or EResult.AccountLoginDeniedThrottle => "Too many attempts - wait a few minutes and try again.",
                    EResult.TwoFactorCodeMismatch or EResult.InvalidLoginAuthCode => "That Steam Guard code was not accepted.",
                    _ => $"Login failed ({ex.Result}).",
                });
            }
        }

        _logOn = new TaskCompletionSource<SteamUser.LoggedOnCallback>(TaskCreationOptions.RunContinuationsAsynchronously);
        _user.LogOn(new SteamUser.LogOnDetails
        {
            Username = accountName,
            AccessToken = _refreshToken,
            ShouldRememberPassword = true,
        });
        var logged = await WaitAsync(_logOn.Task, TimeSpan.FromSeconds(30), "Steam did not answer the login request.", ct);
        if (logged.Result != EResult.OK)
        {
            var expired = logged.Result is EResult.AccessDenied or EResult.InvalidPassword or EResult.Expired or EResult.LoggedInElsewhere && !string.IsNullOrEmpty(savedToken);
            throw new SteamLoginException(expired
                ? "Saved login expired - please sign in again."
                : $"Steam refused the login ({logged.Result}).");
        }

        Country = string.IsNullOrEmpty(logged.IPCountryCode) ? null : logged.IPCountryCode.ToUpperInvariant();
        IsLoggedOn = true;
        return accountName;
    }

    /// <summary>Cookie header that makes steamcommunity.com treat requests as this logged-in account.</summary>
    /// <param name="viaFinalizeLogin">
    /// false = build steamLoginSecure from a client access token (what ArchiSteamFarm does);
    /// true = ask login.steampowered.com to hand out the cookie, the way the browser does.
    /// </param>
    public async Task<string> GetWebCookieHeaderAsync(bool viaFinalizeLogin)
    {
        var steamId = _client.SteamID ?? throw new InvalidOperationException("Not logged in.");
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();

        if (!viaFinalizeLogin)
        {
            var token = await _client.Authentication.GenerateAccessTokenForAppAsync(steamId, _refreshToken!, allowRenewal: false);
            return $"sessionid={sessionId}; steamLoginSecure={steamId.ConvertToUInt64()}%7C%7C{token.AccessToken}";
        }

        var jar = new CookieContainer();
        using var http = new HttpClient(new HttpClientHandler { CookieContainer = jar }) { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Card Idler");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://steamcommunity.com");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://steamcommunity.com/");

        using var form = new MultipartFormDataContent
        {
            { new StringContent(_refreshToken!), "nonce" },
            { new StringContent(sessionId), "sessionid" },
            { new StringContent("https://steamcommunity.com/login/home/?goto="), "redir" },
        };
        using var res = await http.PostAsync("https://login.steampowered.com/jwt/finalizelogin", form);
        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("transfer_info", out var transfers))
            throw new InvalidOperationException("Steam web login was refused: " + (body.Length > 200 ? body[..200] : body));

        foreach (var t in transfers.EnumerateArray())
        {
            var url = t.GetProperty("url").GetString() ?? "";
            if (!url.Contains("steamcommunity.com")) continue;
            using var tf = new MultipartFormDataContent { { new StringContent(steamId.ConvertToUInt64().ToString()), "steamID" } };
            foreach (var p in t.GetProperty("params").EnumerateObject())
                tf.Add(new StringContent(p.Value.ToString()), p.Name);
            using var _ = await http.PostAsync(url, tf);
        }

        var cookie = jar.GetCookies(new Uri("https://steamcommunity.com"))["steamLoginSecure"]?.Value
            ?? throw new InvalidOperationException("Steam web login returned no community cookie.");
        return $"sessionid={sessionId}; steamLoginSecure={cookie}";
    }

    /// <summary>Tells Steam these games are "in game" (an empty list stops idling).</summary>
    public void SetGamesPlayed(IEnumerable<int> appIds)
    {
        var msg = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
        foreach (var id in appIds)
            msg.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = new GameID((uint)id) });
        _client.Send(msg);
    }

    public void LogOff()
    {
        try { if (IsLoggedOn) _user.LogOff(); } catch { /* already gone */ }
        IsLoggedOn = false;
        _client.Disconnect();
    }

    public void Dispose()
    {
        try { LogOff(); } catch { }
        _pumpCts?.Cancel();
    }

    private void StartPump()
    {
        if (_pumpCts != null) return;
        _pumpCts = new CancellationTokenSource();
        var token = _pumpCts.Token;
        Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
                _callbacks.RunWaitCallbacks(TimeSpan.FromMilliseconds(250));
        }, token);
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback c)
    {
        if (IsLoggedOn)
        {
            IsLoggedOn = false;
            Disconnected?.Invoke(c.UserInitiated ? "Disconnected" : "Connection to Steam lost");
        }
        _connected?.TrySetException(new SteamLoginException("Could not reach Steam."));
        _logOn?.TrySetException(new SteamLoginException("Connection to Steam was lost during login."));
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, TimeSpan timeout, string timeoutMessage, CancellationToken ct)
    {
        var done = await Task.WhenAny(task, Task.Delay(timeout, ct));
        if (done != task) { ct.ThrowIfCancellationRequested(); throw new SteamLoginException(timeoutMessage); }
        return await task;
    }

    private static async Task WaitAsync(Task task, TimeSpan timeout, string timeoutMessage, CancellationToken ct)
    {
        var done = await Task.WhenAny(task, Task.Delay(timeout, ct));
        if (done != task) { ct.ThrowIfCancellationRequested(); throw new SteamLoginException(timeoutMessage); }
        await task;
    }

    private sealed class GuardAuthenticator : IAuthenticator
    {
        private readonly IGuardPrompt _prompt;
        public GuardAuthenticator(IGuardPrompt prompt) => _prompt = prompt;

        // Returning false = "no, I won't approve on the phone" -> Steam falls back to a typed code.
        public Task<bool> AcceptDeviceConfirmationAsync() => Task.FromResult(false);

        public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
            => await _prompt.AskCodeAsync(false, "", previousCodeWasIncorrect) ?? throw new SteamLoginException("Sign-in cancelled.");

        public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
            => await _prompt.AskCodeAsync(true, email, previousCodeWasIncorrect) ?? throw new SteamLoginException("Sign-in cancelled.");
    }
}
