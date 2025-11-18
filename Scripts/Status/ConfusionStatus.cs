using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Status;

/// <summary>
/// Status that causes an entity to wander randomly for a duration.
/// Confused entities lose their current goal and move randomly like rats.
/// </summary>
public class ConfusionStatus : Status
{
	private List<Goal> _savedGoals;

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

	public override string OnApplied(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"ConfusionStatus: {target.DisplayName} has no AIComponent");
			return string.Empty;
		}

		// Save current goals and replace with only WanderGoal
		_savedGoals = aiComponent.AvailableGoals;
		aiComponent.AvailableGoals = new List<Goal> { new WanderGoal() };
		aiComponent.CurrentGoal = null; // Force re-evaluation
		aiComponent.ClearPath();

		return $"{target.DisplayName} looks confused!";
	}

	public override string OnTurnProcessed(BaseEntity target)
	{
		// No message during turn processing (only on apply/remove)
		return string.Empty;
	}

	public override string OnRemoved(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			return string.Empty;
		}

		// Restore original goals
		if (_savedGoals != null)
		{
			aiComponent.AvailableGoals = _savedGoals;
		}

		aiComponent.CurrentGoal = null; // Force re-evaluation with restored goals
		aiComponent.ClearPath();

		return $"{target.DisplayName} is no longer confused.";
	}
}
