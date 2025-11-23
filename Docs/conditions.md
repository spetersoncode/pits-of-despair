# Condition System

The condition system manages temporary and persistent effects (buffs, debuffs, equipment bonuses) applied to entities. Conditions integrate with the turn system for automatic processing, emit signals for UI feedback, and support multiple duration modes for different use cases.

## Core Architecture

**Condition**: Abstract base class defining the lifecycle contract for all conditions. Conditions track duration, remaining turns, and provide hooks for application (`OnApplied`), per-turn processing (`OnTurnProcessed`), and removal (`OnRemoved`). Each condition has a unique `TypeId` controlling stacking behavior—reapplying identical types refreshes duration rather than stacking effects.

**BaseEntity Integration**: Conditions are managed directly by `BaseEntity` rather than through a separate component. This eliminates initialization order bugs and makes conditions a first-class entity concept. All entities can have conditions without requiring additional components.

**ConditionFactory**: Centralized factory creating condition instances from string identifiers. Supports all condition parameters (type, amount, duration, duration mode, source ID). Single source of truth for condition instantiation used by effects, skills, and equipment systems.

**ApplyConditionEffect**: Bridge between instantaneous effects and condition system. Creates condition instances via factory, adds to target entity, returns structured effect results. Enables data-driven condition application through YAML definitions.

## Duration Modes

Conditions support four duration modes controlling expiration behavior:

**Temporary**: Turn-based duration that decrements each turn. Used for potions, scrolls, and time-limited effects. Duration specified as dice notation (e.g., "10", "2d3+1").

**Permanent**: Never expires automatically. Used for passive skills and innate abilities. Remains until explicitly removed.

**WhileEquipped**: Lasts while equipment is equipped, removed on unequip. Equipment bonuses flow through conditions using this mode with source tracking.

**WhileActive**: Lasts while source is active, removed when deactivated. Used for auras and toggleable abilities.

## Condition Lifecycle

**Three-Phase Lifecycle**: Template method pattern with virtual hooks guarantees consistent execution order and prevents lifecycle bugs.

**OnApplied**: Fires when condition first added to entity. Used for one-time setup—register stat modifiers, initialize effects, emit application messages. Returns `ConditionMessage` with text and color for UI display.

**OnTurnProcessed**: Optional hook executing each turn while condition active. Used for recurring effects—damage over time, regeneration. Returns `ConditionMessage` for per-turn feedback. Simple buffs skip implementation.

**OnRemoved**: Fires on expiration or manual removal. Used for cleanup—unregister stat modifiers, restore original state, emit expiration messages. Returns `ConditionMessage`. Critical for preventing orphaned modifiers.

**Duration Refresh**: When condition with identical `TypeId` reapplied, existing condition calls `RefreshDuration`. If new duration exceeds remaining, updates to new value. Prevents stacking exploits while allowing duration extension.

## Turn Integration

**Signal-Based Processing**: `BaseEntity` subscribes to `TurnManager` signals during initialization. No manual update loops—turn manager orchestrates timing automatically.

**Phase Awareness**: `IsPlayerControlled` flag determines which turn signal to subscribe to. Player-controlled entities subscribe to `PlayerTurnStarted`, creatures to `CreatureTurnsStarted`. Ensures conditions tick when entity acts.

**Automatic Processing**: Each turn start, entity processes all active conditions—calls `OnTurnProcessed` hooks, decrements `RemainingTurns` for Temporary conditions, removes expired conditions, emits signals for UI.

**Turn Semantics**: Condition lasting "N turns" means N turns the entity takes actions. Duration independent of other entities' turns.

## Non-Stacking Design

**TypeId Uniqueness**: Each condition defines `TypeId` string identifier (armor_modifier, confusion, regen_modifier). Entity maintains at most one condition per `TypeId`—duplicates refresh duration rather than accumulate effects.

**Refresh Logic**: Adding condition already present compares durations. New duration greater than remaining updates `RemainingTurns`. New duration less than or equal ignores application.

**Multiple Types Coexist**: Different `TypeId`s stack freely. Armor buff and evasion buff can both apply. Two different armor buffs from different sources use same `TypeId` so don't stack.

## Source Tracking

**SourceId Pattern**: Conditions can track their origin via optional `SourceId`. Equipment uses format like `equip_<slot>_<itemId>` to identify all conditions from a specific equipped item.

**Bulk Removal**: `RemoveConditionsBySource(prefix)` removes all conditions whose `SourceId` starts with given prefix. Enables clean equipment unequip—single call removes all bonuses from that item.

**Source Queries**: `HasConditionFromSource(prefix)` and `GetConditionsBySource(prefix)` enable checking and iterating conditions from specific sources.

## Signal Communication

**BaseEntity Signals**: Three signals enable loose coupling with UI and other systems.

**ConditionAdded**: Emitted when condition first applied with condition name. UI displays icon or message.

**ConditionRemoved**: Emitted when condition expires or manually removed with condition name. UI removes icon.

**ConditionMessage**: Emitted for condition lifecycle events with message text and color. Application, turn processing, and expiration messages all use this signal.

## Condition Implementations

Current implementations demonstrate patterns for common effect types.

**StatModifierCondition**: Configurable stat modifier supporting Armor, Strength, Agility, Endurance, Will, Evasion. Uses unified `AddStatModifier(StatType, sourceId, amount)` API. `OnApplied` registers modifier. `OnRemoved` removes modifier. Works with all duration modes. Positive amounts are buffs, negative amounts are penalties.

**RegenModifierCondition**: Regeneration modifier. Adds to health component's regeneration rate while active. Supports all duration modes like other modifier conditions.

**ConfusionCondition**: Debuff causing random movement. Demonstrates behavioral modification rather than stat modification.

## Data Configuration

**YAML Definitions**: Conditions applied through effect definitions specifying type (`apply_condition`), `conditionType` (factory identifier), `amount` (modifier strength), `duration` (dice notation or turns).

**Factory Pattern**: `ConditionFactory.Create()` maps `conditionType` strings to condition instantiation. Supports all parameters including duration mode and source ID. Unknown types log errors for designer feedback.

**Equipment Integration**: Items specify `apply_condition` effects in their data. Equipment system applies conditions with `WhileEquipped` duration mode and source tracking on equip, removes by source on unequip.

## System Integration

**Effects System**: Condition application is specialized effect type. `ApplyConditionEffect` bridges instantaneous effect architecture to duration-based condition system. See **[effects.md](effects.md)**.

**Turn System**: Entity subscribes to turn signals, processes conditions automatically. Turn consumption happens at action level, not condition level. Condition processing doesn't consume additional turns.

**Stats System**: Conditions modify stats through multi-source modifier system. Clean registration and removal via component interfaces. Equipment, buffs, debuffs coexist without conflicts.

**Skills System**: Passive skills apply permanent conditions. Auras apply `WhileActive` conditions. Reactive skills can apply temporary conditions on triggers.

**Action System**: `UseItemAction` triggers condition application through effect chain. Two-phase validation—action validates item usability, effect creates and applies condition.

**UI System**: `GameHUD` connects to player's condition signals. `ConditionMessage` signal feeds message log with color-coded feedback. `ConditionAdded`/`ConditionRemoved` signals enable condition icon display.

## Extensibility

### Adding New Condition Types

**Step 1 - Implement Condition Subclass**: Inherit from `Condition` base class. Define `Name` (display), `TypeId` (stacking ID), implement lifecycle hooks. Constructor accepts parameters needed for the condition's behavior.

**Step 2 - Register in Factory**: Add case to `ConditionFactory.Create()` switch statement. Map `conditionType` string to condition instantiation. Pass parameters from factory method to constructor.

**Step 3 - Define in YAML**: Create item or effect with `apply_condition` effect. Specify `conditionType` matching factory case, `amount` for modifier strength, `duration` for turns.

**Step 4 - Test Integration**: Verify application message, turn processing (if applicable), expiration message. Check modifiers applied and removed cleanly. Confirm non-stacking behavior.

**Design Considerations**: Choose `TypeId` carefully—controls stacking. Use `SourceId` when conditions need bulk removal (equipment, skills). Handle missing components gracefully in lifecycle hooks.

## Design Strengths

**First-Class Entity Concept**: Conditions managed directly by `BaseEntity` eliminates component initialization order bugs and simplifies architecture.

**Automatic Processing**: Turn signal integration eliminates manual update loops. Consistent with action-based turn economy.

**Multiple Duration Modes**: Temporary, Permanent, WhileEquipped, WhileActive cover all common use cases with clean semantics.

**Source Tracking**: `SourceId` enables clean bulk operations for equipment and skills without complex bookkeeping.

**Clean Lifecycle**: Template method pattern guarantees consistent execution order. `OnRemoved` always called on expiration.

**Signal Decoupling**: Observer pattern enables UI and systems to react without entity knowing. Extensible without modification.

**Data-Driven Content**: YAML definitions enable rapid iteration. Designers create new effects without code changes.

## Design Trade-offs

**String-Based Types**: Simple and data-friendly but no compile-time YAML validation. Factory logs errors at runtime.

**Non-Stacking Limitation**: Prevents exploits and ensures predictable behavior but eliminates cumulative buffs. Intentional for balance.

**Single Target**: Current architecture single-target only. Area conditions require extending effect application, not condition system itself.

---

*See [effects.md](effects.md) for effect system details, [components.md](components.md) for component architecture patterns.*
