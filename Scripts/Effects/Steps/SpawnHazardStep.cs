using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects.Steps;

/// <summary>
/// Step that creates a persistent tile hazard at the target location.
/// </summary>
public class SpawnHazardStep : IEffectStep
{
    private readonly string _hazardType;
    private readonly int _duration;
    private readonly string? _durationDice;
    private readonly int _damagePerTurn;
    private readonly string? _damageDice;
    private readonly int _radius;
    private readonly DamageType _damageType;

    public SpawnHazardStep(StepDefinition definition)
    {
        _hazardType = definition.HazardType ?? "poison_cloud";
        _duration = definition.Duration > 0 ? definition.Duration : 5;
        _durationDice = definition.DurationDice;
        _damagePerTurn = definition.Amount > 0 ? definition.Amount : 2;
        _damageDice = definition.Dice;
        _radius = definition.Radius;

        _damageType = DamageType.Poison;
        if (!string.IsNullOrEmpty(definition.DamageType))
        {
            if (System.Enum.TryParse<DamageType>(definition.DamageType, ignoreCase: true, out var dt))
            {
                _damageType = dt;
            }
        }
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        var hazardManager = context.ActionContext.TileHazardManager;
        if (hazardManager == null)
        {
            messages.Add("Cannot create hazard - no hazard system available.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Use target position if set, otherwise use target entity position
        var position = context.TargetPosition ?? context.Target?.GridPosition;
        if (position == null)
        {
            messages.Add("No target position for hazard.", Palette.ToHex(Palette.Disabled));
            return;
        }

        // Calculate duration
        int duration = _duration;
        if (!string.IsNullOrEmpty(_durationDice))
        {
            duration = DiceRoller.Roll(_durationDice);
        }
        duration = System.Math.Max(1, duration);

        // Calculate damage per turn
        int damage = _damagePerTurn;
        if (!string.IsNullOrEmpty(_damageDice))
        {
            damage = DiceRoller.Roll(_damageDice);
        }
        damage = System.Math.Max(1, damage);

        // Get color based on hazard type
        var color = GetHazardColor(_hazardType);

        // Create hazard(s)
        if (_radius > 0)
        {
            hazardManager.CreateHazardArea(_hazardType, position.Value, _radius, duration, damage, _damageType, color);
        }
        else
        {
            hazardManager.CreateHazard(_hazardType, position.Value, duration, damage, _damageType, color);
        }

        string message = GetHazardMessage(_hazardType, _radius > 0);
        messages.Add(message, Palette.ToHex(color));
        state.Success = true;
    }

    private Godot.Color GetHazardColor(string hazardType)
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
