namespace PitsOfDespair.Generation.Pipeline;

/// <summary>
/// Defines the role of a generation pass in the pipeline.
/// </summary>
public enum PassRole
{
    /// <summary>
    /// Primary topology generator. Initializes grid with walls and carves primary floor topology.
    /// Exactly one Base pass is required per pipeline.
    /// Examples: BSP, Cellular Automata, Drunkard's Walk
    /// </summary>
    Base,

    /// <summary>
    /// Transforms existing topology from base generator.
    /// Operates on specific regions or tile areas.
    /// Zero or more Modifier passes allowed per pipeline.
    /// Examples: CA applied to BSP rooms, erosion, feature carving
    /// </summary>
    Modifier,

    /// <summary>
    /// Validation, connectivity repair, or metadata analysis.
    /// Does not modify topology (or only repairs).
    /// Examples: Connectivity, Validation, Metadata analysis
    /// </summary>
    PostProcess
}
