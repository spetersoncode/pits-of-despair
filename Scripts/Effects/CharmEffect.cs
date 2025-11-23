using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that permanently charms a target, converting it to the player's faction.
/// The charmed creature will follow and protect the player.
/// </summary>
public class CharmEffect : Effect
{
    public override string Type => "charm";
    public override string Name => "Charm";

    public CharmEffect() { }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var targetName = target.DisplayName;

        // Can't charm entities already in player faction
        if (target.Faction == Faction.Player)
        {
            return EffectResult.CreateFailure(
                $"{targetName} is already friendly!",
                Palette.ToHex(Palette.Disabled)
            );
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

        return EffectResult.CreateSuccess(
            $"{targetName} is charmed and joins your side!",
            Palette.ToHex(Palette.ScrollCharm),
            target
        );
    }
}
