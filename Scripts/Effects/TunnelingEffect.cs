using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Audio;
using PitsOfDespair.Systems.VisualEffects;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Effects;

/// <summary>
/// Line-based terrain modification effect.
/// Converts wall tiles to floor tiles along a line from caster to target.
/// Respects dungeon boundaries to prevent breaking map containment.
/// Range is determined by the targeting definition, not the effect itself.
/// </summary>
public class TunnelingEffect : Effect
{
    private readonly int _range;
    private readonly VisualConfig? _visual;
    private readonly string? _sound;

    public override string Type => "tunneling";
    public override string Name => "Tunneling";

    public TunnelingEffect() { }

    /// <summary>
    /// Creates a tunneling effect from a unified effect definition.
    /// </summary>
    public TunnelingEffect(EffectDefinition definition)
    {
        _range = definition.Range;
        _visual = definition.Visual;
        _sound = definition.Sound;
    }

    public override EffectResult Apply(EffectContext context)
    {
        // TunnelingEffect should be applied via ApplyToTargets instead
        // Single-target apply is not meaningful for terrain effects
        return EffectResult.CreateFailure("Tunneling requires a line target.");
    }

    /// <summary>
    /// Applies the tunneling effect along a line from caster to target.
    /// Handles terrain modification, visuals, and sound through the standard effect flow.
    /// </summary>
    public override List<EffectResult> ApplyToTargets(
        BaseEntity caster,
        List<BaseEntity> targets,
        ActionContext context,
        GridPosition? targetPosition = null)
    {
        var results = new List<EffectResult>();

        if (!targetPosition.HasValue)
        {
            results.Add(EffectResult.CreateFailure("Tunneling requires a target position."));
            return results;
        }

        // Play sound
        if (!string.IsNullOrEmpty(_sound))
        {
            AudioManager.PlayEffectSound(_sound);
        }

        int range = _range > 0 ? _range : 8;
        var tunnelingResult = ExecuteTunneling(caster, targetPosition.Value, range, context);

        // Spawn beam visual
        SpawnBeamVisual(caster, targetPosition.Value, range, context);

        results.Add(new EffectResult(tunnelingResult.Success, tunnelingResult.Message));
        return results;
    }

    /// <summary>
    /// Executes the tunneling logic along a line from caster to target.
    /// Converts wall tiles to floors, respecting boundary safety.
    /// </summary>
    private TunnelingResult ExecuteTunneling(BaseEntity caster, GridPosition targetPosition, int range, ActionContext context)
    {
        var casterPos = caster.GridPosition;
        var mapSystem = context.MapSystem;

        // Get all positions along the line (excluding caster position)
        // Use stopAtWalls=false since we're tunneling THROUGH walls
        var linePositions = LineTargetingHandler.GetLinePositions(
            casterPos,
            targetPosition,
            range,
            mapSystem,
            stopAtWalls: false);

        var transformedPositions = new List<GridPosition>();
        bool hitBoundary = false;

        foreach (var pos in linePositions)
        {
            // Check if this is a boundary wall (can't dig)
            if (mapSystem.IsOnBoundary(pos))
            {
                hitBoundary = true;
                break; // Stop at boundary
            }

            // Check if this is a wall we can dig
            if (mapSystem.IsSafeToDig(pos))
            {
                mapSystem.SetTile(pos, TileType.Floor);
                transformedPositions.Add(pos);
            }
            // Continue through floors (beam passes through open space)
        }

        // Build result message
        string message;
        if (transformedPositions.Count == 0)
        {
            if (hitBoundary)
            {
                message = "The beam strikes the dungeon boundary and dissipates.";
            }
            else
            {
                message = "The beam passes through open air harmlessly.";
            }
        }
        else if (transformedPositions.Count == 1)
        {
            message = hitBoundary
                ? "The beam carves through stone before hitting the dungeon boundary."
                : "The beam carves through stone, creating a passage.";
        }
        else
        {
            message = hitBoundary
                ? $"The beam carves through {transformedPositions.Count} tiles of stone before hitting the dungeon boundary."
                : $"The beam carves through {transformedPositions.Count} tiles of stone, creating a passage.";
        }

        return new TunnelingResult(
            transformedPositions.Count > 0 || hitBoundary, // Success if anything happened
            message,
            transformedPositions,
            hitBoundary);
    }

    /// <summary>
    /// Spawns the beam visual effect from caster to end position.
    /// </summary>
    private void SpawnBeamVisual(BaseEntity caster, GridPosition targetPosition, int range, ActionContext context)
    {
        if (context.VisualEffectSystem == null)
            return;

        var endPos = GetBeamEndPosition(caster, targetPosition, range, context);

        // Use visual config if available, otherwise use default tunneling beam
        if (_visual?.Beam != null)
        {
            var beamDef = VisualEffectDefinitions.GetById(_visual.Beam);
            if (beamDef != null)
            {
                context.VisualEffectSystem.SpawnEffect(beamDef, caster.GridPosition, 1.0f, endPos);
                return;
            }
        }

        // Fallback to default tunneling visual
        var defaultBeam = VisualEffectDefinitions.Tunneling;
        context.VisualEffectSystem.SpawnEffect(defaultBeam, caster.GridPosition, 1.0f, endPos);
    }

    /// <summary>
    /// Gets the last position in the line (for visual effect targeting).
    /// </summary>
    private GridPosition GetBeamEndPosition(BaseEntity caster, GridPosition targetPosition, int range, ActionContext context)
    {
        var casterPos = caster.GridPosition;
        var mapSystem = context.MapSystem;

        var linePositions = LineTargetingHandler.GetLinePositions(
            casterPos,
            targetPosition,
            range,
            mapSystem,
            stopAtWalls: false);

        if (linePositions.Count == 0)
        {
            return casterPos;
        }

        // Find the actual end position (stop at boundary)
        GridPosition endPos = casterPos;
        foreach (var pos in linePositions)
        {
            if (mapSystem.IsOnBoundary(pos))
            {
                break;
            }
            endPos = pos;
        }

        return endPos;
    }
}

/// <summary>
/// Result of applying a tunneling effect.
/// </summary>
public class TunnelingResult
{
    /// <summary>
    /// Whether the effect did something (transformed tiles or hit boundary).
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Description of what happened.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Positions that were transformed from wall to floor.
    /// </summary>
    public List<GridPosition> TransformedPositions { get; }

    /// <summary>
    /// Whether the beam was stopped by hitting the dungeon boundary.
    /// </summary>
    public bool HitBoundary { get; }

    public TunnelingResult(bool success, string message, List<GridPosition> transformedPositions, bool hitBoundary)
    {
        Success = success;
        Message = message;
        TransformedPositions = transformedPositions;
        HitBoundary = hitBoundary;
    }
}
