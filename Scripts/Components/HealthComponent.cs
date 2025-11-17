using Godot;
using PitsOfDespair.Entities;

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

    private BaseEntity? _entity;
    private StatsComponent? _stats;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        _stats = _entity?.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate initial MaxHP
        RecalculateMaxHP();
        CurrentHP = MaxHP;

        // Subscribe to stat changes if StatsComponent exists
        if (_stats != null)
        {
            _stats.StatsChanged += OnStatsChanged;
        }
    }

    /// <summary>
    /// Recalculates MaxHP based on BaseMaxHP and Endurance bonus.
    /// </summary>
    private void RecalculateMaxHP()
    {
        int enduranceBonus = _stats?.GetHPBonus() ?? 0;
        int newMaxHP = BaseMaxHP + enduranceBonus;

        // If MaxHP increases, add the difference to CurrentHP
        // If MaxHP decreases, reduce CurrentHP if it exceeds new max
        int hpDifference = newMaxHP - MaxHP;
        MaxHP = newMaxHP;

        if (hpDifference > 0)
        {
            // MaxHP increased - gain the additional HP
            CurrentHP += hpDifference;
        }
        else if (CurrentHP > MaxHP)
        {
            // MaxHP decreased - cap CurrentHP to new max
            CurrentHP = MaxHP;
        }

        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
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
    /// Apply damage to this entity
    /// </summary>
    /// <param name="amount">Amount of damage to take</param>
    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        int oldHP = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);

        EmitSignal(SignalName.DamageTaken, amount);
        EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);

        if (CurrentHP == 0 && oldHP > 0)
        {
            EmitSignal(SignalName.Died);
        }
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
}
