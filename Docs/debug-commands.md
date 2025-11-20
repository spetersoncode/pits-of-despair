# Debug Commands

Debug commands provide runtime inspection and manipulation tools for testing and development. Commands follow the same architectural patterns as Actions, Effects, and Status systems.

## Usage

**Activating Debug Mode:**
1. Press `Ctrl+D` to toggle debug mode on/off
2. When enabled, press `/` to open the debug console
3. Type commands and press Enter to execute
4. Press ESC to close console, or continue entering commands

**Command Format:**
```
/command [arguments]
```

**Example:**
```
/give potion_health
/help
```

## Available Commands

### `give [itemId]`
Spawns an item directly in player inventory.

**Arguments:**
- `itemId` - The item's data ID (from Items YAML)

**Examples:**
```
/give potion_health
/give sword_iron
/give scroll_blink
```

**Notes:**
- Item must exist in item data
- Fails if inventory is full
- Item spawns at player position then immediately added to inventory

### `help`
Lists all available debug commands with usage information.

**Arguments:** None

**Example:**
```
/help
```

## Architecture

### Base Classes

**DebugCommand** (`Scripts/Debug/DebugCommand.cs`):
```csharp
public abstract class DebugCommand
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Usage { get; }
    public abstract DebugCommandResult Execute(DebugContext context, string[] args);
}
```

**DebugCommandResult** (`Scripts/Debug/DebugCommandResult.cs`):
- `bool Success` - Whether command succeeded
- `string Message` - User-facing message to display
- `string MessageColor` - Hex color for MessageLog

**DebugContext** (`Scripts/Debug/DebugContext.cs`):
Provides access to game systems:
- `Player Player`
- `EntityManager EntityManager`
- `MapSystem MapSystem`
- `EntityFactory EntityFactory`
- `TurnManager TurnManager`

### Factory Pattern

**DebugCommandFactory** (`Scripts/Debug/DebugCommandFactory.cs`):
- Static dictionary-based registration
- String-based command lookup (case-insensitive)
- Returns null for unknown commands

```csharp
private static readonly Dictionary<string, Func<DebugCommand>> _commandRegistry = new()
{
    { "give", () => new GiveCommand() },
    { "help", () => new HelpCommand() }
};
```

### UI Integration

**DebugConsoleModal** (`Scripts/UI/DebugConsoleModal.cs`, `Scenes/UI/DebugConsoleModal.tscn`):
- CenterContainer modal with LineEdit input
- Two-state system: debug mode enabled/disabled, console open/closed
- Outputs to MessageLog for feedback
- Modal behavior - blocks game input when open

**Input Handling** (`Scripts/Systems/InputHandler.cs`):
- `Ctrl+D` - Toggles debug mode (emits `DebugModeToggled` signal)
- `/` - Opens console if debug enabled (emits `DebugConsoleRequested` signal)

**GameHUD Integration** (`Scripts/UI/GameHUD.cs`):
- Manages debug console visibility
- Handles signal connections
- Updates `IsAnyMenuOpen()` to include console

**GameLevel Wiring** (`Scripts/Systems/GameLevel.cs`):
- Creates DebugContext with system references
- Connects InputHandler signals to GameHUD handlers
- Passes context to GameHUD.Initialize()

## Adding New Commands

### 1. Create Command Class

Create a new file in `Scripts/Debug/Commands/`:

```csharp
using PitsOfDespair.Core;

namespace PitsOfDespair.Debug.Commands;

public class MyCommand : DebugCommand
{
    public override string Name => "mycommand";
    public override string Description => "What the command does";
    public override string Usage => "mycommand [arg1] [arg2]";

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        // Validate arguments
        if (args.Length < 1)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: mycommand [arg]",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Access game systems via context
        var player = context.Player;
        var entityManager = context.EntityManager;
        // ... etc

        // Execute command logic
        // ...

        // Return result
        return DebugCommandResult.CreateSuccess(
            "Command executed successfully!",
            Palette.ToHex(Palette.Success)
        );
    }
}
```

### 2. Register in Factory

Add to `DebugCommandFactory._commandRegistry`:

```csharp
private static readonly Dictionary<string, Func<DebugCommand>> _commandRegistry = new()
{
    { "give", () => new GiveCommand() },
    { "help", () => new HelpCommand() },
    { "mycommand", () => new MyCommand() }  // Add here
};
```

### 3. Update Documentation

Add command to "Available Commands" section above.

## Design Principles

**String-Based Registration:**
- Commands identified by string names (not enums)
- Case-insensitive lookup
- Data-driven, console-friendly

**Context Pattern:**
- Explicit dependency injection via DebugContext
- No singletons or global state
- Commands declare what systems they need access to

**Result Objects:**
- Structured return values (not exceptions)
- User-facing messages with color
- Success/failure clearly indicated

**Composition over Inheritance:**
- Single abstract base class
- No deep hierarchies
- Each command is independent

**Separation of Concerns:**
- Commands contain logic (Scripts/Debug/Commands/)
- Factory handles registration (Scripts/Debug/DebugCommandFactory.cs)
- UI handles display (Scripts/UI/DebugConsoleModal.cs)
- Input handling separate (Scripts/Systems/InputHandler.cs)

## Common Patterns

### Accessing Player Components
```csharp
var inventory = context.Player.GetNodeOrNull<InventoryComponent>("InventoryComponent");
if (inventory == null)
{
    return DebugCommandResult.CreateFailure(
        "Player has no inventory!",
        Palette.ToHex(Palette.Danger)
    );
}
```

### Creating Entities
```csharp
var entity = context.EntityFactory.CreateItem(itemId, position);
if (entity == null)
{
    return DebugCommandResult.CreateFailure(
        $"Unknown item ID: {itemId}",
        Palette.ToHex(Palette.Danger)
    );
}
```

### Manipulating Map
```csharp
context.MapSystem.AddEntity(entity);
context.MapSystem.RemoveEntity(entity);
var validPosition = context.MapSystem.GetValidSpawnPosition();
```

### Success/Failure Messages
```csharp
// Success with green color
return DebugCommandResult.CreateSuccess(
    "Item spawned!",
    Palette.ToHex(Palette.Success)
);

// Failure with red color
return DebugCommandResult.CreateFailure(
    "Command failed!",
    Palette.ToHex(Palette.Danger)
);

// Warning with yellow color
return DebugCommandResult.CreateSuccess(
    "Partial success...",
    Palette.ToHex(Palette.Caution)
);
```

### BBCode Formatting
Messages support BBCode for rich formatting:
```csharp
return DebugCommandResult.CreateSuccess(
    $"Spawned [b]{itemName}[/b] at position ({x}, {y}).",
    Palette.ToHex(Palette.Success)
);
```

## Future Extensions

Potential commands to add:
- **Player Manipulation**: `heal`, `damage`, `godmode`, `teleport [x] [y]`
- **Spawn Control**: `spawn [creatureId]`, `clear_enemies`, `spawn_at [id] [x] [y]`
- **Level Control**: `reveal_map`, `next_floor`, `regen_level`
- **Item Testing**: `identify_all`, `equip [slot]`, `drop_all`
- **Stats**: `buff [stat] [amount]`, `set_level [level]`

Commands should:
- Focus on testing and debugging scenarios
- Provide clear, actionable feedback
- Handle edge cases gracefully
- Use existing game systems (don't bypass them)
