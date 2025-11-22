using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;

namespace PitsOfDespair.Components.AI;

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
        float hpRatio = (float)_healthComponent.CurrentHP / _healthComponent.MaxHP;
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

            char itemKey = slot.Key; // Capture for closure
            args.ActionList.Add(
                weight: weight,
                execute: ctx => UseItem(ctx, itemKey),
                debugName: $"Use {template.GetDisplayName(1)}"
            );
        }
    }

    /// <summary>
    /// Handle general item action requests - adds non-healing consumable usage.
    /// </summary>
    private void HandleItemActions(GetActionsEventArgs args)
    {
        // For now, this is a placeholder for non-healing consumables.
        // Items like scrolls of confusion (offensive) would require targeting
        // and should be handled differently.
        // Future implementation could add tactical item usage here.
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

    /// <summary>
    /// Use an item from inventory, applying its effects.
    /// </summary>
    private void UseItem(AIContext context, char itemKey)
    {
        if (_entity == null || _inventory == null)
        {
            return;
        }

        var slot = _inventory.GetSlot(itemKey);
        if (slot == null)
        {
            return;
        }

        var template = slot.Item.Template;
        string itemName = template.GetDisplayName(1);

        // Apply all effects
        var effects = template.GetEffects();
        bool anyEffectSucceeded = false;

        foreach (var effect in effects)
        {
            var effectResult = effect.Apply(_entity, context.ActionContext);
            if (effectResult.Success)
            {
                anyEffectSucceeded = true;
            }
        }

        // If no effects succeeded, don't consume the item
        if (!anyEffectSucceeded)
        {
            return;
        }

        // Handle consumption based on item type
        if (template.GetIsConsumable())
        {
            // Consumable: remove one from stack
            _inventory.RemoveItem(itemKey, 1);
        }
        else if (template.GetMaxCharges() > 0)
        {
            // Charged item: use one charge
            slot.Item.UseCharge();
        }

        // Note: Effect results contain messages that could be displayed by a UI system
        // listening to entity events. For now, AI item usage is silent.
    }
}
