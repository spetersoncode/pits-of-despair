# AutoExplore System

The **AutoExplore System** provides automated dungeon exploration, pathfinding to unexplored areas and autopickup items while maintaining player safety through interrupt conditions.

## Design Philosophy

**Player Safety First**: AutoExplore never paths through danger. Stops immediately when threats appear. Player agency preserved through cautious, interruptible behavior.

**Closest-Wins Targeting**: Finds nearest target regardless of type (unexplored tile or autopickup item). Efficient exploration without arbitrary prioritization.

**Turn-by-Turn Execution**: One movement per turn at normal game pace. Player observes exploration progress. Creature turns still execute between player moves.

## Core Concepts

### Target Finding (Dijkstra)

Uses Dijkstra's algorithm to find nearest reachable target. Expands outward from player position, returning first tile matching target criteria. Guarantees closest target found.

**Valid Targets**: Unexplored walkable tiles, tiles containing autopickup items (potions, scrolls, ammo).

### Safe Pathing

Paths calculated via A* avoid hostile creatures. Each turn re-validates next step safety before moving. Path recalculated if blocked or new threats appear.

### Interrupt Conditions

AutoExplore stops silently when:

- **Enemy Visible**: Any hostile creature (non-walkable entity) in player FOV
- **Item Collected**: Autopickup triggers (potions, scrolls, ammo)
- **Manual Cancel**: Any keypress interrupts exploration

Displays message only when:

- **Exploration Complete**: No reachable unexplored tiles or autopickup items remain

## Integration Points

### Input System

**Keybinding**: `O` triggers autoexplore start. Same key or any other key stops exploration.

**Input Handler**: Intercepts all keypresses during autoexplore. Non-autoexplore keys cancel and consume the input. Prevents accidental actions during exploration.

### Turn System

Connects to `PlayerTurnStarted` signal. Takes one movement action per player turn. Creature turns execute normally between player moves.

### Vision System

Queries `PlayerVisionSystem` for explored tile state and visible enemies. Target finding only considers tiles reachable through explored areas.

### Message Log

Emits signals for feedback. "Exploring..." on start, "Nothing left to explore." when complete. Most stops are silent (reason is visually obvious).

## Algorithm Details

### Target Selection

```
1. Start Dijkstra from player position
2. Expand outward (8-directional, uniform cost)
3. Skip: walls, hostile creatures, out-of-bounds
4. For each visited tile:
   - Check for autopickup items → return as target
   - Check if unexplored + walkable → return as target
5. If queue exhausted → no valid target
```

### Per-Turn Flow

```
1. Check for visible enemies → stop if found
2. If path empty → find new target
3. Validate next step safety → recalculate if blocked
4. Execute MoveAction to next path position
5. Item autopickup triggers → stop (via signal)
```

## Design Decisions

**Dijkstra over A***: Multiple unknown targets makes A* heuristic ineffective. Dijkstra guarantees nearest target. A* used only for pathing to known target.

**Stop on Autopickup**: Brief pause after collecting items lets player evaluate inventory state. Prevents blindly accumulating items while missing important decisions.

**Conservative Pathing**: Never paths adjacent to visible enemies. Rechecks safety each turn. Slightly slower exploration preferred over any risk to player.

**Single-Step Execution**: One move per turn maintains game rhythm, allows creature AI to act, and provides interruptible granularity.

---

*See actions.md for movement action details, turn-system.md for turn flow, and components.md for vision component.*
