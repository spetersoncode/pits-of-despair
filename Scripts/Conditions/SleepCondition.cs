using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that puts an entity to sleep.
/// Sleeping entities skip their turns until the duration expires or they take damage.
/// Uses the goal stack system - clears the stack and prevents any actions.
/// </summary>
public class SleepCondition : Condition
{
	public override string Name => "Asleep";

	public override string TypeId => "sleep";

	public override string? ExamineDescription => "asleep";

	private BaseEntity? _target;
	private HealthComponent? _healthComponent;

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public SleepCondition()
	{
		Duration = "4";
	}

	/// <summary>
	/// Parameterized constructor for creating sleep with specific duration.
	/// </summary>
	/// <param name="duration">Duration as dice notation (e.g., "4", "2d6").</param>
	public SleepCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		_target = target;

		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"SleepCondition: {target.DisplayName} has no AIComponent");
			return ConditionMessage.Empty;
		}

		// Clear goal stack and push a sleep goal
		// The sleeping entity will skip turns until woken
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new SleepGoal());

		// Subscribe to damage taken signal to wake on damage
		_healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (_healthComponent != null)
		{
			_healthComponent.Connect(
				HealthComponent.SignalName.DamageTaken,
				Callable.From<int>(OnDamageTaken)
			);
		}

		return new ConditionMessage(
			$"The {target.DisplayName} falls asleep!",
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
		// Unsubscribe from damage signal
		if (_healthComponent != null && GodotObject.IsInstanceValid(_healthComponent))
		{
			if (_healthComponent.IsConnected(
				HealthComponent.SignalName.DamageTaken,
				Callable.From<int>(OnDamageTaken)))
			{
				_healthComponent.Disconnect(
					HealthComponent.SignalName.DamageTaken,
					Callable.From<int>(OnDamageTaken)
				);
			}
		}

		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			return ConditionMessage.Empty;
		}

		// Reset goal stack to normal behavior (BoredGoal at bottom)
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new BoredGoal());

		_target = null;
		_healthComponent = null;

		return new ConditionMessage(
			$"{target.DisplayName} wakes up!",
			Palette.ToHex(Palette.Default)
		);
	}

	private void OnDamageTaken(int amount)
	{
		if (amount > 0 && _target != null && GodotObject.IsInstanceValid(_target))
		{
			// Wake up! Remove this condition early
			_target.RemoveConditionByType(TypeId);
		}
	}
}

/// <summary>
/// Special goal used during sleep condition.
/// Never finishes, does nothing each turn (entity is asleep).
/// </summary>
internal class SleepGoal : Goal
{
	public override bool IsFinished(AIContext context)
	{
		// Never finishes - sleep condition controls when this is removed
		return false;
	}

	public override void TakeAction(AIContext context)
	{
		// Do nothing - the entity is asleep
		// Skip turn by not pushing any action goal
	}

	public override string GetName() => "Asleep";
}
