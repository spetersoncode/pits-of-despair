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
        ? $"{_damageType} Resistance"
        : $"{_damageType} Vulnerability";

    public override string TypeId => $"resistance_{_damageType}";

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
            ? $"of {_damageType} Resistance"
            : $"of {_damageType} Vulnerability";
    }
}
