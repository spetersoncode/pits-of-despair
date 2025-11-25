using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Systems.Spawning.Placement;

/// <summary>
/// Static provider for dungeon metadata access during spawning.
/// SpawnManager sets the current metadata before population begins.
/// </summary>
public static class MetadataProvider
{
    /// <summary>
    /// Current dungeon metadata. Set by SpawnManager before spawning.
    /// </summary>
    public static DungeonMetadata Current { get; private set; }

    /// <summary>
    /// Set the current metadata for use by placement strategies.
    /// </summary>
    public static void SetMetadata(DungeonMetadata metadata)
    {
        Current = metadata;
    }

    /// <summary>
    /// Clear the current metadata.
    /// </summary>
    public static void Clear()
    {
        Current = null;
    }

    /// <summary>
    /// Check if metadata is available.
    /// </summary>
    public static bool HasMetadata => Current != null;
}
