using Godot;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Handles turn-based player input for movement.
/// </summary>
public partial class InputHandler : Node
{
    private Player _player;

    /// <summary>
    /// Sets the player to control.
    /// </summary>
    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public override void _Input(InputEvent @event)
    {
        if (_player == null)
        {
            return;
        }

        // Only process key presses, not key releases or repeats
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
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
}
