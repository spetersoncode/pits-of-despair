using Godot;
using Godot.Collections;
using System.Linq;
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
    private readonly System.Collections.Generic.Dictionary<GridPosition, BaseEntity> _positionCache = new();

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

        // Add to position cache
        _positionCache[entity.GridPosition] = entity;

        // Subscribe to position changes to keep cache updated
        entity.Connect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>((x, y) => OnEntityPositionChanged(entity, new GridPosition(x, y))));

        // Subscribe to death if entity has health
        var healthComponent = entity.GetNode<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            // Use lambda to capture entity reference
            healthComponent.Connect(HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
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
    /// Uses position cache for O(1) lookup.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>Entity at position, or null if none found.</returns>
    public BaseEntity? GetEntityAtPosition(GridPosition position)
    {
        if (_positionCache.TryGetValue(position, out BaseEntity entity))
        {
            return entity;
        }
        return null;
    }

    /// <summary>
    /// Checks if a position is occupied by any entity (not including player).
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>True if occupied by an entity, false otherwise.</returns>
    public bool IsPositionOccupied(GridPosition position)
    {
        return _positionCache.ContainsKey(position);
    }

    /// <summary>
    /// Remove an entity from management and the scene.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void RemoveEntity(BaseEntity entity)
    {
        if (_entities.Remove(entity))
        {
            // Remove from position cache
            _positionCache.Remove(entity.GridPosition);

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

    /// <summary>
    /// Updates position cache when an entity moves.
    /// </summary>
    private void OnEntityPositionChanged(BaseEntity entity, GridPosition newPosition)
    {
        // Remove old position from cache (need to find it)
        // This is safe because GridPosition struct equality works correctly
        var oldPositionEntry = _positionCache.FirstOrDefault(kvp => kvp.Value == entity);
        if (oldPositionEntry.Value != null)
        {
            _positionCache.Remove(oldPositionEntry.Key);
        }

        // Add new position to cache
        _positionCache[newPosition] = entity;
    }
}
