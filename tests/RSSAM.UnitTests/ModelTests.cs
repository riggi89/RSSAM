// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Globalization;
using RSSAM.Models;

namespace RSSAM.UnitTests;

public sealed class ModelTests
{
    [Fact]
    public void AchievementItem_ReportsChangesAndProtection()
    {
        var item = new AchievementItem
        {
            Id = "ACH_WIN",
            Name = "Winner",
            Permission = 2,
            OriginalState = false
        };

        item.IsChecked = true;

        Assert.True(item.IsModified);
        Assert.True(item.IsProtected);
        Assert.False(item.CanEdit);
    }

    [Fact]
    public void StatItem_ParsesFiniteFloatUsingCurrentCulture()
    {
        using var culture = new CultureScope("de-DE");
        var item = new StatItem
        {
            Id = "distance",
            DisplayName = "Distance",
            Kind = StatValueKind.Float,
            OriginalFloatValue = 1.5f,
            EditableValue = "2,5"
        };

        Assert.True(item.TryGetFloat(out var value));
        Assert.Equal(2.5f, value);
        Assert.True(item.IsModified);

        item.EditableValue = "NaN";
        Assert.False(item.TryGetFloat(out _));
    }

    [Fact]
    public void StatItem_EditingAndPermissionControlReadOnlyState()
    {
        var editable = CreateIntegerStat(permission: 0);
        editable.SetEditingEnabled(true);

        var protectedItem = CreateIntegerStat(permission: 2);
        protectedItem.SetEditingEnabled(true);

        Assert.False(editable.IsReadOnly);
        Assert.True(protectedItem.IsReadOnly);
    }

    [Fact]
    public void GameInfo_RejectsInvalidRequiredValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameInfo(0, "normal", "Game"));
        Assert.Throws<ArgumentException>(() => new GameInfo(1, "", "Game"));
        Assert.Throws<ArgumentException>(() => new GameInfo(1, "normal", ""));
    }

    private static StatItem CreateIntegerStat(int permission) => new()
    {
        Id = "score",
        DisplayName = "Score",
        Kind = StatValueKind.Integer,
        Permission = permission,
        OriginalIntValue = 10,
        EditableValue = "10"
    };

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}
