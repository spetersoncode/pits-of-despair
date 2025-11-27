using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System;
using System.Collections.Generic;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that causes an entity to flee from a fear source.
/// Terrified entities lose their current goal and flee away from the source.
/// Uses the goal stack system - clears the stack and only allows fleeing.
/// </summary>
public class FearCondition : Condition
{
	public override string Name => "Terrified";

	public override string TypeId => "fear";

	public override string? ExamineDescription => "terrified";

	/// <summary>
	/// The entity this creature is fleeing from.
	/// </summary>
	public BaseEntity? FearSource { get; set; }

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public FearCondition()
	{
		Duration = "4";
	}

	/// <summary>
	/// Parameterized constructor for creating fear with specific duration.
	/// </summary>
	/// <param name="duration">Duration as dice notation (e.g., "4", "2d3").</param>
	public FearCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
		if (aiComponent == null)
		{
			GD.PrintErr($"FearCondition: {target.DisplayName} has no AIComponent");
			return ConditionMessage.Empty;
		}

		// Clear goal stack and push a flee-only goal
		// The terrified entity will flee from the fear source each turn
		aiComponent.GoalStack.Clear();
		aiComponent.GoalStack.Push(new FearFleeGoal(FearSource));

		return new ConditionMessage(
			$"The {target.DisplayName} flees in terror!",
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
			$"{target.DisplayName} is no longer afraid.",
			Palette.ToHex(Palette.Default)
		);
	}
}

/// <summary>
/// Special goal used during fear condition.
/// Never finishes, just flees from the fear source each turn.
/// </summary>
internal class FearFleeGoal : Goal
{
	private readonly BaseEntity? _fearSource;

	public FearFleeGoal(BaseEntity? fearSource)
	{
		_fearSource = fearSource;
	}

	public override bool IsFinished(AIContext context)
	{
		// Never finishes - fear condition controls when this is removed
		return false;
	}

	public override void TakeAction(AIContext context)
	{
		// If we have no fear source or it's dead, just wander in panic
		if (_fearSource == null || !IsEntityAlive(_fearSource))
		{
			var wanderGoal = new WanderGoal(originalIntent: this);
			context.AIComponent.GoalStack.Push(wanderGoal);
			return;
		}

		// Flee from the fear source
		var fleeDir = GetFleeDirection(context);
		if (fleeDir != Vector2I.Zero)
		{
			var moveGoal = new MoveDirectionGoal(fleeDir, originalIntent: this);
			context.AIComponent.GoalStack.Push(moveGoal);
		}
		else
		{
			// Cornered - just cower (do nothing)
		}
	}

	private bool IsEntityAlive(BaseEntity entity)
	{
		var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
		return health == null || health.IsAlive();
	}

	private Vector2I GetFleeDirection(AIContext context)
	{
		var myPos = context.Entity.GridPosition;
		var threatPos = _fearSource!.GridPosition;
		var mapSystem = context.ActionContext.MapSystem;
		var entityManager = context.ActionContext.EntityManager;

		// Calculate direction away from threat
		int dx = Math.Sign(myPos.X - threatPos.X);
		int dy = Math.Sign(myPos.Y - threatPos.Y);

		// If we're at the same position as threat, pick a random direction
		if (dx == 0 && dy == 0)
		{
			dx = GD.RandRange(-1, 1);
			dy = GD.RandRange(-1, 1);
		}

		// Try the ideal flee direction first, then adjacent directions
		var directions = GetFleeDirectionPriority(dx, dy);

		foreach (var dir in directions)
		{
			var targetPos = new GridPosition(myPos.X + dir.X, myPos.Y + dir.Y);

			if (mapSystem.IsWalkable(targetPos) &&
				entityManager.GetEntityAtPosition(targetPos) == null)
			{
				return dir;
			}
		}

		return Vector2I.Zero; // No valid flee direction
	}

	private List<Vector2I> GetFleeDirectionPriority(int dx, int dy)
	{
		var directions = new List<Vector2I>();

		// Primary: directly away
		if (dx != 0 || dy != 0)
			directions.Add(new Vector2I(dx, dy));

		// Secondary: perpendicular directions (allows flanking around obstacles)
		if (dx != 0)
		{
			directions.Add(new Vector2I(dx, 1));
			directions.Add(new Vector2I(dx, -1));
		}
		if (dy != 0)
		{
			directions.Add(new Vector2I(1, dy));
			directions.Add(new Vector2I(-1, dy));
		}

		// Tertiary: pure perpendicular
		if (dx != 0)
		{
			directions.Add(new Vector2I(0, 1));
			directions.Add(new Vector2I(0, -1));
		}
		if (dy != 0)
		{
			directions.Add(new Vector2I(1, 0));
			directions.Add(new Vector2I(-1, 0));
		}

		return directions;
	}

	public override string GetName() => $"Terrified (Fleeing from {_fearSource?.DisplayName ?? "Unknown"})";
}
