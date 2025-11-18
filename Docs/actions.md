# Actions System

The **Actions System** provides a unified framework for all discrete, turn-consuming activities in Pits of Despair. It bridges player input and AI decision-making through a common interface, enabling both human and AI-controlled entities to interact with the game world using the same mechanisms.

## Design Philosophy

**Unified Control Flow**: Player and AI entities execute actions through the same interface, ensuring consistent game logic.

**Two-Phase Pattern**: Separate validation from execution for UI previews, AI planning, and free retries without consuming turns.

**Composition-Friendly**: Actions work with component-based entities, not inheritance hierarchies.

**Resource Management**: Actions declare turn consumption, enabling turn economy where failed validations are free.

## Core Concepts

### Action Interface

All actions provide: **Identity** (descriptive name), **Validation** (side-effect-free precondition checking), **Execution** (effect application and results).

Enables AI planning, UI feedback on validity, and free retries for invalid actions.

### Action Results

Results contain success status, message (UI/logging feedback), and turn consumption flag. Separating logical success from resource cost enables failed movement retries (no turn consumed) and successful attacks (turn consumed).

### Action Context

Actions receive required systems through context object (map, entity manager, combat, factory). Minimal disclosure principle promotes loose coupling, clear dependencies, testability via mocking, and reduced interdependencies.

## Architectural Patterns

### Strategy Pattern

Actions are interchangeable strategies. Input handlers select player actions (keyboard → movement), goal evaluators select AI actions (threat → attack). Enables runtime selection, easy extension, and different strategies per actor.

### Two-Phase Execution

**Validation**: Check components, verify preconditions, return boolean (side-effect free).

**Execution**: Assume validation passed, modify components, emit signals, return detailed result (applies effects).

Benefits: AI evaluates options without committing, UI shows availability, failed preconditions don't waste turns.

### Event-Driven Effects

Actions trigger effects through component methods and signals rather than direct state modification. Action requests effect → component validates/applies → component emits signals → systems respond.

Enables decoupled logic, multiple system responses, extensible effects, and clear event chains.

### Component Composition

Actions query for components ("Has health component?") rather than checking types ("Is CombatEntity?"). Entities define capabilities through components, actions work with any entity having appropriate components, and new types emerge from component combinations.

## Integration Points

### Input to Actions

Input handler translates hardware events to actions: movement keys → directional movement, item keys → inventory actions, combat keys → attack actions. Keeps action definitions independent of input mechanisms.

### AI to Actions

AI goals evaluate state and produce actions (threat → attack, lost player → search, low health → flee). Utility-based scoring enables dynamic priority adjustments.

### Turn Management

Actions are the atomic unit of turn consumption. Player/creatures execute one action per turn; turn manager coordinates flow using action results. `ConsumesTurn` flag enables free retries, instant actions, and clear turn economy.

## Design Decisions & Trade-offs

**Two-Phase Validation**: Separate `CanExecute()`/`Execute()` lets AI evaluate options and UI preview validity without turn consumption. Trade-off: potential state mismatch between phases, mitigated by immediate execution.

**Action Context**: Pass systems via context instead of singletons for explicit dependencies, testability, and reduced coupling. Trade-off: parameter overhead, accepted for architecture benefits.

**Message Strings**: Include human-readable messages in results for logging, debugging, and UI feedback. Trade-off: string allocation overhead, mitigated by minimal cost and debugging value.

**Component Queries**: Check component presence instead of entity types for flexible composition and emergent entity types. Trade-off: runtime lookups vs compile-time safety, accepted for composition benefits.

## Action Categories

**Movement & Positioning**: Grid movement with collision, waiting/resting

**Combat**: Melee (adjacent), ranged (line-of-sight), area effects (future)

**Inventory**: Pickup, drop, equip/unequip, item activation

**Information**: Alerting entities, examining objects (future), social interactions (future)

**Environmental**: Doors, containers, traps (future)

All categories share the same interface with category-specific validation/effects.

## Conceptual Model

```
                    ACTIONS SYSTEM FLOW

Input Source          Action Layer         Game Systems
────────────         ─────────────         ────────────

Player Input ──→     Action Object    ──→  Component Effects
(keyboard)           - Validate            - Health changes
                     - Execute             - Position changes
                     - Report              - Inventory updates
AI Goals ──────→     Same Interface   ──→  Signal Emission
(utility eval)                             - Combat events
                                          - Movement events
                                          - Item events

              All return ActionResult:
              - Success (bool)
              - Message (string)
              - ConsumesTurn (bool)
```

## Extension Guidelines

When adding actions: define preconditions, identify required components, determine turn cost, consider AI usage/decision-making, plan signal emission, write validation first.

Follow patterns: component queries (not type checks), signal emission for effects, descriptive messages, player and AI support.

## Related Documentation

- **[Components](components.md)** - Component architecture that actions interact with
- **[Entities](entities.md)** - Entity composition model that supports action execution

---

*The actions system represents the core interaction layer between player intent, AI decision-making, and game world state changes. Its unified interface enables consistent, testable, and extensible gameplay mechanics.*
