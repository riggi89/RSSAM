// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using RSSAM.API;
using RSSAM.Interfaces;

namespace RSSAM.Services;

internal static class SteamClientErrorFormatter
{
    public static string GetMessage(ClientInitializeException exception, ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(localization);

        var key = exception.Failure switch
        {
            ClientInitializeFailure.UnsupportedArchitecture => "Core.Steam.UnsupportedArchitecture",
            ClientInitializeFailure.GetInstallPath => "Core.Steam.InstallPathMissing",
            ClientInitializeFailure.Load => "Core.Steam.ClientLoadFailed",
            ClientInitializeFailure.SessionBusy => "Core.Steam.SessionBusy",
            ClientInitializeFailure.CreateSteamClient => "Core.Steam.InterfaceFailed",
            ClientInitializeFailure.CreateSteamPipe => "Core.Steam.PipeFailed",
            ClientInitializeFailure.ConnectToGlobalUser => "Core.Steam.UserConnectionFailed",
            ClientInitializeFailure.AppIdMismatch => "Core.Steam.AppIdMismatch",
            _ => "Core.Steam.UnknownFailure"
        };

        return localization.Get(key);
    }
}
