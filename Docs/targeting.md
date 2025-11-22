# Cursor Targeting System

The **Cursor Targeting System** provides unified tile selection for both non-action inspection (examine mode) and action targeting (ranged attacks, reach weapons, targeted items). It bridges player input and action execution through visual feedback and validated target selection.

## Design Philosophy

**Mode-Based Unification**: Single cursor implementation handles both passive observation (examine) and active targeting (combat/items) through mode switching, eliminating duplicate cursor logic.

**Grid-Based Tactical**: Action targeting uses Chebyshev distance (square ranges) for clean diagonal mechanics and tactical grid gameplay. Examine mode uses vision-based validation (Euclidean) for natural exploration.

**Consistent Visuals**: All modes share the same cursor appearance (white border box, green fill on entities) with mode-specific overlays (range indicators, trace lines) for action feedback.

**Creature-Centric Actions**: Action modes auto-select nearest valid target and provide Tab-cycling between creatures for efficient selection without manual cursor movement.

## Core Concepts

### Targeting Modes

The system operates in four distinct modes:

**Examine**: Read-only inspection of visible entities. Cursor moves freely within player vision, no range limits. Triggered by X key. Entity descriptions display in message log as cursor moves. No action execution—purely informational.

**RangedAttack**: Bow/crossbow targeting. FOV-based range calculation using weapon range and Chebyshev metric. Auto-targets nearest creature. Triggered by F key with ranged weapon equipped.

**ReachAttack**: Melee weapons with range > 1 (spears, polearms). Square range based on weapon reach. Triggered by activating equipped reach weapon from inventory.

**TargetedItem**: Items requiring target selection (scrolls, wands). Range determined by item properties. Triggered by activating targeted item from inventory.

Mode selection determines validation logic (vision vs range), visual feedback (overlays, trace lines), and outcome (message display vs action execution).

### Distance Metrics

Targeting and vision use different distance calculations to serve distinct gameplay purposes:

**Chebyshev (Action Targeting)**: Square-shaped ranges where diagonals cost same as orthogonal moves. Formula: `max(|dx|, |dy|)`. Creates 3×3, 5×5, etc. grids. Used for all player-initiated targeting (weapons, items) to provide clean grid-based tactical mechanics.

**Euclidean (Vision/Examine)**: Circular-shaped ranges modeling realistic light propagation. Formula: `dx² + dy² <= range²`. Used for creature vision, FOV, and examine mode to simulate natural sight limitations.

This separation creates intuitive gameplay: passive observation feels realistic (circular vision) while active targeting is tactically precise (square grids).

### Cursor Flow

**Initiation**: Player triggers mode via input (X for examine, F for ranged attack, A + item for targeted actions). System determines validation strategy: examine uses vision system, action modes calculate valid tiles using FOV with Chebyshev metric. Action modes auto-select nearest creature.

**Selection**: Player uses arrow keys to move cursor freely within valid area or Tab/Shift+Tab to cycle between creatures (action modes only). Visual feedback adapts to mode: examine shows border only, action modes add range overlay and trace line.

**Resolution**: In examine mode, ESC or X exits. In action modes, Enter/Space/F confirms target position triggering corresponding action, or ESC cancels without consuming turn. Examine mode provides no-cost exploration; action modes lead to turn-consuming execution.

### Visual Feedback

**White Border Box**: Solid 1-pixel white border always visible on cursor tile. Consistent across all modes for clear position tracking.

**Green Fill**: Pulsing green highlight appears only when cursor hovers over entity (any mode). Indicates inspectable/targetable content.

**Range Overlay** (action modes only): Subtle blue highlight on all tiles within weapon/item range respecting line-of-sight. Uses same distance metric as action validation.

**Trace Line** (action modes only): Yellow line from player to cursor showing targeting direction and path.

Visual layers: Range overlay → Trace line → Border box → Fill. Examine mode shows minimal feedback (border + fill), action modes show full tactical information.

## Architectural Patterns

### Mode-Based Validation

Single cursor system branches validation logic based on current mode:

**Examine Mode**: Queries vision system for tile visibility. No range calculation—anywhere player can see is valid. No creature filtering—empty tiles are valid targets (just less informative).

**Action Modes**: Pre-calculate valid tiles using FOV with Chebyshev metric at targeting start. Build sorted list of targetable creatures within range. Validate cursor movement against pre-calculated set.

Shared infrastructure (cursor position, movement, signals) with mode-specific validation prevents code duplication while maintaining distinct behavior.

### Two-Phase Validation

**Cursor System**: Pre-calculates valid tiles (action modes) or checks visibility (examine mode), provides visual feedback, handles input. Does not validate action legality—that's action responsibility.

**Action Validation**: Actions re-validate range, LOS, and target requirements in `CanExecute()`. Prevents desync between targeting initiation and action execution.

Separation enables free cursor movement with visual guidance while ensuring actions validate current game state.

### Mode-Aware Input Routing

InputHandler switches between normal gameplay and cursor targeting mode. During targeting, behavior adapts to mode:

**All Modes**: Movement keys control cursor instead of player. ESC cancels and returns to normal mode.

**Examine Mode**: X also exits (toggle behavior). No confirmation key—examination is continuous until exit.

**Action Modes**: Enter/Space/F confirms target, triggering action execution. Tab/Shift+Tab cycles between creature targets. Confirmation consumes turn if action succeeds.

Mode tracking via flags (`_isReachAttack`, `_pendingItemKey`) routes confirmed position to correct action type.

## Integration Points

### Vision System Integration

Examine mode leverages vision system for tile validation. If player can see tile, they can examine it. This ensures consistency: examine cursor follows same visibility rules as exploration.

Action modes use FOV calculator for range validation but remain independent of vision updates—targeting represents momentary snapshot of reachable positions.

### FOV Calculator Integration

Action targeting leverages FOV's shadowcasting for line-of-sight calculation. Valid targeting tiles = visible tiles within range. This ensures consistency: if player can't see tile (blocked by walls), they can't target it.

FOV's dual distance metric support allows action targeting to use Chebyshev while vision uses Euclidean without separate algorithms.

### Action System Integration

**Reach Weapons**: Equipped melee weapons with range > 1 become activatable. Activation triggers ReachAttack mode using weapon's range. Confirmation creates `ReachAttackAction` with target position.

**Ranged Weapons**: F key checks equipped ranged weapon, retrieves range, starts RangedAttack mode. Confirmation creates `RangedAttackAction`.

**Targeted Items**: Items marked `RequiresTargeting()` (e.g., Scroll of Confusion) enter TargetedItem mode on activation. Confirmation creates `UseTargetedItemAction`.

Actions validate independently—targeting provides position, actions ensure legality.

### UI System Integration

GameHUD coordinates cursor state between InputHandler, CursorTargetingSystem, and visual renderer. Signals communicate mode start/stop, cursor movement, and cancellation:

**Examine Signals**: `CursorStarted(mode)` displays help message, `CursorMoved(entity)` shows entity descriptions in message log, `CursorCanceled(mode)` displays exit message.

**Action Signals**: `CursorStarted(mode)` enters targeting UI state, `TargetConfirmed(position)` triggers action execution, `CursorCanceled(mode)` exits targeting state and clears pending flags.

Signal-based coordination enables synchronized UI updates (cursor display, overlay rendering, input mode switching, message log) without tight coupling.

## Design Decisions & Trade-offs

**Mode Unification**: Single cursor implementation with branching logic reduces duplication (~100 lines saved) and ensures consistent cursor behavior. Trade-off: single system handles multiple responsibilities, but mode enumeration keeps branches clear.

**Chebyshev for Actions**: Square ranges provide clean diagonal mechanics (2 diagonal moves = 2 tiles, not ~2.8) and intuitive grid-based gameplay. Trade-off: less realistic than Euclidean, but better game feel for tactical combat.

**Euclidean for Examine**: Circular vision ranges feel natural for passive observation. Trade-off: examine range shape differs from action ranges, but reinforces conceptual distinction (observation vs action).

**Pre-calculated Valid Tiles**: FOV calculation at targeting start (action modes) provides instant visual feedback and prevents per-frame recalculation. Trade-off: tiles don't update if game state changes during targeting, but player can't move anyway in turn-based.

**Auto-Select Nearest**: Cursor starts on closest creature (action modes) for common case (attack nearest threat). Trade-off: occasionally requires Tab cycling to farther targets, accepted for faster common-case targeting.

**Minimal Examine Feedback**: No range overlay or trace line in examine mode keeps visuals clean for passive observation. Trade-off: less visual information than action modes, but examine doesn't need tactical feedback.

**Consistent Cursor Appearance**: Same white border + green fill across all modes creates unified UX. Trade-off: examine mode loses distinct cyan color, but consistency improves learnability.

## Conceptual Model

```
                CURSOR TARGETING SYSTEM FLOW

Input Trigger      Cursor Layer           Output
─────────────      ────────────           ──────

X (Examine)   ──→  Vision-Based      ──→  Entity Descriptions
                   Validation             (Message Log)
                   White Border
                   Green Fill

F (Ranged)    ──→  FOV + Chebyshev   ──→  RangedAttackAction
A + Spear     ──→  Pre-calc Range         ReachAttackAction
A + Scroll    ──→  Auto-Select Cursor     UseTargetedItemAction
                   Range Overlay
                   Trace Line
                   Confirm Target    ──→  Validate Range/LOS
                                          Execute Action
```

## Related Documentation

- [actions.md](actions.md) - Action system that consumes targeting results
- [effects.md](effects.md) - Item effects that may require targeting
- [text-renderer.md](text-renderer.md) - Visual rendering of cursor overlays
