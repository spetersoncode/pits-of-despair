# Targeting System

The **Targeting System** provides unified tile selection for ranged attacks, reach weapons, and targeted items. It bridges player input and action execution through visual feedback and validated target selection, enabling tactical decision-making in turn-based combat.

## Design Philosophy

**Unified Interface**: All targeting actions—bow attacks, spear thrusts, item usage—share the same targeting flow and visual feedback.

**Grid-Based Tactical**: Targeting uses Chebyshev distance (square ranges) for clean diagonal mechanics and tactical grid gameplay, distinct from vision which uses Euclidean (circular).

**Line-of-Sight Integration**: Leverages FOV calculation for both range validation and wall blocking, ensuring targets are reachable and visible.

**Creature-Centric UI**: Auto-selects nearest valid target and provides Tab-cycling between creatures for efficient selection without manual cursor movement.

## Core Concepts

### Distance Metrics

Targeting and vision use different distance calculations to serve distinct gameplay purposes:

**Chebyshev (Targeting)**: Square-shaped ranges where diagonals cost same as orthogonal moves. Formula: `max(|dx|, |dy|)`. Creates 3×3, 5×5, etc. grids. Used for all player-initiated targeting (weapons, items) to provide clean grid-based tactical mechanics.

**Euclidean (Vision)**: Circular-shaped ranges modeling realistic light propagation. Formula: `dx² + dy² <= range²`. Used for creature vision and FOV to simulate natural sight limitations.

This separation creates intuitive gameplay: passive observation feels realistic (circular vision) while active targeting is tactically precise (square grids).

### Targeting Flow

**Initiation**: Player triggers targeting via input (F key for ranged weapon, A key + item selection for reach/targeted items). System calculates valid tiles using FOV with Chebyshev metric and auto-selects nearest creature.

**Selection**: Player uses arrow keys to move cursor freely within valid range or Tab/Shift+Tab to cycle between creatures. Visual feedback shows valid range overlay, cursor position, and trace line to origin.

**Confirmation**: Enter/Space/F confirms target position, triggering corresponding action (ranged attack, reach attack, item usage). ESC cancels without consuming turn.

**Action Execution**: Confirmed position passed to action system. Actions re-validate range and LOS before execution to prevent stale state issues.

### Visual Feedback

**Range Overlay**: Subtle highlight on all tiles within weapon/item range respecting line-of-sight. Uses same distance metric as action validation.

**Cursor Highlight**: Pulsing green for valid creature targets, red for invalid/empty tiles (when creature required).

**Trace Line**: Visual connection from player to cursor showing targeting direction.

## Architectural Patterns

### Two-Phase Validation

**TargetingSystem**: Pre-calculates valid tiles using FOV, provides visual feedback, handles input. Does not validate action legality—that's action responsibility.

**Action Validation**: Actions re-validate range, LOS, and target requirements in `CanExecute()`. Prevents desync between targeting initiation and action execution.

Separation enables free cursor movement with visual guidance while ensuring actions validate current game state.

### Distance Metric Selection

FOVCalculator accepts distance metric parameter, enabling different systems to use appropriate ranges:

**Vision systems** (default): Pass `DistanceMetric.Euclidean` for realistic circular sight cones.

**Targeting systems**: Pass `DistanceMetric.Chebyshev` for tactical square grids.

Single algorithm (shadowcasting) with configurable distance check provides flexibility without duplication.

### Mode-Based Input Handling

InputHandler switches between normal gameplay and targeting mode. During targeting:
- Movement keys control cursor instead of player
- Confirmation keys trigger action execution
- Cancel returns to normal mode without turn cost

Targeting state tracked via flags (`_isReachAttack`, `_pendingItemKey`) to route confirmed position to correct action type.

## Integration Points

### FOV Calculator Integration

Targeting leverages FOV's shadowcasting for line-of-sight calculation. Valid targeting tiles = visible tiles within range. This ensures consistency: if player can't see tile (blocked by walls), they can't target it.

FOV's dual distance metric support allows targeting to use Chebyshev while vision uses Euclidean without separate algorithms.

### Action System Integration

**Reach Weapons**: Equipped melee weapons with range > 1 become activatable. Activation triggers targeting mode using weapon's range. Confirmation creates `ReachAttackAction` with target position.

**Ranged Weapons**: F key checks equipped ranged weapon, retrieves range, starts targeting. Confirmation creates `RangedAttackAction`.

**Targeted Items**: Items marked `RequiresTargeting()` (e.g., Scroll of Confusion) enter targeting on activation. Confirmation creates `ActivateTargetedItemAction`.

Actions validate independently—targeting provides position, actions ensure legality.

### UI System Integration

GameHUD coordinates targeting state between InputHandler, TargetingSystem, and visual renderer. Signals communicate targeting start/stop, enabling synchronized UI updates (cursor display, overlay rendering, input mode switching).

## Design Decisions & Trade-offs

**Chebyshev for Targeting**: Square ranges provide clean diagonal mechanics (2 diagonal moves = 2 tiles, not ~2.8) and intuitive grid-based gameplay. Trade-off: less realistic than Euclidean, but better game feel for tactical combat.

**Pre-calculated Valid Tiles**: FOV calculation at targeting start provides instant visual feedback and prevents per-frame recalculation. Trade-off: tiles don't update if game state changes during targeting (player can't move anyway in turn-based, so minimal issue).

**Auto-Select Nearest**: Cursor starts on closest creature for common case (attack nearest threat). Trade-off: occasionally requires Tab cycling to farther targets, accepted for faster common-case targeting.

**Creature-Centric Cycling**: Tab jumps between creatures rather than adjacent tiles. Trade-off: can't easily target empty tiles with Tab, but arrow keys provide fine control when needed.

## Conceptual Model

```
                TARGETING SYSTEM FLOW

Input Trigger      Targeting Layer        Action Layer
─────────────      ───────────────        ────────────

F (Ranged)    ──→  Calculate Range   ──→  RangedAttackAction
A + Spear     ──→  (FOV + Chebyshev)      ReachAttackAction
A + Scroll    ──→  Auto-Select Cursor     ActivateTargetedItemAction
                   Show Overlay
                   Handle Input
                   Confirm Target    ──→  Validate Range/LOS
                                          Execute Action
```

## Related Documentation

- **[Actions](actions.md)** - Action system that consumes targeting results
- **[Effects](effects.md)** - Item effects that may require targeting

---

*The targeting system bridges player intent and action execution through visual feedback and validated selection, enabling tactical combat in grid-based gameplay.*
