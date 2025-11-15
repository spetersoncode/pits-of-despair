using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Factory for creating entities from EntityData resources.
/// Handles component instantiation based on data configuration.
/// </summary>
public partial class EntityFactory : Node
{
    /// <summary>
    /// Create an entity from EntityData and position it on the grid.
    /// </summary>
    /// <param name="data">The EntityData resource defining the entity.</param>
    /// <param name="position">The grid position to place the entity.</param>
    /// <returns>The created and configured BaseEntity.</returns>
    public BaseEntity CreateEntity(EntityData data, GridPosition position)
    {
        // Create base entity
        var entity = new BaseEntity
        {
            GridPosition = position,
            Glyph = data.Glyph.Length > 0 ? data.Glyph[0] : '?', // Convert string to char
            GlyphColor = data.GlyphColor,
            Name = data.Name // Set node name for debugging
        };

        // Add MovementComponent if movement data is present
        if (data.Movement != null)
        {
            var movementComponent = new MovementComponent
            {
                Name = "MovementComponent"
            };
            entity.AddChild(movementComponent);
        }

        // Future component additions based on data:
        // if (data.Health != null) { entity.AddChild(new HealthComponent()); }
        // if (data.AI != null) { entity.AddChild(new AIComponent()); }
        // if (data.Combat != null) { entity.AddChild(new CombatComponent()); }

        return entity;
    }
}
