# Glyph Atlas

Complete catalog of glyphs used in Pits of Despair. For design philosophy and extension guidelines, see [glyphs.md](glyphs.md). For color definitions, see [color.md](color.md).

## Map Terrain

| Glyph | Meaning | Color |
|-------|---------|-------|
| `.` | Floor tile | Basalt |
| `#` | Wall tile | Default (white) |

## Player & Special

| Glyph | Meaning | Color |
|-------|---------|-------|
| `@` | Player character | Player (yellow) |
| `*` | Gold pile | Gold |
| `>` | Stairs down | Stairs |
| `Ω` | Throne of Despair | Throne |
| `?` | Unknown/fallback | Default (white) |

## Items by Type

### Consumables

| Glyph | Type | Default Color | Notes |
|-------|------|---------------|-------|
| `!` | Potion | Default | Uses "X of Y" naming |
| `♪` | Scroll | Default | Uses "X of Y" naming |

### Equipment

| Glyph | Type | Default Color | Notes |
|-------|------|---------------|-------|
| `/` | Slashing weapon | Default | Swords, axes |
| `†` | Piercing weapon | Default | Spears, daggers |
| `¶` | Bludgeoning weapon | Default | Clubs, maces |
| `}` | Ranged weapon | Default | Bows, crossbows |
| `[` | Armor | Default | All body armor |
| `=` | Ring | Default | Uses "X of Y" naming |
| `↑` | Ammunition | Default | Auto-pickup enabled |

### Magic Items

| Glyph | Type | Default Color | Notes |
|-------|------|---------------|-------|
| `~` | Wand | Default | Charged items, "X of Y" naming |
| `\|` | Staff | Default | Two-handed magic implements |

## Creatures by Type

| Glyph | Type | Variants |
|-------|------|----------|
| `g` | Goblinoid | goblin, goblin_scout, goblin_ruffian, goblin_archer, goblin_cutter |
| `r` | Rodents | rat, elder_rat |
| `u` | Undead | skeleton, zombie |
| `b` | Others | wild_boar |
| `c` | Others | cat |
| `d` | Others | wild_dog |
| `l` | Others | cave_lizard |

**Variant Rule**: Creatures of the same type use distinct colors for threat differentiation.

## Projectiles

Projectiles use **line-based rendering** rather than glyphs. Animated line segments oriented along flight direction provide smooth visual feedback. See [text-renderer.md](text-renderer.md) for details.

## Glyph Assignment

Glyphs assigned through multi-tier system:

1. **Hardcoded**: Player, gold, stairs, throne (direct C# assignment)
2. **Type defaults**: ItemData.TypeInfo and CreatureData.TypeInfo dictionaries
3. **Damage-type logic**: Weapons auto-assign glyph based on AttackData.DamageType
4. **YAML overrides**: Explicit `glyph:` field in data files
5. **Fallback**: `?` with warning when all else fails

## Item Type Registry

From `ItemData.TypeInfo`:

| Type | Glyph | Equippable | Consumable | Slot | "X of Y" |
|------|-------|------------|------------|------|----------|
| potion | `!` | No | Yes | — | Yes |
| scroll | `♪` | No | Yes | — | Yes |
| weapon | varies | Yes | No | MeleeWeapon | No |
| armor | `[` | Yes | No | Armor | No |
| ammo | `↑` | Yes | Yes | Ammo | No |
| ring | `=` | Yes | No | Ring | Yes |
| wand | `~` | No | No | — | Yes |
| staff | `\|` | No | No | — | Yes |

## Creature Type Registry

From YAML sheet defaults (`Data/Creatures/*.yaml`):

| Type | Glyph | Notes |
|------|-------|-------|
| goblinoid | `g` | |
| rodents | `r` | |
| undead | `u` | |
| others | — | Mixed glyphs, each creature overrides |

## Decorations

**Design Rule**: Decorations use symbols and Unicode characters only. Letters are reserved for creatures to ensure instant visual distinction.

### Generic Dungeon

| Glyph | Name | Color | Walkable | Destructible |
|-------|------|-------|----------|--------------|
| `∪` | Clay vase | Brick | Yes | Yes |
| `◎` | Wooden barrel | Walnut | Yes | Yes |
| `▫` | Wooden crate | Oak | Yes | Yes |
| `╥` | Stone pillar | Granite | No | No |
| `¥` | Brazier | Iron | No | No |
| `░` | Rubble | Granite | Yes | No |
| `¤` | Bones | Bone | Yes | No |
| `≈` | Cobwebs | FogOfWar | Yes | No |
| `∿` | Puddle | Water | Yes | No |
| `·` | Moss | Moss | Yes | No |
| `⌇` | Crack | Basalt | Yes | No |

### Goblinoid Theme

| Glyph | Name | Color | Walkable | Destructible |
|-------|------|-------|----------|--------------|
| `╬` | Crude totem | Walnut | No | Yes |
| `☠` | Skull pile | Bone | Yes | No |
| `π` | Meat rack | ToughLeather | Yes | Yes |
| `≡` | Filthy bedroll | Wool | Yes | No |
| `⚑` | Crude banner | Rust | Yes | Yes |
| `⌂` | Scrap pile | CrudeIron | Yes | No |
| `†` | Bone trophy | Bone | Yes | No |
| `○` | Firepit remains | Basalt | Yes | No |

## See Also

- [glyphs.md](glyphs.md) - Design philosophy and extension guidelines
- [color.md](color.md) - Color semantics and palette
- [yaml.md](yaml.md) - Data file configuration
