using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that creates a persistent tile hazard at the target location.
/// Used by poison cloud wands, fire traps, etc.
/// </summary>
public class CreateHazardEffect : Effect
{
    public override string Type => "create_hazard";
    public override string Name => "Create Hazard";

    /// <summary>
    /// Type of hazard to create (e.g., "poison_cloud", "fire").
    /// </summary>
    public string HazardType { get; set; } = "poison_cloud";

    /// <summary>
    /// Duration in turns.
    /// </summary>
    public int Duration { get; set; } = 5;

    /// <summary>
    /// Dice notation for duration (e.g., "2d3"). Overrides Duration if set.
    /// </summary>
    public string? DurationDice { get; set; }

    /// <summary>
    /// Damage dealt per turn to entities in the hazard.
    /// </summary>
    public int DamagePerTurn { get; set; } = 2;

    /// <summary>
    /// Dice notation for damage per turn.
    /// </summary>
    public string? DamageDice { get; set; }

    /// <summary>
    /// Radius of the hazard area (0 = single tile).
    /// </summary>
    public int Radius { get; set; } = 0;

    /// <summary>
    /// Damage type of the hazard.
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Poison;

    /// <summary>
    /// Target position for the hazard (set by targeting system).
    /// </summary>
    public GridPosition? TargetPosition { get; set; }

    public CreateHazardEffect() { }

    public CreateHazardEffect(EffectDefinition definition)
    {
        HazardType = definition.HazardType ?? "poison_cloud";
        Duration = definition.Duration > 0 ? definition.Duration : 5;
        DurationDice = definition.DurationDice;
        DamagePerTurn = definition.Amount > 0 ? definition.Amount : 2;
        DamageDice = definition.Dice;
        Radius = definition.Radius;

        if (!string.IsNullOrEmpty(definition.DamageType))
        {
            if (System.Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
            {
                DamageType = dt;
            }
        }
    }

    public override EffectResult Apply(EffectContext context)
    {
        var hazardManager = context.ActionContext.TileHazardManager;
        if (hazardManager == null)
        {
            return EffectResult.CreateFailure(
                "Cannot create hazard - no hazard system available.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Use target position if set, otherwise use target entity position
        var position = TargetPosition ?? context.Target?.GridPosition;
        if (position == null)
        {
            return EffectResult.CreateFailure(
                "No target position for hazard.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Calculate duration
        int duration = Duration;
        if (!string.IsNullOrEmpty(DurationDice))
        {
            duration = DiceRoller.Roll(DurationDice);
        }
        duration = System.Math.Max(1, duration);

        // Calculate damage per turn
        int damage = DamagePerTurn;
        if (!string.IsNullOrEmpty(DamageDice))
        {
            damage = DiceRoller.Roll(DamageDice);
        }
        damage = System.Math.Max(1, damage);

        // Get color based on hazard type
        var color = GetHazardColor(HazardType);

        // Create hazard(s)
        if (Radius > 0)
        {
            hazardManager.CreateHazardArea(HazardType, position.Value, Radius, duration, damage, DamageType, color);
        }
        else
        {
            hazardManager.CreateHazard(HazardType, position.Value, duration, damage, DamageType, color);
        }

        string message = GetHazardMessage(HazardType, Radius > 0);
        return EffectResult.CreateSuccess(message, Palette.ToHex(color));
    }

    private Color GetHazardColor(string hazardType)
    {
        return hazardType switch
        {
            "poison_cloud" => Palette.Poison,
            "fire" => Palette.Fire,
            "ice" => Palette.Ice,
            "acid" => Palette.Acid,
            _ => Palette.Poison
        };
    }

    private string GetHazardMessage(string hazardType, bool isArea)
    {
        var areaText = isArea ? "A cloud of" : "A wisp of";
        return hazardType switch
        {
            "poison_cloud" => $"{areaText} poisonous vapor fills the air!",
            "fire" => isArea ? "Flames erupt across the area!" : "Fire ignites!",
            "ice" => isArea ? "A freezing mist spreads!" : "Ice forms!",
            "acid" => isArea ? "Acid sprays everywhere!" : "Acid pools!",
            _ => "A hazard appears!"
        };
    }
}
