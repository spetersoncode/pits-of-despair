using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Makes creature opportunistically pick up valuable items when bored.
/// Responds to OnIAmBored to push SeekItemGoal with configurable probability.
/// Only creatures with this component will collect items.
/// </summary>
public partial class ItemCollectComponent : Node, IAIEventHandler
{
    /// <summary>
    /// Chance to attempt item pickup each turn when bored (0.0 - 1.0).
    /// </summary>
    [Export] public float PickupChance { get; set; } = 0.5f;

    public void HandleAIEvent(string eventName, GetActionsEventArgs args)
    {
        if (eventName != AIEvents.OnIAmBored)
            return;

        var context = args.Context;

        // Don't collect items if we see enemies - combat takes precedence
        var enemies = context.GetVisibleEnemies();
        if (enemies.Count > 0)
            return;

        // Check if entity has inventory
        var inventory = context.Entity.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventory == null)
            return;

        // Random chance to attempt pickup (not every turn)
        if (GD.Randf() > PickupChance)
            return;

        // Find visible items
        var visibleItems = context.GetVisibleItems();
        if (visibleItems.Count == 0)
            return;

        // Find the best item (highest score that's worth picking)
        BaseEntity? bestItem = null;
        int bestScore = 0;

        foreach (var itemEntity in visibleItems)
        {
            var itemData = itemEntity.ItemData?.Template;
            if (itemData == null)
                continue;

            // Skip items not worth picking up
            if (!ItemEvaluator.IsItemWorthPicking(itemData, context.Entity))
                continue;

            int score = ItemEvaluator.EvaluateItem(itemData, context.Entity);
            if (score > bestScore)
            {
                bestScore = score;
                bestItem = itemEntity;
            }
        }

        if (bestItem == null)
            return;

        // Push seek item goal
        var seekGoal = new SeekItemGoal(bestItem);
        context.AIComponent.GoalStack.Push(seekGoal);
        args.Handled = true;
    }
}
