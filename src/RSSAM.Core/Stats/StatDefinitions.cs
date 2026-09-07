/* Portions derived from Steam Achievement Manager.
 * Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 * Modified for RSSAM by Daniel Riggi (riggi89), Copyright (c) 2026.
 * See LICENSE.md and NOTICE.md.
 */

namespace RSSAM.Models;

internal abstract class StatDefinition
{
    public string Id = "";
    public string DisplayName = "";
    public int Permission;
}

internal sealed class IntegerStatDefinition : StatDefinition
{
    public int MinValue;
    public int MaxValue;
    public int MaxChange;
    public bool IncrementOnly;
    public bool SetByTrustedGameServer;
    public int DefaultValue;
}

internal sealed class FloatStatDefinition : StatDefinition
{
    public float MinValue;
    public float MaxValue;
    public float MaxChange;
    public bool IncrementOnly;
    public float DefaultValue;
}

internal sealed class AchievementDefinition
{
    public string Id = "";
    public string Name = "";
    public string Description = "";
    public string? IconNormal;
    public string? IconLocked;
    public bool IsHidden;
    public int Permission;
}
