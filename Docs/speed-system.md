# Speed System

The speed system provides energy-based turn scheduling inspired by Dungeon Crawl Stone Soup (DCSS). Actions have delay costs measured in **aut** (action time units), and entity speed determines how quickly they accumulate time to act.

## Core Mechanics

**Player-centric time**: Time advances when the player acts. The player's action delay is added to all creatures' accumulated time. Creatures with enough time act immediately, potentially multiple times if very fast.

**Energy accumulation**: Each entity has `AccumulatedTime`. When the player acts with delay cost D, all creatures gain D time. Creatures act when `AccumulatedTime >= ActionDelay`.

**Speed formula**: Base speed of 10 = average (1.0x delay multiplier). Higher speed = faster. Delay calculation uses weighted random rounding for fractional results.

## Components

### SpeedComponent

Manages entity speed and accumulated time.

**Properties**:
- `BaseSpeed` - Base speed stat (10 = average, higher = faster)
- `AccumulatedTime` - Energy counter tracking time accumulated
- `IsHasted` - Bypasses minimum delay floor (6 aut) when true
- `EffectiveSpeed` - Computed speed after modifiers

**Methods**:
- `CalculateDelay(actionDelayCost)` - Converts base delay to actual delay using speed
- `GetSpeedDescription()` - Returns "Very Quick", "Average", "Slow", etc.
- `AddTime(amount)` / `DeductTime(amount)` - Modify accumulated time
- `AddSpeedModifier(source, amount)` / `RemoveSpeedModifier(source)` - Temporary speed bonuses/penalties

**Speed descriptors** (based on effective delay for standard 10 aut action):
- 6-7 aut: Very Quick (167-140%)
- 8-9 aut: Quick (125-110%)
- 10 aut: Average (100%)
- 11-12 aut: Slow (90-83%)
- 13+ aut: Very Slow (77%-)

### ActionResult

Actions return `ActionResult` with `DelayCost` property.

**Properties**:
- `Success` - Whether action succeeded
- `Message` - Feedback message
- `DelayCost` - Delay cost in aut (default: 10)
- `CostsTime` - Computed property (`DelayCost > 0`)

**Standard delays**:
- `ActionDelay.Standard` = 10 aut (most actions)
- Failed actions = 0 aut (free retry)
- Custom delays set per-action (fast/slow variants)

## Systems

### TimeSystem

Central scheduler managing energy for all entities.

**Responsibilities**:
- Register/unregister player and creature SpeedComponents
- Advance time when player acts
- Identify creatures ready to act (energy >= threshold)
- Order ready creatures by speed (fastest first)

**Methods**:
- `RegisterPlayer(speedComponent)` - Registers player separately
- `RegisterCreature(speedComponent)` - Tracks creature
- `AdvanceTime(playerDelay)` - Adds time to all creatures
- `GetNextReadyCreature(actionDelayCost)` - Returns fastest creature with enough energy (or null)
- `DeductCreatureTime(speedComponent, delay)` - Removes time after action

### TurnManager

Orchestrates turn flow with TimeSystem integration.

**Turn cycle**:
1. Player acts → calculates delay via SpeedComponent
2. TurnManager receives delay from Player's `TurnCompleted` signal
3. `TimeSystem.AdvanceTime(playerDelay)` adds time to all creatures
4. Loop: Get next ready creature from TimeSystem
   - AISystem processes creature turn
   - Deduct creature's action delay
   - Repeat until no creatures ready
5. Return to player input

**Visual effects**: Turn transitions wait for visual effects to complete before processing creatures.

### AISystem

Processes individual creature turns on demand.

**Method**: `ProcessSingleCreatureTurn(speedComponent)`
1. Get entity from SpeedComponent parent
2. Build AIContext with perception data
3. Process goal stack → execute action
4. Calculate actual delay via `speedComponent.CalculateDelay(actionDelay)`
5. Return delay for TimeSystem to deduct

**Goal stack unchanged**: Goals still use action system normally. Speed only affects timing between actions.

## Integration

### Entity Creation

**Player**: Creates SpeedComponent in `_Ready()` with default speed (10).

**Creatures**: EntityFactory creates SpeedComponent from `CreatureData.Speed` field.

### Data Files

**CreatureData** (`Data/Creatures/*.yaml`):
```yaml
speed: 10  # Average speed (default if omitted)
```

Examples:
- Fast creature: `speed: 15` (faster actions)
- Slow creature: `speed: 5` (slower actions)

**SkillDefinition** (`Data/Skills/*.yaml`):
```yaml
delayCost: 10  # Standard delay (default if omitted)
```

Examples:
- Quick skill: `delayCost: 7`
- Slow skill: `delayCost: 15`
- Free skill: `delayCost: 0`

### Actions

All actions use `DelayCost` property. Most use `ActionDelay.Standard` (10 aut) default.

**Custom delays**:
```csharp
return ActionResult.CreateSuccess("Fast strike!", delayCost: 7);
return ActionResult.CreateSuccess("Heavy swing!", delayCost: 15);
```

**Failed actions**: Return `CreateFailure()` which sets `DelayCost = 0` for free retry.

## Weighted Random Rounding

Fractional delays (e.g., 6.4 aut) use weighted random rounding:
- 6.4 aut = 60% chance of 6, 40% chance of 7
- Adds variance while maintaining correct average over time
- Matches DCSS behavior

## Multiple Actions Per Turn

Fast creatures can act multiple times if they have enough energy:

**Example**: Speed 20 creature (5 aut delay) when player acts with 10 aut:
1. Creature gains 10 aut → `AccumulatedTime = 10`
2. Acts (costs 5 aut) → `AccumulatedTime = 5`
3. Still has 5 aut → acts again → `AccumulatedTime = 0`
4. Result: 2 actions vs player's 1

**Loop safety**: TurnManager has 1000 iteration limit to prevent infinite loops.

## Minimum Delay Floor

Actions cannot be faster than 6 aut unless hasted:
- Prevents extreme speed from trivializing gameplay
- Haste condition bypasses floor for temporary speed bursts
- Matches DCSS minimum delay mechanic

## Speed Modifiers

**Source tracking**: SpeedComponent supports named modifiers:
```csharp
speedComponent.AddSpeedModifier("haste_potion", +5);
speedComponent.RemoveSpeedModifier("haste_potion");
```

**Use cases**:
- Temporary buffs/debuffs (haste, slow)
- Equipment bonuses (boots of speed)
- Status conditions (slowed, quickened)

## See Also

- [turn-based.md](turn-based.md) - Turn cycle and phase transitions
- [actions.md](actions.md) - Action system and execution
- [components.md](components.md) - Component architecture patterns
