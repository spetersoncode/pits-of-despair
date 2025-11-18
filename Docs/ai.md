# AI System

The AI system uses **goal-based utility scoring** for decision-making. Each AI entity maintains available goals, evaluates them every turn using utility scores (0-100 range), and executes the highest-scoring goal. This enables flexible, priority-driven behavior that naturally blends different AI personalities without rigid state transitions.

## Architecture

### Core Components

**AIComponent**: Data and state management for AI entities. Stores available goals, current goal state, pathfinding data (current path queue), positional tracking (spawn position, last known player position), and behavior state (turn counters for search/flee). Designer-tweakable exports: `SearchTurns` (default: 12), `SearchRadius` (default: 3), `FleeTurns` (default: 20).

**VisionComponent**: Lightweight marker component indicating vision capability. Stores `VisionRange` (default: 10 tiles). Used by both player fog-of-war and AI perception.

**AISystem**: Central coordinator orchestrating AI decision-making. Subscribes to `TurnManager.CreatureTurnsStarted` signal, processes all registered AIComponents by building context, evaluating goals, handling transitions, and executing selected goals. Signals `TurnManager.EndCreatureTurns()` when complete.

**AIContext**: Per-turn perception bundle built by AISystem. Provides entity references, player reference, system references (MapSystem, EntityManager), perception state (IsPlayerVisible, DistanceToPlayer), and component caches (VisionComponent, HealthComponent, AttackComponent).

### Decision-Making Flow

**Evaluation Phase**: Build AIContext with current state, update state tracking (visibility, distance, turn counters), call `GoalEvaluator.EvaluateBestGoal()` on all available goals, return goal with highest score.

**Transition Phase**: If goal changed, call `OnDeactivated()` on old goal and `OnActivated()` on new goal. Enables cleanup and initialization for goal-specific logic.

**Execution Phase**: Call `Execute()` on selected goal, which returns `ActionResult` with success status and message.

### Goal Interface

All goals inherit from abstract `Goal` base class:

- `CalculateScore(AIContext)`: Returns 0-100 utility score (0 = invalid, higher = more desirable)
- `Execute(AIContext)`: Performs the goal's action, returns `ActionResult`
- `OnActivated(AIContext)`: Called when goal becomes active
- `OnDeactivated(AIContext)`: Called when goal stops being active
- `GetName()`: Returns debug name string

## Available Goals

**MeleeAttackGoal**: Score 90f when player visible. If adjacent, executes `AttackAction`; otherwise pathfinds to player using A* and moves toward them. Updates last known player position and turn counters. Handles path blocking by recalculating when obstacles appear.

**SearchLastKnownPositionGoal**: Score 70f (decays 10 points per turn since player seen, minimum 0). Paths to last known player position, wanders randomly within `SearchRadius` tiles of that position for `SearchTurnsRemaining` turns. Clears last known position when search expires, falling through to next goal.

**ReturnToSpawnGoal**: Score 10f base + (3 per turn since player seen). Paths to spawn position. Score increases gradually, encouraging creatures to give up and return home. Prevents endless wandering.

**WanderGoal**: Score 20f (low priority). Randomly selects from valid adjacent tiles (8 directions), checking walkability and occupancy. Used for passive roaming behavior.

**IdleGoal**: Score 1f (absolute fallback, always valid). Does nothing—entity waits in place. Ensures entities always have a valid action.

**FleeForHelpGoal**: Score 85f (when player visible), 75f base (decays when fleeing). Multi-level fleeing: attempts to flee toward nearby allies using Dijkstra pathfinding, falls back to fleeing directly away from player, falls back to fleeing from last known position. Yells for help every 4 turns. Ally detection finds creatures with SearchLastKnown goal within 20-tile radius.

## Perception and Pathfinding

**FOVCalculator**: Recursive shadowcasting algorithm providing symmetric, efficient line-of-sight. Casts light through 8 octants from origin position within vision range, respecting walls as blockers. Returns HashSet of visible grid positions. Completes in O(vision_range²).

**AStarPathfinder**: Uses Chebyshev distance heuristic for 8-directional movement with equal cost. Checks terrain walkability and treats occupied tiles as obstacles (allows pathfinding to player position for squeeze-through). Returns queue of GridPositions to follow.

**DijkstraMapBuilder**: Multi-target pathfinding for fleeing behavior. Flood-fills from multiple goal positions simultaneously, builds distance map where each cell equals distance to nearest goal. Gradient descent follows downhill to nearest goal. Used by FleeForHelpGoal to find nearest ally.

## System Integration

**Turn Coordinator**: TurnManager emits `CreatureTurnsStarted` signal → AISystem processes all creatures → AISystem calls `EndCreatureTurns()` → cycle repeats with `PlayerTurnStarted`.

**Action System**: All AI movement and combat goes through actions (`MoveAction`, `AttackAction`, `YellForHelpAction`, `WaitAction`). Goals execute actions via `entity.ExecuteAction(action, context)`. Actions validate and execute atomically.

**Entity Factory**: Creates AIComponent and initializes goals during creature instantiation using GoalFactory.

**Combat and Movement**: CombatSystem executes AttackActions issued by goals, MovementSystem executes MoveActions.

**Map and Entity Queries**: MapSystem provides terrain walkability for pathfinding validation, EntityManager tracks creature positions with O(1) occupancy lookup.

## Configuration

**GoalFactory**: Registry maps string IDs to goal factories:
- "MeleeAttack" → MeleeAttackGoal
- "SearchLastKnown" → SearchLastKnownPositionGoal
- "ReturnToSpawn" → ReturnToSpawnGoal
- "Wander" → WanderGoal
- "FleeForHelp" → FleeForHelpGoal
- "Idle" → IdleGoal

**Creature Data**: YAML files in `Data/Creatures/` specify goal lists, vision range, and AI flags. Example:

```yaml
name: goblin
goals:
  - MeleeAttack
  - SearchLastKnown
  - ReturnToSpawn
visionRange: 10
hasMovement: true
hasAI: true
```

Idle goal automatically added if not specified. Goal order doesn't affect execution—scoring determines priority.

## AI Personalities

**Aggressive** (Rats, Goblins): Goals [MeleeAttack, SearchLastKnown, ReturnToSpawn]. Attack when player visible, search briefly when lost, return to patrol after timeout, fallback to wandering.

**Cowardly** (Goblin Scouts): Goals [FleeForHelp]. Flee when player visible, call allies, maintain safe distance, eventually stop fleeing when flee turns expire.

**Passive Roamers** (Rats): Goals [MeleeAttack, Wander]. Only attack if player adjacent, otherwise wander randomly. No searching or returning.

## Design Patterns

**Utility Scoring Over State Machines**: Goals compete based on utility rather than rigid state transitions. Allows flexible blending—fleeing creature naturally transitions from FleeForHelp to Wander as flee turns decay.

**Composition of Goals**: Creature behavior emerges from available goals plus scoring functions. Different goal combinations create distinct personalities.

**Per-Turn Pathfinding**: Goals recalculate paths each turn rather than caching across turns. Adjusts to dynamic obstacles (moving creatures) at small CPU cost.

**State Tracking as Context**: AIComponent tracks shared state variables (TurnsSincePlayerSeen, LastKnownPlayerPosition, SearchTurnsRemaining, FleeturnsRemaining) read by multiple goals. AISystem updates state once per turn, reducing duplication.

**Signal-Based Communication**: Turn coordination uses signals rather than polling. Maintains loose coupling between systems.

## Adding New Goals

Create class inheriting from `Goal` base class. Implement required methods:

- `CalculateScore(AIContext)`: Return 0-100 utility score based on current context
- `Execute(AIContext)`: Perform goal's action, return ActionResult
- `OnActivated(AIContext)`: Initialize goal-specific state (optional)
- `OnDeactivated(AIContext)`: Clean up goal-specific state (optional)
- `GetName()`: Return descriptive string for debugging

Register in `GoalFactory._goalRegistry` dictionary with string key. Add goal ID to creature YAML files in `CreatureData.Goals` list. Goals with score > 0 compete for execution priority—highest score wins.

---

*See also: **[Actions](actions.md)** for action system integration, **[Components](components.md)** for component architecture, **[Turn System](turn-based.md)** for turn coordination.*
