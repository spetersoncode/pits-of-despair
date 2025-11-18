# Effects System

The effects system provides instantaneous and time-based modifications to entities through a data-driven, component-based architecture. Effects are cleanly separated into two complementary mechanisms: instantaneous effects (healing, teleportation) and status effects (buffs, debuffs with duration).

## Core Architecture

**Effects**: Stateless, pure-function style operations that apply modifications and return results. Effects are lightweight C# classes (not Godot nodes) instantiated on-demand, applied once, then discarded. Each effect validates preconditions, queries for required components, applies modifications, and returns structured feedback containing success status, message text, and color for UI display.

**Status Effects**: Time-based persistent conditions with lifecycle hooks. Status effects track remaining duration, process each turn, and clean up when expired. Unlike instantaneous effects, statuses maintain state and integrate with the turn system for automatic processing.

**Effect Results**: Structured return values encapsulate success/failure, human-readable messages, and display colors. Effects can fail gracefully without exceptions—missing required components or invalid states return failure results rather than crashing.

**Action Context**: Provides effects with access to game systems (map, entities, combat) without tight coupling. Effects receive context as a parameter, enabling complex spatial queries and entity interactions while remaining testable in isolation.

## Effect Application Flow

**Trigger Path**: Effects are primarily triggered through item activation. Player uses item → action validates → retrieve effects from item data → apply each effect in sequence → collect results → consume item if any effect succeeded → emit feedback signals.

**Two-Phase Validation**: Action layer validates item exists and is usable (first phase). Each effect validates its specific preconditions when applied (second phase). Failures at either phase prevent resource consumption—players don't waste items on invalid targets.

**Component Queries**: Effects query targets for required components rather than checking entity types. Healing effects check for HealthComponent, teleport effects check position. This enables effects to work with any entity composition—player and creatures share identical effect code paths.

**Result Collection**: Multiple effects on single item execute sequentially. At least one must succeed to consume the item. Partial success still consumes resources (some effects worked). All feedback messages collected and displayed to player.

## Instantaneous Effects

Current instantaneous effect types demonstrate the system's flexibility and composition-friendly design.

**Healing**: Restores hit points to target. Queries for HealthComponent, validates against max HP, returns actual healing amount accounting for cap. Fails gracefully if target lacks health component or is already at maximum.

**Teleportation**: Moves entity to random walkable position anywhere on map. Queries all walkable tiles from map system, filters occupied positions via entity manager, selects random valid destination. Fizzles if no valid positions exist (rare edge case).

**Blink**: Short-range random teleport within radius. Finds valid positions within Chebyshev distance (default 5), selects random unoccupied walkable tile. Unlike teleportation, range-limited and tactical. Fizzles if no valid nearby positions.

**Status Application**: Bridge between instantaneous and duration-based systems. Creates status instances and adds to target's status component. Uses internal factory to instantiate correct status type from string identifier and parameters. See **[status.md](status.md)** for comprehensive status system architecture.

## Data Configuration

**YAML Definitions**: Effects defined in item data files using flexible schema. Effect definitions specify type (string identifier), amount, range, duration as needed. Different effect types use different parameter subsets—healing uses amount, blink uses range, status application uses all three.

**Factory Pattern**: Item data contains factory method converting effect definitions to concrete effect instances. String-based type identification enables data-driven content. Unknown effect types logged as errors—designers get immediate feedback on typos or missing implementations.

**Designer-Driven Content**: New potion or scroll variants created entirely in YAML without code changes. Designers iterate rapidly on item effects, amounts, durations. Code provides effect implementations; data assembles them into items.

**Effect Composition**: Items can have multiple effects in sequence. Healing potion that also grants temporary armor. Teleport scroll that blinds caster. Poison that damages and slows. Emergent complexity from simple effect building blocks.

## System Integration

**Item Integration**: ActivateItemAction is unified entry point for effect triggering. Works with consumables (destroyed on use) and charged items (multiple uses, recharge over turns). Effect success determines consumption—at least one effect must succeed to use charge or destroy consumable.

**Component Integration**: Effects work with composition-based entity architecture. Query for capabilities rather than checking types. Any entity with HealthComponent can be healed. Any entity with StatusComponent can receive statuses. Player-specific and creature-specific code paths eliminated.

**Turn System Integration**: Status effects process automatically each turn via signal subscription. No manual update loops or iteration. Turn consumption happens at action level (ActivateItemAction costs turn), not effect level. Consistent with action system philosophy.

**Combat Separation**: Effects currently separate from combat resolution. Combat uses attack components and dice rolls; effects are targeted modifications. Clear architectural boundary enables independent development. Future potential for combat-triggered effects (poison on hit, life steal).

## Design Patterns

**Strategy Pattern**: Effect is abstract strategy defining Apply interface. Concrete effects (Heal, Blink, Teleport) are strategies. ActivateItemAction is context executing strategies. Runtime selection based on item data enables data-driven effect composition.

**Template Method**: Status base class defines lifecycle hooks. Concrete statuses override OnApplied, OnTurnProcessed, OnRemoved for specific behavior. StatusComponent orchestrates lifecycle timing. Guarantees consistent execution, prevents lifecycle bugs.

**Factory Pattern**: Centralized effect creation from untyped YAML data. Type-safe instantiation with validation. Extension point for new effect types—add case to switch statement, implement effect class, define YAML schema.

**Observer Pattern**: StatusComponent emits signals for status events (added, removed, turn processing). UI and systems subscribe without StatusComponent knowing about them. Decoupled communication enables extensibility without modification.

**Two-Phase Validation**: Item activation validates first, then effects validate preconditions. Granular failure feedback. Free retries for invalid actions—doesn't waste resources on precondition failures.

## Extensibility

### Adding Instantaneous Effects

Complete effect integration requires touching four architectural layers: effect implementation, factory registration, data schema, and effect definition.

**Effect Implementation**: Subclass Effect base class and implement Apply method receiving target entity and action context. Define constructor accepting parameters from YAML (amount, range, etc.). Query target for required components—return failure result if missing. Apply modifications through component interfaces. Return success result with message and color for UI feedback. Effect should validate all preconditions and fail gracefully.

**Factory Registration**: Register effect in ItemData factory converting YAML definitions to effect instances. Add case to type switch mapping string identifier to effect instantiation. Pass parameters from effect definition to effect constructor. Handle missing or invalid parameters with error logging. Factory provides type safety boundary between untyped data and typed effect objects.

**Data Schema**: Extend EffectDefinition data structure if effect requires parameters not already present. Effect definitions support Type (string identifier), Amount, Range, Duration fields. New parameter types require adding properties to definition structure and populating from YAML deserializer.

**Effect Definition**: Create YAML item definitions using new effect type. Specify type identifier matching factory case. Provide required parameters. Effects automatically available to any item type (potions, scrolls, wands, etc.). Test with simple consumable item before using in charged items or complex compositions.

**Design Considerations**: Component dependencies determine which entities effect can target. Spatial effects need map system access through context. Multi-step effects should decompose into simpler effects composed in YAML. Effect messages should follow existing tone and formatting patterns for consistency.

**Examples for Implementation**: Direct damage (ignore armor, query HealthComponent only). Attribute boosts (query StatsComponent, apply temporary modifier). Map revelation (context provides MapSystem, mark tiles visible). Item identification (context provides inventory, mark item as identified). Summon creature (context provides EntityManager, spawn at position).

### Adding Status Effects

Status effects extend instantaneous effects with turn-based lifecycle requiring additional integration with StatusComponent and turn management. See **[status.md](status.md)** for complete implementation guide including status subclass creation, factory registration, lifecycle hooks, component integration, and turn processing patterns.

### Advanced Extensions

**Area of Effect**: Current architecture supports single-target only. Multi-target effects require choosing extension approach—modify Effect base class Apply signature to accept target collection, or create AreaEffect subclass with area-specific Apply method. Context provides EntityManager for spatial queries (get entities in radius). Map system provides tile pattern matching (cone, line, circle). Area selection needs targeting system allowing player to choose impact point. Friendly fire rules determine whether effects apply to allies, enemies, or both. Visual feedback shows affected area before confirmation.

**Triggered Effects**: Conditional effects fire in response to events rather than item activation. Requires event subscription mechanism—status effects can leverage OnTurnProcessed for turn-based triggers. Damage triggers require subscribing to HealthComponent signals (DamageTaken, Healed). Movement triggers subscribe to position change events. Attack triggers subscribe to combat resolution signals. Triggered effects need lifecycle management—subscribe on application, unsubscribe on removal. Could extend Status system with event subscription hooks, or create separate TriggeredEffect system parallel to Status. Condition evaluation determines when to fire—health threshold, damage type, target type, etc.

## Design Strengths

**Data-Driven Flexibility**: Effects defined in YAML enable rapid iteration without code changes. Designers create content independently. New potion variants trivial to add. Prototyping fast. Potential modding support.

**Composition-Friendly**: Component queries eliminate entity type checking. No inheritance hierarchies required. Player and creatures share effect code. Emergent complexity from component combinations. Extensible through component addition.

**Clean Separation**: Effects define what happens. Actions define when (turn consumption). Components define where (target capabilities). Statuses define how long. Clear responsibilities prevent tangling.

**Graceful Degradation**: Missing components cause informative failures, not crashes. Invalid applications don't waste resources. Clear feedback messages explain failures. Robust to edge cases.

**Testability**: Effects are pure functions with explicit dependencies. Mockable action context for unit testing. Deterministic status lifecycle. No hidden state or global dependencies.

## Design Trade-offs

**String-Based Types**: Simple and data-friendly but no compile-time YAML validation. Factory logs errors for unknown types at runtime. Trade-off favors designer iteration speed over type safety.

**Single-Target Limitation**: Simplifies implementation and semantics but prevents area-of-effect abilities. Future extension possible through new base class or signature modification. Current constraint acceptable for item-based effects.

**Instantaneous vs Duration Split**: Clear separation between one-time and persistent effects but complicates delayed effects (apply damage in N turns). Workaround: Status with OnTurnProcessed achieves similar result.

**Non-Stacking Statuses**: Prevents exploits and ensures predictable behavior but eliminates cumulative buffs. Intentional design choice for game balance. Same effect type refreshes duration rather than stacking magnitude.

**Centralized Factories**: Single point of creation enables validation and logging but requires all effect types known to factory. Easy to extend with new cases. Clear error messages guide implementation.

---

*See [status.md](status.md) for status system architecture. See [components.md](components.md) for component architecture and composition patterns. See [actions.md](actions.md) for action system integration and turn consumption.*
