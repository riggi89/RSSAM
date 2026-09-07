// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using RSSAM.Localization;
using RSSAM.Search;

namespace RSSAM.UnitTests;

public sealed class LocalizationAndSearchTests
{
    [Fact]
    public void Localization_NormalizesLanguageAndFallsBackToKey()
    {
        var localization = new LocalizationService("en");

        Assert.Equal("en-US", localization.Language);
        Assert.Equal("english", localization.SteamLanguage);
        Assert.Equal("Missing.Test.Key", localization.Get("Missing.Test.Key"));

        localization.SetLanguage("unsupported");
        Assert.Equal("de-DE", localization.Language);
        Assert.Equal("german", localization.SteamLanguage);
    }

    [Fact]
    public void DelegateSearchProvider_ForwardsQuery()
    {
        string? received = null;
        var provider = new DelegateSearchProvider("Games", "Search games", query => received = query);

        provider.Apply("portal");

        Assert.Equal("portal", received);
    }

    [Fact]
    public void DelegateSearchProvider_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new DelegateSearchProvider("", "Search", _ => { }));
        Assert.Throws<ArgumentNullException>(() => new DelegateSearchProvider("Games", null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new DelegateSearchProvider("Games", "Search", null!));
    }
}
