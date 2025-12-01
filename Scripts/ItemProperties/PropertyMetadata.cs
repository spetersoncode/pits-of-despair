namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Metadata for property distribution and spawning.
/// Defines when and how properties can appear on items.
/// </summary>
public class PropertyMetadata
{
    /// <summary>
    /// The property type identifier (e.g., "flaming", "evasion").
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// First floor this property can appear on.
    /// </summary>
    public int IntroFloor { get; init; } = 1;

    /// <summary>
    /// Spawn weight relative to other properties (1.0 = normal).
    /// </summary>
    public float SpawnWeight { get; init; } = 1.0f;

    /// <summary>
    /// Minimum value for the property amount.
    /// </summary>
    public int MinAmount { get; init; } = 1;

    /// <summary>
    /// Maximum value for the property amount.
    /// </summary>
    public int MaxAmount { get; init; } = 1;

    /// <summary>
    /// Item types this property can be applied to.
    /// </summary>
    public ItemType ValidTypes { get; init; } = ItemType.None;

    /// <summary>
    /// Optional color override for ring display (e.g., "Palette.Jade").
    /// </summary>
    public string? ColorOverride { get; init; }

    /// <summary>
    /// Whether this is a spawn-time modifier (applied at creation, not stored).
    /// </summary>
    public bool IsSpawnTimeModifier { get; init; } = false;
}
