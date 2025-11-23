# Targeting System

The **Targeting System** provides unified tile and entity selection for all player-initiated actions: skill activation, ranged/reach attacks, targeted items, and examination. It uses a handler-based architecture that bridges player input, validation logic, and action execution through visual feedback.

## Design Philosophy

**Handler-Based Unification**: All targeting scenarios (skills, weapons, items) use the same handler pattern with pluggable implementations per targeting type. Eliminates separate code paths for skills vs items vs weapons.

**Data-Driven Configuration**: `TargetingDefinition` captures all targeting parameters (type, range, area size, LOS, distance metric, filter) allowing items and skills to define targeting through data rather than code.

**Grid-Based Tactical**: Action targeting uses Chebyshev distance (square ranges) for clean diagonal mechanics. Ranged weapons can use Euclidean distance for circular ranges when appropriate.

**Creature-Centric Actions**: Action modes auto-select nearest valid target and provide Tab-cycling between creatures for efficient selection without manual cursor movement.

## Core Concepts

### Targeting Types

The system supports these targeting types via dedicated handlers:

| Type | Description | Selection Required |
|------|-------------|-------------------|
| **Self** | Targets the caster only | No |
| **Adjacent** | Any tile within 1 Chebyshev distance | Yes |
| **Tile** | Any tile within range | Yes |
| **Enemy** | Hostile entities within range | Yes |
| **Ally** | Friendly entities within range | Yes |
| **Creature** | Any entity within range | Yes |
| **Area** | Tile selection with AoE preview | Yes |
| **Ranged** | Euclidean distance (circular range) | Yes |
| **Reach** | Chebyshev distance (square range) | Yes |

### Target Filters

Filters determine which entities are valid targets:

- **Self**: Only the caster
- **Enemy**: Hostile entities
- **Ally**: Friendly entities
- **Creature**: Any entity (enemy or ally)
- **Tile**: Position-based, no entity required

### Distance Metrics

**Chebyshev**: Square-shaped ranges where diagonals cost same as orthogonal moves. Formula: `max(|dx|, |dy|)`. Creates 3x3, 5x5 grids. Used for reach weapons, most skills, and action targeting.

**Euclidean**: Circular-shaped ranges. Formula: `sqrt(dx² + dy²)`. Used for ranged weapons (bows, crossbows) and vision/FOV.

### Targeting Definition

`TargetingDefinition` is the unified configuration for all targeting:

```csharp
public class TargetingDefinition
{
    public TargetingType Type { get; init; }
    public int Range { get; init; } = 1;
    public int AreaSize { get; init; } = 0;
    public bool RequiresLOS { get; init; } = true;
    public DistanceMetric Metric { get; init; } = DistanceMetric.Chebyshev;
    public TargetFilter Filter { get; init; } = TargetFilter.Enemy;
    public bool RequiresSelection => Type != TargetingType.Self;
}
```

Factory methods create definitions from data:
- `TargetingDefinition.FromSkill(skill)` - Reads skill targeting properties
- `TargetingDefinition.FromItem(item)` - Uses item targeting or smart defaults
- Static factories: `Self()`, `Enemy(range)`, `Ally(range)`, `Area(range, size)`, `Ranged(range)`, `Reach(range)`

### Targeting Handlers

Abstract `TargetingHandler` base with implementations per type:

```csharp
public abstract class TargetingHandler
{
    public abstract TargetingType TargetType { get; }
    public abstract List<GridPosition> GetValidTargetPositions(...);
    public abstract bool IsValidTarget(...);
    public virtual List<BaseEntity> GetAffectedEntities(...);
    public virtual List<GridPosition> GetAffectedPositions(...); // For AoE preview
    public virtual bool RequiresSelection => true;
}
```

Handlers are created via factory: `TargetingHandler.CreateForDefinition(definition)`.

## Cursor Targeting System

### Targeting Modes

The cursor system operates in these modes:

**Examine**: Read-only inspection. Cursor moves freely within player vision. Triggered by X key. No action execution.

**RangedAttack**: Bow/crossbow targeting. Uses Euclidean distance within weapon range. Triggered by F key with ranged weapon equipped.

**ReachAttack**: Melee weapons with range > 1. Uses Chebyshev distance. Triggered by activating equipped reach weapon.

**TargetedItem**: Items requiring target selection. Range/type determined by item data. Triggered by activating targeted item.

**SkillTargeting**: Skills requiring target selection. Configuration from skill definition. Triggered by activating targeted skill.

### Unified StartTargeting

All action targeting uses the unified entry point:

```csharp
public void StartTargeting(BaseEntity caster, TargetingDefinition definition, ActionContext context)
```

This replaces mode-specific start methods with a single data-driven approach.

### Area Preview

For AoE targeting, `AffectedPositions` property provides the list of tiles that will be affected by the current cursor position. UI renders these with distinct highlighting.

### Visual Feedback

**White Border Box**: Solid 1-pixel white border on cursor tile. Consistent across all modes.

**Green Fill**: Pulsing green highlight when cursor hovers over entity.

**Range Overlay** (action modes): Subtle blue highlight on valid tiles within range respecting LOS.

**Trace Line** (action modes): Yellow line from player to cursor.

**Area Preview** (AoE skills): Additional highlight showing affected area around cursor.

## Architectural Patterns

### Handler Registration

Handlers register by type in static factory:

```csharp
TargetingHandler.CreateForType(TargetingType type) => type switch
{
    TargetingType.Self => new SelfTargetingHandler(),
    TargetingType.Enemy => new EnemyTargetingHandler(),
    TargetingType.Area => new AreaTargetingHandler(),
    // ... etc
};
```

### LOS Integration

Handlers check LOS via `FOVCalculator.HasLineOfSight()` when `RequiresLOS` is true. Area and Self handlers bypass LOS. Line and Cone handlers use custom LOS logic.

### Two-Phase Validation

**Handler Phase**: `GetValidTargetPositions()` pre-calculates valid tiles at targeting start. Provides visual feedback and cursor constraints.

**Action Phase**: Actions re-validate in `CanExecute()` using same handler. Prevents desync between targeting and execution.

## Integration Points

### Skill System Integration

Skills define targeting in YAML:

```yaml
targeting: enemy    # TargetingType
range: 5           # Tile range
area_size: 2       # For AoE skills
```

`TargetingDefinition.FromSkill()` reads these properties. `PlayerActionHandler.ActivateSkill()` checks if targeting is required and either executes immediately (self-targeting) or starts cursor targeting.

### Item System Integration

Items can define targeting in YAML:

```yaml
targeting:
  type: enemy
  range: 6
  requires_los: true
```

If no targeting block, `TargetingDefinition.FromItem()` uses smart defaults based on item type (ranged attack, throwable, usable).

### Action System Integration

Actions receive targets from cursor confirmation:
- `UseSkillAction(skill, targets)` - Skill execution with resolved targets
- `UseTargetedItemAction(itemKey, position, affectedEntities)` - Item usage with target info
- `RangedAttackAction(target)` - Ranged weapon attack
- `ReachAttackAction(target)` - Reach weapon attack

## File Organization

```
Scripts/Systems/Targeting/
├── TargetingType.cs          # Enum: Self, Adjacent, Tile, Enemy, etc.
├── TargetFilter.cs           # Enum: Self, Enemy, Ally, Creature, Tile
├── TargetingDefinition.cs    # Configuration + factory methods
├── TargetingHandler.cs       # Abstract base + factory
└── Handlers/
    ├── SelfTargetingHandler.cs
    ├── AdjacentTargetingHandler.cs
    ├── TileTargetingHandler.cs
    ├── EnemyTargetingHandler.cs
    ├── AllyTargetingHandler.cs
    ├── CreatureTargetingHandler.cs
    ├── AreaTargetingHandler.cs
    ├── RangedTargetingHandler.cs
    └── ReachTargetingHandler.cs

Scripts/Systems/
└── CursorTargetingSystem.cs  # UI layer using handlers
```

## Adding New Targeting Types

1. Add value to `TargetingType` enum
2. Create handler class extending `TargetingHandler`
3. Implement `GetValidTargetPositions()` and `IsValidTarget()`
4. Override `GetAffectedEntities()` if entity filtering differs
5. Override `GetAffectedPositions()` if area preview needed
6. Register in `TargetingHandler.CreateForType()` factory

## Design Decisions & Trade-offs

**Handler Pattern**: Enables clean separation of targeting logic per type while sharing validation infrastructure. Trade-off: more files than switch-based approach, but better extensibility.

**Data-Driven Definitions**: Items and skills configure targeting through data rather than code. Trade-off: less flexibility than custom code, but enables designer iteration without programming.

**Dual Distance Metrics**: Chebyshev for grid-based tactical feel, Euclidean for ranged weapons. Trade-off: slight inconsistency, but matches player expectations (bows feel "ranged", melee feels "grid").

**LOS in Handlers**: Each handler respects LOS requirements. Trade-off: duplicated LOS checks across handlers, but keeps validation co-located with targeting logic.

**Pre-calculated Valid Tiles**: FOV calculation at targeting start prevents per-frame recalculation. Trade-off: tiles don't update if game state changes, acceptable in turn-based.

## Related Documentation

- [actions.md](actions.md) - Action system that consumes targeting results
- [skills.md](skills.md) - Skill system using targeting for activation
- [effects.md](effects.md) - Item effects that may require targeting
- [text-renderer.md](text-renderer.md) - Visual rendering of cursor overlays
