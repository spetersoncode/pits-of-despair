using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that stuns an entity.
/// Stunned entities have a 50% chance to lose their action each turn and -2 evasion.
/// Uses the goal stack system - clears the stack and pushes a StunGoal.
/// </summary>
public class StunCondition : Condition
{
	private const int EvasionPenalty = -2;
	private const string ModifierSourceId = "condition_stun";

	public override string Name => "Stunned";

	public override string TypeId => "stun";

	public override string? ExamineDescription => "stunned";

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public StunCondition()
	{
		Duration = "1d4";
	}

	/// <summary>
	/// Parameterized constructor for creating stun with specific duration.
	/// </summary>
	/// <param name="duration">Duration as dice notation (e.g., "1d4", "2d3").</param>
	public StunCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"StunCondition: {target.DisplayName} has no AIComponent");
			return ConditionMessage.Empty;
		}

		// Clear goal stack and push a stun goal
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new StunGoal());

		// Apply evasion penalty
		var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
		stats?.AddStatModifier(StatType.Evasion, ModifierSourceId, EvasionPenalty);

		return new ConditionMessage(
			$"{target.DisplayName} is stunned!",
			Palette.ToHex(Palette.StatusDebuff)
		);
	}

	public override ConditionMessage OnTurnProcessed(BaseEntity target)
	{
		// No message during turn processing
		return ConditionMessage.Empty;
	}

	public override ConditionMessage OnRemoved(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent != null)
		{
			// Reset goal stack to normal behavior (BoredGoal at bottom)
			aiComponent.GoalStack.Clear();
			aiComponent.GoalStack.Push(new BoredGoal());
		}

		// Remove evasion penalty
		var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
		stats?.RemoveStatModifier(StatType.Evasion, ModifierSourceId);

		return new ConditionMessage(
			$"{target.DisplayName} recovers from stun.",
			Palette.ToHex(Palette.Default)
		);
	}
}

/// <summary>
/// Special goal used during stun condition.
/// Each turn, 50% chance to lose action (do nothing), otherwise wait.
/// </summary>
internal class StunGoal : Goal
{
	private const float ActionLossChance = 0.5f;

	public override bool IsFinished(AIContext context)
	{
		// Never finishes - stun condition controls when this is removed
		return false;
	}

	public override void TakeAction(AIContext context)
	{
		// Roll for action loss
		if (GD.Randf() < ActionLossChance)
		{
			// Lost action - do nothing this turn
			return;
		}

		// Not stunned this turn - but still dazed, execute wait action
		context.Entity.ExecuteAction(new WaitAction(), context.ActionContext);
	}

	public override string GetName() => "Stunned";
}
