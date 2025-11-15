using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Entities;

/// <summary>
/// The player character.
/// Now uses component-based architecture with MovementComponent.
/// </summary>
public partial class Player : BaseEntity
{
    [Signal]
    public delegate void TurnCompletedEventHandler();

    private MovementComponent? _movementComponent;
    private GridPosition _previousPosition;

    public override void _Ready()
    {
        // Set player properties
        DisplayName = "Player";
        Glyph = '@';
        GlyphColor = Colors.Yellow;

        // Get MovementComponent child
        _movementComponent = GetNode<MovementComponent>("MovementComponent");

        // Initialize attack component with player's default attack
        var attackComponent = GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent != null)
        {
            var playerPunch = new AttackData
            {
                AttackName = "Punch",
                MinDamage = 1,
                MaxDamage = 4,
                Range = 1
            };
            attackComponent.Attacks = new Godot.Collections.Array<AttackData> { playerPunch };
        }

        // Track position changes to emit Moved signal for backwards compatibility
        PositionChanged += OnPositionChanged;
    }

    /// <summary>
    /// Initialize player at spawn position.
    /// Called by GameLevel after map generation.
    /// </summary>
    /// <param name="spawnPosition">The grid position to spawn at.</param>
    public void Initialize(GridPosition spawnPosition)
    {
        SetGridPosition(spawnPosition);
        _previousPosition = spawnPosition;
    }

    /// <summary>
    /// Attempts to move the player in the specified direction.
    /// Delegates to MovementComponent which emits signals for MovementSystem to validate.
    /// </summary>
    /// <param name="direction">Direction to move (use Vector2I for grid directions).</param>
    public void TryMove(Vector2I direction)
    {
        if (_movementComponent == null)
        {
            GD.PushWarning("Player: MovementComponent not found!");
            return;
        }

        // Store current position to check if move succeeded
        _previousPosition = GridPosition;

        // Request move via component - MovementSystem will validate and update position
        _movementComponent.RequestMove(direction);
    }

    /// <summary>
    /// Handle position changes to emit turn completion.
    /// </summary>
    private void OnPositionChanged(int x, int y)
    {
        // Only emit turn completed if position actually changed
        if (x != _previousPosition.X || y != _previousPosition.Y)
        {
            EmitSignal(SignalName.TurnCompleted);
            _previousPosition = new GridPosition(x, y);
        }
    }

    /// <summary>
    /// Gets the world position for rendering (based on tile size).
    /// </summary>
    public Vector2 GetWorldPosition(int tileSize)
    {
        return GridPosition.ToWorld(tileSize);
    }

    /// <summary>
    /// Property for backwards compatibility with renderer.
    /// </summary>
    public GridPosition CurrentPosition => GridPosition;
}
