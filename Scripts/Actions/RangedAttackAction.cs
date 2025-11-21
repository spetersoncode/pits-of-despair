using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Linq;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for performing a ranged attack at a target position.
/// Validates line of sight, spawns a projectile, and applies damage on impact.
/// </summary>
public class RangedAttackAction : Action
{
    private readonly GridPosition _targetPosition;
    private readonly int _attackIndex;

    public override string Name => "Ranged Attack";

    public RangedAttackAction(GridPosition targetPosition, int attackIndex = 0)
    {
        _targetPosition = targetPosition;
        _attackIndex = attackIndex;
    }

    public override bool CanExecute(BaseEntity actor, ActionContext context)
    {
        if (actor == null || context == null)
        {
            return false;
        }

        // Validate attacker has attack component
        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
        {
            return false;
        }

        // Validate attack index and that it's a ranged attack
        var attackData = attackComponent.GetAttack(_attackIndex);
        if (attackData == null || attackData.Type != AttackType.Ranged)
        {
            return false;
        }

        // Validate range (use Euclidean distance for circular range, matching FOV)
        int distanceSquared = DistanceHelper.EuclideanDistance(actor.GridPosition, _targetPosition);
        int rangeSquared = attackData.Range * attackData.Range;
        if (distanceSquared > rangeSquared || distanceSquared == 0)
        {
            return false;
        }

        // Validate line of sight using FOV calculator
        var visibleTiles = FOVCalculator.CalculateVisibleTiles(
            actor.GridPosition,
            attackData.Range,
            context.MapSystem);

        if (!visibleTiles.Contains(_targetPosition))
        {
            return false;
        }

        // Check ammunition requirement (player only)
        if (actor is Player && !string.IsNullOrEmpty(attackData.AmmoType))
        {
            var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
            if (inventory == null || !HasRequiredAmmo(inventory, attackData.AmmoType))
            {
                return false;
            }
        }

        return true;
    }

    public override ActionResult Execute(BaseEntity actor, ActionContext context)
    {
        if (!CanExecute(actor, context))
        {
            return ActionResult.CreateFailure("Cannot attack that target.");
        }

        var attackComponent = actor.GetNodeOrNull<AttackComponent>("AttackComponent");
        var attackData = attackComponent!.GetAttack(_attackIndex);

        // Consume ammunition if required (player only)
        if (actor is Player && !string.IsNullOrEmpty(attackData.AmmoType))
        {
            var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");
            if (inventory != null && !ConsumeAmmo(inventory, attackData.AmmoType))
            {
                return ActionResult.CreateFailure($"Out of {attackData.AmmoType}s!");
            }
        }

        // Find target entity at position (may be null if shooting at empty tile)
        BaseEntity target = null;
        var entityAtTarget = context.EntityManager.GetEntityAtPosition(_targetPosition);
        if (entityAtTarget != null)
        {
            // Check if entity has health component
            if (entityAtTarget.GetNodeOrNull<HealthComponent>("HealthComponent") != null)
            {
                target = entityAtTarget;
            }
        }

        // Spawn projectile for all entities (player and NPCs) - it will handle the combat on impact
        if (actor is Player player)
        {
            // Player uses signal-based approach for consistency with existing system
            var projectileTarget = target != null ? target.GridPosition : _targetPosition;
            player.EmitSignal(Player.SignalName.RangedAttackRequested,
                actor.GridPosition.ToVector2I(), projectileTarget.ToVector2I(), target, _attackIndex);
        }
        else
        {
            // NPCs use direct projectile spawning
            var projectileTarget = target != null ? target.GridPosition : _targetPosition;
            context.ProjectileSystem.SpawnProjectile(actor.GridPosition, projectileTarget, target, actor, _attackIndex);
        }

        return ActionResult.CreateSuccess();
    }

    /// <summary>
    /// Checks if the inventory contains the required ammunition type.
    /// </summary>
    private bool HasRequiredAmmo(InventoryComponent inventory, string ammoType)
    {
        return inventory.Inventory.Any(slot =>
            slot.Item.Template.Type?.ToLower() == "ammo" &&
            slot.Item.Template.Name.Contains(ammoType, System.StringComparison.OrdinalIgnoreCase) &&
            slot.Item.Quantity > 0);
    }

    /// <summary>
    /// Consumes one unit of the required ammunition from inventory.
    /// </summary>
    /// <returns>True if ammo was consumed successfully, false if no ammo available.</returns>
    private bool ConsumeAmmo(InventoryComponent inventory, string ammoType)
    {
        var ammoSlot = inventory.Inventory.FirstOrDefault(slot =>
            slot.Item.Template.Type?.ToLower() == "ammo" &&
            slot.Item.Template.Name.Contains(ammoType, System.StringComparison.OrdinalIgnoreCase) &&
            slot.Item.Quantity > 0);

        if (ammoSlot == null)
        {
            return false;
        }

        // Remove one ammo from the stack
        inventory.RemoveItem(ammoSlot.Key, 1);
        return true;
    }
}
