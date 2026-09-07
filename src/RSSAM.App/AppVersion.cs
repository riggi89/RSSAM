// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM;

internal static class AppVersion
{
    public const string Fallback = "1.0.31";

    public static string Display
    {
        get
        {
            var version = typeof(AppVersion).Assembly.GetName().Version;
            if (version is null)
                return Fallback;

            var build = Math.Max(0, version.Build);
            return $"{version.Major}.{version.Minor}.{build}";
        }
    }
}
