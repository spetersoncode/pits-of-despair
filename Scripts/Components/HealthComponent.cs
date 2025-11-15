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
    /// Maximum hit points
    /// </summary>
    [Export] public int MaxHP { get; set; } = 10;

    /// <summary>
    /// Current hit points
    /// </summary>
    public int CurrentHP { get; private set; }

    private BaseEntity? _entity;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        CurrentHP = MaxHP;
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
