# Color Design

This document describes the color design philosophy and palette organization for **Pits of Despair**.

## Design Philosophy

### Why a Centralized Palette?

**Pits of Despair** uses a centralized, carefully curated palette rather than arbitrary color choices throughout the codebase. This approach provides:

- **Visual Consistency**: Same semantic concept = same color everywhere
- **Maintainability**: Change a color once, update everywhere
- **Semantic Clarity**: Colors communicate meaning, not just appearance
- **Aesthetic Coherence**: All colors designed together as a unified system

### Core Design Principles

**High Contrast on Black**

All colors maximize readability against pure black backgrounds, essential for classic roguelike aesthetics where clarity trumps subtlety. Every color must pop against darkness.

**Semantic Over Arbitrary**

Colors have meaning. Use `HealthCritical`, `Steel`, or `Poison`—not `Red3`, `Gray7`, or `Green2`. Color names communicate purpose directly.

**Material-Driven Design**

Colors represent physical materials (metals, stones, woods, fabrics), creating intuitive visual consistency. Players recognize materials without labels.

**Intentional Limitation**

Deliberately constrained palette prevents color proliferation that dilutes visual language. Not every shade variation warrants its own entry.

**Color as Functional Distinction**

Color enables instant recognition when glyphs repeat. Multiple swords sharing `/` glyph differentiate through color (Bronze, Iron, Steel). Creature variants need distinct colors for quick threat assessment. This gameplay clarity enables rapid scanning and tactical decisions without reading descriptions.

## Palette Organization

The palette is organized into **semantic categories** that reflect the game's structure and player needs. Each category serves a distinct purpose in the visual language.

### Category Types

**UI & Feedback**
- System states (empty space, fog of war, disabled elements)
- Player feedback (success, danger, caution, alerts)
- Health visualization (progressive gradient from healthy to critical)
- Targeting and interaction indicators

**Materials**
- **Metals**: Progression from crude to precious (iron → steel → silver → gold)
- **Stone**: Geological materials (dark volcanic to light worked stone)
- **Organic**: Leather, cloth, wood, bone - equipment materials
- **Natural**: Earth tones for organic items and environments

**Creatures & Effects**
- Organic and biological colors for creatures
- Elemental effects (fire, ice, lightning, poison, acid)
- Magical spell signatures
- Special effect highlighting

### Organizational Philosophy

**Progression Systems**: Material categories represent progression (crude → refined metals, common → rare stones), creating visual hierarchies players learn.

**Thematic Grouping**: Related concepts share color families (bright elemental effects, metallic metals), building intuitive associations.

**Practical Purpose**: Categories emerge from gameplay needs, not arbitrary organization.

## Design Patterns

### Semantic Naming

Color names follow consistent patterns that communicate purpose:

- **State-Based**: `HealthCritical`, `HealthFull`
- **Material-Based**: `Iron`, `Steel`, `Copper`, `Bronze`
- **Function-Based**: `Success`, `Danger`, `Disabled`
- **Element-Based**: `Fire`, `Ice`, `Lightning`, `Poison`

Never name colors by appearance alone (`BrightRed`, `DarkGreen`). Always name by semantic meaning.

### Semantic Usage

Use colors by meaning, not appearance. `Success` and `HealthFull` may both be green, but serve different semantic purposes. Correct semantic usage ensures consistency when palette changes.

### Color Reuse for Semantic Clarity

Colors may intentionally share the same hex value when serving distinct semantic purposes. For example:

- `CombatDamage` (#DD6655) and `HealthCritical` (#DD6655) both use red, but have different semantic meanings
- `StatusBuff` (#66DD66) and `HealthFull` (#66DD66) both use green for positive associations
- `CombatBlocked` (#99AABB) and `Steel` (#99AABB) both use steel blue-gray, reinforcing armor/defense

This is intentional and correct. Color reuse provides:

- **Semantic Association**: Related concepts share visual language (buffs and full health both feel positive)
- **Palette Economy**: Prevents unnecessary color proliferation while maintaining clear meaning
- **Code Clarity**: Named constants communicate intent (`CombatDamage` vs `HealthCritical`) even with identical values
- **Maintainability**: Semantic names allow independent changes if needs diverge later

Always choose the semantically appropriate color name for context. Use `CombatDamage` for message log combat, `HealthCritical` for health bars—even though they're currently the same red. The name documents intent and provides flexibility.

### Material Consistency

Equipment colors match composition (steel swords use Steel, bronze armor uses Bronze), creating instant material recognition.

### Distinct Items, Distinct Colors

Prioritize visual distinction when assigning colors:

**Same-glyph items must differ**: Short sword (Iron), Longsword (Steel), Bastard sword (ForgedSteel) all use `/` but different colors.

**Creature variants never share colors**: Goblin, Goblin warrior, Goblin shaman each get distinct colors for threat assessment.

**Clarity over realism**: When material accuracy conflicts with distinction (two copper items with similar glyphs), prioritize gameplay clarity through quality variations (CrudeIron vs Iron vs Steel). Instant recognition outweighs material accuracy.

### Fog of War Pattern

Three visibility states: **Unexplored** (black), **Explored** (FogOfWar gray), **Visible** (full color). Applies to all entities.

### Alpha Transparency for Overlays

Palette colors are opaque. Derive semi-transparent overlays (targeting, area effects) at runtime by applying alpha to base colors. Don't add palette entries for transparency variations.

## Guidelines for Adding Colors

### When to Add Colors

Add colors for: new semantic categories (status effects, UI states), material progression gaps, or concepts current palette can't express.

Don't add for: one-off tweaks, semantic duplicates, runtime variations (alpha/brightness), or personal preference.

### Adding New Colors

Choose semantic name, categorize appropriately, ensure high contrast against black, check distinctiveness, document meaning. Grow thoughtfully, not arbitrarily.

## Color Accessibility

Important information doesn't rely on color alone: health shows numeric percentages, targeting uses borders/text, status effects include symbols, UI feedback combines color with text. High-contrast design and bright, saturated colors aid readability.

## Technical Implementation

Centralized system provides global access to semantic colors, supports YAML color references by name, converts between formats (native objects vs hex strings), and organizes colors using code regions. Explore codebase for implementation details.

---

*This color system creates visual consistency, semantic clarity, and aesthetic coherence across the entire game. When in doubt, favor existing colors over creating new ones, and always prioritize meaning over appearance.*
