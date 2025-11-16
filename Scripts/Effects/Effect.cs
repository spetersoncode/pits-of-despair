using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects;

/// <summary>
/// Base class for all effects in the game.
/// Effects can be applied by items, spells, traps, or environmental hazards.
/// </summary>
public abstract class Effect
{
    /// <summary>
    /// The name of this effect type.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Applies this effect to the target entity.
    /// </summary>
    /// <param name="target">The entity receiving the effect.</param>
    /// <param name="context">The action context containing game systems.</param>
    /// <returns>The result of applying the effect.</returns>
    public abstract EffectResult Apply(BaseEntity target, ActionContext context);
}

/// <summary>
/// Represents the result of applying an effect.
/// </summary>
public class EffectResult
{
    /// <summary>
    /// Whether the effect was successfully applied.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A message describing what happened when the effect was applied.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Optional color for the message (hex format, e.g., "#66ff66").
    /// </summary>
    public string MessageColor { get; set; }

    public EffectResult(bool success, string message, string messageColor = "#ffffff")
    {
        Success = success;
        Message = message;
        MessageColor = messageColor;
    }
}
