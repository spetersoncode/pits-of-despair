using System.Collections.Generic;

namespace PitsOfDespair.Data;

/// <summary>
/// Groups decorations for weighted random selection.
/// Each set corresponds to a theme (or generic/null for universal decorations).
/// </summary>
public class DecorationSet
{
    /// <summary>
    /// Unique identifier for this decoration set.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Theme this set belongs to. Null for generic decorations.
    /// </summary>
    public string ThemeId { get; set; }

    /// <summary>
    /// Weighted list of decorations in this set.
    /// </summary>
    public List<DecorationEntry> Decorations { get; set; } = new();

    /// <summary>
    /// Individual decoration definitions within this set.
    /// Key is the decoration ID, value is the full DecorationData.
    /// </summary>
    public Dictionary<string, DecorationData> Entries { get; set; } = new();
}

/// <summary>
/// Entry in a decoration set with weight for random selection.
/// </summary>
public class DecorationEntry
{
    /// <summary>
    /// Reference to a decoration ID in the Entries dictionary.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Selection weight. Higher values = more likely to be selected.
    /// </summary>
    public int Weight { get; set; } = 1;
}
