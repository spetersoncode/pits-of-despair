using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Systems;

namespace PitsOfDespair.Systems;

/// <summary>
/// Handles turn-based player input for movement.
/// </summary>
public partial class InputHandler : Node
{
    private Player _player;
    private TurnManager _turnManager;

    /// <summary>
    /// Sets the player to control.
    /// </summary>
    public void SetPlayer(Player player)
    {
        // Unsubscribe from old player if exists
        if (_player != null)
        {
            _player.TurnCompleted -= OnPlayerTurnCompleted;
        }

        _player = player;

        // Subscribe to new player's turn completion
        if (_player != null)
        {
            _player.TurnCompleted += OnPlayerTurnCompleted;
        }
    }

    /// <summary>
    /// Sets the turn manager to coordinate turn flow.
    /// </summary>
    public void SetTurnManager(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }

    public override void _ExitTree()
    {
        // Clean up signal connections
        if (_player != null)
        {
            _player.TurnCompleted -= OnPlayerTurnCompleted;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_player == null || _turnManager == null)
        {
            return;
        }

        // Only process input during player turn
        if (!_turnManager.IsPlayerTurn)
        {
            return;
        }

        // Only process key presses, not key releases or repeats
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Check for wait action first
            if (IsWaitKey(keyEvent.Keycode))
            {
                _player.Wait();
                GetViewport().SetInputAsHandled();
                return;
            }

            Vector2I direction = GetDirectionFromKey(keyEvent.Keycode);

            if (direction != Vector2I.Zero)
            {
                _player.TryMove(direction);
                // Accept the event to prevent it from propagating
                GetViewport().SetInputAsHandled();
            }
        }
    }

    /// <summary>
    /// Called when the player completes their turn (after successful action).
    /// </summary>
    private void OnPlayerTurnCompleted()
    {
        _turnManager?.EndPlayerTurn();
    }

    /// <summary>
    /// Converts a keycode to a movement direction.
    /// </summary>
    private Vector2I GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            // Arrow keys (cardinal directions)
            Key.Up => Vector2I.Up,
            Key.Down => Vector2I.Down,
            Key.Left => Vector2I.Left,
            Key.Right => Vector2I.Right,

            // WASD (cardinal directions)
            Key.W => Vector2I.Up,
            Key.S => Vector2I.Down,
            Key.A => Vector2I.Left,
            Key.D => Vector2I.Right,

            // Diagonal movement (QEZC)
            Key.Q => new Vector2I(-1, -1),  // Up-Left
            Key.E => new Vector2I(1, -1),   // Up-Right
            Key.Z => new Vector2I(-1, 1),   // Down-Left
            Key.C => new Vector2I(1, 1),    // Down-Right

            // Numpad (8 directions)
            Key.Kp8 => Vector2I.Up,         // North
            Key.Kp2 => Vector2I.Down,       // South
            Key.Kp4 => Vector2I.Left,       // West
            Key.Kp6 => Vector2I.Right,      // East
            Key.Kp7 => new Vector2I(-1, -1), // Northwest
            Key.Kp9 => new Vector2I(1, -1),  // Northeast
            Key.Kp1 => new Vector2I(-1, 1),  // Southwest
            Key.Kp3 => new Vector2I(1, 1),   // Southeast

            _ => Vector2I.Zero
        };
    }

    /// <summary>
    /// Checks if the given keycode is a wait action key.
    /// </summary>
    private bool IsWaitKey(Key keycode)
    {
        return keycode switch
        {
            Key.Space => true,   // Spacebar
            Key.Kp5 => true,     // Numpad 5
            _ => false
        };
    }
}
