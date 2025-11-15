using Godot;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// Lightweight component that marks an entity as having vision capabilities.
/// Stores vision range property. Used by Player for fog-of-war and by monsters for AI.
/// </summary>
public partial class VisionComponent : Node
{
    /// <summary>
    /// Maximum range this entity can see in tiles.
    /// Default: 10 tiles (medium range, balanced for roguelike gameplay)
    /// </summary>
    [Export]
    public int VisionRange { get; set; } = 10;

    /// <summary>
    /// Gets the parent entity this component is attached to.
    /// </summary>
    public BaseEntity GetEntity()
    {
        return GetParent<BaseEntity>();
    }
}
