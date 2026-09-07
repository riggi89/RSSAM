// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

namespace RSSAM.Search;

public interface IGlobalSearchProvider
{
    string ContextId { get; }
    string Placeholder { get; }
    void Apply(string query);
}
