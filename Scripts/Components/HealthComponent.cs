using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;
using System.Collections.Generic;

namespace PitsOfDespair.Components;

/// <summary>
/// Component managing entity hit points and death.
/// Implements IAIEventHandler to respond to OnGetDefensiveActions for healing.
/// </summary>
public partial class HealthComponent : Node, IAIEventHandler
{
    /// <summary>
    /// HP percentage threshold below which AI will consider healing.
    /// Default is 30% (0.3f).
    /// </summary>
    [Export] public float HealingThreshold { get; set; } = 0.3f;

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
    /// Base maximum health (before Endurance modifiers).
    /// Actual MaxHealth is: BaseMaxHealth + (Endurance × 5)
    /// </summary>
    [Export] public int BaseMaxHealth { get; set; } = 10;

    /// <summary>
    /// Maximum health including Endurance bonus.
    /// Calculated as: BaseMaxHealth + (Endurance × 5)
    /// </summary>
    public int MaxHealth { get; private set; }

    /// <summary>
    /// Current health
    /// </summary>
    public int CurrentHealth { get; private set; }

    /// <summary>
    /// The last entity that dealt damage to this entity.
    /// Used for tracking kill attribution (e.g., XP awards).
    /// </summary>
    public BaseEntity? LastDamageSource { get; private set; }

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
    private TurnManager? _turnManager;
    private bool _turnSignalConnected = false;

    /// <summary>
    /// Accumulated regeneration points. At 100 points, heal 1 HP.
    /// Uses DCSS-style regeneration system.
    /// </summary>
    private int _regenPoints = 0;

    /// <summary>
    /// Multi-source max health modifiers from skills, items, buffs, etc.
    /// Key is source identifier (e.g., "skill_tough"), value is Health bonus.
    /// </summary>
    private readonly Dictionary<string, int> _maxHealthModifiers = new();

    /// <summary>
    /// Multi-source regen rate modifiers from equipment, conditions, etc.
    /// Key is source identifier (e.g., "equipped_ring1"), value is regen bonus.
    /// DCSS formula: Ring of Regeneration adds +80 per instance.
    /// </summary>
    private readonly Dictionary<string, int> _regenModifiers = new();

    /// <summary>
    /// Total regeneration bonus from all modifier sources.
    /// </summary>
    public int TotalRegenBonus
    {
        get
        {
            int total = 0;
            foreach (var value in _regenModifiers.Values)
            {
                total += value;
            }
            return total;
        }
    }

    /// <summary>
    /// Base regeneration rate per turn (including modifiers).
    /// Formula: 20 + MaxHealth / 6 + TotalRegenBonus
    /// At 100 points accumulated, heal 1 HP.
    /// </summary>
    public int BaseRegenRate => 20 + MaxHealth / 6 + TotalRegenBonus;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        _stats = _entity?.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate initial MaxHealth
        RecalculateMaxHealth();
        CurrentHealth = MaxHealth;

        // Connect to stat changes if StatsComponent exists
        if (_stats != null)
        {
            _stats.Connect(StatsComponent.SignalName.StatsChanged, Callable.From(OnStatsChanged));
        }

        // Connect to turn signals for passive regeneration
        ConnectToTurnManager();
    }

    /// <summary>
    /// Connects to TurnManager for passive regeneration.
    /// Player entities connect to PlayerTurnStarted, creatures to CreatureTurnsStarted.
    /// </summary>
    private void ConnectToTurnManager()
    {
        if (_turnSignalConnected)
            return;

        // Find TurnManager by traversing up to GameLevel
        Node current = this;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                _turnManager = current.GetNodeOrNull<TurnManager>("TurnManager");
                break;
            }
            current = current.GetParent();
        }

        if (_turnManager == null)
        {
            // TurnManager not found - entity may be outside game context (e.g., editor preview)
            return;
        }

        // Connect to appropriate turn signal based on entity type
        bool isPlayer = _entity is Player;
        if (isPlayer)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }
        else
        {
            _turnManager.Connect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnTurnStarted));
        }

        _turnSignalConnected = true;
    }

    public override void _ExitTree()
    {
        // Clean up turn signal connections
        if (_turnManager != null && _turnSignalConnected)
        {
            bool isPlayer = _entity is Player;
            if (isPlayer)
            {
                _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
            }
            else
            {
                _turnManager.Disconnect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnTurnStarted));
            }
        }
    }

    /// <summary>
    /// Called at the start of each turn. Processes passive regeneration.
    /// </summary>
    private void OnTurnStarted()
    {
        ProcessRegeneration();
    }

    /// <summary>
    /// Processes passive regeneration using DCSS-style accumulating points.
    /// Each turn adds BaseRegenRate to accumulated points.
    /// At 100 points, heals 1 HP (excess carries over).
    /// </summary>
    private void ProcessRegeneration()
    {
        // Don't regenerate if dead or at full health
        if (!IsAlive() || CurrentHealth >= MaxHealth)
        {
            // Reset regen points when at full health to avoid stockpiling
            _regenPoints = 0;
            return;
        }

        // Accumulate regeneration points
        _regenPoints += BaseRegenRate;

        // Heal 1 HP for every 100 points accumulated
        while (_regenPoints >= 100 && CurrentHealth < MaxHealth)
        {
            Heal(1);
            _regenPoints -= 100;
        }
    }

    /// <summary>
    /// Recalculates MaxHealth based on BaseMaxHealth, Endurance bonus, and external modifiers.
    /// MaxHealth is floored at BaseMaxHealth (negative END can't reduce HP below base).
    /// </summary>
    private void RecalculateMaxHealth()
    {
        int enduranceBonus = _stats?.GetHealthBonus() ?? 0;
        int modifierBonus = GetTotalMaxHealthModifiers();
        int newMaxHealth = BaseMaxHealth + enduranceBonus + modifierBonus;

        // Enforce floor: MaxHealth can never go below BaseMaxHealth
        // This handles negative Endurance (debuffs, weak creatures)
        if (newMaxHealth < BaseMaxHealth)
            newMaxHealth = BaseMaxHealth;

        // If MaxHealth increases, add the difference to CurrentHealth
        // If MaxHealth decreases, reduce CurrentHealth if it exceeds new max
        int hpDifference = newMaxHealth - MaxHealth;
        MaxHealth = newMaxHealth;

        if (hpDifference > 0)
        {
            // MaxHealth increased - gain the additional HP (healing mechanic for END buffs)
            CurrentHealth += hpDifference;
        }
        else if (CurrentHealth > MaxHealth)
        {
            // MaxHealth decreased - cap CurrentHealth to new max
            CurrentHealth = MaxHealth;
        }

        CallDeferred(MethodName.EmitHealthChangedSignal, CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Called when stats change (e.g., equipment changes, buffs/debuffs).
    /// Recalculates MaxHealth based on new Endurance value.
    /// </summary>
    private void OnStatsChanged()
    {
        RecalculateMaxHealth();
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
    /// <param name="source">The entity that dealt the damage (for kill attribution)</param>
    /// <param name="applyArmor">Whether to apply armor reduction (default false for melee compatibility)</param>
    /// <param name="armorPiercing">Amount of armor to ignore (only used when applyArmor is true)</param>
    /// <returns>The actual amount of damage dealt after modifiers</returns>
    public int TakeDamage(int amount, DamageType damageType = DamageType.Bludgeoning,
                         BaseEntity? source = null, bool applyArmor = false, int armorPiercing = 0)
    {
        if (amount <= 0)
            return 0;

        // Track damage source for kill attribution
        if (source != null)
        {
            LastDamageSource = source;
        }

        // Check immunity - immune entities take 0 damage
        if (Immunities.Contains(damageType))
        {
            EmitSignal(SignalName.DamageModifierApplied, (int)damageType, "immune");
            return 0;
        }

        // Apply armor reduction if requested (for spell effects)
        // Note: Melee attacks handle armor in CombatSystem, so applyArmor defaults to false
        if (applyArmor)
        {
            var stats = _entity?.GetNodeOrNull<StatsComponent>("StatsComponent");
            if (stats != null)
            {
                int effectiveArmor = Mathf.Max(0, stats.TotalArmor - armorPiercing);
                amount = Mathf.Max(0, amount - effectiveArmor);
                if (amount <= 0)
                    return 0;
            }
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
        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        EmitSignal(SignalName.DamageTaken, amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

        if (CurrentHealth == 0 && oldHealth > 0)
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
        if (amount <= 0 || CurrentHealth == MaxHealth)
            return;

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
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
        return CurrentHealth > 0;
    }

    #region Max Health Modifiers

    /// <summary>
    /// Adds a max Health modifier from a named source.
    /// Used by passive skills, items, buffs, etc.
    /// </summary>
    /// <param name="source">Source identifier (e.g., "skill_tough", "item_amulet")</param>
    /// <param name="value">Health bonus value (can be positive or negative)</param>
    public void AddMaxHealthModifier(string source, int value)
    {
        _maxHealthModifiers[source] = value;
        RecalculateMaxHealth();
    }

    /// <summary>
    /// Removes a max Health modifier by source name.
    /// </summary>
    /// <param name="source">Source identifier to remove</param>
    public void RemoveMaxHealthModifier(string source)
    {
        if (_maxHealthModifiers.Remove(source))
        {
            RecalculateMaxHealth();
        }
    }

    /// <summary>
    /// Gets the total max Health bonus from all modifier sources.
    /// </summary>
    private int GetTotalMaxHealthModifiers()
    {
        int total = 0;
        foreach (var value in _maxHealthModifiers.Values)
        {
            total += value;
        }
        return total;
    }

    #endregion

    #region Regen Modifiers

    /// <summary>
    /// Adds a regen rate modifier from a named source.
    /// Used by equipment (Ring of Regeneration), conditions, etc.
    /// DCSS formula: +80 per instance adds roughly 1 HP per 1.25 turns.
    /// </summary>
    /// <param name="source">Source identifier (e.g., "equipped_ring1", "condition_regen")</param>
    /// <param name="value">Regen bonus value (typically 80 for Ring of Regeneration)</param>
    public void AddRegenModifier(string source, int value)
    {
        _regenModifiers[source] = value;
    }

    /// <summary>
    /// Removes a regen rate modifier by source name.
    /// </summary>
    /// <param name="source">Source identifier to remove</param>
    public void RemoveRegenModifier(string source)
    {
        _regenModifiers.Remove(source);
    }

    #endregion

    /// <summary>
    /// Helper method to emit HealthChanged signal (called deferred to avoid re-entrancy issues).
    /// </summary>
    private void EmitHealthChangedSignal(int current, int max)
    {
        EmitSignal(SignalName.HealthChanged, current, max);
    }

    /// <summary>
    /// Handle AI events - responds to OnGetDefensiveActions when health is low.
    /// Note: This is a placeholder for creatures that can self-heal.
    /// Actual healing items would be handled by inventory/item components.
    /// </summary>
    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnGetDefensiveActions)
        {
            return;
        }

        // Check if health is below threshold
        float hpRatio = (float)CurrentHealth / MaxHealth;
        if (hpRatio >= HealingThreshold)
        {
            return;
        }

        // Note: HealthComponent itself doesn't provide healing actions.
        // This is here as a hook for future natural regeneration abilities.
        // Item-based healing would be handled by inventory components.
        // Creature natural healing abilities would be separate components.
    }
}
