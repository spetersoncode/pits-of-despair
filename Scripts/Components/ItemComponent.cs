using Godot;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// Component marking an entity as a collectible item.
/// Items are passable and can be picked up by the player.
/// </summary>
public partial class ItemComponent : Node
{
    /// <summary>
    /// Reference to the item's data definition
    /// Used for inventory stacking (items with same DataFileId stack)
    /// </summary>
    public ItemData ItemData { get; set; } = null!;

    private BaseEntity? _entity;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();

        // Items are passable (entities can walk over them)
        if (_entity != null)
        {
            _entity.Passable = true;
        }
    }

    /// <summary>
    /// Get the parent entity
    /// </summary>
    public BaseEntity? GetEntity()
    {
        return _entity;
    }
}
