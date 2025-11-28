using System.Collections.Generic;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that creates a clone of the target creature.
/// The clone spawns adjacent to the target with the same faction.
/// </summary>
public class CloneStep : IEffectStep
{
    public CloneStep(StepDefinition definition)
    {
        // No special parameters needed
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var entityFactory = context.ActionContext.EntityFactory;
        var mapSystem = context.ActionContext.MapSystem;
        var entityManager = context.ActionContext.EntityManager;

        if (target == null)
        {
            messages.Add("No target to clone.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Check if target has a CreatureId (can't clone player)
        if (string.IsNullOrEmpty(target.CreatureId))
        {
            messages.Add($"{target.DisplayName} cannot be cloned.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Find an adjacent walkable position for the clone
        var clonePosition = FindAdjacentWalkablePosition(target.GridPosition, mapSystem, entityManager);
        if (clonePosition == null)
        {
            messages.Add("No space to create a clone!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Create the clone
        var clone = entityFactory.CreateCreature(target.CreatureId, clonePosition.Value);
        if (clone == null)
        {
            messages.Add("Failed to create clone.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Clone inherits the original's faction and appearance
        clone.Faction = target.Faction;
        clone.GlyphColor = target.GlyphColor;

        // Copy protection target for friendly AI behavior
        var targetAI = target.GetNodeOrNull<AIComponent>("AIComponent");
        var cloneAI = clone.GetNodeOrNull<AIComponent>("AIComponent");
        if (targetAI != null && cloneAI != null && targetAI.ProtectionTarget != null)
        {
            cloneAI.ProtectionTarget = targetAI.ProtectionTarget;
        }

        // Register with entity manager
        entityManager.AddEntity(clone);

        messages.Add($"A clone of {target.DisplayName} appears!", Palette.ToHex(Palette.Cyan));
        state.Success = true;
    }

    private GridPosition? FindAdjacentWalkablePosition(
        GridPosition center,
        Systems.MapSystem mapSystem,
        Systems.Entity.EntityManager entityManager)
    {
        // Check all 8 adjacent positions
        var directions = new List<(int dx, int dy)>
        {
            (0, -1),  // N
            (1, -1),  // NE
            (1, 0),   // E
            (1, 1),   // SE
            (0, 1),   // S
            (-1, 1),  // SW
            (-1, 0),  // W
            (-1, -1)  // NW
        };

        foreach (var (dx, dy) in directions)
        {
            var pos = new GridPosition(center.X + dx, center.Y + dy);

            if (mapSystem.IsWalkable(pos) && entityManager.GetEntityAtPosition(pos) == null)
            {
                return pos;
            }
        }

        return null;
    }
}
