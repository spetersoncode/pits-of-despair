namespace PitsOfDespair.Systems.Input;

/// <summary>
/// Defines input contexts for context-aware keybinding resolution.
/// Higher priority contexts take precedence over lower priority ones.
/// </summary>
public enum InputContext
{
    /// <summary>
    /// Default gameplay context (movement, actions, menu toggles).
    /// </summary>
    Gameplay,

    /// <summary>
    /// Modal dialog context (level-up, inventory, etc.).
    /// Takes priority over Gameplay context.
    /// </summary>
    Modal,
}

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
    Descend,
    AutoExplore,

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

    // Level-Up Stat Selection
    SelectStrength,
    SelectAgility,
    SelectEndurance,
    SelectWill,

    // Item Selection (A-Z)
    // Handled separately via letter key range checking
}
