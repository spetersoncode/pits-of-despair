namespace PitsOfDespair.Systems.Input;

/// <summary>
/// Defines all possible input actions in the game.
/// Used to decouple key bindings from action execution.
/// </summary>
public enum InputAction
{
    // Movement
    MoveNorth,
    MoveSouth,
    MoveEast,
    MoveWest,
    MoveNorthEast,
    MoveNorthWest,
    MoveSouthEast,
    MoveSouthWest,

    // Basic Actions
    Wait,
    Pickup,

    // Combat
    FireRanged,

    // Menu Toggles
    ToggleInventory,
    ToggleActivate,
    ToggleDrop,
    ToggleEquip,
    ToggleExamine,
    ToggleHelp,

    // Debug
    ToggleDebug,
    ToggleDebugConsole,

    // Cursor Targeting
    CursorConfirm,
    CursorCancel,
    CursorCycleNext,
    CursorCyclePrevious,
    CursorZoomIn,
    CursorZoomOut,

    // Modal Common
    ModalClose,

    // Item Selection (A-Z)
    // Handled separately via letter key range checking
}
