using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Systems;

/// <summary>
/// System that validates and executes entity movement requests.
/// This is the only system that directly references MapSystem for movement validation.
/// </summary>
public partial class MovementSystem : Node
{
    private MapSystem? _mapSystem;
    private EntityManager? _entityManager;
    private BaseEntity? _player;

    /// <summary>
    /// Set the MapSystem reference for movement validation.
    /// </summary>
    /// <param name="mapSystem">The map system to use for IsWalkable() checks.</param>
    public void SetMapSystem(MapSystem mapSystem)
    {
        _mapSystem = mapSystem;
    }

    /// <summary>
    /// Set the EntityManager reference for entity collision detection.
    /// </summary>
    /// <param name="entityManager">The entity manager to use for entity queries.</param>
    public void SetEntityManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Set the Player reference for player collision detection.
    /// </summary>
    /// <param name="player">The player entity.</param>
    public void SetPlayer(BaseEntity player)
    {
        _player = player;
    }

    /// <summary>
    /// Register a MovementComponent to listen for movement requests.
    /// Called by GameLevel or EntityManager when entities with MovementComponents are created.
    /// Uses lambda closure to capture component reference for proper signal handling.
    /// </summary>
    /// <param name="component">The MovementComponent to register.</param>
    public void RegisterMovementComponent(MovementComponent component)
    {
        // Use lambda to capture the component reference in a closure
        component.Connect(MovementComponent.SignalName.MoveRequested, Callable.From<Vector2I>((direction) => OnMoveRequested(component, direction)));
    }

    /// <summary>
    /// Handle movement requests from MovementComponents.
    /// Validates the move via MapSystem and updates entity position if valid.
    /// Implements bump-to-attack: if target position has entity with health, triggers attack instead.
    /// </summary>
    /// <param name="component">The MovementComponent that requested the move.</param>
    /// <param name="direction">The direction vector to move.</param>
    private void OnMoveRequested(MovementComponent component, Vector2I direction)
    {
        if (_mapSystem == null)
        {
            GD.PushWarning("MovementSystem: MapSystem not set, cannot validate movement");
            return;
        }

        var entity = component.GetEntity();
        if (entity == null)
        {
            GD.PushWarning("MovementSystem: MovementComponent has no parent entity");
            return;
        }

        var currentPos = entity.GridPosition;
        var targetPos = currentPos.Add(direction);

        // Check if there's an entity at the target position
        var targetEntity = GetEntityAtPosition(targetPos);

        if (targetEntity != null)
        {
            // If the target entity is walkable (like items), allow movement through
            if (targetEntity.IsWalkable)
            {
                // Continue to normal movement validation
                // (Items don't block movement)
            }
            else
            {
                // Target is not walkable (like creatures)
                // Bump-to-attack is PLAYER-ONLY mechanic
                // Creatures use explicit attack actions instead
                if (entity == _player)
                {
                    // Check if target has health (can be attacked)
                    var targetHealth = targetEntity.GetNodeOrNull<HealthComponent>("HealthComponent");
                    if (targetHealth != null)
                    {
                        // Check if player can attack
                        var attackComponent = entity.GetNodeOrNull<AttackComponent>("AttackComponent");
                        if (attackComponent != null)
                        {
                            // Bump-to-attack: request attack instead of moving
                            attackComponent.RequestAttack(targetEntity, 0);
                            return; // Attack replaces movement as turn action
                        }
                    }
                }
                // Target exists and is not walkable - treat as blocked tile
                // (Creatures don't bump-to-attack, they're just blocked)
                return;
            }
        }

        // No entity at target, validate move with MapSystem
        if (_mapSystem.IsWalkable(targetPos))
        {
            // Update entity position
            entity.SetGridPosition(targetPos);
        }
    }

    /// <summary>
    /// Get entity at a specific grid position (checks both player and managed entities).
    /// </summary>
    private BaseEntity? GetEntityAtPosition(GridPosition position)
    {
        // Check player
        if (_player != null && _player.GridPosition.Equals(position))
        {
            return _player;
        }

        // Check managed entities
        if (_entityManager != null)
        {
            return _entityManager.GetEntityAtPosition(position);
        }

        return null;
    }
}
