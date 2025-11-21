using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
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

        // Check ammunition requirement (all entities that require ammo)
        if (!string.IsNullOrEmpty(attackData.AmmoType))
        {
            var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
            if (equipComponent == null || !HasRequiredAmmo(equipComponent, attackData.AmmoType))
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

        // Consume ammunition if required (all entities that require ammo)
        if (!string.IsNullOrEmpty(attackData.AmmoType))
        {
            var equipComponent = actor.GetNodeOrNull<EquipComponent>("EquipComponent");
            var inventory = actor.GetNodeOrNull<InventoryComponent>("InventoryComponent");

            if (equipComponent == null || inventory == null || !ConsumeAmmo(equipComponent, inventory, attackData.AmmoType))
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
    /// Checks if the equipped ammunition matches the required type and has quantity available.
    /// </summary>
    private bool HasRequiredAmmo(EquipComponent equipComponent, string ammoType)
    {
        var equippedAmmoKey = equipComponent.GetEquippedKey(EquipmentSlot.Ammo);
        if (!equippedAmmoKey.HasValue)
        {
            return false;
        }

        var inventory = equipComponent.GetParent<BaseEntity>()?.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
        {
            return false;
        }

        var ammoSlot = inventory.GetSlot(equippedAmmoKey.Value);
        if (ammoSlot == null)
        {
            return false;
        }

        // Validate ammo type matches (substring matching)
        return ammoSlot.Item.Template.Type?.ToLower() == "ammo" &&
               ammoSlot.Item.Template.Name.Contains(ammoType, System.StringComparison.OrdinalIgnoreCase) &&
               ammoSlot.Item.Quantity > 0;
    }

    /// <summary>
    /// Consumes one unit of equipped ammunition.
    /// If ammo depleted, attempts to auto-equip next matching ammo from inventory.
    /// </summary>
    /// <returns>True if ammo was consumed successfully, false if no ammo available.</returns>
    private bool ConsumeAmmo(EquipComponent equipComponent, InventoryComponent inventory, string ammoType)
    {
        var equippedAmmoKey = equipComponent.GetEquippedKey(EquipmentSlot.Ammo);
        if (!equippedAmmoKey.HasValue)
        {
            return false;
        }

        var ammoSlot = inventory.GetSlot(equippedAmmoKey.Value);
        if (ammoSlot == null || ammoSlot.Item.Quantity <= 0)
        {
            return false;
        }

        // Validate ammo type matches
        if (ammoSlot.Item.Template.Type?.ToLower() != "ammo" ||
            !ammoSlot.Item.Template.Name.Contains(ammoType, System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Remove one ammo from the stack
        inventory.RemoveItem(equippedAmmoKey.Value, 1);

        // If ammo depleted, attempt to auto-equip next matching ammo
        var updatedAmmo = inventory.GetSlot(equippedAmmoKey.Value);
        if (updatedAmmo == null || updatedAmmo.Item.Quantity <= 0)
        {
            TryAutoEquipNextAmmo(equipComponent, inventory, ammoType);
        }

        return true;
    }

    /// <summary>
    /// Attempts to auto-equip the next available matching ammo from inventory.
    /// If no matching ammo found, unequips the ammo slot.
    /// </summary>
    private void TryAutoEquipNextAmmo(EquipComponent equipComponent, InventoryComponent inventory, string ammoType)
    {
        // Search inventory for next matching ammo
        var nextAmmoSlot = inventory.Inventory.FirstOrDefault(slot =>
            slot.Item.Template.Type?.ToLower() == "ammo" &&
            slot.Item.Template.Name.Contains(ammoType, System.StringComparison.OrdinalIgnoreCase) &&
            slot.Item.Quantity > 0);

        if (nextAmmoSlot != null)
        {
            // Auto-equip next matching ammo
            equipComponent.Equip(nextAmmoSlot.Key, EquipmentSlot.Ammo);
        }
        else
        {
            // No matching ammo found, unequip empty slot
            equipComponent.Unequip(EquipmentSlot.Ammo);
        }
    }
}
