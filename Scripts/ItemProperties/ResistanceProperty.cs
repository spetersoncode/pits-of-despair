using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.ItemProperties;

/// <summary>
/// Property that provides resistance or vulnerability to specific damage types.
/// Resistance reduces incoming damage, vulnerability increases it.
/// </summary>
public class ResistanceProperty : ItemProperty, IResistanceProperty
{
    private readonly DamageType _damageType;
    private readonly float _multiplier;

    public override string Name => _multiplier < 1f
        ? $"{_damageType.ToString().ToLower()} resistance"
        : $"{_damageType.ToString().ToLower()} vulnerability";

    public override string TypeId => $"resistance_{_damageType}";
    public override ItemType ValidItemTypes => ItemType.Ring;

    /// <summary>
    /// Creates a new resistance property.
    /// </summary>
    /// <param name="damageType">The damage type this affects.</param>
    /// <param name="multiplier">Damage multiplier (0.5 = half damage, 2.0 = double damage).</param>
    public ResistanceProperty(DamageType damageType, float multiplier = 0.5f)
    {
        _damageType = damageType;
        _multiplier = multiplier;
    }

    public bool AppliesToDamageType(DamageType damageType)
    {
        return damageType == _damageType;
    }

    public float GetDamageMultiplier()
    {
        return _multiplier;
    }

    public override string? GetSuffix()
    {
        return _multiplier < 1f
            ? $"of {_damageType.ToString().ToLower()} resistance"
            : $"of {_damageType.ToString().ToLower()} vulnerability";
    }

    public override Color? GetColorOverride()
    {
        // Colors based on damage type
        return _damageType switch
        {
            DamageType.Fire => Palette.Fire,
            DamageType.Cold => Palette.Ice,
            DamageType.Lightning => Palette.Lightning,
            DamageType.Poison => Palette.Poison,
            DamageType.Acid => Palette.Acid,
            DamageType.Necrotic => Palette.Shadow,
            _ => null
        };
    }
}
