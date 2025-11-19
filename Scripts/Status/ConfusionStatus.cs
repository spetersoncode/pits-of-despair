using System.Collections.Generic;
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

	public override StatusMessage OnApplied(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"ConfusionStatus: {target.DisplayName} has no AIComponent");
			return StatusMessage.Empty;
		}

		// Save current goals and replace with only WanderGoal
		_savedGoals = aiComponent.AvailableGoals;
		aiComponent.AvailableGoals = new List<Goal> { new WanderGoal() };
		aiComponent.CurrentGoal = null; // Force re-evaluation
		aiComponent.ClearPath();

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

		// Restore original goals
		if (_savedGoals != null)
		{
			aiComponent.AvailableGoals = _savedGoals;
		}

		aiComponent.CurrentGoal = null; // Force re-evaluation with restored goals
		aiComponent.ClearPath();

		return new StatusMessage(
			$"{target.DisplayName} is no longer confused.",
			Palette.ToHex(Palette.Default)
		);
	}
}
