using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Systems;

/// <summary>
/// Represents a persistent hazard on a tile that affects entities each turn.
/// Examples: poison cloud, fire, ice patches.
/// </summary>
public class TileHazard
{
    /// <summary>
    /// The type of hazard (e.g., "poison_cloud", "fire", "ice").
    /// </summary>
    public string HazardType { get; set; }

    /// <summary>
    /// Grid position of the hazard.
    /// </summary>
    public GridPosition Position { get; set; }

    /// <summary>
    /// Remaining turns before the hazard expires.
    /// </summary>
    public int RemainingTurns { get; set; }

    /// <summary>
    /// Damage dealt to entities occupying this tile each turn.
    /// </summary>
    public int DamagePerTurn { get; set; }

    /// <summary>
    /// Damage type for the hazard.
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Poison;

    /// <summary>
    /// Color used for the visual effect.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Reference to the visual node for this hazard (managed by TileHazardManager).
    /// </summary>
    public ColorRect? VisualNode { get; set; }

    /// <summary>
    /// The shader material for animating the visual.
    /// </summary>
    public ShaderMaterial? Material { get; set; }

    public TileHazard(string hazardType, GridPosition position, int duration, int damagePerTurn, DamageType damageType, Color color)
    {
        HazardType = hazardType;
        Position = position;
        RemainingTurns = duration;
        DamagePerTurn = damagePerTurn;
        DamageType = damageType;
        Color = color;
    }
}
