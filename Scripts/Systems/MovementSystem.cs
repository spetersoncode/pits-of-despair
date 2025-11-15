using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems;

/// <summary>
/// System that validates and executes entity movement requests.
/// This is the only system that directly references MapSystem for movement validation.
/// </summary>
public partial class MovementSystem : Node
{
    private MapSystem? _mapSystem;

    /// <summary>
    /// Set the MapSystem reference for movement validation.
    /// </summary>
    /// <param name="mapSystem">The map system to use for IsWalkable() checks.</param>
    public void SetMapSystem(MapSystem mapSystem)
    {
        _mapSystem = mapSystem;
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
        component.MoveRequested += (direction) => OnMoveRequested(component, direction);
    }

    /// <summary>
    /// Handle movement requests from MovementComponents.
    /// Validates the move via MapSystem and updates entity position if valid.
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

        // Validate move with MapSystem
        if (_mapSystem.IsWalkable(targetPos))
        {
            // Update entity position
            entity.SetGridPosition(targetPos);
        }
    }
}
