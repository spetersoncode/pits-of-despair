using Godot;
using Godot.Collections;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages all non-player entities in the game.
/// Handles entity tracking and lifecycle (registration, removal, queries).
/// Spawning is handled by SpawnManager.
/// </summary>
public partial class EntityManager : Node
{
    /// <summary>
    /// Emitted when an entity is added to the manager.
    /// </summary>
    [Signal]
    public delegate void EntityAddedEventHandler(BaseEntity entity);

    /// <summary>
    /// Emitted when an entity is removed from the manager.
    /// </summary>
    [Signal]
    public delegate void EntityRemovedEventHandler(BaseEntity entity);

    private readonly System.Collections.Generic.List<BaseEntity> _entities = new();

    /// <summary>
    /// Register an entity with the manager.
    /// Adds entity to scene tree, tracking list, and emits EntityAdded signal.
    /// Called by SpawnManager when spawning entities.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    public void AddEntity(BaseEntity entity)
    {
        AddChild(entity);
        _entities.Add(entity);

        // Subscribe to death if entity has health
        var healthComponent = entity.GetNode<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            // Use lambda to capture entity reference
            healthComponent.Died += () => OnEntityDied(entity);
        }

        EmitSignal(SignalName.EntityAdded, entity);
    }

    /// <summary>
    /// Get all entities managed by this system.
    /// </summary>
    /// <returns>Read-only list of all entities.</returns>
    public System.Collections.Generic.IReadOnlyList<BaseEntity> GetAllEntities()
    {
        return _entities.AsReadOnly();
    }

    /// <summary>
    /// Get entity at a specific grid position.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>Entity at position, or null if none found.</returns>
    public BaseEntity? GetEntityAtPosition(GridPosition position)
    {
        foreach (var entity in _entities)
        {
            if (entity.GridPosition.Equals(position))
            {
                return entity;
            }
        }
        return null;
    }

    /// <summary>
    /// Remove an entity from management and the scene.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void RemoveEntity(BaseEntity entity)
    {
        if (_entities.Remove(entity))
        {
            EmitSignal(SignalName.EntityRemoved, entity);
            entity.QueueFree();
        }
    }

    /// <summary>
    /// Handle entity death by removing it from the game.
    /// </summary>
    private void OnEntityDied(BaseEntity entity)
    {
        RemoveEntity(entity);
    }
}
