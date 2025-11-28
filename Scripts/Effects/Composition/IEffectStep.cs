namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// Interface for individual steps in a composite effect pipeline.
/// Steps execute sequentially, sharing state and accumulating messages.
/// </summary>
public interface IEffectStep
{
    /// <summary>
    /// Executes this step in the effect pipeline.
    /// </summary>
    /// <param name="context">The effect context with target, caster, and game systems.</param>
    /// <param name="state">Mutable state shared between steps.</param>
    /// <param name="messages">Collector for accumulating messages.</param>
    void Execute(EffectContext context, EffectState state, MessageCollector messages);
}
