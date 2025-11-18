# Text Renderer

The text renderer is the visual expression layer for Pits of Despair, implementing a tile-based ASCII/Unicode glyph rendering system using a monospace font. A single renderer handles all map and entity display through custom drawing, maintaining strict layer ordering and supporting advanced features like fog-of-war, targeting overlays, and projectile animation.

## Core Architecture

**Single Renderer Pattern**: One `TextRenderer` (Godot Control node) handles all visual output. No entity-level renderers exist—entities emit signals when their visual state changes, and the renderer redraws the entire viewport. This centralized approach ensures consistent layer ordering and simplifies global effects.

**Signal-Based Reactivity**: Systems never directly call renderer methods after initialization. Changes propagate through signals that trigger `QueueRedraw()`, causing the renderer to pull current state from systems during `_Draw()`. Decouples visual output from game logic.

**Player-Centered Viewport**: The player remains at the viewport center at all times. The renderer calculates a world offset each frame based on player position, translating the entire world grid to keep the player stationary on screen. Classic roguelike UX pattern with simple offset math.

## Grid-to-Pixel Coordinate System

Game logic operates entirely on integer `GridPosition` coordinates (X, Y tile indices). The renderer converts to pixel space only during drawing:

```
pixelPosition = (gridPosition * TileSize) + cameraOffset
```

**TileSize**: 18 pixels (configurable). Matches font size for 1:1 character-to-tile alignment.

**Camera Offset**: Calculated each frame as `viewportCenter - (playerGridPosition * TileSize)`. Shifts the entire world to center the player.

**GridPosition**: Lightweight value type supporting distance calculations, direction vectors, and bidirectional pixel conversion. Hashable for use in collections (visibility sets, entity lookups).

## Rendering Layers

The renderer draws in strict depth order during `_Draw()`:

**Layer 1 - Background**: Solid black rectangle covering the viewport. Establishes high-contrast base.

**Layer 2 - Map Tiles**: Floor glyphs (`.`) and wall glyphs (`#`) colored by tile type. Fog-of-war dimming applied to explored-but-not-visible tiles. Unexplored tiles completely skipped.

**Layer 3 - Items**: Walkable entities (items) rendered after tiles but before creatures. Items remain visible once discovered (persistent memory) and are dimmed when in fog-of-war.

**Layer 4 - Creatures**: Non-item entities, only when currently visible (not in fog). Skip items already drawn in Layer 3.

**Layer 5 - Player**: Always drawn at viewport center with full color, regardless of fog state. Visual anchor point.

**Layer 6 - Projectiles**: Active projectiles mid-flight, using directional glyphs (`→`, `←`, `↓`, `↑`) interpolated between origin and target.

**Layer 7 - Targeting Overlay**: Semi-transparent range highlights, trace lines, and cursor position indicators. Only drawn when targeting mode active.

**Rationale**: Strict ordering prevents visual ambiguity. Each layer has single responsibility. Targeting and projectiles always on top for immediate feedback.

## Fog-of-War System

**Two-State Visibility Model**:

**Visible Tiles**: Recalculated each player turn using recursive shadowcasting FOV algorithm. Range determined by entity's `VisionComponent`. Walls block line-of-sight. Prevents view clipping into opaque tiles.

**Explored Tiles**: Cumulative set of all tiles ever seen. Persists across turns. Tiles in this set but not in visible set receive fog-of-war dimming.

**Rendering Application**:
- Visible: Full color rendering
- Explored but not visible: Rendered in dim gray (`FogOfWar` palette color)
- Unexplored: Not drawn at all

**Design Decision**: Balances immersion (limited vision) with usability (map memory). Common roguelike pattern. FOV recalculation only on player turns (not every frame) optimizes performance.

## Color and Styling

**Semantic Palette**: Centralized `Palette` class with 61+ named colors organized by purpose:

- **UI & Feedback**: `Empty`, `FogOfWar`, `Default`, `Success`, `Danger`, `Caution`, `Alert`
- **Health States**: Progressive gradient from `HealthFull` (green) through `HealthCritical` (red)
- **Targeting**: `TargetingRangeOverlay`, `TargetingValid`, `TargetingInvalid`, `TargetingLine`
- **Materials**: Iron, Steel, Bronze, Gold, Obsidian, Granite, Leather, Wood, Bone, etc.
- **Effects**: Fire, Ice, Lightning, Poison, Acid

**Design Philosophy**: Colors named by meaning, not appearance. Maintains visual consistency. High contrast against black backgrounds. Material-driven differentiation for equipment. Alpha transparency applied at runtime (not stored in palette).

**Integration**: Palette supports both native Godot Color rendering and BBCode hex conversion for UI text labels, ensuring consistent visual language across rendering systems.

## Entity Glyph System

Each entity defines:
- **Glyph**: Single Unicode grapheme cluster (character)
- **GlyphColor**: Palette color for rendering
- **DisplayName**: Human-readable name for UI/messages

**Glyph Validation**: Single-character constraint enforced. Rejects multi-character strings with warning. Defaults to `?` if invalid. Unicode support enables visual variety while maintaining simple positioning.

**Examples**: Player (`@` in yellow), projectiles (arrows in white), items (material-colored characters), creatures (varied glyphs colored by type).

**Rationale**: Monospace rendering efficiency. Unicode provides distinction. Single-character constraint simplifies tile alignment and positioning math.

## Targeting System Integration

When targeting mode activates, the renderer draws additional overlay elements:

**Range Tiles**: Semi-transparent blue rectangles (0.3 alpha) highlighting all valid target positions. Range shape determined by distance metric (Manhattan/Chebyshev for square ranges).

**Trace Line**: Yellow line (0.5 alpha, 2.0 pixel width) from player position to cursor position. Visualizes line-of-effect.

**Cursor Highlight**: Pulsing rectangle at cursor position. Green tint for valid creature targets, red for empty tiles. Sine wave alpha modulation (0.25±0.15 for valid, 0.15±0.1 for invalid).

**Continuous Redraw**: Renderer forces redraw every frame while targeting active. Ensures smooth pulsing animation and immediate visual feedback during cursor movement.

## Projectile Animation

Projectiles are rendered as interpolated glyphs between origin and target positions:

**Animation Model**: Progress value from 0.0 (origin) to 1.0 (target). Godot tweens provide smooth transitions. Duration calculated as `distance / ProjectileSpeed` (30 tiles/second).

**Rendering**: Grid position calculated via linear interpolation. Directional glyph selected based on flight direction. Projectile drawn on top of all static elements but below targeting overlay.

**Continuous Redraw**: Renderer checks for active projectiles during `_Process()` and forces redraw while any projectiles exist. Ensures smooth animation frames.

## Font and Text Positioning

**Font**: Fira Mono Medium (monospace) at 18 pixels. Matches tile size for perfect glyph-to-tile alignment.

**Rendering Methods**: `DrawChar()` for single characters (tiles), `DrawString()` for text strings (entities). Both support Unicode.

**Baseline Positioning**: Godot's `DrawString()` uses baseline positioning. Fine-tuned vertical/horizontal offsets center glyphs within tile space. Offsets vary by entity type to account for visual weight differences.

**Unicode Support**: Full grapheme cluster support. Single-character validation counts user-perceived characters (handles combining marks correctly).

## UI Text Rendering (Separate System)

While the TextRenderer handles map/entity display, UI panels use Godot's built-in `Label` and `RichTextLabel` nodes:

**MessageLog**: Rich text with BBCode color codes. Queue-based history (max 100 messages). Auto-scrolling.

**SidePanel**: Dynamic health bars, equipment lists with colored glyphs, stats display, visible entities list. Uses Palette colors for consistency.

**GameHUD**: Coordinates all UI panels. Manages menu state. Routes messages between systems and UI.

**Separation**: Map rendering uses custom `_Draw()` for control over layers/effects. UI rendering leverages Godot's text layout and BBCode for rich content. Shared Palette maintains visual consistency across both systems.

## Integration with Game Systems

**MapSystem**: Provides tile type, glyph, and color for each grid position. Emits `MapChanged` signal on modifications.

**EntityManager**: Notifies renderer of entity additions/removals. Each entity emits `PositionChanged` signal when moving.

**PlayerVisionSystem**: Provides visibility queries (`IsVisible()`, `IsExplored()`) for fog-of-war. Emits `VisionChanged` signal on FOV updates.

**TargetingSystem**: Provides range, cursor position, valid targets when active. Renderer draws overlay based on this state.

**ProjectileSystem**: Maintains collection of active projectiles. Renderer iterates this collection for interpolated rendering.

**Signal Flow**: System change → signal emission → `QueueRedraw()` → `_Draw()` pulls current state from all systems → render viewport.

## Performance Considerations

**Godot QueueRedraw Optimization**: `_Draw()` only runs when needed, not every frame. Signal-based triggers ensure efficient redraw scheduling.

**Continuous Modes**: Targeting and projectile animation force per-frame redraw for smooth feedback. Disabled when not active.

**Single Draw Call**: All entities rendered in one pass. No entity-level renderers or child nodes to manage. Minimizes overhead.

**FOV Recalculation**: Only on player turn completion. Not every frame. Boolean arrays for visibility state have minimal memory overhead.

## Design Rationale

**Custom Drawing vs Node-Based Rendering**: Maximum control over layer ordering and global effects. Simpler than managing hundreds of child nodes. Enables efficient fog-of-war and overlay rendering.

**Player-Centered Camera**: UX clarity and classic roguelike expectation. Simple offset math. Player always spatially oriented.

**Two-State Visibility**: Balances tactical limitation (FOV) with usability (map memory). Standard roguelike pattern. Supports exploration-based progression.

**Semantic Color Naming**: Maintainability and iteration speed. Visual consistency. Decouples color meaning from specific RGB values.

**Signal-Based Updates**: Decoupling per project principles. Systems evolve independently. Renderer subscribes to relevant changes without tight coupling.

---

*For entity composition patterns, see [entities.md](entities.md). For fog-of-war AI integration, see [ai.md](ai.md). For color palette details, see [color.md](color.md).*
