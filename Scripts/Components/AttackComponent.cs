using Godot;
using Godot.Collections;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;

namespace PitsOfDespair.Components;

/// <summary>
/// Component managing entity attacks.
/// Implements IAIEventHandler to respond to OnGetMeleeActions events.
/// </summary>
public partial class AttackComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Emitted when an attack is requested (target, attackIndex)
    /// </summary>
    [Signal]
    public delegate void AttackRequestedEventHandler(BaseEntity target, int attackIndex);

    /// <summary>
    /// Natural attacks for this entity (baseline attacks - used when no weapon equipped).
    /// </summary>
    [Export] public Array<AttackData> NaturalAttacks { get; set; } = new();

    /// <summary>
    /// Current effective attacks for this entity (weapon attacks if equipped, otherwise natural attacks).
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

    /// <summary>
    /// Handle AI events - responds to OnGetMeleeActions and OnGetRangedActions.
    /// </summary>
    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (_entity == null || args.Target == null)
        {
            return;
        }

        if (eventName == AIEvents.OnGetMeleeActions)
        {
            HandleMeleeActions(args);
        }
        else if (eventName == AIEvents.OnGetRangedActions)
        {
            HandleRangedActions(args);
        }
    }

    /// <summary>
    /// Handle melee action requests - adds melee attacks when in range.
    /// </summary>
    private void HandleMeleeActions(GetActionsEventArgs args)
    {
        // Check if we're in melee range
        int distance = DistanceHelper.ChebyshevDistance(
            _entity!.GridPosition,
            args.Target!.GridPosition);

        if (distance > 1)
        {
            return;
        }

        // Add each available melee attack to the action list
        for (int i = 0; i < Attacks.Count; i++)
        {
            var attack = Attacks[i];
            if (attack == null || attack.Type != AttackType.Melee)
            {
                continue;
            }

            int attackIndex = i; // Capture for closure
            args.ActionList.Add(
                weight: 1,
                execute: ctx => ExecuteMeleeAttack(ctx, args.Target, attackIndex),
                debugName: attack.Name ?? $"Melee {attackIndex}"
            );
        }
    }

    /// <summary>
    /// Handle ranged action requests - adds ranged attacks when target is in range and visible.
    /// </summary>
    private void HandleRangedActions(GetActionsEventArgs args)
    {
        var target = args.Target!;

        for (int i = 0; i < Attacks.Count; i++)
        {
            var attack = Attacks[i];
            if (attack == null || attack.Type != AttackType.Ranged)
            {
                continue;
            }

            // Check range using Euclidean distance (matches FOV and RangedAttackAction)
            int distanceSquared = DistanceHelper.EuclideanDistance(_entity!.GridPosition, target.GridPosition);
            int rangeSquared = attack.Range * attack.Range;
            if (distanceSquared > rangeSquared || distanceSquared == 0)
            {
                continue;
            }

            // Check line of sight
            var visibleTiles = FOVCalculator.CalculateVisibleTiles(
                _entity.GridPosition,
                attack.Range,
                args.Context.ActionContext.MapSystem);

            if (!visibleTiles.Contains(target.GridPosition))
            {
                continue;
            }

            // Check ammunition if required
            if (!string.IsNullOrEmpty(attack.AmmoType))
            {
                if (!HasRequiredAmmo(attack.AmmoType))
                {
                    continue;
                }
            }

            int attackIndex = i; // Capture for closure
            args.ActionList.Add(
                weight: 1,
                execute: ctx => ExecuteRangedAttack(ctx, target, attackIndex),
                debugName: attack.Name ?? $"Ranged {attackIndex}"
            );
        }
    }

    /// <summary>
    /// Check if entity has required ammunition equipped.
    /// </summary>
    private bool HasRequiredAmmo(string ammoType)
    {
        var equipComponent = _entity!.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null)
        {
            return false;
        }

        var equippedAmmoKey = equipComponent.GetEquippedKey(EquipmentSlot.Ammo);
        if (!equippedAmmoKey.HasValue)
        {
            return false;
        }

        var inventory = _entity.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
        {
            return false;
        }

        var ammoSlot = inventory.GetSlot(equippedAmmoKey.Value);
        if (ammoSlot == null)
        {
            return false;
        }

        return ammoSlot.Item.Template.Type?.ToLower() == "ammo" &&
               ammoSlot.Item.Template.Name.Contains(ammoType, System.StringComparison.OrdinalIgnoreCase) &&
               ammoSlot.Item.Quantity > 0;
    }

    /// <summary>
    /// Execute a melee attack against the target.
    /// </summary>
    private void ExecuteMeleeAttack(AIContext context, BaseEntity target, int attackIndex)
    {
        RequestAttack(target, attackIndex);
    }

    /// <summary>
    /// Execute a ranged attack against the target using RangedAttackAction.
    /// </summary>
    private void ExecuteRangedAttack(AIContext context, BaseEntity target, int attackIndex)
    {
        var action = new RangedAttackAction(target.GridPosition, attackIndex);
        _entity!.ExecuteAction(action, context.ActionContext);
    }
}
