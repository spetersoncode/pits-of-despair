using Godot;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using System.Collections.Generic;

namespace PitsOfDespair.Components;

/// <summary>
/// Component managing entity hit points and death
/// </summary>
public partial class HealthComponent : Node
{
    /// <summary>
    /// Emitted when health changes (current, max)
    /// </summary>
    [Signal]
    public delegate void HealthChangedEventHandler(int current, int max);

    /// <summary>
    /// Emitted when entity dies (HP reaches 0)
    /// </summary>
    [Signal]
    public delegate void DiedEventHandler();

    /// <summary>
    /// Emitted when damage is taken (amount)
    /// </summary>
    [Signal]
    public delegate void DamageTakenEventHandler(int amount);

    /// <summary>
    /// Emitted when a damage modifier is applied (damageType, modifierType).
    /// modifierType is "immune", "resisted", or "vulnerable"
    /// </summary>
    [Signal]
    public delegate void DamageModifierAppliedEventHandler(int damageType, string modifierType);

    /// <summary>
    /// Base maximum hit points (before Endurance modifiers).
    /// Actual MaxHP is: BaseMaxHP + (Endurance × 2)
    /// </summary>
    [Export] public int BaseMaxHP { get; set; } = 10;

    /// <summary>
    /// Maximum hit points including Endurance bonus.
    /// Calculated as: BaseMaxHP + (Endurance × 2)
    /// </summary>
    public int MaxHP { get; private set; }

    /// <summary>
    /// Current hit points
    /// </summary>
    public int CurrentHP { get; private set; }

    /// <summary>
    /// Damage types this entity is immune to (takes 0 damage).
    /// </summary>
    public List<DamageType> Immunities { get; set; } = new();

    /// <summary>
    /// Damage types this entity resists (takes half damage, rounded down).
    /// </summary>
    public List<DamageType> Resistances { get; set; } = new();

    /// <summary>
    /// Damage types this entity is vulnerable to (takes double damage).
    /// </summary>
    public List<DamageType> Vulnerabilities { get; set; } = new();

    private BaseEntity? _entity;
    private StatsComponent? _stats;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        _stats = _entity?.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate initial MaxHP
        RecalculateMaxHP();
        CurrentHP = MaxHP;

        // Connect to stat changes if StatsComponent exists
        if (_stats != null)
        {
            _stats.Connect(StatsComponent.SignalName.StatsChanged, Callable.From(OnStatsChanged));
        }
    }

    /// <summary>
    /// Recalculates MaxHP based on BaseMaxHP and Endurance bonus.
    /// MaxHP is floored at BaseMaxHP (negative END can't reduce HP below base).
    /// </summary>
    private void RecalculateMaxHP()
    {
        int enduranceBonus = _stats?.GetHPBonus() ?? 0;
        int newMaxHP = BaseMaxHP + enduranceBonus;

        // Enforce floor: MaxHP can never go below BaseMaxHP
        // This handles negative Endurance (debuffs, weak creatures)
        if (newMaxHP < BaseMaxHP)
            newMaxHP = BaseMaxHP;

        // If MaxHP increases, add the difference to CurrentHP
        // If MaxHP decreases, reduce CurrentHP if it exceeds new max
        int hpDifference = newMaxHP - MaxHP;
        MaxHP = newMaxHP;

        if (hpDifference > 0)
        {
            // MaxHP increased - gain the additional HP (healing mechanic for END buffs)
            CurrentHP += hpDifference;
        }
        else if (CurrentHP > MaxHP)
        {
            // MaxHP decreased - cap CurrentHP to new max
            CurrentHP = MaxHP;
        }

        CallDeferred(MethodName.EmitHealthChangedSignal, CurrentHP, MaxHP);
    }

    /// <summary>
    /// Called when stats change (e.g., equipment changes, buffs/debuffs).
    /// Recalculates MaxHP based on new Endurance value.
    /// </summary>
    private void OnStatsChanged()
    {
        RecalculateMaxHP();
    }

    /// <summary>
    /// Calculates what the actual damage would be after applying modifiers, without actually applying it.
    /// Used to preview damage for UI/logging purposes before applying the damage.
    /// </summary>
    /// <param name="amount">Base amount of damage</param>
    /// <param name="damageType">Type of damage being dealt</param>
    /// <returns>The actual amount of damage after modifiers (0 if immune)</returns>
    public int CalculateDamage(int amount, DamageType damageType = DamageType.Bludgeoning)
    {
        if (amount <= 0)
            return 0;

        // Check immunity - immune entities take 0 damage
        if (Immunities.Contains(damageType))
            return 0;

        // Apply vulnerability - double damage
        if (Vulnerabilities.Contains(damageType))
            return amount * 2;

        // Apply resistance - half damage (rounded down)
        if (Resistances.Contains(damageType))
            return amount / 2;

        return amount;
    }

    /// <summary>
    /// Apply damage to this entity with damage type modifiers.
    /// </summary>
    /// <param name="amount">Base amount of damage to take</param>
    /// <param name="damageType">Type of damage being dealt</param>
    /// <returns>The actual amount of damage dealt after modifiers</returns>
    public int TakeDamage(int amount, DamageType damageType = DamageType.Bludgeoning)
    {
        if (amount <= 0)
            return 0;

        // Check immunity - immune entities take 0 damage
        if (Immunities.Contains(damageType))
        {
            EmitSignal(SignalName.DamageModifierApplied, (int)damageType, "immune");
            return 0;
        }

        // Apply vulnerability - double damage
        if (Vulnerabilities.Contains(damageType))
        {
            EmitSignal(SignalName.DamageModifierApplied, (int)damageType, "vulnerable");
            amount *= 2;
        }
        // Apply resistance - half damage (rounded down)
        else if (Resistances.Contains(damageType))
        {
            EmitSignal(SignalName.DamageModifierApplied, (int)damageType, "resisted");
            amount /= 2;
        }

        // Apply damage after modifiers
        int oldHP = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);

        EmitSignal(SignalName.DamageTaken, amount);
        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);

        if (CurrentHP == 0 && oldHP > 0)
        {
            EmitSignal(SignalName.Died);
        }

        return amount;
    }

    /// <summary>
    /// Heal this entity
    /// </summary>
    /// <param name="amount">Amount of HP to restore</param>
    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHP == MaxHP)
            return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
    }

    /// <summary>
    /// Get the parent entity
    /// </summary>
    public BaseEntity? GetEntity()
    {
        return _entity;
    }

    /// <summary>
    /// Check if entity is alive
    /// </summary>
    public bool IsAlive()
    {
        return CurrentHP > 0;
    }

    /// <summary>
    /// Helper method to emit HealthChanged signal (called deferred to avoid re-entrancy issues).
    /// </summary>
    private void EmitHealthChangedSignal(int current, int max)
    {
        EmitSignal(SignalName.HealthChanged, current, max);
    }
}
