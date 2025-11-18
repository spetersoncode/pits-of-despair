# Status System

The status system manages temporary, turn-based conditions (buffs, debuffs, persistent effects) that apply to entities for a specified duration. Statuses integrate with the turn system for automatic processing, emit signals for UI feedback, and use multi-source modifier tracking for clean application and removal.

## Core Architecture

**Status**: Abstract base class defining the lifecycle contract for all status effects. Statuses track duration, remaining turns, and provide hooks for application (OnApplied), per-turn processing (OnTurnProcessed), and removal (OnRemoved). Each status has a unique TypeId controlling stacking behavior—reapplying identical types refreshes duration rather than stacking effects.

**StatusComponent**: Godot Node component managing all active statuses on an entity. Subscribes to turn manager signals based on IsPlayerControlled flag, automatically processes statuses each turn, decrements remaining turns, removes expired statuses, and emits signals for UI feedback. StatusComponent enforces non-stacking rules and orchestrates lifecycle timing.

**ApplyStatusEffect**: Bridge between instantaneous effects and status system. Creates status instances from string identifiers using factory pattern, adds statuses to target's StatusComponent, returns structured effect results. Enables data-driven status application through YAML item definitions.

## Status Lifecycle

**Three-Phase Lifecycle**: Template method pattern with virtual hooks guarantees consistent execution order and prevents lifecycle bugs.

**OnApplied**: Fires when status first added to entity. Used for one-time setup—register stat modifiers with unique source IDs, initialize visual effects, emit application messages. Returns message string for UI display. Component queries happen here to cache references.

**OnTurnProcessed**: Optional hook executing each turn while status active. Used for recurring effects—damage over time, healing, random effects. Returns message string for per-turn feedback. Not all statuses implement this (simple buffs skip it).

**OnRemoved**: Fires on expiration or manual removal. Used for cleanup—unregister stat modifiers by source ID, restore original state, emit expiration messages. Returns message string. Critical for preventing orphaned modifiers.

**Duration Refresh**: When status with identical TypeId reapplied, existing status calls RefreshDuration. If new duration exceeds remaining, updates to new value. Otherwise keeps current. Prevents stacking exploits while allowing duration extension.

## Turn Integration

**Signal-Based Processing**: StatusComponent subscribes to TurnManager signals during initialization. No manual update loops or iteration—turn manager orchestrates timing automatically.

**Phase Awareness**: IsPlayerControlled flag determines which turn signal to subscribe to. Player-controlled entities (player) subscribe to PlayerTurnStarted. Creatures subscribe to CreatureTurnsStarted. Ensures statuses tick when entity acts, not at arbitrary times.

**Automatic Decrementing**: Each turn start, StatusComponent processes all active statuses—calls OnTurnProcessed hooks, decrements RemainingTurns, collects expired statuses, calls OnRemoved for each expired, emits signals for UI. Happens automatically without component knowledge of status internals.

**Turn Semantics**: Status lasting "N turns" means N turns the entity takes actions. Duration independent of calendar time or other entities' turns. Player buff lasts 10 player turns regardless of creature activity.

## Non-Stacking Design

**TypeId Uniqueness**: Each status defines TypeId string identifier (armor_buff, poison, haste). StatusComponent maintains at most one status per TypeId—duplicates refresh duration rather than accumulate effects.

**Refresh Logic**: Adding status already present compares durations. New duration greater than remaining updates RemainingTurns. New duration less than or equal ignores application (existing status more potent). Message indicates duration refreshed.

**Multiple Types Coexist**: Different TypeIds stack freely. Armor buff and haste can both apply. Two different armor buffs (potion vs spell) use same TypeId so don't stack. Prevents buff stacking exploits while allowing diverse effects.

**Design Rationale**: Non-stacking ensures predictable, balanced gameplay. Players can't stack infinite armor buffs. UI displays cleanly (one icon per effect type). Matches roguelike tradition—consistent, understandable rules.

## Multi-Source Modifier Integration

**GUID-Based Source IDs**: Statuses generate unique source identifiers when registering modifiers. Pattern: `status_<type>_<guid>` ensures each status instance tracked separately. Different armor buff instances have different source IDs despite same TypeId.

**Clean Registration**: OnApplied generates source ID, calls component methods with ID (stats.AddArmorSource, stats.AddStrengthModifier). Component tracks modifier by source. Multiple sources coexist without conflicts.

**Clean Removal**: OnRemoved calls component removal methods with source ID (stats.RemoveArmorSource). Component removes exact modifier added by this status. No orphaned modifiers from forgotten cleanup.

**Stat Component Integration**: StatsComponent designed for multi-source modifiers. Equipment provides base stats, buffs add temporary sources, debuffs subtract. Each tracked independently. Status system leverages this for temporary stat modifications.

**Example Flow**: Armor potion creates ArmorBuffStatus with Amount=5, Duration=10. OnApplied generates source ID `status_armor_buff_a3f2c1d4`, calls stats.AddArmorSource with ID and amount. Ten turns later, OnRemoved calls stats.RemoveArmorSource with same ID. Clean addition and removal.

## Signal Communication

**StatusComponent Signals**: Three signals enable loose coupling with UI and other systems.

**StatusAdded**: Emitted when status first applied with status name. UI can display icon or message. Systems can react to specific status types (AI flees from poisoned player).

**StatusRemoved**: Emitted when status expires or manually removed with status name. UI removes icon. Systems react to status ending.

**StatusMessage**: Emitted for status lifecycle events with message text. Application messages, turn processing messages, expiration messages all use this signal. GameHUD subscribes to display in message log.

**TurnManager Integration**: StatusComponent subscribes to turn signals (PlayerTurnStarted, CreatureTurnsStarted). Turn manager doesn't know about statuses—just emits turn phase signals. StatusComponent reacts by processing statuses. Clean decoupling.

**Observer Pattern**: UI and systems observe StatusComponent without StatusComponent knowing observers exist. Add new status visualizations without modifying core component. Extensible without modification.

## Component Composition

**Entity Integration**: StatusComponent is child node on entities. Player scene includes StatusComponent with IsPlayerControlled=true. Creatures receive StatusComponent from EntityFactory with IsPlayerControlled=false. Presence of component indicates entity can receive status effects.

**Component Queries**: Effects query for StatusComponent before applying statuses. Missing component causes graceful failure—effect returns failure result, no crash. Not all entities need status support (decorative objects, simple items).

**Sibling References**: StatusComponent queries parent entity for other components when processing statuses. Armor buffs query StatsComponent. Poison queries HealthComponent. Standard sibling communication pattern.

**Initialization Order**: StatusComponent._Ready finds TurnManager via parent hierarchy (GetParent()?.GetParent()). Subscribes to appropriate turn signal. Handles missing TurnManager gracefully (logs error, continues without turn processing).

**Cleanup**: _ExitTree disconnects turn signals to prevent memory leaks. Signal disconnection happens automatically when entity removed from scene. Critical for proper Godot lifecycle management.

## Status Implementations

Current implementations demonstrate patterns for common effect types.

**ArmorBuffStatus**: Temporary armor increase with configurable amount and duration. OnApplied generates source ID, queries StatsComponent, calls AddArmorSource. OnRemoved calls RemoveArmorSource with same ID. No OnTurnProcessed—simple buff without recurring effect. Shows multi-source modifier pattern.

**Future Implementations**: Poison (OnTurnProcessed damages health per turn), Regeneration (OnTurnProcessed heals per turn), Haste (OnApplied modifies speed stat), Slow (speed penalty), Paralysis (prevents actions), Confusion (randomizes movement), Invisibility (modifies visibility flag).

## Data Configuration

**YAML Definitions**: Statuses defined in item data using ApplyStatusEffect. Effect definition specifies type (apply_status), statusType (string identifier), amount (modifier strength), duration (turns). Data-driven content enables rapid iteration.

**Factory Pattern**: ApplyStatusEffect contains factory method mapping statusType strings to Status subclass instantiation. Switch statement creates correct status type, passes amount and duration to constructor. Unknown types log errors for immediate designer feedback.

**Item Integration**: Items specify effects list containing status applications. Multiple status effects in sequence possible. Mix instantaneous and status effects freely. Item consumed if any effect succeeds.

## System Integration

**Effects System**: Status application is specialized effect type. ApplyStatusEffect bridges instantaneous effect architecture to duration-based status system. See **[effects.md](effects.md)** for effect system details.

**Turn System**: StatusComponent subscribes to turn signals, processes automatically. Turn consumption happens at action level (ActivateItemAction costs turn), not status level. Status processing free—doesn't consume additional turns.

**Stats System**: Statuses modify stats through multi-source modifier system. Clean registration and removal via source IDs. Equipment, buffs, debuffs coexist without conflicts. See stats component documentation for modifier details.

**Combat System**: Currently separate from combat resolution. Attacks use AttackComponent and dice rolls, not effects. Future potential for combat-triggered statuses (poison on hit, life steal). Statuses can modify combat through stat modifiers (armor affects defense).

**Action System**: ActivateItemAction triggers status application through effect chain. Two-phase validation—action validates item usability, effect validates target has StatusComponent. Failure at either phase prevents resource consumption. See **[actions.md](actions.md)** for action details.

**UI System**: GameHUD connects to player's StatusComponent signals. StatusMessage signal feeds message log with color-coded feedback. StatusAdded/Removed signals enable future status icon display. UI decoupled from status processing.

## Extensibility

### Adding New Status Types

**Step 1 - Implement Status Subclass**: Inherit from Status base class. Define Name (display), TypeId (stacking ID), implement lifecycle hooks. OnApplied for setup, OnTurnProcessed for recurring effects (optional), OnRemoved for cleanup. Constructor accepts amount and duration.

**Step 2 - Register in Factory**: Add case to ApplyStatusEffect.CreateStatus switch statement. Map statusType string to status instantiation. Pass amount and duration from effect definition to status constructor. Log error for invalid parameters.

**Step 3 - Define in YAML**: Create item with apply_status effect. Specify statusType matching factory case, amount for modifier strength, duration in turns. Test with simple consumable before complex compositions.

**Step 4 - Test Integration**: Verify application message, turn processing, expiration message. Check stat modifiers applied and removed cleanly. Confirm non-stacking behavior with duplicate applications. Validate UI feedback.

**Component Integration**: Query required components in lifecycle hooks. HealthComponent for damage/healing, StatsComponent for modifiers, MovementComponent for speed changes. Return empty string if component missing (silent failure). Component presence determines applicability.

**Design Considerations**: Choose TypeId carefully—controls stacking. Generate unique source IDs for modifiers. Handle missing components gracefully. Write clear messages for application and removal. Consider reapplication behavior (duration refresh makes sense for most statuses).

### Advanced Patterns

**Multi-Component Statuses**: Status affecting multiple systems queries all required components. Slow status modifies both movement speed and evasion. OnApplied registers modifiers with both Stats and Movement components. OnRemoved cleans both. Atomic application—all or nothing.

**Triggered Effects**: Status reacting to events subscribes to signals in OnApplied. Damage reflection subscribes to HealthComponent.DamageTaken. OnTurnProcessed checks flag set by signal handler. OnRemoved disconnects signals. Lifecycle management critical for preventing signal leaks.

**Conditional Processing**: OnTurnProcessed checks conditions before applying effects. Poison only damages if health above threshold. Regeneration heals if below max. Status can query multiple components for decision logic.

**Stacking Variants**: Custom stacking behavior overrides default by checking in ApplyStatusEffect before adding. Accumulating poison adds damage per turn rather than refreshing duration. Requires tracking accumulated value. Non-standard pattern—use sparingly.

## Design Strengths

**Automatic Processing**: Turn signal integration eliminates manual update loops. Status ticking happens automatically without special-case logic. Consistent with action-based turn economy.

**Turn Phase Awareness**: IsPlayerControlled flag ensures statuses process at correct turn phase. Player buffs tick on player turn, creature buffs on creature turn. Prevents timing exploits.

**Clean Lifecycle**: Template method pattern guarantees consistent execution order. Developers can't forget cleanup—OnRemoved always called. Prevents orphaned modifiers.

**Multi-Source Tracking**: GUID-based source IDs enable clean modifier management. Multiple statuses, equipment, and buffs coexist without conflicts. Removal doesn't affect other sources.

**Signal Decoupling**: Observer pattern enables UI and systems to react without StatusComponent knowing. Extensible without modification. New features subscribe to existing signals.

**Data-Driven Content**: YAML definitions enable rapid iteration. Designers create new potions and scrolls without code changes. Factory pattern provides type safety boundary.

**Composition-Friendly**: StatusComponent as child node aligns with entity composition. Works with any entity type. Component queries determine applicability.

**Graceful Degradation**: Missing StatusComponent causes effect failure, not crash. Missing required components in status return silent failure. Robust to edge cases.

## Design Trade-offs

**String-Based Types**: Simple and data-friendly but no compile-time YAML validation. Factory logs errors for unknown types at runtime. Trade-off favors designer iteration speed over type safety.

**Non-Stacking Limitation**: Prevents exploits and ensures predictable behavior but eliminates cumulative buffs. Intentional design choice for balance. Workaround: different TypeIds for stackable variants (rare cases).

**Phase Coupling**: StatusComponent initialization depends on finding TurnManager via parent hierarchy. Fragile to scene structure changes. Trade-off: simple pattern vs dependency injection complexity.

**Single Target**: Current architecture single-target only. Area-of-effect status application requires extending effect application, not status system itself. Status processing unaffected by multi-target.

**Instantaneous vs Duration Split**: Status effects separate from instantaneous effects. Delayed one-time effects (damage in N turns) awkward—requires Status with OnTurnProcessed checking counter. Workaround exists but inelegant.

---

*See [effects.md](effects.md) for effect system integration and instantaneous effects. See [components.md](components.md) for component architecture patterns.*
