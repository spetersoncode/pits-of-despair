using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// Accumulates messages during effect execution for batch emission.
/// Provides typed methods for common message patterns.
/// </summary>
public class MessageCollector
{
    private readonly List<(string Message, string Color)> _messages = new();

    /// <summary>
    /// Adds a generic message with optional color.
    /// </summary>
    public void Add(string message, string? color = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _messages.Add((message, color ?? Palette.ToHex(Palette.Default)));
    }

    /// <summary>
    /// Adds a damage message for the target.
    /// </summary>
    public void AddDamage(BaseEntity target, int damage, DamageType damageType)
    {
        if (damage <= 0)
            return;

        string color = GetDamageColor(damageType);
        _messages.Add(($"{target.DisplayName} takes {damage} damage!", color));
    }

    /// <summary>
    /// Adds a damage message with custom format.
    /// </summary>
    public void AddDamage(string message, DamageType damageType)
    {
        if (string.IsNullOrEmpty(message))
            return;

        string color = GetDamageColor(damageType);
        _messages.Add((message, color));
    }

    /// <summary>
    /// Adds a healing message for the target.
    /// </summary>
    public void AddHeal(BaseEntity target, int amount)
    {
        if (amount <= 0)
            return;

        _messages.Add((
            $"{target.DisplayName} heals {amount} Health.",
            Palette.ToHex(Palette.Success)
        ));
    }

    /// <summary>
    /// Adds a save resistance message.
    /// </summary>
    public void AddSaveResist(BaseEntity target)
    {
        _messages.Add((
            $"The {target.DisplayName} resists!",
            Palette.ToHex(Palette.Default)
        ));
    }

    /// <summary>
    /// Adds a miss message for attack rolls.
    /// </summary>
    public void AddMiss(BaseEntity target, string? attackName = null)
    {
        string name = string.IsNullOrEmpty(attackName) ? "The attack" : attackName;
        _messages.Add((
            $"{name} misses the {target.DisplayName}!",
            Palette.ToHex(Palette.Disabled)
        ));
    }

    /// <summary>
    /// Adds a condition applied message.
    /// </summary>
    public void AddConditionApplied(string message, string? color = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _messages.Add((message, color ?? Palette.ToHex(Palette.StatusDebuff)));
    }

    /// <summary>
    /// Emits all accumulated messages via the combat system.
    /// </summary>
    public void Emit(BaseEntity target, EffectContext context)
    {
        var combatSystem = context.ActionContext.CombatSystem;
        if (combatSystem == null)
            return;

        foreach (var (message, color) in _messages)
        {
            combatSystem.EmitActionMessage(target, message, color);
        }
    }

    /// <summary>
    /// Gets the count of accumulated messages.
    /// </summary>
    public int Count => _messages.Count;

    /// <summary>
    /// Checks if any messages have been accumulated.
    /// </summary>
    public bool HasMessages => _messages.Count > 0;

    /// <summary>
    /// Clears all accumulated messages.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }

    /// <summary>
    /// Gets the appropriate color for a damage type.
    /// </summary>
    private static string GetDamageColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Fire => Palette.ToHex(Palette.Fire),
            DamageType.Cold => Palette.ToHex(Palette.Ice),
            DamageType.Lightning => Palette.ToHex(Palette.Lightning),
            DamageType.Acid => Palette.ToHex(Palette.Acid),
            DamageType.Poison => Palette.ToHex(Palette.Poison),
            DamageType.Necrotic => Palette.ToHex(Palette.Crimson),
            _ => Palette.ToHex(Palette.CombatDamage)
        };
    }
}
