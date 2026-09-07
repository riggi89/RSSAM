// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using RSSAM.Interfaces;

namespace RSSAM.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, string> _fallback;
    private Dictionary<string, string> _strings;

    public LocalizationService(string language = "de-DE")
    {
        _fallback = LoadDictionary("en-US");
        _strings = _fallback;
        SetLanguage(language);
    }

    public string Language { get; private set; } = "de-DE";

    public string SteamLanguage => Language.Equals("de-DE", StringComparison.OrdinalIgnoreCase)
        ? "german"
        : "english";

    public event EventHandler? LanguageChanged;

    public void SetLanguage(string language)
    {
        var normalized = NormalizeLanguage(language);
        var changed = !string.Equals(Language, normalized, StringComparison.OrdinalIgnoreCase);

        Language = normalized;

        var culture = CultureInfo.GetCultureInfo(normalized);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var localized = LoadDictionary(normalized);
        _strings = localized.Count > 0 ? localized : _fallback;

        if (changed)
            LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        if (_strings.TryGetValue(key, out var value))
            return value;

        if (_fallback.TryGetValue(key, out value))
            return value;

        return key;
    }

    public string Format(string key, params object?[] args)
        => string.Format(CultureInfo.CurrentCulture, Get(key), args);

    private static string NormalizeLanguage(string? language)
        => string.Equals(language, "en-US", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? "en-US"
            : "de-DE";

    private static Dictionary<string, string> LoadDictionary(string language)
    {
        // 1) Preferred: embedded resources. LogicalName is explicitly fixed in
        // RSSAM.Core.csproj so it is independent of AssemblyName/RootNamespace.
        var embedded = TryLoadEmbeddedDictionary(language);
        if (embedded.Count > 0)
            return embedded;

        // 2) Fallback: the same JSON resources are copied to the output/publish
        // directory. This also makes local debugging resilient if MSBuild changes
        // resource manifest naming in a future SDK version.
        var fileBased = TryLoadFileDictionary(language);
        if (fileBased.Count > 0)
            return fileBased;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> TryLoadEmbeddedDictionary(string language)
    {
        try
        {
            var assembly = typeof(LocalizationService).Assembly;
            var expectedName = $"RSSAM.Localization.Resources.{language}.json";
            var resourceNames = assembly.GetManifestResourceNames();

            var resourceName = resourceNames.FirstOrDefault(name =>
                    string.Equals(name, expectedName, StringComparison.OrdinalIgnoreCase))
                ?? resourceNames.FirstOrDefault(name =>
                    name.EndsWith($".Localization.Resources.{language}.json", StringComparison.OrdinalIgnoreCase))
                ?? resourceNames.FirstOrDefault(name =>
                    name.EndsWith($".{language}.json", StringComparison.OrdinalIgnoreCase) &&
                    name.Contains("Localization", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
                return EmptyDictionary();

            using var stream = assembly.GetManifestResourceStream(resourceName);
            return stream is null ? EmptyDictionary() : DeserializeDictionary(stream);
        }
        catch
        {
            return EmptyDictionary();
        }
    }

    private static Dictionary<string, string> TryLoadFileDictionary(string language)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(LocalizationService).Assembly.Location);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Localization", "Resources", $"{language}.json"),
            Path.Combine(AppContext.BaseDirectory, "Resources", "Localization", $"{language}.json"),
            assemblyDirectory is null
                ? string.Empty
                : Path.Combine(assemblyDirectory, "Localization", "Resources", $"{language}.json")
        };

        foreach (var path in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                using var stream = File.OpenRead(path);
                var dictionary = DeserializeDictionary(stream);
                if (dictionary.Count > 0)
                    return dictionary;
            }
            catch
            {
                // Try the next location. Localization must never prevent startup.
            }
        }

        return EmptyDictionary();
    }

    private static Dictionary<string, string> DeserializeDictionary(Stream stream)
        => JsonSerializer.Deserialize<Dictionary<string, string>>(stream) is { } data
            ? new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase)
            : EmptyDictionary();

    private static Dictionary<string, string> EmptyDictionary()
        => new(StringComparer.OrdinalIgnoreCase);
}
