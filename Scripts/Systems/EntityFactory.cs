using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Factory for creating entities from JSON data.
/// Handles component instantiation based on data configuration.
/// </summary>
public partial class EntityFactory : Node
{
    private DataLoader _dataLoader;

    public override void _Ready()
    {
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
    }

    /// <summary>
    /// Create an entity from creature ID and position it on the grid.
    /// </summary>
    /// <param name="creatureId">The creature ID (JSON filename without extension).</param>
    /// <param name="position">The grid position to place the entity.</param>
    /// <returns>The created and configured BaseEntity, or null if creature not found.</returns>
    public BaseEntity CreateEntity(string creatureId, GridPosition position)
    {
        var data = _dataLoader.GetCreature(creatureId);
        if (data == null)
        {
            GD.PrintErr($"EntityFactory: Failed to create entity '{creatureId}' - creature data not found");
            return null;
        }

        return CreateEntityFromJsonData(data, position);
    }

    /// <summary>
    /// Create an item from item ID and position it on the grid.
    /// </summary>
    /// <param name="itemId">The item ID (JSON filename without extension).</param>
    /// <param name="position">The grid position to place the item.</param>
    /// <returns>The created and configured Item, or null if item not found.</returns>
    public Item CreateItem(string itemId, GridPosition position)
    {
        var data = _dataLoader.GetItem(itemId);
        if (data == null)
        {
            GD.PrintErr($"EntityFactory: Failed to create item '{itemId}' - item data not found");
            return null;
        }

        // Create item entity
        var item = new Item
        {
            GridPosition = position,
            DisplayName = data.Name,
            Glyph = data.Glyph.Length > 0 ? data.Glyph[0] : '?',
            GlyphColor = data.GetColor(),
            ItemType = data.ItemType,
            ItemData = data, // Store full item data for inventory system
            Name = data.Name // Set node name for debugging
        };

        // Future: Add PickupComponent when implementing inventory system

        return item;
    }

    /// <summary>
    /// Create an entity from EntityData and position it on the grid.
    /// DEPRECATED: Use CreateEntity(string creatureId, GridPosition position) instead.
    /// </summary>
    /// <param name="data">The EntityData resource defining the entity.</param>
    /// <param name="position">The grid position to place the entity.</param>
    /// <returns>The created and configured BaseEntity.</returns>
    [System.Obsolete("Use CreateEntity(string creatureId, GridPosition position) instead")]
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

    /// <summary>
    /// Create an entity from JSON creature data.
    /// </summary>
    private BaseEntity CreateEntityFromJsonData(CreatureData data, GridPosition position)
    {
        // Create base entity
        var entity = new BaseEntity
        {
            GridPosition = position,
            DisplayName = data.Name,
            Glyph = data.Glyph.Length > 0 ? data.Glyph[0] : '?', // Convert string to char
            GlyphColor = data.GetColor(),
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
            // Convert attack data to AttackData array
            var attacks = new Godot.Collections.Array<AttackData>();
            foreach (var attackData in data.Attacks)
            {
                var attack = new AttackData
                {
                    Name = attackData.Name,
                    MinDamage = attackData.MinDamage,
                    MaxDamage = attackData.MaxDamage,
                    Range = attackData.Range
                };
                attacks.Add(attack);
            }

            var attackComponent = new AttackComponent
            {
                Name = "AttackComponent",
                Attacks = attacks
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
