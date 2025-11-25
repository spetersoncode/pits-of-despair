using System.Collections.Generic;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Defines a faction theme containing creatures that thematically belong together.
/// Roles are inferred from creature stats, not manually tagged.
/// </summary>
public class FactionTheme
{
    /// <summary>
    /// Unique identifier for this theme (e.g., "goblinoid", "undead", "vermin").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this theme.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of creature IDs that belong to this theme.
    /// Roles/archetypes are inferred from creature stats at runtime.
    /// </summary>
    public List<string> Creatures { get; set; } = new();

    /// <summary>
    /// Optional color override for theme visualization (hex color).
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for this theme.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Minimum floor depth where this theme can appear.
    /// </summary>
    public int MinFloor { get; set; } = 1;

    /// <summary>
    /// Maximum floor depth where this theme can appear.
    /// </summary>
    public int MaxFloor { get; set; } = 99;
}
