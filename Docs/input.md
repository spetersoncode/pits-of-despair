# Input System

**Status:** Implemented
**Location:** `Scripts/Systems/Input/`

## Overview

The input system converts player keyboard input into game actions and routes input to the appropriate handlers based on game state (gameplay, menus, cursor targeting, debug). The architecture is designed with **separation of concerns** and **future extensibility** for runtime keybinding customization.

## Architecture

### Core Components

#### InputHandler (`InputHandler.cs`)
**Orchestrator** that routes input to specialized processors based on game state.

**Responsibilities:**
- Listen to Godot's `_Input()` event
- Coordinate with game systems (TurnManager, GameHUD, CursorTargetingSystem)
- Route input to processors based on priority:
  1. Cursor targeting (when active)
  2. System input (debug, help)
  3. Menu toggles (inventory, activate, drop, equip, examine)
  4. Gameplay (movement, combat, wait, pickup)
- Emit signals for UI coordination

**Design:** Lightweight orchestrator (~250 lines, down from 575)

#### KeybindingConfig (`KeybindingConfig.cs`)
**Centralized keybinding definitions** for the entire game.

**Contents:**
- `Keybindings`: Dictionary mapping `InputAction` → List of `Key` values
- `ModifierKeybindings`: Special bindings requiring Ctrl/Shift/Alt
- `ActionDescriptions`: Human-readable descriptions for each action
- `HelpCategories`: Organization of actions for help screen generation
- `GetKeyDisplayName()`: Display-friendly key names (e.g., "↑", "Space")
- `GetKeybindingDisplay()`: Formatted binding strings (e.g., "↑/Numpad 8")

**Design:** Static class for efficient access; designed for future serialization

#### KeybindingService (`Services/KeybindingService.cs`)
**Query service** for resolving input events to actions.

**Methods:**
- `TryGetAction(InputEventKey, out InputAction)`: Resolve key event to action
- `IsActionPressed(InputEventKey, InputAction)`: Check if event matches action
- `IsLetterKey(InputEventKey, out char)`: Check for A-Z keys (item selection)
- `TryGetMovementDirection(InputAction, out Vector2I)`: Get direction vector
- `IsMovementAction(InputAction)`: Check if action is movement

**Future extensibility:** Placeholder methods for runtime rebinding

#### InputActions (`InputActions.cs`)
Enum defining all possible input actions in the game:
- **Movement:** MoveNorth, MoveSouth, MoveEast, MoveWest, diagonals
- **Basic Actions:** Wait, Pickup
- **Combat:** FireRanged
- **Menu Toggles:** ToggleInventory, ToggleActivate, ToggleDrop, etc.
- **Debug:** ToggleDebug, ToggleDebugConsole
- **Cursor:** CursorConfirm, CursorCancel, CursorCycleNext/Previous
- **Modal:** ModalClose

### Input Processors

#### GameplayInputProcessor (`Processors/GameplayInputProcessor.cs`)
Processes **turn-based gameplay input** during the player's turn.

**Handles:**
- Movement actions (all 8 directions)
- Wait action
- Pickup action
- Fire ranged weapon (starts targeting mode)
- Targeted item actions (via GameHUD signals)
- Reach attack targeting (via GameHUD signals)

**Dependencies:** KeybindingService, Player, ActionContext, CursorTargetingSystem

#### CursorInputProcessor (`Processors/CursorInputProcessor.cs`)
Processes input during **cursor targeting mode** (examine, ranged attacks, targeted items).

**Handles:**
- Movement (8 directions)
- Cancel (ESC, or X in examine mode)
- Confirm target (Enter/Space/F in action modes)
- Cycle targets (Tab, Shift+Tab, Numpad +/-)

**Mode-aware:** Distinguish between examine mode (read-only) and action modes (confirmable)

#### MenuInputProcessor (`Processors/MenuInputProcessor.cs`)
**Static utility** providing shared input handling for UI modals.

**Methods:**
- `IsCloseKey(InputEventKey)`: Check for ESC key
- `TryGetLetterKey(InputEventKey, out char)`: Check for A-Z keys
- `IsKey(InputEventKey, Key)`: Check for specific key
- `GetKeyDisplayName(Key)`: Get display-friendly key name

**Usage:** All modals (HelpModal, InventoryModal, ItemSelectionModal, etc.)

## Input Flow

```
User Input
    ↓
InputHandler._Input()
    ↓
Priority Routing:
    1. CursorInputProcessor (if cursor active)
    2. System Input (debug, help)
    3. Menu Toggles (if no menu open)
    4. GameplayInputProcessor (if player turn)
    ↓
KeybindingService resolves Key → InputAction
    ↓
Processor handles action → Creates game action → Executes
```

## Current Keybindings

See `KeybindingConfig.cs` for authoritative source. Summary:

| Category | Action | Keys | Description |
|----------|--------|------|-------------|
| **Movement** | MoveNorth | ↑, Numpad 8 | Move North |
| | MoveSouth | ↓, Numpad 2 | Move South |
| | MoveEast | →, Numpad 6 | Move East |
| | MoveWest | ←, Numpad 4 | Move West |
| | Diagonals | Numpad 7/9/1/3 | Northwest/Northeast/Southwest/Southeast |
| **Actions** | Wait | Space, Numpad 5 | Wait / Pass Turn |
| | Pickup | G | Pickup Item |
| | FireRanged | F | Fire Ranged Weapon / Target |
| **Menus** | ToggleInventory | I | Open Inventory |
| | ToggleActivate | A | Activate Item |
| | ToggleDrop | D | Drop Item |
| | ToggleEquip | E | Equip/Unequip Item |
| | ToggleExamine | X | Examine Mode |
| **System** | ToggleHelp | ? (Shift+/) | Show Help |
| | ToggleDebug | Ctrl+D | Toggle Debug Overlay |
| | ToggleDebugConsole | / | Open Debug Console |
| **Targeting** | CursorConfirm | Enter, Space, F | Confirm Target |
| | CursorCancel | Esc | Cancel Targeting |
| | CursorCycleNext | Tab, Numpad + | Cycle Next Target |
| | CursorCyclePrevious | Shift+Tab, Numpad - | Cycle Previous Target |
| **Modal** | ModalClose | Esc | Close Modal |

## Adding New Keybindings

1. **Add action to `InputActions.cs`:**
   ```csharp
   public enum InputAction
   {
       // ...
       MyNewAction,
   }
   ```

2. **Add keybinding to `KeybindingConfig.cs`:**
   ```csharp
   public static readonly Dictionary<InputAction, List<Key>> Keybindings = new()
   {
       // ...
       { InputAction.MyNewAction, new() { Key.M } },
   };
   ```

3. **Add description to `ActionDescriptions`:**
   ```csharp
   { InputAction.MyNewAction, "Do Something Cool" },
   ```

4. **(Optional) Add to `HelpCategories`** for help screen:
   ```csharp
   { "Actions", new() { /* ... */ InputAction.MyNewAction } },
   ```

5. **Handle action in appropriate processor:**
   - Gameplay: `GameplayInputProcessor.ProcessInput()`
   - Cursor: `CursorInputProcessor.HandleCursorAction()`
   - System/Menu: `InputHandler.ProcessSystemInput()` or `ProcessMenuInput()`

## Integration with Game Systems

### GameLevel Wiring
`GameLevel.cs` connects InputHandler to:
- Player (action execution, signal connections)
- TurnManager (turn coordination)
- ActionContext (action execution context)
- GameHUD (menu state, targeting signals)
- CursorTargetingSystem (targeting mode coordination)

### UI Modal Integration
All modals use `MenuInputProcessor` for consistent input handling:
- **HelpModal:** Close key, dynamically generates help from `KeybindingConfig`
- **InventoryModal / ItemSelectionModal:** Close key, letter key selection
- **ItemDetailModal:** Close key, letter key rebinding, `=` key for rebind mode
- **DebugConsoleModal:** Close key

### Signal-Based Decoupling
InputHandler emits signals for UI coordination:
- `InventoryToggleRequested`
- `HelpRequested`
- `ActivateItemRequested`
- `DropItemRequested`
- `EquipMenuRequested`
- `DebugModeToggled`
- `DebugConsoleRequested`

GameHUD emits signals back to InputHandler:
- `StartItemTargeting(char itemKey)` → Start cursor targeting for item
- `StartReachAttackTargeting(char itemKey)` → Start reach attack targeting

## Future Extensions

### Runtime Keybinding Customization
Architecture is designed to support:
1. **Rebinding UI:** Allow players to customize keybindings in settings menu
2. **Persistence:** Save/load keybindings to/from file or Godot project settings
3. **Validation:** Prevent duplicate bindings, ensure all actions remain bound
4. **Keybinding Service Methods:**
   - `RebindAction(InputAction, Key)`
   - `SaveKeybindings()`
   - `LoadKeybindings()`

### Gamepad Support
To add gamepad support:
1. Extend `KeybindingConfig` to support `InputEventJoypadButton`
2. Update `KeybindingService.TryGetAction()` to handle joypad events
3. Add gamepad input mode detection in `InputHandler`
4. Update help screen to show context-sensitive controls

### Alternative Input Methods
- **Mouse targeting:** Click to move, right-click to target
- **Touch controls:** Virtual d-pad for mobile
- **Voice commands:** Accessibility option

## Design Principles

### Separation of Concerns
- **InputHandler:** Orchestration and routing
- **KeybindingConfig:** Data (keybindings, descriptions)
- **KeybindingService:** Query logic
- **Processors:** Domain-specific input handling (gameplay, cursor, menus)

### Single Source of Truth
- All keybindings defined in `KeybindingConfig.cs`
- Help screen dynamically generated from config (eliminates duplication)
- UI labels use `GetKeybindingDisplay()` for consistency

### Extensibility
- New actions require minimal changes (add to enum, config, handler)
- Processors can be swapped or extended without affecting others
- Designed for future runtime customization

### Testability
- Processors are standalone classes with minimal dependencies
- KeybindingService can be tested in isolation
- Input routing logic separated from action execution

## Related Systems

- **[actions.md](actions.md):** Game actions created by input processors
- **[targeting.md](targeting.md):** CursorTargetingSystem integration
- **Turn-based System:** TurnManager coordination for gameplay input
- **UI System:** Modal integration and signal coordination
