// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Search;

public sealed class DelegateSearchProvider : IGlobalSearchProvider
{
    private readonly Action<string> _apply;

    public DelegateSearchProvider(string contextId, string placeholder, Action<string> apply)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextId);
        ArgumentNullException.ThrowIfNull(placeholder);
        ArgumentNullException.ThrowIfNull(apply);

        ContextId = contextId;
        Placeholder = placeholder;
        _apply = apply;
    }

    public string ContextId { get; }
    public string Placeholder { get; }

    public void Apply(string query) => _apply(query);
}
