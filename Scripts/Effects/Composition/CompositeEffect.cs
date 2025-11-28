using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Audio;
using PitsOfDespair.Systems.VisualEffects;
using PitsOfDespair.Targeting;

namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// Effect composed of a sequence of steps executed in pipeline fashion.
/// Steps share state and accumulate messages for batch emission.
/// </summary>
public class CompositeEffect : Effect
{
    private readonly string _type;
    private readonly string _name;
    private readonly string? _sound;
    private readonly List<IEffectStep> _steps;
    private readonly VisualConfig? _visual;
    private readonly int _range;
    private readonly int _radius;

    public override string Type => _type;
    public override string Name => _name;

    public CompositeEffect(
        string type,
        string name,
        string? sound,
        List<IEffectStep> steps,
        VisualConfig? visual = null,
        int range = 0,
        int radius = 0)
    {
        _type = type;
        _name = name;
        _sound = sound;
        _steps = steps;
        _visual = visual;
        _range = range;
        _radius = radius;
    }

    public override EffectResult Apply(EffectContext context)
    {
        var state = new EffectState();
        var messages = new MessageCollector();

        // Execute steps in sequence
        foreach (var step in _steps)
        {
            if (!state.Continue)
                break;

            step.Execute(context, state, messages);
        }

        // Emit accumulated messages
        messages.Emit(context.Target, context);

        // Return result based on state
        return new EffectResult(state.Success, string.Empty)
        {
            AffectedEntity = context.Target,
            DamageDealt = state.DamageDealt
        };
    }

    /// <summary>
    /// Applies this effect to multiple targets.
    /// Handles visual effects based on VisualConfig, including projectile sequencing.
    /// </summary>
    public override List<EffectResult> ApplyToTargets(
        BaseEntity caster,
        List<BaseEntity> targets,
        ActionContext context,
        GridPosition? targetPosition = null)
    {
        // If has projectile visual, defer execution until projectile arrives
        if (_visual?.Projectile != null && context.VisualEffectSystem != null)
        {
            return ApplyWithProjectile(caster, targets, context, targetPosition);
        }

        // Spawn immediate visual (beam/cone/impact)
        SpawnImmediateVisual(caster, context, targetPosition);

        // Play sound once at the start
        if (!string.IsNullOrEmpty(_sound))
        {
            AudioManager.PlayEffectSound(_sound);
        }

        var results = new List<EffectResult>();
        foreach (var target in targets)
        {
            var effectContext = EffectContext.ForItem(target, caster, context, targetPosition);
            results.Add(Apply(effectContext));
        }
        return results;
    }

    /// <summary>
    /// Applies effect with projectile visual - effect is deferred until projectile arrives.
    /// </summary>
    private List<EffectResult> ApplyWithProjectile(
        BaseEntity caster,
        List<BaseEntity> targets,
        ActionContext context,
        GridPosition? targetPosition)
    {
        var projectileDef = VisualEffectDefinitions.GetById(_visual!.Projectile!);
        if (projectileDef == null || projectileDef.Type != VisualEffectType.Projectile)
        {
            // Fallback to immediate application if projectile not found
            SpawnImmediateVisual(caster, context, targetPosition);
            return ApplyImmediately(caster, targets, context, targetPosition);
        }

        // For area effects, spawn single projectile to target position
        if (targetPosition.HasValue && _visual.Impact != null)
        {
            // Capture data for callback
            var capturedTargets = new List<BaseEntity>(targets);

            context.VisualEffectSystem!.SpawnProjectile(
                projectileDef,
                caster.GridPosition,
                targetPosition.Value,
                () =>
                {
                    // Play sound on impact
                    if (!string.IsNullOrEmpty(_sound))
                    {
                        AudioManager.PlayEffectSound(_sound);
                    }

                    // Spawn impact visual
                    SpawnImpactVisual(context, targetPosition.Value);

                    // Apply effects to targets
                    foreach (var target in capturedTargets)
                    {
                        var effectContext = EffectContext.ForItem(target, caster, context, targetPosition);
                        var result = Apply(effectContext);

                        // Emit damage message for combat log
                        if (result.Success && result.DamageDealt > 0)
                        {
                            context.CombatSystem?.EmitActionMessage(
                                caster,
                                result.Message ?? string.Empty,
                                Core.Palette.ToHex(Core.Palette.Fire));
                        }
                    }
                });

            // Return empty results since effects are deferred
            return new List<EffectResult>();
        }

        // For targeted effects (single target), spawn projectile to each target
        var results = new List<EffectResult>();
        foreach (var target in targets)
        {
            var capturedTarget = target;

            context.VisualEffectSystem!.SpawnProjectile(
                projectileDef,
                caster.GridPosition,
                target.GridPosition,
                () =>
                {
                    // Play sound on impact
                    if (!string.IsNullOrEmpty(_sound))
                    {
                        AudioManager.PlayEffectSound(_sound);
                    }

                    // Spawn impact visual at target
                    if (_visual.Impact != null)
                    {
                        SpawnImpactVisual(context, capturedTarget.GridPosition);
                    }

                    // Apply effect
                    var effectContext = EffectContext.ForItem(capturedTarget, caster, context, targetPosition);
                    Apply(effectContext);
                });
        }

        // Return empty results since effects are deferred
        return results;
    }

    /// <summary>
    /// Applies effects immediately without projectile.
    /// </summary>
    private List<EffectResult> ApplyImmediately(
        BaseEntity caster,
        List<BaseEntity> targets,
        ActionContext context,
        GridPosition? targetPosition)
    {
        if (!string.IsNullOrEmpty(_sound))
        {
            AudioManager.PlayEffectSound(_sound);
        }

        var results = new List<EffectResult>();
        foreach (var target in targets)
        {
            var effectContext = EffectContext.ForItem(target, caster, context, targetPosition);
            results.Add(Apply(effectContext));
        }
        return results;
    }

    /// <summary>
    /// Spawns immediate visuals (beam, cone, impact) based on VisualConfig.
    /// Uses stored _range and _radius from effect definition.
    /// </summary>
    private void SpawnImmediateVisual(BaseEntity caster, ActionContext context, GridPosition? targetPosition)
    {
        if (_visual == null || context.VisualEffectSystem == null || !targetPosition.HasValue)
            return;

        // Beam visual for line effects
        if (_visual.Beam != null)
        {
            var beamDef = VisualEffectDefinitions.GetById(_visual.Beam);
            if (beamDef != null)
            {
                // Calculate beam end position using line algorithm
                int range = _range > 0 ? _range : 8;
                var linePositions = LineTargetingHandler.GetLinePositions(
                    caster.GridPosition,
                    targetPosition.Value,
                    range,
                    context.MapSystem,
                    stopAtWalls: true
                );
                var endPos = linePositions.Count > 0 ? linePositions[^1] : targetPosition.Value;
                context.VisualEffectSystem.SpawnEffect(beamDef, caster.GridPosition, 1.0f, endPos);
            }
        }

        // Cone visual for cone effects
        if (_visual.Cone != null)
        {
            var coneDef = VisualEffectDefinitions.GetById(_visual.Cone);
            if (coneDef != null && coneDef.Type == VisualEffectType.Cone)
            {
                int range = _range > 0 ? _range : 4;
                int radius = _radius > 0 ? _radius : 3;
                context.VisualEffectSystem.SpawnConeEffect(coneDef, caster.GridPosition, targetPosition.Value, range, radius);
            }
        }

        // Impact visual for area effects (without projectile)
        if (_visual.Impact != null && _visual.Projectile == null)
        {
            SpawnImpactVisual(context, targetPosition.Value);
        }
    }

    /// <summary>
    /// Spawns an impact visual at the specified position.
    /// </summary>
    private void SpawnImpactVisual(ActionContext context, GridPosition position)
    {
        if (_visual?.Impact == null || context.VisualEffectSystem == null)
            return;

        var impactDef = VisualEffectDefinitions.GetById(_visual.Impact);
        if (impactDef != null)
        {
            float radius = _radius > 0 ? _radius : 2;
            context.VisualEffectSystem.SpawnEffect(impactDef, position, radius);
        }
    }
}
