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

All colors are designed for maximum readability against pure black backgrounds. This is essential for the classic roguelike aesthetic where clarity and visual hierarchy matter more than subtle gradients. Every color must "pop" against darkness.

**Semantic Over Arbitrary**

Colors have meaning. We use `HealthCritical`, `Steel`, or `Poison` - not `Red3`, `Gray7`, or `Green2`. When you see a color name, you understand its purpose without consulting a reference chart.

**Material-Driven Design**

Many colors represent physical materials: metals, stones, woods, fabrics. This creates intuitive visual consistency - steel items look like steel, bronze looks like bronze, leather looks like leather. Players develop recognition patterns for materials without explicit labels.

**Intentional Limitation**

The palette is deliberately constrained. Not every subtle shade variation gets its own entry. This constraint forces consistency and prevents color proliferation that would dilute the visual language.

**Color as Functional Distinction**

In a text-based roguelike, color is a critical tool for instant recognition. When multiple items share the same glyph (e.g., multiple swords all displayed as `/`), color becomes the primary way players distinguish them at a glance. Similarly, different creatures must be visually distinct so players can quickly assess threats and make tactical decisions.

**Design Rule**: Items of the same type should use different colors whenever possible. A bronze sword, iron sword, and steel sword might all use the `/` glyph, but their distinct colors (Bronze, Iron, Steel) let players instantly recognize which is which. The same principle applies to creatures - different goblin types, different undead, different beasts should all have distinct colors.

This isn't just aesthetics - it's gameplay clarity. Players scan the dungeon floor quickly, and color coding enables instant pattern recognition without reading detailed descriptions.

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

**Progression Systems**: Material categories often represent progression - crude metals to refined, common stones to rare. This creates visual hierarchies players learn to read.

**Thematic Grouping**: Related concepts share color families. All elemental effects are bright and distinct. All metals share a metallic quality. This builds intuitive associations.

**Practical Purpose**: Categories emerge from gameplay needs, not arbitrary organization. Health states exist because players need instant health feedback. Material colors exist because items need material-based distinction.

## Design Patterns

### Semantic Naming

Color names follow consistent patterns that communicate purpose:

**State-Based**: `HealthCritical`, `HealthFull`
**Material-Based**: `Iron`, `Steel`, `Copper`, `Bronze`
**Function-Based**: `Success`, `Danger`, `Disabled`
**Element-Based**: `Fire`, `Ice`, `Lightning`, `Poison`

Never name colors by appearance alone (`BrightRed`, `DarkGreen`). Always name by semantic meaning.

### Semantic Usage

Use colors by their **meaning**, not their **appearance**:

- ✅ Use `Success` for positive messages
- ❌ Don't use `HealthFull` for positive messages just because it's also green

Both colors might be green, but they serve different semantic purposes. Using the correct semantic color ensures consistency if you later adjust the palette.

### Material Consistency

Equipment colors should match their composition:

- Steel swords use Steel color
- Bronze armor uses Bronze color
- Leather armor uses leather colors
- Wooden clubs use wood colors

This creates instant visual recognition of equipment materials.

### Distinct Items, Distinct Colors

When assigning colors to items and creatures, prioritize visual distinction:

**Items of the Same Type**

Items sharing the same glyph must use different colors to be distinguishable:

- ✅ Short sword (Iron), Longsword (Steel), Bastard sword (ForgedSteel) - All use `/` glyph but different colors
- ❌ Short sword (Iron), Longsword (Iron), Bastard sword (Iron) - Indistinguishable at a glance

Even items with different glyphs benefit from color variety when they appear together frequently.

**Creature Variants**

Different creature types should never share colors, even if related:

- ✅ Goblin (one color), Goblin warrior (different color), Goblin shaman (third color)
- ❌ All goblins using the same green - Players can't distinguish threats

**Balancing Realism and Clarity**

Sometimes material realism conflicts with distinction. A copper dagger and copper sword are both copper - they "should" be copper-colored. But if they commonly appear together and share similar glyphs, consider:

- Using material progression (CrudeIron vs Iron vs Steel)
- Differentiating by quality or craftsmanship
- Prioritizing gameplay clarity over strict material accuracy

The player experience of "I can tell these apart instantly" outweighs "these are technically the same material."

**Visual Scanning**

Players should be able to scan a room full of items or creatures and immediately understand what they're seeing through color alone. This enables quick tactical decisions in a turn-based environment where information density matters.

### Fog of War Pattern

The game uses color to distinguish three visibility states:

1. **Unexplored** (Empty/black) - Never seen
2. **Explored but not visible** (FogOfWar/gray) - Seen before, memory
3. **Currently visible** (Full color) - Real-time

This pattern extends to all entities, not just tiles.

### Alpha Transparency for Overlays

Palette colors are opaque. When you need semi-transparent overlays (targeting ranges, area effects), derive them at runtime:

- Take the base palette color
- Apply desired alpha transparency
- Don't add separate palette entries for transparency variations

This keeps the palette focused on semantic colors, not rendering variations.

## Guidelines for Adding Colors

### When to Add Colors

Add new palette colors when:

- **New semantic category emerges**: New status effects, new UI states, new feedback types
- **Material progression needs steps**: New equipment tiers between existing materials
- **UI requires new feedback states**: New gameplay systems need distinct visual communication
- **Existing colors don't communicate meaning**: The current palette can't express a new concept

### When NOT to Add Colors

Don't add colors for:

- **One-off visual tweaks**: Use existing semantic colors
- **Slight variations without semantic difference**: If it means the same thing, it should use the same color
- **Runtime variations**: Alpha blending, brightness modulation, etc.
- **Personal preference**: "I just like this shade better" is not sufficient reason

### Adding New Colors Process

When adding a color:

1. **Choose a semantic name** that communicates purpose
2. **Place in appropriate category** (or create new category if needed)
3. **Ensure high contrast** against pure black
4. **Check distinctiveness** against similar colors
5. **Document the meaning** and intended usage

The palette should grow thoughtfully, not arbitrarily.

## Color Accessibility

While the game embraces classic roguelike aesthetics with heavy color use, important information should not rely on color alone:

- **Health states**: Also shown as numeric percentages
- **Targeting**: Combined with borders/highlights and text indicators
- **Status effects**: Accompanied by symbols or text descriptions
- **UI feedback**: Color enhances messages but doesn't carry them alone

The high-contrast-on-black design naturally provides good readability for most players. The bright, saturated colors aid clarity rather than relying on subtle distinctions.

## Technical Implementation Notes

The palette is implemented as a centralized color definition system that:

- Provides global access to semantic colors
- Supports referencing colors by name in data files (YAML)
- Converts between color formats as needed (native color objects vs. hex strings for text formatting)
- Organizes colors into logical groups using code regions

For specific implementation details, explore the codebase. The design principles above should guide you to the right patterns regardless of how the technical implementation evolves.

---

*This color system creates visual consistency, semantic clarity, and aesthetic coherence across the entire game. When in doubt, favor existing colors over creating new ones, and always prioritize meaning over appearance.*
