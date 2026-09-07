// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RSSAM.Presentation.Shell;

namespace RSSAM.Views;

public sealed partial class ChangelogPage : Page, IShellContentPage
{
    private const string ChangelogResourceName = "RSSAM.CHANGELOG.md";

    // This page has no dynamic shell state. Explicit no-op accessors satisfy
    // the common page contract without creating an unused backing event.
    event EventHandler? IShellContentPage.ShellStateChanged
    {
        add { }
        remove { }
    }

    public string? SearchContextId => null;
    public string? SearchPlaceholder => null;
    public string StatusText => string.Empty;
    public bool CanGoBack => false;

    public ChangelogPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        SizeChanged += ChangelogPage_SizeChanged;
        BuildEntries();
    }

    public IReadOnlyList<ShellToolbarItem> GetToolbarItems() => [];
    public void ApplySearch(string query) { }
    public void GoBack() { }

    private void BuildEntries()
    {
        EntriesPanel.Children.Clear();

        foreach (var entry in ReadEntries())
        {
            var stack = new StackPanel
            {
                Spacing = 7,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            stack.Children.Add(new TextBlock
            {
                Text = entry.Heading,
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            foreach (var line in entry.Lines)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "• " + line,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Left
                });
            }

            var card = new Border
            {
                Style = (Style)Application.Current.Resources["SectionCardStyle"],
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = stack
            };

            EntriesPanel.Children.Add(card);
        }
    }

    private static IReadOnlyList<ChangelogEntry> ReadEntries()
    {
        using var stream = typeof(ChangelogPage).Assembly
            .GetManifestResourceStream(ChangelogResourceName);

        if (stream is null)
        {
            return
            [
                new ChangelogEntry(
                    "Changelog unavailable",
                    ["The embedded CHANGELOG.md resource could not be loaded."])
            ];
        }

        using var reader = new StreamReader(stream);
        var entries = new List<ChangelogEntry>();
        string? heading = null;
        List<string>? lines = null;

        while (reader.ReadLine() is { } rawLine)
        {
            if (rawLine.StartsWith("## ", StringComparison.Ordinal))
            {
                AddEntry(entries, heading, lines);
                heading = rawLine[3..].Trim();
                lines = [];
                continue;
            }

            if (heading is not null &&
                rawLine.StartsWith("- ", StringComparison.Ordinal))
            {
                lines!.Add(RemoveInlineMarkdown(rawLine[2..].Trim()));
            }
        }

        AddEntry(entries, heading, lines);
        return entries;
    }

    private static void AddEntry(
        ICollection<ChangelogEntry> entries,
        string? heading,
        IReadOnlyList<string>? lines)
    {
        if (!string.IsNullOrWhiteSpace(heading) && lines is { Count: > 0 })
            entries.Add(new ChangelogEntry(heading, lines));
    }

    private static string RemoveInlineMarkdown(string text)
        => text.Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal);

    private void ChangelogPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ContentPanel.Margin = e.NewSize.Width < 760
            ? new Thickness(16)
            : new Thickness(32);
    }

    private sealed record ChangelogEntry(
        string Heading,
        IReadOnlyList<string> Lines);
}
