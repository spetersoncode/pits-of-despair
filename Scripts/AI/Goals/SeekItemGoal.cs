using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that seeks out and picks up a specific item on the ground.
/// Pushes ApproachGoal to move to item's position, then executes PickupAction.
/// </summary>
public class SeekItemGoal : Goal
{
    /// <summary>
    /// The item entity we're trying to pick up.
    /// </summary>
    public BaseEntity TargetItem { get; private set; }

    /// <summary>
    /// Creates a seek item goal for a specific ground item.
    /// </summary>
    public SeekItemGoal(BaseEntity targetItem, Goal originalIntent = null)
    {
        TargetItem = targetItem;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        // Finished if item no longer exists (picked up or destroyed)
        if (TargetItem == null || !GodotObject.IsInstanceValid(TargetItem))
        {
            return true;
        }

        // Finished if item no longer has ItemData (already picked up)
        if (TargetItem.ItemData == null)
        {
            return true;
        }

        return false;
    }

    public override void TakeAction(AIContext context)
    {
        // If item is invalid, fail
        if (TargetItem == null || !GodotObject.IsInstanceValid(TargetItem) || TargetItem.ItemData == null)
        {
            Fail(context);
            return;
        }

        // Check if we're standing on the item
        if (context.Entity.GridPosition == TargetItem.GridPosition)
        {
            // Execute pickup action
            var pickupAction = new PickupAction();
            var result = pickupAction.Execute(context.Entity, context.ActionContext);

            if (result.Success)
            {
                // Goal complete - will be finished on next check
                return;
            }
            else
            {
                // Pickup failed (inventory full?) - fail the goal
                Fail(context);
                return;
            }
        }

        // Not at item position yet - push approach goal to move there
        // DesiredDistance = 0 means we want to stand on the item's tile
        var approachGoal = new ApproachGoal(TargetItem.GridPosition, desiredDistance: 0, originalIntent: this);
        context.AIComponent.GoalStack.Push(approachGoal);
    }

    public override string GetName()
    {
        if (TargetItem?.ItemData?.Template != null)
        {
            return $"Seek {TargetItem.ItemData.Template.GetDisplayName(1)}";
        }
        return "Seek Item";
    }
}
