using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Resource containing movement configuration for entities.
/// Currently minimal but maintains consistent pattern for future expansion.
/// Future properties: speed, movement types, energy cost, etc.
/// </summary>
[GlobalClass]
public partial class MovementData : Resource
{
    // Currently minimal - placeholder for future movement properties
    // Examples of future properties:
    // - MovementSpeed
    // - MovementType (Walk, Fly, Swim, etc.)
    // - EnergyCost
    // - CanMoveDiagonally
}
