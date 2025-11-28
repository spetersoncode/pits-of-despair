using System.Collections.Generic;

namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// Mutable state passed between effect steps in a composite effect pipeline.
/// Allows steps to communicate results and control flow.
/// </summary>
public class EffectState
{
    /// <summary>
    /// Whether the pipeline should continue executing steps.
    /// Set to false by prechecks (saves, attack rolls) to stop on failure.
    /// </summary>
    public bool Continue { get; set; } = true;

    /// <summary>
    /// Overall success of the effect. At least one step must succeed
    /// for the effect to be considered successful.
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// Total damage dealt by damage steps. Used by vampiric effects.
    /// </summary>
    public int DamageDealt { get; set; } = 0;

    /// <summary>
    /// Whether the target succeeded on a saving throw.
    /// </summary>
    public bool SaveSucceeded { get; set; } = false;

    /// <summary>
    /// Whether the target failed a saving throw.
    /// </summary>
    public bool SaveFailed { get; set; } = false;

    /// <summary>
    /// Whether an attack roll hit the target.
    /// </summary>
    public bool AttackHit { get; set; } = false;

    /// <summary>
    /// Whether an attack roll missed the target.
    /// </summary>
    public bool AttackMissed { get; set; } = false;

    /// <summary>
    /// Custom properties for step-specific data sharing.
    /// </summary>
    public Dictionary<string, object> Properties { get; } = new();

    /// <summary>
    /// Resets the state for reuse with a new target.
    /// </summary>
    public void Reset()
    {
        Continue = true;
        Success = false;
        DamageDealt = 0;
        SaveSucceeded = false;
        SaveFailed = false;
        AttackHit = false;
        AttackMissed = false;
        Properties.Clear();
    }
}
