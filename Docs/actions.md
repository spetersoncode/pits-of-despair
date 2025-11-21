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

**Combat**: Melee (adjacent), reach (extended melee with LOS), ranged (line-of-sight), area effects (future)

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

## Adding New Actions

Complete action integration requires implementing the action interface, registering with input and AI systems, and ensuring component-based validation.

### Action Implementation

Implement BaseAction abstract class defining `GetName()`, `CanExecute()`, and `Execute()` methods. Validation must be pure—no state modifications, signal emissions, or component changes. Query for required components with GetNodeOrNull pattern, returning false if missing. Check spatial requirements (adjacency, line-of-sight) and game state preconditions (inventory space, resources, valid targets).

Execution assumes validation passed—apply effects immediately without re-validation. Query components and call modification methods (HealthComponent.TakeDamage, InventoryComponent.AddItem). Emit signals for event notification. Construct ActionResult with success flag, descriptive message, and turn consumption flag.

**Turn Consumption and Context**: Failed preconditions never consume turns (free retries). Successful execution usually consumes turn. ActionContext provides system access (MapSystem, EntityManager, CombatSystem)—request only needed systems. Actions work through component interfaces, not entity types.

**Result Messages**: Include descriptive feedback. Success messages confirm actions ("You move north"), failure messages explain rejection ("Path blocked"). Use third person for AI entities ("The goblin attacks").

**Design Considerations**: Consider both player and AI use cases. Player actions need clear feedback; AI actions need evaluable outcomes. Multi-step actions should decompose into atomic actions. Complex logic belongs in components—actions orchestrate, components implement.

### System Integration

**Input Binding**: Input handler translates key presses to action instances (arrow keys → MovementAction, 'g' → PickupItemAction). Actions remain independent of input mechanism—same action works for player keyboard, gamepad, or AI selection. Input handler can call `CanExecute()` before execution for UI feedback and free validation.

**AI Integration**: AI goals evaluate state and produce candidate actions. Goals use `CanExecute()` to filter invalid actions before scoring. ActionResult feedback updates AI world model—success messages inform learning, turn consumption affects planning horizon.

### Testing and Validation

Actions are highly testable due to explicit dependencies. Mock ActionContext with test doubles. Create test entities with specific components. Verify `CanExecute()` preconditions and `Execute()` state changes. Test with real components to verify signal emissions and turn consumption. Ensure `CanExecute()` and `Execute()` agree—if validation passes, execution must not fail on same state.

### Implementation Examples

**Examine Action**: Read entity description without consuming turn. Validation checks entity exists at position. Execution retrieves description, emits message, returns success without turn cost.

**Throw Action**: Ranged item attack with arc trajectory. Validation checks item throwability, target range, and line-of-sight. Execution calculates damage, applies to target health, removes consumable item, emits combat signal, consumes turn. Demonstrates inventory and combat system integration.

**Shout Action**: Alert nearby entities to position. Validation always succeeds. Execution queries EntityManager for entities in radius, calls AlertComponent.AlertToPosition on each, emits sound event, consumes turn. Enables stealth mechanics and AI coordination.

**Reach Attack Action**: Melee weapon attack with extended range (e.g., spears). Validation checks weapon is melee type with range > 1, target within weapon range using Chebyshev distance, line-of-sight to target (unlike basic melee), and target has health component. Execution uses standard melee mechanics (STR modifier for attack/damage) via AttackComponent.RequestAttack. Demonstrates hybrid combat mechanics—melee damage model with ranged targeting requirements. Activated via activate menu (A key) for equipped reach weapons, entering targeting mode.

## See Also

- [components.md](components.md) - Component architecture that actions interact with
- [entities.md](entities.md) - Entity composition model that supports action execution
- [effects.md](effects.md) - Effect system often triggered by actions
