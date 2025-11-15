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
            DisplayName = data.Name,
            Glyph = data.Glyph.Length > 0 ? data.Glyph[0] : '?', // Convert string to char
            GlyphColor = data.GlyphColor,
            Name = data.Name // Set node name for debugging
        };

        // Add MovementComponent if entity can move
        if (data.HasMovement)
        {
            var movementComponent = new MovementComponent
            {
                Name = "MovementComponent"
            };
            entity.AddChild(movementComponent);
        }

        // Add VisionComponent if vision range is specified
        if (data.VisionRange > 0)
        {
            var visionComponent = new VisionComponent
            {
                Name = "VisionComponent",
                VisionRange = data.VisionRange
            };
            entity.AddChild(visionComponent);
        }

        // Add HealthComponent if max HP is specified
        if (data.MaxHP > 0)
        {
            var healthComponent = new HealthComponent
            {
                Name = "HealthComponent",
                MaxHP = data.MaxHP
            };
            entity.AddChild(healthComponent);
        }

        // Add AttackComponent if attacks are specified
        if (data.Attacks.Count > 0)
        {
            var attackComponent = new AttackComponent
            {
                Name = "AttackComponent",
                Attacks = data.Attacks
            };
            entity.AddChild(attackComponent);
        }

        // Add AIComponent if entity has AI behavior enabled
        if (data.HasAI)
        {
            var aiComponent = new AIComponent
            {
                Name = "AIComponent"
            };
            entity.AddChild(aiComponent);

            // Initialize AI with spawn position
            // Note: Must be called after AddChild so component is in tree
            aiComponent.Initialize(position);
        }

        return entity;
    }
}
