using Godot;
using Godot.Collections;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using Faction = PitsOfDespair.Core.Faction;

namespace PitsOfDespair.Systems.Entity;

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
    private readonly System.Collections.Generic.Dictionary<GridPosition, System.Collections.Generic.Dictionary<EntityLayer, BaseEntity>> _positionCache = new();
    private readonly System.Collections.Generic.List<(BaseEntity entity, HealthComponent health)> _healthConnections = new();
    private Player _player;

    /// <summary>
    /// Sets the player reference for XP awarding.
    /// </summary>
    public void SetPlayer(Player player)
    {
        _player = player;
    }

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

        // Add to multi-layer position cache
        if (!_positionCache.TryGetValue(entity.GridPosition, out var layerDict))
        {
            layerDict = new System.Collections.Generic.Dictionary<EntityLayer, BaseEntity>();
            _positionCache[entity.GridPosition] = layerDict;
        }
        layerDict[entity.Layer] = entity;

        // Subscribe to position changes to keep cache updated
        entity.Connect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>((x, y) => OnEntityPositionChanged(entity, new GridPosition(x, y))));

        // Subscribe to death if entity has health
        var healthComponent = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            // Use lambda to capture entity reference
            healthComponent.Connect(HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
            _healthConnections.Add((entity, healthComponent));
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
    /// Get the top-layer entity at a specific grid position.
    /// Returns entity with highest render priority (Player > Creature > Item > Decoration).
    /// Uses position cache for O(1) lookup.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>Top-layer entity at position, or null if none found.</returns>
    public BaseEntity? GetEntityAtPosition(GridPosition position)
    {
        // Check player first (tracked separately from other entities)
        if (_player != null && _player.GridPosition == position)
        {
            return _player;
        }

        if (_positionCache.TryGetValue(position, out var layerDict))
        {
            // Check layers in priority order (highest first)
            if (layerDict.TryGetValue(EntityLayer.Creature, out var creature))
                return creature;
            if (layerDict.TryGetValue(EntityLayer.Item, out var item))
                return item;
            if (layerDict.TryGetValue(EntityLayer.Feature, out var feature))
                return feature;
            if (layerDict.TryGetValue(EntityLayer.Decoration, out var decoration))
                return decoration;
        }
        return null;
    }

    /// <summary>
    /// Get all entities at a specific grid position.
    /// Uses position cache for O(1) lookup.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>List of all entities at position.</returns>
    public System.Collections.Generic.List<BaseEntity> GetEntitiesAtPosition(GridPosition position)
    {
        var result = new System.Collections.Generic.List<BaseEntity>();

        if (_positionCache.TryGetValue(position, out var layerDict))
            result.AddRange(layerDict.Values);

        if (_player != null && _player.GridPosition == position)
            result.Add(_player);

        return result;
    }

    /// <summary>
    /// Get item entity at a specific grid position.
    /// Uses position cache for O(1) lookup.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>Item entity at position, or null if no item found.</returns>
    public BaseEntity? GetItemAtPosition(GridPosition position)
    {
        if (_positionCache.TryGetValue(position, out var layerDict))
        {
            layerDict.TryGetValue(EntityLayer.Item, out var item);
            return item;
        }
        return null;
    }

    /// <summary>
    /// Get entity at a specific position and layer.
    /// Uses position cache for O(1) lookup.
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <param name="layer">The entity layer to query.</param>
    /// <returns>Entity at position and layer, or null if not found.</returns>
    public BaseEntity? GetEntityAtLayer(GridPosition position, EntityLayer layer)
    {
        // Special case: Player layer
        if (layer == EntityLayer.Player)
            return (_player != null && _player.GridPosition == position) ? _player : null;

        // Check cache
        if (_positionCache.TryGetValue(position, out var layerDict))
        {
            layerDict.TryGetValue(layer, out var entity);
            return entity;
        }
        return null;
    }

    /// <summary>
    /// Get an entity by its instance ID hash.
    /// Used for tracking entities across frames (e.g., aura targets).
    /// </summary>
    /// <param name="idHash">Hash code of the entity's instance ID</param>
    /// <returns>Entity with matching ID, or null if not found</returns>
    public BaseEntity? GetEntityById(int idHash)
    {
        // Check player first
        if (_player != null && _player.GetInstanceId().GetHashCode() == idHash)
            return _player;

        return _entities.FirstOrDefault(e => e.GetInstanceId().GetHashCode() == idHash);
    }

    /// <summary>
    /// Get all entities within a radius of a position.
    /// Uses Chebyshev distance (king's move distance).
    /// </summary>
    /// <param name="center">Center position</param>
    /// <param name="radius">Maximum distance (inclusive)</param>
    /// <returns>List of entities within radius</returns>
    public System.Collections.Generic.List<BaseEntity> GetEntitiesInRadius(GridPosition center, int radius)
    {
        var result = new System.Collections.Generic.List<BaseEntity>();

        // Check player
        if (_player != null)
        {
            int playerDist = Helpers.DistanceHelper.ChebyshevDistance(center, _player.GridPosition);
            if (playerDist <= radius)
                result.Add(_player);
        }

        // Check all managed entities
        foreach (var entity in _entities)
        {
            int dist = Helpers.DistanceHelper.ChebyshevDistance(center, entity.GridPosition);
            if (dist <= radius)
                result.Add(entity);
        }

        return result;
    }

    /// <summary>
    /// Checks if a position is occupied by any entity (not including player).
    /// </summary>
    /// <param name="position">The grid position to check.</param>
    /// <returns>True if occupied by an entity, false otherwise.</returns>
    public bool IsPositionOccupied(GridPosition position)
    {
        return _positionCache.TryGetValue(position, out var layerDict) && layerDict.Count > 0;
    }

    /// <summary>
    /// Remove an entity from management and the scene.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void RemoveEntity(BaseEntity entity)
    {
        if (_entities.Remove(entity))
        {
            // Remove from multi-layer position cache
            if (_positionCache.TryGetValue(entity.GridPosition, out var layerDict))
            {
                layerDict.Remove(entity.Layer);
                if (layerDict.Count == 0)
                    _positionCache.Remove(entity.GridPosition);
            }

            EmitSignal(SignalName.EntityRemoved, entity);
            entity.QueueFree();
        }
    }

    /// <summary>
    /// Handle entity death by removing it from the game.
    /// Awards XP to player if the dead entity was killed by a player faction entity.
    /// </summary>
    private void OnEntityDied(BaseEntity entity)
    {
        // Award XP if player exists and this is a creature (not the player, has stats)
        if (_player != null && entity != _player)
        {
            var healthComponent = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            var creatureStats = entity.GetNodeOrNull<StatsComponent>("StatsComponent");
            var playerStats = _player.GetNodeOrNull<StatsComponent>("StatsComponent");

            // Only award XP if the killer was a player faction entity
            bool killedByPlayerFaction = healthComponent?.LastDamageSource?.Faction == Faction.Player;

            if (creatureStats != null && playerStats != null && killedByPlayerFaction)
            {
                // Calculate XP reward using threat-based formula
                int xpAwarded = CalculateXPReward(creatureStats.Threat, playerStats.Level);

                // Award XP to player
                playerStats.GainExperience(xpAwarded);
            }
        }

        RemoveEntity(entity);
    }

    /// <summary>
    /// Gets the XP reward that would be awarded for defeating the given entity.
    /// Returns 0 if entity is the player, has no stats, or if player doesn't exist.
    /// </summary>
    public int GetXPReward(BaseEntity entity)
    {
        if (_player == null || entity == _player)
            return 0;

        var creatureStats = entity.GetNodeOrNull<StatsComponent>("StatsComponent");
        var playerStats = _player.GetNodeOrNull<StatsComponent>("StatsComponent");

        if (creatureStats == null || playerStats == null)
            return 0;

        return CalculateXPReward(creatureStats.Threat, playerStats.Level);
    }

    /// <summary>
    /// Calculates XP reward based on creature threat vs player level.
    /// Formula: baseXP × (1.0 + (creatureThreat - playerLevel) × 0.3) with 20% minimum floor
    /// Base XP: creatureThreat × 8
    /// </summary>
    private int CalculateXPReward(int creatureThreat, int playerLevel)
    {
        // Base XP scales with creature threat rating
        int baseXP = creatureThreat * 8;

        // Delta multiplier: +30% per threat above player level, -30% per threat below
        float deltaMultiplier = 1.0f + (creatureThreat - playerLevel) * 0.3f;

        // Apply minimum floor of 20% (never less than 1 XP for level 1+ creatures)
        float finalMultiplier = Mathf.Max(0.2f, deltaMultiplier);

        // Calculate final XP (rounded)
        int xp = Mathf.RoundToInt(baseXP * finalMultiplier);

        return Mathf.Max(1, xp); // Always award at least 1 XP
    }

    /// <summary>
    /// Updates position cache when an entity moves.
    /// Ignores position changes from removed entities to prevent ghost cache entries.
    /// </summary>
    private void OnEntityPositionChanged(BaseEntity entity, GridPosition newPosition)
    {
        // Ignore position changes from removed entities (prevents ghost entries)
        if (!_entities.Contains(entity))
            return;

        // Remove from old position (search all positions for this entity)
        foreach (var kvp in _positionCache.ToList())
        {
            if (kvp.Value.TryGetValue(entity.Layer, out var cached) && cached == entity)
            {
                kvp.Value.Remove(entity.Layer);
                if (kvp.Value.Count == 0)
                    _positionCache.Remove(kvp.Key);
                break;
            }
        }

        // Add to new position
        if (!_positionCache.TryGetValue(newPosition, out var layerDict))
        {
            layerDict = new System.Collections.Generic.Dictionary<EntityLayer, BaseEntity>();
            _positionCache[newPosition] = layerDict;
        }
        layerDict[entity.Layer] = entity;
    }

    public override void _ExitTree()
    {
        // Disconnect from all entities
        foreach (var entity in _entities.ToArray())
        {
            if (entity != null && GodotObject.IsInstanceValid(entity))
            {
                entity.Disconnect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>((x, y) => OnEntityPositionChanged(entity, new GridPosition(x, y))));
            }
        }

        // Disconnect from all health components
        foreach (var (entity, healthComponent) in _healthConnections)
        {
            if (healthComponent != null && GodotObject.IsInstanceValid(healthComponent))
            {
                healthComponent.Disconnect(HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
            }
        }
        _healthConnections.Clear();
    }
}
