using PitsOfDespair.Core;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that reveals a portion of the map around the player.
/// Used by the Scroll of Magic Mapping.
/// </summary>
public class MagicMappingEffect : Effect
{
	public override string Type => "magic_mapping";
	public override string Name => "Magic Mapping";

	private readonly int _radius;

	public MagicMappingEffect()
	{
		_radius = 10; // Default 20x20 area
	}

	public MagicMappingEffect(EffectDefinition definition)
	{
		// Use Radius from definition, default to 10 (20x20 area)
		_radius = definition.Radius > 0 ? definition.Radius : 10;
	}

	public override EffectResult Apply(EffectContext context)
	{
		var target = context.Target;
		var visionSystem = context.ActionContext.PlayerVisionSystem;

		if (visionSystem == null)
		{
			return EffectResult.CreateFailure(
				"No vision system available.",
				Palette.ToHex(Palette.Disabled)
			);
		}

		// Reveal area around the target (usually the player)
		visionSystem.RevealAreaAsExplored(target.GridPosition, _radius);

		return EffectResult.CreateSuccess(
			"The dungeon's layout burns into your mind!",
			Palette.ToHex(Palette.Arcane),
			target
		);
	}
}
