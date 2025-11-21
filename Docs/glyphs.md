# Glyph Design Guide

Visual representation in Pits of Despair follows a constrained glyph system prioritizing instant recognition and tactical clarity. Every entity displays as a single Unicode character with semantic color, enabling rapid scanning and threat assessment during turn-based gameplay.

## Design Philosophy

**Clarity Through Constraint**: Single-character glyphs force intentional design choices. Players learn visual vocabulary quickly—no ambiguous multi-tile sprites or subtle animations to decode mid-combat.

**Color as Differentiator**: When glyphs repeat (multiple weapons as `/`, creature families as `g`), color provides instant distinction. See **[color.md](color.md)** for complete palette and semantic naming conventions.

**High Contrast Mandate**: All colors designed for pure black backgrounds. No subtle shades—readability supersedes realism.

## Core Properties

Every entity defines three visual properties:

**Glyph**: Single Unicode character as string (supports grapheme clusters). Validated to reject multi-character strings. Defaults to `?` with warning if invalid.

**GlyphColor**: Semantic Palette reference determining render color.

**DisplayName**: Human-readable text for UI and messages.

Glyphs are data properties, not components. No entity-level renderers—centralized TextRenderer reads properties during draw calls.

## Glyph Assignment Patterns

### Map Tiles
- **Floor**: `.` (period) in Basalt—neutral ground with subtle stone tone
- **Wall**: `#` (hash) in Default (white)—strong boundaries at maximum contrast

### Player
- **Glyph**: `@` (at symbol)—roguelike tradition
- **Color**: Player (yellow #FFFF00)—always visible against all other colors

### Items

**Type-Based Defaults**: ItemTypeInfo dictionary provides fallback glyph/color. Individual items override via YAML. Weapons auto-assign glyph based on damage type. See [glyph-atlas.md](glyph-atlas.md) for complete registry.

**Material Progression**: Same glyph + escalating color indicates quality tiers (Iron → Steel → ForgedSteel).

### Creatures

**Type-Based Defaults**: CreatureTypeInfo dictionary provides species glyph/color. Variants override for visual distinction. See [glyph-atlas.md](glyph-atlas.md) for complete registry.

**Distinct Variant Rule**: Creature variants within type NEVER share colors—enables instant threat differentiation.

### Projectiles

Projectiles use line-based rendering rather than glyphs. See [text-renderer.md](text-renderer.md) for details.

## Data-Driven Configuration

Glyphs configured in YAML with type inheritance pattern.

**Items**:
```yaml
name: short sword
type: weapon          # Inherits "/" glyph, Silver color
color: Steel          # Override type default
```

**Creatures**:
```yaml
name: goblin ruffian
type: goblinoid       # Inherits "g" glyph, white color
color: Rust           # Override for variant distinction
```

**ApplyDefaults Pattern**: After YAML deserialization, entities check Type field and populate missing glyph/color from TypeInfo dictionaries. Enables designer iteration without code changes.

## Clarity and Readability Rules

### Single-Character Constraint
Enforced via validation using StringInfo.GetTextElementEnumerator (counts grapheme clusters, handles combining marks). Simplifies positioning math—1:1 tile-to-glyph alignment, no text wrapping or overflow logic.

### Color Differentiation
Same-glyph items use different colors for instant distinction. Creature variants within type receive distinct colors for threat assessment. See **[color.md](color.md)** for material progression patterns and distinct color assignment rules.

### Fog-of-War Visual States
Three-tier visibility system prevents ambiguity:
- **Unexplored**: Not rendered (pure black)
- **Explored**: Tiles in FogOfWar (#404040), items at 50% color dimming
- **Visible**: Full color rendering

Players distinguish between "seen before" and "currently observable" without UI indicators.

## Color System

Glyphs use semantic colors from centralized Palette. See **[color.md](color.md)** for complete palette design philosophy, semantic naming conventions, and usage guidelines.

## Rendering Architecture

**Single Renderer Pattern**: TextRenderer Control node handles ALL visual output. No per-entity renderers—entities are data containers only.

**Rendering Layers** (strict Z-order):
1. Background (solid black)
2. Map Tiles (floor/walls with fog dimming)
3. Items (persistent visibility, dimmed in fog)
4. Creatures (current FOV only)
5. Player (viewport center, full color)
6. Projectiles (animated line segments)
7. Targeting Overlay (semi-transparent highlights)

**Grid-to-Pixel Mapping**: TileSize 18px matches FontSize 18px (Fira Mono Medium). DrawChar() for map tiles, DrawString() for entity glyphs (Unicode support). Player-centered viewport with camera offset calculation.

## UI Consistency

**BBCode Color System**: UI components (MessageLog, SidePanel, EquipPanel, InventoryPanel) use RichTextLabel with BBCode color tags. Palette.ToHex() converts Godot Colors to hex strings.

**Equipment Display Example**:
```
Melee: [color=#99AABB]/ short sword[/color]
Armor: [color=#D0B090][ padded armor[/color]
```

Same glyph/color pairs appear in map renderer and UI panels—unified visual language.

## Extension Guidelines

### Adding New Glyphs
1. Choose Unicode character with clear visual distinction from existing glyphs
2. Select semantic Palette color by meaning—see **[color.md](color.md)** for guidelines
3. Add to YAML data (items/creatures) or TypeInfo dictionary (type defaults)
4. Test against black background at 18px—ensure readability

### Creating Glyph Families
When adding entity categories with shared glyphs:
1. Define TypeInfo entry with default glyph/color
2. Plan variant color palette in advance (ensure sufficient visual separation)
3. Document color assignments to prevent variant conflicts

## See Also

- [glyph-atlas.md](glyph-atlas.md) - Complete glyph catalog and type registries
- [color.md](color.md) - Color palette and semantic naming
- [text-renderer.md](text-renderer.md) - Glyph rendering implementation
- [yaml.md](yaml.md) - YAML glyph/color configuration
