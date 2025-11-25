namespace PitsOfDespair.Generation.Pipeline;

/// <summary>
/// Interface for a single step in the dungeon generation pipeline.
/// Each pass executes on the shared GenerationContext, modifying the grid
/// and/or metadata according to its role.
/// </summary>
public interface IGenerationPass
{
    /// <summary>
    /// Display name for this pass (used in logging and debugging).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution priority. Lower values execute first.
    /// Typical ranges: Base=0, Modifiers=100-200, PostProcess=300+
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// The role of this pass in the pipeline.
    /// </summary>
    PassRole Role { get; }

    /// <summary>
    /// Execute this generation pass on the shared context.
    /// </summary>
    /// <param name="context">Shared generation context containing grid, metadata, and pass data.</param>
    void Execute(GenerationContext context);

    /// <summary>
    /// Check if this pass can execute given the current context state.
    /// </summary>
    /// <param name="context">Shared generation context.</param>
    /// <returns>True if the pass can execute, false to skip.</returns>
    bool CanExecute(GenerationContext context);
}
