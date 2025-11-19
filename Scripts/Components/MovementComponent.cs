using Godot;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// Component that enables entity movement.
/// Emits signals for movement requests rather than executing moves directly.
/// The MovementSystem handles validation and execution.
/// </summary>
public partial class MovementComponent : Node
{
    /// <summary>
    /// Emitted when the entity requests to move in a direction.
    /// The MovementSystem subscribes to this signal to validate and execute moves.
    /// </summary>
    [Signal]
    public delegate void MoveRequestedEventHandler(Vector2I direction);

    private BaseEntity? _entity;

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
    }

    /// <summary>
    /// Request a move in the specified direction.
    /// Emits MoveRequested signal for the MovementSystem to process.
    /// </summary>
    /// <param name="direction">The direction vector (e.g., Vector2I.Right, Vector2I.Up)</param>
    public void RequestMove(Vector2I direction)
    {
        EmitSignal(SignalName.MoveRequested, direction);
    }

    /// <summary>
    /// Get the parent entity this component belongs to.
    /// </summary>
    /// <returns>The parent BaseEntity, or null if not properly parented.</returns>
    public BaseEntity? GetEntity()
    {
        return _entity;
    }
}
