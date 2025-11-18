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

## Adding New Actions

Complete action integration requires implementing the action interface, registering with input and AI systems, and ensuring component-based validation.

### Action Implementation

**Interface Definition**: Implement BaseAction abstract class defining GetName, CanExecute, and Execute methods. GetName returns descriptive identifier for logging and debugging. CanExecute performs side-effect-free validation checking preconditions. Execute applies effects assuming validation passed and returns detailed result.

**Validation Design**: CanExecute must be pure—no state modifications, no signal emissions, no component changes. Query for required components returning false if missing. Check game state preconditions (enough inventory space, valid target, sufficient resources). Validate spatial requirements (adjacency for melee, line-of-sight for ranged). AI and UI call CanExecute multiple times—performance matters but clarity matters more.

**Execution Contract**: Execute assumes CanExecute returned true—don't re-validate, apply effects immediately. Query components and call modification methods (HealthComponent.TakeDamage, InventoryComponent.AddItem). Emit signals for event notification (combat events, movement events). Construct ActionResult with success flag, descriptive message, and turn consumption flag. Messages follow existing tone and provide useful feedback.

**Turn Consumption**: Determine whether action consumes turn through ActionResult.ConsumesTurn flag. Failed preconditions never consume turns (free retries). Successful execution usually consumes turn (movement, attacks, item use). Instant actions like examining or waiting might succeed without consuming turn. Clear turn economy prevents player frustration.

**Component Dependencies**: Actions work through component interfaces, not entity types. Query for components with GetNodeOrNull pattern. Missing components cause validation failure, not crashes. Required components documented in action class. Examples: movement needs position, attacks need AttackComponent, healing needs HealthComponent.

**Context Usage**: ActionContext provides access to game systems. MapSystem for spatial queries (walkable tiles, line-of-sight). EntityManager for entity lookup (position to entity, entity list). CombatSystem for combat resolution (damage calculation, armor application). ActionFactory for creating sub-actions (future). Request only needed systems—context provides minimal disclosure.

**Result Messages**: Include descriptive messages explaining what happened. Success messages confirm action ("You move north", "You hit the goblin for 5 damage"). Failure messages explain why action failed ("Path blocked", "No target in range"). Messages appear in message log and inform player decisions. Use third person for AI entities ("The goblin attacks").

**Design Considerations**: Consider both player and AI use cases. Player actions need clear feedback; AI actions need evaluable outcomes. Spatial actions work with grid coordinates. Targeted actions validate target entity. Multi-step actions should decompose into simpler atomic actions. Complex logic belongs in components, not actions—actions orchestrate, components implement.

### Input System Integration

**Key Binding**: Player actions triggered by input handler translating key presses to action instances. Input handler maps keys to action creation (arrow keys → MovementAction with direction, 'g' → PickupItemAction at position, 'a-z' → ActivateItemAction with slot). Keeps actions independent of input mechanism—same action works for player keyboard, player gamepad, or AI selection.

**Action Creation**: Input handler instantiates action with parameters from game state. Movement direction from key mapping. Item activation from inventory slot. Attack target from player position and facing. Action receives all required parameters in constructor—no hidden dependencies.

**Validation Feedback**: Input handler can call CanExecute before execution for UI feedback. Show unavailable actions as disabled. Prevent invalid input (can't move into wall). Free validation enables responsive UI without turn consumption.

### AI System Integration

**Goal Selection**: AI goals evaluate game state and produce candidate actions. Threat detection creates AttackAction toward enemy. Low health creates FleeAction away from danger. Item pickup creates movement toward valuable items. Each goal scores actions by utility.

**Action Scoring**: Goals rate actions by expected value. Attack actions scored by damage potential and hit chance. Movement actions scored by distance to objective. Healing actions scored by health deficit. Scores enable priority-based selection among competing goals.

**Validation in Planning**: AI uses CanExecute to filter invalid actions before scoring. Don't score unreachable movement. Don't consider attacks without targets. Failed validation removes option from consideration. Enables AI to adapt to changing state.

**Execution Feedback**: AI examines ActionResult to update world model. Success messages inform learning. Failure messages indicate obstacles. Turn consumption affects planning horizon. Results drive next decision cycle.

### Testing and Validation

**Unit Testing**: Actions are highly testable due to explicit dependencies. Mock ActionContext with test doubles. Create test entities with specific components. Call CanExecute and verify preconditions. Call Execute and verify component state changes. Validate ActionResult properties.

**Integration Testing**: Test action with real components and systems. Verify signal emissions reach subscribers. Confirm turn consumption behavior. Test edge cases (blocked movement, invalid targets, missing resources). Ensure player and AI paths both work.

**Validation Consistency**: CanExecute and Execute must agree. If CanExecute returns true, Execute must not fail on same state. Race conditions possible with delayed execution—mitigate by immediate execution after validation. Document any state dependencies.

### Examples for Implementation

**Examine Action**: Read entity description without consuming turn. CanExecute checks entity exists at target position. Execute retrieves description from entity, emits message signal, returns success without turn consumption. No component modifications—pure information retrieval.

**Rest Action**: Skip turn to recover resources (future: health regeneration, stamina recovery). CanExecute always returns true—can always wait. Execute emits wait message, triggers recovery effects through signals, consumes turn. Simple but enables resource regeneration mechanics.

**Throw Action**: Ranged item attack with arc trajectory. CanExecute checks item is throwable, target in range, line-of-sight clear. Execute calculates damage, applies to target health, removes item from inventory (consumable), emits combat signal, consumes turn. Combines inventory and combat systems.

**Shout Action**: Alert nearby entities to position. CanExecute always succeeds. Execute queries EntityManager for entities in radius, calls AlertComponent.AlertToPosition on each, emits sound event for audio, consumes turn. Enables stealth mechanics and AI aggro management.

**Craft Action**: Combine items to create new item (future). CanExecute validates recipe exists, has required ingredients, inventory has space. Execute removes ingredients from inventory, adds crafted item, emits crafting signal, consumes turn. Complex validation with multiple resource checks.

## Related Documentation

- **[Components](components.md)** - Component architecture that actions interact with
- **[Entities](entities.md)** - Entity composition model that supports action execution
- **[Effects](effects.md)** - Effect system often triggered by actions

---

*The actions system represents the core interaction layer between player intent, AI decision-making, and game world state changes. Its unified interface enables consistent, testable, and extensible gameplay mechanics.*
