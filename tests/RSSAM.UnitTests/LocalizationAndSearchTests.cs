// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using RSSAM.Localization;
using RSSAM.Search;

namespace RSSAM.UnitTests;

public sealed class LocalizationAndSearchTests
{
    [Fact]
    public void Localization_NormalizesLanguageAndFallsBackToKey()
    {
        using var culture = new CultureScope();
        var localization = new LocalizationService("en");

        Assert.Equal("en-US", localization.Language);
        Assert.Equal("english", localization.SteamLanguage);
        Assert.Equal("Idler", localization.Get("Nav.CardIdler"));
        Assert.Equal("German (Deutsch)", localization.Get("Settings.Language.German"));
        Assert.Equal("Missing.Test.Key", localization.Get("Missing.Test.Key"));

        localization.SetLanguage("es");
        Assert.Equal("es-ES", localization.Language);
        Assert.Equal("spanish", localization.SteamLanguage);
        Assert.Equal("Juegos", localization.Get("Nav.Games"));
        Assert.Equal("Buscar juegos con cromos disponibles …", localization.Get("Search.CardIdler"));

        localization.SetLanguage("fr-FR");
        Assert.Equal("fr-FR", localization.Language);
        Assert.Equal("french", localization.SteamLanguage);
        Assert.Equal("Jeux", localization.Get("Nav.Games"));
        Assert.Equal("Se connecter avec un code QR", localization.Get("CardIdler.QrSignIn"));

        localization.SetLanguage("tr");
        Assert.Equal("tr-TR", localization.Language);
        Assert.Equal("turkish", localization.SteamLanguage);
        Assert.Equal("Oyunlar", localization.Get("Nav.Games"));
        Assert.Equal("Ayrıntı görünümü", localization.Get("CardIdler.View.Detail"));

        localization.SetLanguage("unsupported");
        Assert.Equal("de-DE", localization.Language);
        Assert.Equal("german", localization.SteamLanguage);
        Assert.Equal("Deutsch (German)", localization.Get("Settings.Language.German"));
    }

    [Fact]
    public void LocalizationResources_HaveMatchingKeysAndFormatPlaceholders()
    {
        var assembly = typeof(LocalizationService).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("RSSAM.Localization.Resources.", StringComparison.Ordinal) &&
                           name.EndsWith(".json", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(5, resourceNames.Length);

        var dictionaries = resourceNames.ToDictionary(
            name => name,
            name =>
            {
                using var stream = assembly.GetManifestResourceStream(name);
                Assert.NotNull(stream);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
            },
            StringComparer.Ordinal);

        var baseline = dictionaries.Single(pair => pair.Key.EndsWith(".en-US.json", StringComparison.Ordinal)).Value;
        var baselineKeys = baseline.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();

        foreach (var (resourceName, dictionary) in dictionaries)
        {
            Assert.Equal(baselineKeys, dictionary.Keys.OrderBy(key => key, StringComparer.Ordinal));

            foreach (var key in baselineKeys)
            {
                Assert.Equal(
                    GetPlaceholderIndexes(baseline[key]),
                    GetPlaceholderIndexes(dictionary[key]));
            }
        }
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

    private static string[] GetPlaceholderIndexes(string value)
        => Regex.Matches(value, @"\{(\d+)(?:[^{}]*)\}")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(index => index, StringComparer.Ordinal)
            .ToArray();

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _currentCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _currentUiCulture = CultureInfo.CurrentUICulture;
        private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _currentCulture;
            CultureInfo.CurrentUICulture = _currentUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        }
    }
}
