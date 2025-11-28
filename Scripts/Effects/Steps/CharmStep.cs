using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Effects.Composition;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that permanently charms a target, converting it to the player's faction.
/// The charmed creature will follow and protect the player.
/// </summary>
public class CharmStep : IEffectStep
{
    public CharmStep(StepDefinition definition)
    {
        // No properties needed currently
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var target = context.Target;

        // Can't charm entities already in player faction
        if (target.Faction == Faction.Player)
        {
            messages.Add($"{target.DisplayName} is already friendly!", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Convert to player faction
        target.Faction = Faction.Player;
        target.GlyphColor = Palette.Player;

        // Set up AI to follow and protect player
        var aiComponent = target.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.ProtectionTarget = context.ActionContext.Player;
        }

        messages.Add($"{target.DisplayName} is charmed and joins your side!", Palette.ToHex(Palette.ScrollCharm));
        state.Success = true;
    }
}
