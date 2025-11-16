using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Entities;

/// <summary>
/// Represents an item entity in the game world.
/// Items can be spawned on the ground and will eventually be pickable.
/// Items do not have movement, health, AI, or attack components.
/// </summary>
public partial class Item : BaseEntity
{
    /// <summary>
    /// The type of item (e.g., "consumable", "equipment", "quest").
    /// </summary>
    public string ItemType { get; set; } = "generic";

    public Item()
    {
        // Items are passable - entities can walk over them
        Passable = true;
    }

    public override void _Ready()
    {
        // Items are simple entities with no active components
        // Future: Add PickupComponent when implementing inventory system
    }
}
