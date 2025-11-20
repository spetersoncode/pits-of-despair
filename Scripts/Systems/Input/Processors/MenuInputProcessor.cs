using Godot;
using PitsOfDespair.Systems.Input.Services;

namespace PitsOfDespair.Systems.Input.Processors;

/// <summary>
/// Provides shared input processing utilities for UI modals.
/// Handles common patterns like ESC to close and A-Z letter selection.
/// </summary>
public static class MenuInputProcessor
{
    private static readonly KeybindingService _keybindingService = new();

    /// <summary>
    /// Checks if the key event is the modal close key (ESC).
    /// </summary>
    public static bool IsCloseKey(InputEventKey keyEvent)
    {
        // Directly check for Escape key to ensure it always works in modals
        // regardless of modifier state
        return keyEvent.Keycode == Key.Escape;
    }

    /// <summary>
    /// Checks if the key event is a letter key (A-Z) and returns the letter.
    /// Used for item selection in inventory/activate/drop/equip modals.
    /// </summary>
    public static bool TryGetLetterKey(InputEventKey keyEvent, out char letter)
    {
        return _keybindingService.IsLetterKey(keyEvent, out letter);
    }

    /// <summary>
    /// Checks if the key event is a specific key.
    /// </summary>
    public static bool IsKey(InputEventKey keyEvent, Key key)
    {
        return keyEvent.Keycode == key;
    }

    /// <summary>
    /// Gets the display name for a key (for UI labels).
    /// </summary>
    public static string GetKeyDisplayName(Key key)
    {
        return KeybindingConfig.GetKeyDisplayName(key);
    }
}
