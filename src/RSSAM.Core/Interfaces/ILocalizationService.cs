// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Interfaces;

public interface ILocalizationService
{
    string Language { get; }
    string SteamLanguage { get; }
    event EventHandler? LanguageChanged;
    void SetLanguage(string language);
    string Get(string key);
    string Format(string key, params object?[] args);
}
