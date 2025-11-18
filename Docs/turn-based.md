# Turn-Based System

The turn-based system coordinates discrete gameplay turns between player and creatures through signal-based orchestration. Player and AI actions flow through identical validation and execution paths.

## Turn Cycle

**Alternating phases**: Player executes one action, all creatures execute one action each, cycle repeats.

**Flow**: `TurnManager` emits phase signals → `InputHandler` processes player input or `AISystem` processes creatures → entities execute actions → phase ends → next phase begins.

**State**: Single boolean (`IsPlayerTurn`) tracks current phase. Turn transitions occur only after successful action execution.

## Core Coordinators

**TurnManager**: Maintains turn state and emits phase transition signals (`PlayerTurnStarted`, `PlayerTurnEnded`, `CreatureTurnsStarted`, `CreatureTurnsEnded`). Enforces valid state transitions—prevents ending non-existent turns.

**InputHandler**: Translates keyboard input to actions during player phase. Validates `IsPlayerTurn` before processing movement/attack inputs. Non-turn-consuming inputs (menus, help) work during any phase. Listens for player's `TurnCompleted` signal to trigger phase transition.

**AISystem**: Processes all creatures sequentially during creature phase. Builds perception context per creature, evaluates goal scores, executes highest-priority goal's action. Signals turn manager when all creatures complete.

## Action Architecture

**Two-phase execution**: `CanExecute()` validates without side effects, `Execute()` applies changes and returns result.

**ActionContext**: Passed to all actions, contains system references (MapSystem, EntityManager, CombatSystem, etc.). Eliminates singleton dependencies, enables testing with mocks.

**ActionResult**: Returns success status, feedback message, and `ConsumesTurn` flag. Failed validation returns `ConsumesTurn = false` for free retries. Successful execution returns `ConsumesTurn = true` to advance turn.

**Turn consumption strategy**: Invalid actions (blocked movement, invalid targets) don't consume turns. Valid execution consumes turn regardless of outcome—attacking a missed target still costs a turn.

## Signal-Based Integration

Actions don't directly modify other systems—they emit signals through components. CombatSystem subscribes to attack signals, InventorySystem subscribes to item signals, MessageLog subscribes to feedback signals. This decouples action logic from system implementations.

**Example flow**: AttackAction calls `AttackComponent.RequestAttack()` → component emits `AttackRequested` → CombatSystem processes damage → CombatSystem emits `AttackHit` → UI and MessageLog respond.

## Player Turn Execution

1. InputHandler receives keyboard input
2. Validates `IsPlayerTurn` is true
3. Creates action instance (MoveAction, AttackAction, etc.)
4. Calls `Player.ExecuteAction(action, context)`
5. Player executes action, processes result
6. If `ConsumesTurn` true: emit `TurnCompleted`
7. InputHandler catches signal, calls `TurnManager.EndPlayerTurn()`
8. Turn manager transitions to creature phase

## Creature Turn Execution

1. TurnManager emits `CreatureTurnsStarted`
2. AISystem builds AIContext per creature (visibility, distance, components)
3. GoalEvaluator scores all available goals (0-100 range)
4. Execute highest-scoring goal
5. Goal creates action, calls `creature.ExecuteAction()`
6. Process next creature sequentially
7. After all creatures: call `TurnManager.EndCreatureTurns()`
8. Turn manager transitions back to player phase

## Component Integration

Actions query for components rather than entity types. MoveAction requires position component, AttackAction requires attack component, PickupAction requires inventory component. Missing components cause `CanExecute()` to return false.

**Emergence**: Entity capabilities determined by component composition. Adding AIComponent makes entity autonomous, adding AttackComponent enables combat, adding InventoryComponent enables pickup.

## Design Patterns

**Signal decoupling**: Systems communicate through signals rather than direct references. Enables independent testing and flexible composition.

**Utility-based AI**: Goals compete on numerical scores rather than rigid state transitions. Allows dynamic behavior blending and easy priority tuning.

**Context objects**: ActionContext and AIContext passed explicitly rather than accessed globally. Clear dependencies, testable with mocks.

**Sequential processing**: Creatures act one at a time in deterministic order rather than parallel. Simplifies debugging, ensures fairness, provides predictable gameplay.

**Action-goal composition**: Goals select appropriate actions based on state. Same action instances used by player input and AI. Enables behavior extension through new goals without modifying actions.

---

*See [actions.md](actions.md) for action system details and [ai.md](ai.md) for goal-based decision architecture.*
