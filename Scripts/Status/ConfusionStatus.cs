using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Status that causes an entity to wander randomly for a duration.
/// Confused entities lose their current goal and move randomly like rats.
/// Uses the goal stack system - clears the stack and only allows wandering.
/// </summary>
public class ConfusionStatus : Status
{
	public override string Name => "Confused";

	public override string TypeId => "confusion";

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public ConfusionStatus()
	{
		// Duration will be set by the effect (2d3)
		Duration = 4;
	}

	/// <summary>
	/// Parameterized constructor for creating confusion with specific duration.
	/// </summary>
	public ConfusionStatus(int duration)
	{
		Duration = duration;
	}

	public override StatusMessage OnApplied(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"ConfusionStatus: {target.DisplayName} has no AIComponent");
			return StatusMessage.Empty;
		}

		aiComponent.ClearPath();

		// Clear goal stack and push a wander-only goal
		// The confused entity will just wander randomly each turn
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new ConfusedWanderGoal());

		return new StatusMessage(
			$"{target.DisplayName} looks confused!",
			Palette.ToHex(Palette.StatusDebuff)
		);
	}

	public override StatusMessage OnTurnProcessed(BaseEntity target)
	{
		// No message during turn processing (only on apply/remove)
		return StatusMessage.Empty;
	}

	public override StatusMessage OnRemoved(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			return StatusMessage.Empty;
		}

		aiComponent.ClearPath();

		// Reset goal stack to normal behavior (BoredGoal at bottom)
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new BoredGoal());

		return new StatusMessage(
			$"{target.DisplayName} is no longer confused.",
			Palette.ToHex(Palette.Default)
		);
	}
}

/// <summary>
/// Special goal used during confusion status.
/// Never finishes, just wanders randomly each turn.
/// </summary>
internal class ConfusedWanderGoal : Goal
{
	public override bool IsFinished(AIContext context)
	{
		// Never finishes - confusion status controls when this is removed
		return false;
	}

	public override void TakeAction(AIContext context)
	{
		// Just push a wander goal each turn
		var wanderGoal = new WanderGoal(originalIntent: this);
		context.AIComponent.GoalStack.Push(wanderGoal);
	}

	public override string GetName() => "Confused (Wandering)";
}
