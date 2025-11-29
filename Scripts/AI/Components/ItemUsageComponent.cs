using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// AI component that allows creatures to use consumable items from inventory.
/// Responds to OnGetDefensiveActions (for healing) and OnGetItemActions (for other consumables).
/// </summary>
public partial class ItemUsageComponent : Node, IAIEventHandler
{
    /// <summary>
    /// HP percentage threshold below which AI will consider using healing items.
    /// Default is 30% (0.3f).
    /// </summary>
    [Export] public float HealingThreshold { get; set; } = 0.3f;

    /// <summary>
    /// Base weight for healing item usage. Increases as HP decreases.
    /// </summary>
    [Export] public int BaseHealingWeight { get; set; } = 10;

    /// <summary>
    /// Base weight for offensive item usage (scrolls of confusion, etc.).
    /// </summary>
    [Export] public int BaseOffensiveItemWeight { get; set; } = 15;

    /// <summary>
    /// Whether this AI should use offensive items (scrolls, wands targeting enemies).
    /// </summary>
    [Export] public bool UseOffensiveItems { get; set; } = true;

    private BaseEntity? _entity;
    private InventoryComponent? _inventory;
    private EquipComponent? _equipComponent;
    private HealthComponent? _healthComponent;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity != null)
        {
            _inventory = _entity.GetNodeOrNull<InventoryComponent>("InventoryComponent");
            _equipComponent = _entity.GetNodeOrNull<EquipComponent>("EquipComponent");
            _healthComponent = _entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        }
    }

    /// <summary>
    /// Handle AI events - responds to defensive and item action requests.
    /// </summary>
    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (_entity == null || _inventory == null)
        {
            return;
        }

        if (eventName == AIEvents.OnGetDefensiveActions)
        {
            HandleDefensiveActions(args);
        }
        else if (eventName == AIEvents.OnGetItemActions)
        {
            HandleItemActions(args);
        }
    }

    /// <summary>
    /// Handle defensive action requests - adds healing item usage when health is low.
    /// </summary>
    private void HandleDefensiveActions(GetActionsEventArgs args)
    {
        if (_healthComponent == null)
        {
            return;
        }

        // Check if health is below threshold
        float hpRatio = (float)_healthComponent.CurrentHealth / _healthComponent.MaxHealth;
        if (hpRatio >= HealingThreshold)
        {
            return;
        }

        // Find healing items in inventory
        foreach (var slot in _inventory!.Inventory)
        {
            // Skip equipped items
            if (_equipComponent != null && _equipComponent.IsEquipped(slot.Key))
            {
                continue;
            }

            var template = slot.Item.Template;

            // Skip non-activatable items
            if (!template.IsActivatable())
            {
                continue;
            }

            // Check for charged items with no charges
            if (template.GetMaxCharges() > 0 && slot.Item.CurrentCharges <= 0)
            {
                continue;
            }

            // Check if this item has healing effects
            bool hasHealingEffect = false;
            foreach (var effectDef in template.Effects)
            {
                if (effectDef.Type?.ToLower() == "heal")
                {
                    hasHealingEffect = true;
                    break;
                }
            }

            if (!hasHealingEffect)
            {
                continue;
            }

            // Calculate weight based on HP level
            int weight = CalculateHealingWeight(hpRatio);

            var useItemAction = new UseItemAction(slot.Key);
            var aiAction = new AIAction(
                action: useItemAction,
                weight: weight,
                debugName: $"Use {template.GetDisplayName(1)}"
            );
            args.ActionList.Add(aiAction);
        }
    }

    /// <summary>
    /// Handle general item action requests - adds offensive consumable usage.
    /// Finds targeted items (scrolls of confusion, etc.) and adds actions for them.
    /// </summary>
    private void HandleItemActions(GetActionsEventArgs args)
    {
        if (!UseOffensiveItems)
        {
            return;
        }

        // Need a target to use offensive items on
        var target = args.Target;
        if (target == null)
        {
            // Try to find a visible enemy
            var enemies = args.Context.GetVisibleEnemies();
            target = args.Context.GetClosestEnemy(enemies);
        }

        if (target == null)
        {
            return;
        }

        // Find offensive items in inventory (items that require targeting)
        foreach (var slot in _inventory!.Inventory)
        {
            // Skip equipped items
            if (_equipComponent != null && _equipComponent.IsEquipped(slot.Key))
            {
                continue;
            }

            var template = slot.Item.Template;

            // Skip non-activatable items
            if (!template.IsActivatable())
            {
                continue;
            }

            // Check for charged items with no charges
            if (template.GetMaxCharges() > 0 && slot.Item.CurrentCharges <= 0)
            {
                continue;
            }

            // Only use items that require targeting (offensive items)
            if (!template.RequiresTargeting())
            {
                continue;
            }

            // Check if target is within item range
            int distanceToTarget = DistanceHelper.ChebyshevDistance(_entity!.GridPosition, target.GridPosition);
            int itemRange = template.GetTargetingRange();

            if (distanceToTarget > itemRange)
            {
                continue;
            }

            // Create targeted item action
            var useTargetedItemAction = new UseTargetedItemAction(slot.Key, target.GridPosition);
            var aiAction = new AIAction(
                action: useTargetedItemAction,
                weight: BaseOffensiveItemWeight,
                debugName: $"Use {template.GetDisplayName(1)} on {target.Name}"
            );
            args.ActionList.Add(aiAction);
        }
    }

    /// <summary>
    /// Calculate the weight for healing item usage based on HP ratio.
    /// Lower HP = higher weight = more likely to heal.
    /// </summary>
    private int CalculateHealingWeight(float hpRatio)
    {
        if (hpRatio < 0.15f)
        {
            // Critical HP - very high priority
            return BaseHealingWeight * 10;
        }
        else if (hpRatio < 0.25f)
        {
            // Low HP - high priority
            return BaseHealingWeight * 5;
        }
        else
        {
            // Below threshold but not critical
            return BaseHealingWeight;
        }
    }
}
