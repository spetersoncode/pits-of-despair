using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System;
using System.Collections.Generic;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that moves the entity away from a threat.
/// Prioritizes fleeing toward allies if available, otherwise moves directly away.
/// Can fire defensive action events (yelling for help, etc.) while fleeing.
/// </summary>
public class FleeGoal : Goal
{
    public BaseEntity FleeFrom { get; private set; }
    public int TurnsRemaining { get; private set; }
    public int SafeDistance { get; private set; }

    public FleeGoal(BaseEntity fleeFrom, int turns, int safeDistance, Goal originalIntent = null)
    {
        FleeFrom = fleeFrom;
        TurnsRemaining = turns;
        SafeDistance = safeDistance;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        // Done when: turns expired AND (can't see threat OR far enough away)
        if (TurnsRemaining <= 0)
        {
            bool canSee = context.CanSee(FleeFrom);
            int distance = DistanceHelper.ChebyshevDistance(
                context.Entity.GridPosition,
                FleeFrom.GridPosition);

            return !canSee || distance >= SafeDistance;
        }
        return false;
    }

    public override void TakeAction(AIContext context)
    {
        TurnsRemaining--;

        // Try to gather defensive actions (yelling, healing, etc.)
        // TODO: Phase 4+ will implement FireEvent on BaseEntity
        var args = new GetActionsEventArgs { Context = context };
        // context.Entity.FireEvent(AIEvents.OnGetDefensiveActions, args);

        if (!args.ActionList.IsEmpty)
        {
            // Execute defensive action (might be yell for help)
            var action = args.ActionList.PickRandomWeighted();
            action?.Invoke(context);
            return;
        }

        // Otherwise, flee movement
        FleeFromThreat(context);
    }

    private void FleeFromThreat(AIContext context)
    {
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Try to flee toward allies first
        var allyPositions = FindNearbyAllies(context);
        if (allyPositions.Count > 0)
        {
            var allyMap = DijkstraMapBuilder.BuildDistanceMap(
                allyPositions,
                mapSystem,
                entityManager);

            var nextPos = DijkstraMapBuilder.GetNearestGoalDirection(
                context.Entity.GridPosition,
                allyMap);

            if (nextPos != null && IsSafeMove(context, nextPos.Value))
            {
                PushMoveGoal(context, nextPos.Value);
                return;
            }
        }

        // Fallback: move directly away from threat
        var fleeDir = GetFleeDirection(context);
        if (fleeDir != Vector2I.Zero)
        {
            var moveGoal = new MoveDirectionGoal(fleeDir, originalIntent: this);
            context.AIComponent.GoalStack.Push(moveGoal);
        }
        // If no valid flee direction, we're cornered - just wait
    }

    private List<GridPosition> FindNearbyAllies(AIContext context)
    {
        var allies = new List<GridPosition>();
        var entityManager = context.ActionContext.EntityManager;
        var myFaction = context.Entity.Faction;
        var myPos = context.Entity.GridPosition;

        // Check all entities for allies
        foreach (var entity in entityManager.GetAllEntities())
        {
            // Skip self
            if (entity == context.Entity)
                continue;

            // Must be same faction (ally)
            if (entity.Faction != myFaction)
                continue;

            // Skip dead allies
            var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (health != null && !health.IsAlive())
                continue;

            // Only consider allies within reasonable distance
            int distance = DistanceHelper.ChebyshevDistance(myPos, entity.GridPosition);
            if (distance <= 15)
            {
                allies.Add(entity.GridPosition);
            }
        }

        return allies;
    }

    private bool IsSafeMove(AIContext context, GridPosition targetPos)
    {
        // Don't move closer to the threat
        int currentDist = DistanceHelper.ChebyshevDistance(
            context.Entity.GridPosition,
            FleeFrom.GridPosition);
        int newDist = DistanceHelper.ChebyshevDistance(
            targetPos,
            FleeFrom.GridPosition);

        return newDist >= currentDist;
    }

    private void PushMoveGoal(AIContext context, GridPosition targetPos)
    {
        var currentPos = context.Entity.GridPosition;
        var direction = new Vector2I(
            targetPos.X - currentPos.X,
            targetPos.Y - currentPos.Y);

        var moveGoal = new MoveDirectionGoal(direction, originalIntent: this);
        context.AIComponent.GoalStack.Push(moveGoal);
    }

    private Vector2I GetFleeDirection(AIContext context)
    {
        var myPos = context.Entity.GridPosition;
        var threatPos = FleeFrom.GridPosition;
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

    public override string GetName() => $"Flee from {FleeFrom?.DisplayName ?? "Unknown"}";
}
