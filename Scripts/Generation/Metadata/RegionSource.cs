namespace PitsOfDespair.Generation.Metadata;

/// <summary>
/// Indicates how a region was created.
/// </summary>
public enum RegionSource
{
    /// <summary>
    /// Found by flood-fill metadata analysis.
    /// </summary>
    Detected,

    /// <summary>
    /// Created by BSP room generation.
    /// </summary>
    BSPRoom,

    /// <summary>
    /// Created or transformed by cellular automata.
    /// </summary>
    Cave,

    /// <summary>
    /// Stamped from a prefab definition.
    /// </summary>
    Prefab,

    /// <summary>
    /// User-defined or custom algorithm.
    /// </summary>
    Custom
}
