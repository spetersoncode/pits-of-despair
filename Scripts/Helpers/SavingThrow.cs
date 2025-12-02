using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Core;
using PitsOfDespair.Effects;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Handles opposed saving throw rolls for effect resistance.
/// Opposed roll: Caster 2d6+attackStat vs Target 2d6+saveStat.
/// Defender wins ties.
/// </summary>
public static class SavingThrow
{
    /// <summary>
    /// Attempts a saving throw for the target to resist an effect.
    /// Emits a message to the combat log if the target resists.
    /// </summary>
    /// <param name="context">The effect context with caster and target</param>
    /// <param name="saveStat">Target's save stat (e.g., "end", "wil"). If null/empty, no save is attempted.</param>
    /// <param name="attackStat">Caster's attack stat. If null/empty, only saveModifier is used (for items with innate power).</param>
    /// <param name="saveModifier">Modifier to attack roll. Positive = harder to resist, negative = easier.</param>
    /// <returns>True if the target resisted the effect, false if the effect should apply</returns>
    public static bool TryResist(EffectContext context, string? saveStat, string? attackStat, int saveModifier)
    {
        // No save configured - effect applies automatically
        if (string.IsNullOrEmpty(saveStat))
            return false;

        // Get attack modifier: caster stat + modifier, or just modifier for items with innate power
        int attackMod = string.IsNullOrEmpty(attackStat)
            ? saveModifier
            : context.GetCasterStat(attackStat) + saveModifier;

        int saveMod = context.GetTargetStat(saveStat);

        // Opposed 2d6 rolls
        int attackRoll = DiceRoller.Roll(2, 6, attackMod);
        int saveRoll = DiceRoller.Roll(2, 6, saveMod);

        // Defender wins ties
        bool resisted = saveRoll >= attackRoll;

        if (resisted)
        {
            string message = $"The {context.Target.DisplayName} resists!";
            context.ActionContext.CombatSystem.EmitActionMessage(
                context.Target,
                message,
                Palette.ToHex(Palette.Default)
            );
        }

        return resisted;
    }

    /// <summary>
    /// Attempts a saving throw without emitting any messages.
    /// Used by composable effects where MessageCollector handles messaging.
    /// </summary>
    /// <param name="context">The effect context with caster and target</param>
    /// <param name="saveStat">Target's save stat (e.g., "end", "wil"). If null/empty, no save is attempted.</param>
    /// <param name="attackStat">Caster's attack stat. If null/empty, only saveModifier is used.</param>
    /// <param name="saveModifier">Modifier to attack roll. Positive = harder to resist.</param>
    /// <returns>True if the target resisted the effect, false if the effect should apply</returns>
    public static bool TryResistSilent(EffectContext context, string? saveStat, string? attackStat, int saveModifier)
    {
        // No save configured - effect applies automatically
        if (string.IsNullOrEmpty(saveStat))
            return false;

        // Get attack modifier: caster stat + modifier, or just modifier for items with innate power
        int attackMod = string.IsNullOrEmpty(attackStat)
            ? saveModifier
            : context.GetCasterStat(attackStat) + saveModifier;

        int saveMod = context.GetTargetStat(saveStat);

        // Opposed 2d6 rolls
        int attackRoll = DiceRoller.Roll(2, 6, attackMod);
        int saveRoll = DiceRoller.Roll(2, 6, saveMod);

        // Defender wins ties
        return saveRoll >= attackRoll;
    }

    /// <summary>
    /// Attempts a saving throw using averaged attack stats (for hybrid skills).
    /// Used by composable effects where MessageCollector handles messaging.
    /// </summary>
    /// <param name="context">The effect context with caster and target</param>
    /// <param name="saveStat">Target's save stat (e.g., "end", "wil"). If null/empty, no save is attempted.</param>
    /// <param name="attackStats">Multiple caster stats to average. If null/empty, only saveModifier is used.</param>
    /// <param name="saveModifier">Modifier to attack roll. Positive = harder to resist.</param>
    /// <returns>True if the target resisted the effect, false if the effect should apply</returns>
    public static bool TryResistSilent(EffectContext context, string? saveStat, List<string>? attackStats, int saveModifier)
    {
        // No save configured - effect applies automatically
        if (string.IsNullOrEmpty(saveStat))
            return false;

        // Calculate attack modifier from averaged stats
        int attackMod;
        if (attackStats == null || attackStats.Count == 0)
        {
            attackMod = saveModifier;
        }
        else
        {
            // Average the stats (rounded down)
            int totalStats = attackStats.Sum(stat => context.GetCasterStat(stat));
            attackMod = (totalStats / attackStats.Count) + saveModifier;
        }

        int saveMod = context.GetTargetStat(saveStat);

        // Opposed 2d6 rolls
        int attackRoll = DiceRoller.Roll(2, 6, attackMod);
        int saveRoll = DiceRoller.Roll(2, 6, saveMod);

        // Defender wins ties
        return saveRoll >= attackRoll;
    }
}
