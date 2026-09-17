// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using RSSAM.Models;
using RSSAM.Services;

namespace RSSAM.UnitTests;

public sealed class AchievementCsvExporterTests
{
    [Fact]
    public void Create_ExportsStableColumnsAndInvariantValues()
    {
        var game = new GameInfo(278360, "normal", "A Story About My Uncle");
        var achievement = new AchievementItem
        {
            Id = "ACH_FINISH",
            Name = "The End",
            Description = "Finish the story",
            IsChecked = true,
            OriginalState = true,
            Permission = 2,
            IsHidden = true,
            UnlockTime = new DateTime(2026, 9, 17, 18, 30, 0, DateTimeKind.Utc)
        };

        var csv = AchievementCsvExporter.Create(game, [achievement]);

        Assert.StartsWith("AppId,Game,AchievementId,Name,Description,Unlocked,UnlockTimeUtc,Protected,Hidden\r\n", csv);
        Assert.Contains("278360,A Story About My Uncle,ACH_FINISH,The End,Finish the story,true,2026-09-17T18:30:00.0000000Z,true,true\r\n", csv);
    }

    [Fact]
    public void Create_EscapesCommasQuotesAndLineBreaks()
    {
        var game = new GameInfo(1, "normal", "Game, \"Deluxe\"");
        var achievement = new AchievementItem
        {
            Id = "ACH_1",
            Name = "One, Two",
            Description = "Line 1\r\nLine \"2\""
        };

        var csv = AchievementCsvExporter.Create(game, [achievement]);

        Assert.Contains("\"Game, \"\"Deluxe\"\"\"", csv);
        Assert.Contains("\"One, Two\"", csv);
        Assert.Contains("\"Line 1\r\nLine \"\"2\"\"\"", csv);
    }
}
