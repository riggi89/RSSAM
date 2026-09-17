// RSSAM original code.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Globalization;
using System.Text;
using RSSAM.Models;

namespace RSSAM.Services;

public static class AchievementCsvExporter
{
    private static readonly string[] Headers =
    [
        "AppId",
        "Game",
        "AchievementId",
        "Name",
        "Description",
        "Unlocked",
        "UnlockTimeUtc",
        "Protected",
        "Hidden"
    ];

    public static string Create(GameInfo game, IEnumerable<AchievementItem> achievements)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(achievements);

        var builder = new StringBuilder();
        AppendRow(builder, Headers);

        foreach (var achievement in achievements)
        {
            ArgumentNullException.ThrowIfNull(achievement);

            AppendRow(
                builder,
                game.Id.ToString(CultureInfo.InvariantCulture),
                game.Name,
                achievement.Id,
                achievement.Name,
                achievement.Description,
                achievement.IsChecked ? "true" : "false",
                achievement.UnlockTime?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                achievement.IsProtected ? "true" : "false",
                achievement.IsHidden ? "true" : "false");
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, params string[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
                builder.Append(',');

            AppendEscaped(builder, values[index]);
        }

        builder.Append("\r\n");
    }

    private static void AppendEscaped(StringBuilder builder, string? value)
    {
        value ??= string.Empty;

        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            builder.Append(value);
            return;
        }

        builder.Append('"');
        builder.Append(value.Replace("\"", "\"\"", StringComparison.Ordinal));
        builder.Append('"');
    }
}
