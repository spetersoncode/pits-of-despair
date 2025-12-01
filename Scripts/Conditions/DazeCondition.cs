using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that causes an entity to lose their next action.
/// The condition is consumed when the entity's turn comes - they skip their action
/// and the daze is removed.
/// </summary>
public class DazeCondition : Condition
{
	public override string Name => "Dazed";

	public override string TypeId => "daze";

	public override string? ExamineDescription => "dazed";

	public DazeCondition()
	{
		Duration = "1";
	}

	public DazeCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		// Push a goal that skips the target's next action
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent != null)
		{
			aiComponent.GoalStack.Push(new DazeGoal());
		}

		return new ConditionMessage(
			$"The {target.DisplayName} is dazed!",
			Palette.ToHex(Palette.StatusDebuff)
		);
	}

	public override ConditionMessage OnTurnProcessed(BaseEntity target)
	{
		return ConditionMessage.Empty;
	}

	public override ConditionMessage OnRemoved(BaseEntity target)
	{
		return ConditionMessage.Empty;
	}
}

/// <summary>
/// One-shot goal that skips the entity's next action when dazed.
/// Finishes immediately after executing once.
/// </summary>
internal class DazeGoal : Goal
{
	private bool _hasActed;

	public override bool IsFinished(AIContext context)
	{
		return _hasActed;
	}

	public override void TakeAction(AIContext context)
	{
		// Skip action - emit message and mark as done
		context.Entity.EmitSignal(
			BaseEntity.SignalName.ConditionMessage,
			$"The {context.Entity.DisplayName} is too dazed to act!",
			Palette.ToHex(Palette.StatusDebuff)
		);
		_hasActed = true;
	}

	public override string GetName() => "Dazed";
}
