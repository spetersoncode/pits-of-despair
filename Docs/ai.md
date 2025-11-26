# AI System

The AI system uses a **hierarchical goal stack** for decision-making. Goals persist on a stack until completed, can push sub-goals for complex multi-step behaviors, and support failure recovery through intent tracking. This enables composable, stateful behaviors that naturally decompose complex actions into simple, reusable primitives.

## Architecture

### Core Components

**GoalStack**: Stack data structure managing active goals. Top goal executes each turn. Goals remain until `IsFinished()` returns true. Supports failure recovery via `FailToIntent()` which pops goals back to the original decision point.

**Goal**: Base class for all AI behaviors. Key methods:
- `IsFinished(AIContext)`: Returns true when goal's objective is complete
- `TakeAction(AIContext)`: Executes one step—may push sub-goals or perform actions
- `Fail(AIContext)`: Handles failure by popping back to `OriginalIntent` for replanning
- `OriginalIntent`: Reference to parent goal for failure recovery chain

**AIContext**: Per-turn perception bundle containing entity references, system access (via `ActionContext`), visibility state, and component caches. Provides helper methods: `GetVisibleEnemies()`, `GetClosestEnemy()`, `CanSee()`, `GetEnemiesNearProtectionTarget()`, `GetVisibleItems()`, `GetClosestItem()`.

**AIComponent**: Data and state management. Stores `GoalStack`, protection target, follow distance, spawn position. Designer-tweakable exports for behavior tuning.

**AISystem**: Central coordinator subscribing to `TurnManager.CreatureTurnsStarted`. Each turn: builds AIContext, removes finished goals, ensures BoredGoal fallback, calls `TakeAction()` on top goal.

### Decision-Making Flow

**Turn Processing**:
1. Build AIContext with current perception state
2. Call `GoalStack.RemoveFinished()` to pop completed goals
3. If stack empty, push `BoredGoal` as fallback
4. Call `TakeAction()` on top goal
5. Goal may: execute action directly, push sub-goals, or fail (triggering replan)

**Goal Composition**: High-level intent goals delegate to tactical sub-goals. Example: `KillTargetGoal` pushes `ApproachGoal` for movement, which pushes `MoveDirectionGoal` for individual steps. Each layer handles its concern without knowing implementation details of sub-goals.

**Failure Recovery**: Goals track their `OriginalIntent`. When `Fail()` is called, stack pops back to the intent goal, allowing it to replan with updated world state. Prevents stuck behaviors when paths are blocked or targets move.

### Event-Driven Actions

Components inject behavior without coupling to goals via the event system:

**IAIEventHandler**: Interface for components responding to AI events. Implement `HandleAIEvent(eventName, args)` to add actions.

**GetActionsEventArgs**: Event payload containing `WeightedActionList`, `AIContext`, optional `Target`, and `Handled` flag.

**WeightedActionList**: Weighted random selection utility. Components add `AIAction` objects; `PickRandomWeighted()` selects probabilistically. Enables behavioral variety—creatures don't always make the same choice.

**AIAction**: Adapter between Action system and AI. Wraps an Action with weight (selection probability) and debug name. `Execute(AIContext)` runs the action and returns `ActionResult`, enabling goals to react to success/failure.

**Event Types** (defined in `AIEvents`):
- `OnGetMeleeActions`: Gather melee attack options
- `OnGetDefensiveActions`: Gather healing, blocking, yelling for help
- `OnGetRangedActions`: Gather ranged attack options
- `OnGetItemActions`: Gather usable item actions
- `OnGetMovementActions`: Gather special movement abilities
- `OnIAmBored`: Let components push goals when idle (flee triggers, patrol routes)
- `OnRangedAttackSuccess`: Post-attack hooks (shoot-and-scoot behavior)

## Available Goals

**BoredGoal**: Permanent fallback at stack bottom, never finishes. Decision hub that fires `OnIAmBored` event, checks for protection target (follow/defend), finds visible enemies (push `KillTargetGoal`), opportunistically picks up nearby items (push `SeekItemGoal`), or random wander. All creature behavior flows through BoredGoal when no other goals active.

**KillTargetGoal**: Pursues and kills a target entity. Priority-ordered action attempts:
1. Melee attack (if adjacent) via `OnGetMeleeActions` event
2. Defensive actions via `OnGetDefensiveActions` event
3. Ranged attack via `OnGetRangedActions` event
4. Item usage via `OnGetItemActions` event
5. Movement: pushes `ApproachGoal` to close distance

Finished when target is dead or invalid.

**ApproachGoal**: Pathfinds toward a position or entity. Tracks moving entities automatically by updating `TargetPosition` each turn. Uses `NavigationWeightMap` for capability-aware pathfinding. Pushes `MoveDirectionGoal` for individual steps. Finished when within `DesiredDistance`.

**FleeGoal**: Moves away from a threat for a duration. Prioritizes fleeing toward allies using Dijkstra maps. Fires `OnGetDefensiveActions` for yelling/healing while fleeing. Finished when turns expire AND (can't see threat OR far enough away).

**FollowEntityGoal**: Follows a target entity, staying within a specified distance. Used by bodyguards (following protection target) and pack followers (following leader). Pushes `ApproachGoal` to close distance. Aborts when enemies visible. Finished when close enough or target gone.

**WanderGoal**: Single random step in a valid direction. Optional radius constraint around a center point for search behavior. Pushes `MoveDirectionGoal`. Completes after one move attempt.

**MoveDirectionGoal**: Atomic single-tile movement. Executes `MoveAction` directly. Marks `_completed` or `_failed` based on result. Calls `Fail()` if blocked, allowing parent goal to replan.

**SeekItemGoal**: Moves to and picks up a specific ground item. Pushes `ApproachGoal` with `desiredDistance: 0` to stand on item tile, then executes `PickupAction`. Finished when item is picked up or no longer exists. Used by `BoredGoal` for opportunistic item collection.

## Navigation System

**NavigationWeightMap**: Per-cell cost map for weighted A* pathfinding. Costs depend on terrain, entities, hazards, and creature capabilities. Standard weights:
- `NormalFloor`: 1 (base cost)
- `Door`: 2 (slight penalty for intelligent creatures)
- `Hazard`: 3 (webs, shallow water)
- `OtherCreature`: 50 (prefer going around)
- `DangerousHazard`: 100 (fire, acid)
- `Impassable`: 999 (walls for non-burrowers)
- `BurrowWall`: 20 (burrowers can dig through)
- `DoorUnintelligent`: 999 (animals can't open doors)

**CreatureCapabilities**: Per-creature movement profile:
- `IsIntelligent`: Can open doors, use complex paths (default: true)
- `CanFly`: Ignores ground hazards (default: false)
- `CanBurrow`: Can path through walls at high cost (default: false)

Capabilities extracted from entity data; defaults to intelligent non-flying non-burrowing.

## Perception

**FOVCalculator**: Recursive shadowcasting for line-of-sight. 8-octant symmetric visibility within vision range. O(vision_range²) complexity. Used by `AIContext.CanSee()` and `GetVisibleEnemies()`.

**AStarPathfinder**: Weighted A* with Chebyshev distance heuristic for 8-directional movement. Uses `NavigationWeightMap` costs. Returns path as `Queue<GridPosition>`.

**DijkstraMapBuilder**: Multi-target distance map for fleeing toward allies. Flood-fills from goal positions, gradient descent finds nearest goal. Used by `FleeGoal`.

## System Integration

**Turn Coordination**: `TurnManager.CreatureTurnsStarted` → AISystem processes all creatures → AISystem calls `EndCreatureTurns()` → cycle repeats.

**Action System**: Goals execute actions via `entity.ExecuteAction(action, context)`. All movement, combat, and abilities use the action system for validation and execution.

**Entity Events**: Goals fire events on entities via `entity.FireEvent(eventName, args)`. Components implementing `IAIEventHandler` receive these events and can add actions or set `Handled = true`.

## Configuration

**Creature Data**: YAML files specify AI flags and vision range. Goals are no longer listed in YAML—all creatures use the goal stack with `BoredGoal` as entry point. Behavior emerges from components and faction.

```yaml
name: goblin
visionRange: 10
hasMovement: true
hasAI: true
faction: Hostile
```

**Faction-Based Behavior**: Hostility determined by faction. `Hostile` creatures attack `Friendly` and `Player`. `Friendly` creatures have protection targets and use follow/defend behavior. See [factions.md](factions.md).

## AI Personalities

Personalities emerge from component configuration and faction rather than goal lists:

**Aggressive** (Most hostiles): BoredGoal finds enemies → KillTargetGoal → ApproachGoal → attack. Standard hostile behavior.

**Cowardly**: Component listens to `OnIAmBored`, pushes `FleeGoal` when health low or enemies visible. Can yell for help via `OnGetDefensiveActions`.

**Bodyguard** (Friendly faction): BoredGoal fires `OnIAmBored` → `FollowLeaderComponent` pushes `FollowEntityGoal` when distant. Combat check in BoredGoal handles enemies. Stays near and protects VIP.

**Ranged Tactical**: Component provides ranged attacks via `OnGetRangedActions`, listens to `OnRangedAttackSuccess` to push retreat behavior (shoot-and-scoot).

**Item User**: `ItemUsageComponent` responds to `OnGetDefensiveActions` (healing items when HP low) and `OnGetItemActions` (offensive items like confusion scrolls targeting enemies). Configurable thresholds and weights.

## Design Patterns

**Goal Stack Over Utility Scoring**: Goals persist until complete rather than re-evaluating every turn. Enables multi-step behaviors and stateful execution. Sub-goals handle tactical details while intent goals track objectives.

**Composition via Sub-Goals**: Complex behaviors decompose into simple primitives. `KillTargetGoal` doesn't know pathfinding—it delegates to `ApproachGoal`. `ApproachGoal` doesn't know movement details—it delegates to `MoveDirectionGoal`.

**Event-Driven Action Gathering**: Components inject behavior without goal coupling. Attack components respond to `OnGetMeleeActions`. Healing items respond to `OnGetDefensiveActions`. Goals request actions without knowing what's available.

**Intent-Based Failure Recovery**: `OriginalIntent` chain enables replanning. When `MoveDirectionGoal` fails (blocked), `ApproachGoal` can recalculate path. When `ApproachGoal` fails (unreachable), `KillTargetGoal` can try alternatives.

**Capability-Aware Navigation**: Pathfinding costs depend on creature capabilities. Intelligent creatures path through doors. Burrowers path through walls. Flying creatures ignore ground hazards. One algorithm handles all creature types.

## Adding New Goals

1. Create class inheriting from `Goal`
2. Implement `IsFinished(AIContext)`: Return true when objective complete
3. Implement `TakeAction(AIContext)`: Execute one step, may push sub-goals
4. Optionally override `Fail(AIContext)` for custom failure handling
5. Accept `Goal originalIntent` in constructor for failure recovery chain

Goals can be pushed by other goals or by components via events. Most goals delegate to existing sub-goals (`ApproachGoal`, `MoveDirectionGoal`) rather than implementing movement directly.

## Adding New AI Events

1. Add constant to `AIEvents` class
2. Have goals fire the event with appropriate `GetActionsEventArgs`
3. Components implement `IAIEventHandler` and check `eventName`
4. Components create `AIAction` objects wrapping Action instances with weight and debug name
5. Components add `AIAction` to `args.ActionList` or set `args.Handled = true`

Event names follow pattern `On[Situation]` (e.g., `OnGetMeleeActions`, `OnIAmBored`).

## Item Integration

**ItemEvaluator**: Utility class for AI item prioritization. Scores items by type (healing, offensive, equipment) and entity needs (damaged → healing items more valuable, has ranged weapon → ammo more valuable). `IsItemWorthPicking()` filters items the AI has no use for.

**Item Pickup Flow**: `BoredGoal` checks for visible items when idle → `ItemEvaluator` scores and filters candidates → highest-scoring item becomes target → `SeekItemGoal` pushed → `ApproachGoal` moves to item → `PickupAction` collects item.

**Item Usage Flow**: `KillTargetGoal` fires `OnGetItemActions` during combat → `ItemUsageComponent` finds offensive items → checks range to target → creates `UseTargetedItemAction` wrapped in `AIAction` → weighted selection may choose item over attack.

---

*See [actions.md](actions.md) for action system, [components.md](components.md) for component architecture, [turn-based.md](turn-based.md) for turn coordination, [factions.md](factions.md) for faction system, [items.md](items.md) for item system.*
