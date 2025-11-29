using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Collision type for knockback effects.
/// </summary>
public enum CollisionType
{
    None,
    Wall,
    Entity
}

/// <summary>
/// Step that pushes the target away from the caster.
/// Supports optional OnCollision sub-pipeline that runs when knockback is blocked.
/// </summary>
public class KnockbackStep : IEffectStep
{
    private readonly int _distance;
    private readonly string? _scalingStat;
    private readonly float _scalingMultiplier;
    private readonly List<StepDefinition>? _onCollisionDefinitions;

    public KnockbackStep(StepDefinition definition)
    {
        _distance = definition.Distance > 0 ? definition.Distance : 1;
        _scalingStat = definition.ScalingStat;
        _scalingMultiplier = definition.ScalingMultiplier;
        _onCollisionDefinitions = definition.OnCollision;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;

        // Knockback requires a caster to determine direction
        if (context.Caster == null)
        {
            return;
        }

        var casterPos = context.Caster.GridPosition;
        var targetPos = target.GridPosition;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        // Calculate direction away from caster
        int dx = targetPos.X - casterPos.X;
        int dy = targetPos.Y - casterPos.Y;

        // Normalize to -1, 0, or 1
        int dirX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int dirY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // If target is at same position as caster, pick a random direction
        if (dirX == 0 && dirY == 0)
        {
            dirX = GD.RandRange(-1, 1);
            dirY = GD.RandRange(-1, 1);
            if (dirX == 0 && dirY == 0)
                dirX = 1;
        }

        // Calculate final distance with scaling
        int finalDistance = _distance;
        if (!string.IsNullOrEmpty(_scalingStat) && context.Caster != null)
        {
            int statValue = context.GetCasterStat(_scalingStat);
            finalDistance += (int)(statValue * _scalingMultiplier);
        }

        if (finalDistance <= 0)
        {
            return;
        }

        // Try to push the target as far as possible, tracking collision info
        var currentPos = targetPos;
        int tilesKnocked = 0;
        CollisionType collisionType = CollisionType.None;
        BaseEntity? collidedEntity = null;

        for (int i = 0; i < finalDistance; i++)
        {
            var nextPos = new GridPosition(currentPos.X + dirX, currentPos.Y + dirY);

            // Check if next position is walkable
            if (!mapSystem.IsWalkable(nextPos))
            {
                collisionType = CollisionType.Wall;
                break;
            }

            // Check if next position is occupied by an entity
            var entityAtPos = entityManager.GetEntityAtPosition(nextPos);
            if (entityAtPos != null)
            {
                collisionType = CollisionType.Entity;
                collidedEntity = entityAtPos;
                break;
            }

            currentPos = nextPos;
            tilesKnocked++;
        }

        // Handle case where target couldn't move at all
        if (tilesKnocked == 0 && collisionType == CollisionType.None)
        {
            messages.Add($"The {target.DisplayName} cannot be pushed!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Move the target to the final position (if they moved at all)
        if (tilesKnocked > 0)
        {
            target.SetGridPosition(currentPos);

            string message = tilesKnocked == 1
                ? $"The {target.DisplayName} is knocked back!"
                : $"The {target.DisplayName} is knocked back {tilesKnocked} tiles!";

            messages.Add(message, Palette.ToHex(Palette.CombatDamage));
        }

        state.Success = true;

        // Run collision sub-effects if collision occurred and OnCollision is defined
        if (collisionType != CollisionType.None)
        {
            // Add collision message (tagged with target entity for proper grouping)
            messages.CurrentEntity = target;
            if (collisionType == CollisionType.Wall)
            {
                messages.Add($"The {target.DisplayName} crashes into the wall!", Palette.ToHex(Palette.CombatDamage));
            }
            else if (collisionType == CollisionType.Entity && collidedEntity != null)
            {
                messages.Add($"The {target.DisplayName} slams into the {collidedEntity.DisplayName}!", Palette.ToHex(Palette.CombatDamage));
            }

            // Run collision sub-pipeline if defined
            if (_onCollisionDefinitions != null && _onCollisionDefinitions.Count > 0)
            {
                RunCollisionEffects(context, collisionType, collidedEntity, messages);
            }
        }
    }

    /// <summary>
    /// Runs the OnCollision sub-pipeline for each collision target.
    /// Wall collision: runs once for knocked target.
    /// Entity collision: runs twice (knocked target + collided entity), each with independent state.
    /// </summary>
    private void RunCollisionEffects(
        EffectContext context,
        CollisionType collisionType,
        BaseEntity? collidedEntity,
        MessageCollector messages)
    {
        // Build the collision steps
        var collisionSteps = CompositeEffectBuilder.BuildSteps(_onCollisionDefinitions);
        if (collisionSteps.Count == 0)
            return;

        // Always run for the knocked target
        RunSubPipeline(collisionSteps, context.Target, context, messages);

        // For entity collision, also run for the collided entity
        if (collisionType == CollisionType.Entity && collidedEntity != null)
        {
            RunSubPipeline(collisionSteps, collidedEntity, context, messages);
        }
    }

    /// <summary>
    /// Runs a sub-pipeline of steps for a specific target.
    /// Creates fresh EffectContext and EffectState for independent execution.
    /// </summary>
    private static void RunSubPipeline(
        List<IEffectStep> steps,
        BaseEntity target,
        EffectContext originalContext,
        MessageCollector messages)
    {
        // Create new context with the sub-target but same caster and action context
        var subContext = EffectContext.ForItem(
            target,
            originalContext.Caster,
            originalContext.ActionContext,
            originalContext.TargetPosition
        );

        // Fresh state for independent save rolls
        var subState = new EffectState();

        // Set current entity for message grouping (handles same-name entities correctly)
        var previousEntity = messages.CurrentEntity;
        messages.CurrentEntity = target;

        // Execute each step
        foreach (var step in steps)
        {
            if (!subState.Continue)
                break;

            step.Execute(subContext, subState, messages);
        }

        // Restore previous entity context
        messages.CurrentEntity = previousEntity;
    }
}
