using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Audio;

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

    public override string Type => _type;
    public override string Name => _name;

    public CompositeEffect(string type, string name, string? sound, List<IEffectStep> steps)
    {
        _type = type;
        _name = name;
        _sound = sound;
        _steps = steps;
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
    /// Plays sound once at start, then executes pipeline for each target.
    /// </summary>
    public override List<EffectResult> ApplyToTargets(
        BaseEntity caster,
        List<BaseEntity> targets,
        ActionContext context,
        GridPosition? targetPosition = null)
    {
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
}
