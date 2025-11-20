# Glyph Atlas

Complete catalog of all glyphs currently used in Pits of Despair. For design philosophy, assignment patterns, and extension guidelines, see **[glyphs.md](glyphs.md)**. For color definitions and semantic naming, see **[color.md](color.md)**.

## Map Terrain

| Glyph | Character | Meaning | Color | Notes |
|-------|-----------|---------|-------|-------|
| `.` | Period | Floor tile | Basalt (#3A4A4A) | Neutral dungeon ground |
| `#` | Hash | Wall tile | Default (white) | Solid boundaries |

## Player

| Glyph | Character | Meaning | Color | Notes |
|-------|-----------|---------|-------|-------|
| `@` | At symbol | Player character | Player (#FFFF00) | Roguelike tradition |

## Items

### Consumables

| Glyph | Character | Type | Color | Examples |
|-------|-----------|------|-------|----------|
| `!` | Exclamation | Potion | HealthFull (healing), Default (others) | healing_8, barkskin_2, bulls_strength_2 |
| `♪` | Music note | Scroll | Cyan, Default | blink, teleport, confusion |

### Equipment

| Glyph | Character | Type | Color | Examples |
|-------|-----------|------|-------|----------|
| `/` | Slash | Melee weapon | Material colors (Steel, Iron, Bronze, Oak, AshWood) | club, short_sword, spear, knobbed_club |
| `}` | Right brace | Ranged weapon | Material colors (Mahogany) | short_bow |
| `[` | Left bracket | Armor | Material colors (Wool, SoftLeather, Silver, etc.) | padded, leather, hide, chain_shirt, full_plate |

**Material Color Pattern**: Same glyph + material color indicates equipment quality. Steel weapons use Steel (#D3D3D3), iron uses Iron (#828282), wooden uses Oak/AshWood browns.

## Creatures

| Glyph | Character | Type | Variants |
|-------|-----------|------|----------|
| `g` | Letter G | Goblinoid | goblin, goblin_scout, goblin_ruffian |
| `r` | Letter R | Rodents | rat, elder_rat |

**Variant Color Rule**: Creature variants within same type always use distinct colors for instant threat differentiation.

## Special

| Glyph | Character | Meaning | Color | Notes |
|-------|-----------|---------|-------|-------|
| `*` | Asterisk | Gold pile | Gold (#CCAA66) | Auto-collected on contact |
| `?` | Question mark | Unknown/undefined entity | Default (white) | Fallback when glyph assignment fails |

## Projectiles

| Glyph | Character | Direction | Color | Notes |
|-------|-----------|-----------|-------|-------|
| `→` | Right arrow | Moving right | Default (white) | Dynamic, updates during flight |
| `←` | Left arrow | Moving left | Default (white) | Dynamic, updates during flight |
| `↑` | Up arrow | Moving up | Default (white) | Dynamic, updates during flight |
| `↓` | Down arrow | Moving down | Default (white) | Dynamic, updates during flight |

**Behavior**: Projectile glyph recalculates each animation frame based on trajectory direction.

## Complete Glyph Set Summary

**Total distinct glyphs in use: 19**

**By category:**
- Map tiles: 2 (`.`, `#`)
- Player: 1 (`@`)
- Items: 6 (`!`, `♪`, `/`, `}`, `[`, `*`)
- Creatures: 2 (`g`, `r`)
- Projectiles: 4 (`→`, `←`, `↑`, `↓`)
- Special: 4 (`@`, `*`, `?`, and map tiles counted above)

**Unicode character classes:**
- ASCII punctuation: 7 (`.`, `#`, `@`, `!`, `/`, `[`, `*`, `?`)
- ASCII letters: 2 (`g`, `r`)
- Arrows: 4 (`→`, `←`, `↑`, `↓`)
- Special symbols: 2 (`♪`, `}`)

## Color Distribution

Colors group items by semantic meaning and material composition:

**Health/Healing**: HealthFull (#00FF00)
**Magic**: Cyan (#00FFFF)
**Metals**: Steel (#D3D3D3), Iron (#828282), Silver (#C0C0C0)
**Woods**: Oak (#8B7355), AshWood (#B5A380), Mahogany (#C04000)
**Leather**: SoftLeather (#C19A6B), Wool (#E4D4C8)
**Creatures**: Default (white), CommonCreature (#BBBBBB), Coral (#FF6F61), Rust (#B7410E), Fur (#8C7853)
**Terrain**: Basalt (#3A4A4A)
**Player**: Player (#FFFF00)
**Currency**: Gold (#CCAA66)

See **[color.md](color.md)** for complete palette with hex values and usage guidelines.

## Assignment Hierarchy

Glyphs assigned through multi-tier system:

1. **Hardcoded** (Player, Gold, map tiles): Direct assignment in C# constructors
2. **Type defaults** (ItemData.cs, CreatureData.cs): TypeInfo dictionary lookups by `type` field
3. **YAML overrides** (individual data files): Explicit `glyph:` field in entity definitions
4. **Fallback** (DataDefaults.cs): `?` with warning when all else fails

**Data-driven pattern**: Most entities inherit type defaults, designers override selectively in YAML for visual variety.

## Rendering Integration

All glyphs rendered by centralized TextRenderer (Scripts/Systems/TextRenderer.cs):

**Font**: Fira Mono Medium, 18px monospace
**Grid alignment**: 1 tile = 1 character, TileSize 18px = FontSize 18px
**Layer order**: Background → Map → Items → Creatures → Player → Projectiles → Targeting
**Fog-of-war**: Unexplored (black), Explored (dimmed), Visible (full color)

UI components (MessageLog, SidePanel, EquipmentPanel) display same glyph/color pairs using BBCode tags with Palette.ToHex() conversion—unified visual language across map and UI.

## Extension Process

**Adding new entity with existing glyph:**
1. Create YAML file in appropriate Data/ subdirectory
2. Set `type:` field to inherit existing glyph (weapon, armor, goblinoid, etc.)
3. Override `color:` for visual distinction (required for creature variants)

**Adding new glyph family:**
1. Choose distinct Unicode character (test readability at 18px on black)
2. Select semantic Palette color by meaning (see color.md)
3. Add TypeInfo entry to ItemData.cs or CreatureData.cs
4. Document color assignment conventions to prevent variant conflicts

See **[glyphs.md](glyphs.md)** for complete extension guidelines and design philosophy.

---

*This atlas catalogs current glyph assignments as quick reference. Design philosophy and architectural patterns documented in glyphs.md; color semantics in color.md.*
