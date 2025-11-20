using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Systems.Input;

/// <summary>
/// Centralized keybinding configuration for the entire game.
/// Designed to support future runtime rebinding and persistence.
/// </summary>
public static class KeybindingConfig
{
    /// <summary>
    /// Primary keybinding definitions. Each action can have multiple keys.
    /// First key in the list is the "primary" binding shown in help text.
    /// </summary>
    public static readonly Dictionary<InputAction, List<Key>> Keybindings = new()
    {
        // Movement - Arrow Keys
        { InputAction.MoveNorth, new() { Key.Up, Key.Kp8 } },
        { InputAction.MoveSouth, new() { Key.Down, Key.Kp2 } },
        { InputAction.MoveEast, new() { Key.Right, Key.Kp6 } },
        { InputAction.MoveWest, new() { Key.Left, Key.Kp4 } },
        { InputAction.MoveNorthEast, new() { Key.Kp9 } },
        { InputAction.MoveNorthWest, new() { Key.Kp7 } },
        { InputAction.MoveSouthEast, new() { Key.Kp3 } },
        { InputAction.MoveSouthWest, new() { Key.Kp1 } },

        // Basic Actions
        { InputAction.Wait, new() { Key.Space, Key.Kp5 } },
        { InputAction.Pickup, new() { Key.G } },

        // Combat
        { InputAction.FireRanged, new() { Key.F } },

        // Menu Toggles
        { InputAction.ToggleInventory, new() { Key.I } },
        { InputAction.ToggleActivate, new() { Key.A } },
        { InputAction.ToggleDrop, new() { Key.D } },
        { InputAction.ToggleEquip, new() { Key.E } },
        { InputAction.ToggleExamine, new() { Key.X } },
        // Note: Help uses Shift+Slash, handled via ModifierKeybindings

        // Debug
        { InputAction.ToggleDebugConsole, new() { Key.Slash } },

        // Cursor Targeting
        { InputAction.CursorConfirm, new() { Key.Enter, Key.Space, Key.F } },
        { InputAction.CursorCancel, new() { Key.Escape } },
        { InputAction.CursorCycleNext, new() { Key.Tab, Key.KpAdd } },
        { InputAction.CursorCyclePrevious, new() { Key.KpSubtract } },

        // Modal Common
        { InputAction.ModalClose, new() { Key.Escape } },

        // Level-Up Stat Selection
        { InputAction.SelectStrength, new() { Key.S } },
        { InputAction.SelectAgility, new() { Key.A } },
        { InputAction.SelectEndurance, new() { Key.E } },
        { InputAction.SelectWill, new() { Key.W } },
    };

    /// <summary>
    /// Special keybindings that require modifier keys.
    /// Format: (Action, Key, RequiresCtrl, RequiresShift, RequiresAlt)
    /// </summary>
    public static readonly List<(InputAction Action, Key Key, bool Ctrl, bool Shift, bool Alt)> ModifierKeybindings = new()
    {
        (InputAction.ToggleDebug, Key.D, Ctrl: true, Shift: false, Alt: false),
        (InputAction.ToggleHelp, Key.Slash, Ctrl: false, Shift: true, Alt: false),
    };

    /// <summary>
    /// Gets a human-readable string representation of a key.
    /// </summary>
    public static string GetKeyDisplayName(Key key)
    {
        return key switch
        {
            Key.Up => "↑",
            Key.Down => "↓",
            Key.Left => "←",
            Key.Right => "→",
            Key.Kp1 => "Numpad 1",
            Key.Kp2 => "Numpad 2",
            Key.Kp3 => "Numpad 3",
            Key.Kp4 => "Numpad 4",
            Key.Kp5 => "Numpad 5",
            Key.Kp6 => "Numpad 6",
            Key.Kp7 => "Numpad 7",
            Key.Kp8 => "Numpad 8",
            Key.Kp9 => "Numpad 9",
            Key.KpAdd => "Numpad +",
            Key.KpSubtract => "Numpad -",
            Key.Space => "Space",
            Key.Enter => "Enter",
            Key.Escape => "Esc",
            Key.Tab => "Tab",
            Key.Question => "?",
            Key.Slash => "/",
            _ => key.ToString().ToUpper()
        };
    }

    /// <summary>
    /// Gets a formatted keybinding string for display (e.g., "G" or "↑/Numpad 8").
    /// Shows primary key and alternates separated by slash.
    /// </summary>
    public static string GetKeybindingDisplay(InputAction action)
    {
        if (Keybindings.TryGetValue(action, out var keys) && keys.Count > 0)
        {
            var keyNames = keys.ConvertAll(GetKeyDisplayName);
            return string.Join("/", keyNames);
        }

        // Check modifier keybindings
        foreach (var (Action, Key, Ctrl, Shift, Alt) in ModifierKeybindings)
        {
            if (Action == action)
            {
                // Special case: Shift+Slash displays as just "?"
                if (Key == Key.Slash && Shift && !Ctrl && !Alt)
                {
                    return "?";
                }

                var modifiers = new List<string>();
                if (Ctrl) modifiers.Add("Ctrl");
                if (Shift) modifiers.Add("Shift");
                if (Alt) modifiers.Add("Alt");
                modifiers.Add(GetKeyDisplayName(Key));
                return string.Join("+", modifiers);
            }
        }

        return "Unbound";
    }
}
