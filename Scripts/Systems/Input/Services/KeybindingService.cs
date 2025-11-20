using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Systems.Input.Services;

/// <summary>
/// Service for querying keybindings and resolving input events to actions.
/// Designed to support future runtime rebinding and persistence.
/// </summary>
public class KeybindingService
{
    /// <summary>
    /// Attempts to get the action bound to the given key event.
    /// </summary>
    /// <param name="keyEvent">The key event to check.</param>
    /// <param name="action">The action bound to this key, if any.</param>
    /// <returns>True if an action is bound to this key.</returns>
    public bool TryGetAction(InputEventKey keyEvent, out InputAction action)
    {
        // Check modifier keybindings first (more specific)
        foreach (var (Action, Key, Ctrl, Shift, Alt) in KeybindingConfig.ModifierKeybindings)
        {
            if (keyEvent.Keycode == Key &&
                keyEvent.CtrlPressed == Ctrl &&
                keyEvent.ShiftPressed == Shift &&
                keyEvent.AltPressed == Alt)
            {
                action = Action;
                return true;
            }
        }

        // Check standard keybindings
        // Note: We don't check modifier state here to allow keys like Key.Question
        // which may have shift state, and to be lenient with standard bindings
        foreach (var (Action, Keys) in KeybindingConfig.Keybindings)
        {
            if (Keys.Contains(keyEvent.Keycode))
            {
                action = Action;
                return true;
            }
        }

        action = default;
        return false;
    }

    /// <summary>
    /// Gets all keys bound to the given action.
    /// </summary>
    public List<Key> GetKeysForAction(InputAction action)
    {
        if (KeybindingConfig.Keybindings.TryGetValue(action, out var keys))
        {
            return new List<Key>(keys);
        }

        // Check modifier keybindings
        foreach (var (Action, Key, _, _, _) in KeybindingConfig.ModifierKeybindings)
        {
            if (Action == action)
            {
                return new List<Key> { Key };
            }
        }

        return new List<Key>();
    }

    /// <summary>
    /// Checks if a key event matches the given action.
    /// </summary>
    public bool IsActionPressed(InputEventKey keyEvent, InputAction action)
    {
        if (TryGetAction(keyEvent, out var boundAction))
        {
            return boundAction == action;
        }
        return false;
    }

    /// <summary>
    /// Checks if the key event is a letter key (A-Z).
    /// Used for item selection in modals.
    /// </summary>
    public bool IsLetterKey(InputEventKey keyEvent, out char letter)
    {
        if (keyEvent.Keycode >= Key.A && keyEvent.Keycode <= Key.Z)
        {
            letter = (char)('A' + (keyEvent.Keycode - Key.A));
            return true;
        }

        letter = default;
        return false;
    }

    /// <summary>
    /// Gets the direction vector for a movement action.
    /// </summary>
    public bool TryGetMovementDirection(InputAction action, out Vector2I direction)
    {
        direction = action switch
        {
            InputAction.MoveNorth => Vector2I.Up,
            InputAction.MoveSouth => Vector2I.Down,
            InputAction.MoveEast => Vector2I.Right,
            InputAction.MoveWest => Vector2I.Left,
            InputAction.MoveNorthEast => new Vector2I(1, -1),
            InputAction.MoveNorthWest => new Vector2I(-1, -1),
            InputAction.MoveSouthEast => new Vector2I(1, 1),
            InputAction.MoveSouthWest => new Vector2I(-1, 1),
            _ => Vector2I.Zero
        };

        return direction != Vector2I.Zero;
    }

    /// <summary>
    /// Checks if the action is a movement action.
    /// </summary>
    public bool IsMovementAction(InputAction action)
    {
        return action switch
        {
            InputAction.MoveNorth or
            InputAction.MoveSouth or
            InputAction.MoveEast or
            InputAction.MoveWest or
            InputAction.MoveNorthEast or
            InputAction.MoveNorthWest or
            InputAction.MoveSouthEast or
            InputAction.MoveSouthWest => true,
            _ => false
        };
    }

    // Future: Add methods for runtime rebinding
    // public void RebindAction(InputAction action, Key newKey) { ... }
    // public void SaveKeybindings() { ... }
    // public void LoadKeybindings() { ... }
}
