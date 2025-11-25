using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that causes an entity to wander randomly for a duration.
/// Confused entities lose their current goal and move randomly like rats.
/// Uses the goal stack system - clears the stack and only allows wandering.
/// </summary>
public class ConfusionCondition : Condition
{
	public override string Name => "Confused";

	public override string TypeId => "confusion";

	public override string? ExamineDescription => "confused";

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public ConfusionCondition()
	{
		Duration = "4";
	}

	/// <summary>
	/// Parameterized constructor for creating confusion with specific duration.
	/// </summary>
	/// <param name="duration">Duration as dice notation (e.g., "4", "2d3").</param>
	public ConfusionCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"ConfusionCondition: {target.DisplayName} has no AIComponent");
			return ConditionMessage.Empty;
		}

		// Clear goal stack and push a wander-only goal
		// The confused entity will just wander randomly each turn
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new ConfusedWanderGoal());

		return new ConditionMessage(
			$"The {target.DisplayName} looks confused!",
			Palette.ToHex(Palette.StatusDebuff)
		);
	}

	public override ConditionMessage OnTurnProcessed(BaseEntity target)
	{
		// No message during turn processing (only on apply/remove)
		return ConditionMessage.Empty;
	}

	public override ConditionMessage OnRemoved(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			return ConditionMessage.Empty;
		}

		// Reset goal stack to normal behavior (BoredGoal at bottom)
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new BoredGoal());

		return new ConditionMessage(
			$"{target.DisplayName} is no longer confused.",
			Palette.ToHex(Palette.Default)
		);
	}
}

/// <summary>
/// Special goal used during confusion condition.
/// Never finishes, just wanders randomly each turn.
/// </summary>
internal class ConfusedWanderGoal : Goal
{
	public override bool IsFinished(AIContext context)
	{
		// Never finishes - confusion condition controls when this is removed
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
