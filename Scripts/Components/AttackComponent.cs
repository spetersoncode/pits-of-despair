using Godot;
using Godot.Collections;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// Component managing entity attacks
/// </summary>
public partial class AttackComponent : Node
{
    /// <summary>
    /// Emitted when an attack is requested (target, attackIndex)
    /// </summary>
    [Signal]
    public delegate void AttackRequestedEventHandler(BaseEntity target, int attackIndex);

    /// <summary>
    /// Natural attacks for this entity (baseline attacks - used when no weapon equipped).
    /// Set from creature YAML or hardcoded for special entities like the player.
    /// </summary>
    [Export] public Array<AttackData> NaturalAttacks { get; set; } = new();

    /// <summary>
    /// Current effective attacks for this entity.
    /// This is either weapon attacks (if equipped) or natural attacks (if unarmed).
    /// </summary>
    [Export] public Array<AttackData> Attacks { get; set; } = new();

    private BaseEntity? _entity;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
    }

    /// <summary>
    /// Request an attack on a target entity
    /// </summary>
    /// <param name="target">Entity to attack</param>
    /// <param name="attackIndex">Index of attack to use (default 0)</param>
    public void RequestAttack(BaseEntity target, int attackIndex = 0)
    {
        if (attackIndex < 0 || attackIndex >= Attacks.Count)
        {
            GD.PushWarning($"Invalid attack index {attackIndex}");
            return;
        }

        EmitSignal(SignalName.AttackRequested, target, attackIndex);
    }

    /// <summary>
    /// Get the parent entity
    /// </summary>
    public BaseEntity? GetEntity()
    {
        return _entity;
    }

    /// <summary>
    /// Get attack data at specified index
    /// </summary>
    public AttackData? GetAttack(int index)
    {
        if (index < 0 || index >= Attacks.Count)
            return null;

        return Attacks[index];
    }
}
