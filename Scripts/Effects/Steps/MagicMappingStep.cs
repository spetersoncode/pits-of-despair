using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that reveals a portion of the map around the target.
/// </summary>
public class MagicMappingStep : IEffectStep
{
    private readonly int _radius;

    public MagicMappingStep(StepDefinition definition)
    {
        _radius = definition.Radius > 0 ? definition.Radius : 10;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;
        var visionSystem = context.ActionContext.PlayerVisionSystem;

        if (visionSystem == null)
        {
            messages.Add("No vision system available.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Reveal area around the target (usually the player)
        visionSystem.RevealAreaAsExplored(target.GridPosition, _radius);

        messages.Add("The dungeon's layout burns into your mind!", Palette.ToHex(Palette.Arcane));
        state.Success = true;
    }
}
