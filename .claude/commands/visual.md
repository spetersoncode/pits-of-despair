Provide visual design analysis and recommendations for **Pits of Despair** roguelike aesthetics, focusing on glyph selection, color assignment, and visual consistency.

**Input:** {0} (optional - specific entity, color scheme, or design topic)

## Your Role

You are analyzing visual design for a Godot-based roguelike with ASCII/Unicode glyphs rendered on pure black backgrounds. Focus on information-dense UI design and semantic color systems using a **hybrid approach**:
- **Glyphs:** Prefer unique glyphs for distinct entity types with selective Unicode for clarity
- **Colors:** Use categorical color coding to support large bestiary (same glyph, many color variants)

## Process

### 1. Consult Key Documentation

**Required reading before making recommendations:**
- **`Docs/color.md`** - Color design philosophy, palette organization, semantic naming, guidelines for adding colors
- **`Docs/glyphs.md`** - Glyph assignment patterns, visual properties, type-based defaults, fog-of-war rendering
- **`Docs/glyph-atlas.md`** - Complete catalog of all 19 glyphs currently in use with color assignments
- **`Docs/Reference/glyph-analysis.md`** - DCSS/Brogue comparative analysis with modern design strategies
- **`Scripts/Core/Palette.cs`** - Actual palette implementation with 61+ colors organized by semantic categories

### 2. Understand Context

Determine:
- **Entity type:** Item, creature, tile, UI element, effect?
- **Gameplay role:** Threat level, material quality, semantic meaning?
- **Similar entities:** What shares glyphs or categories?
- **Design approach needed:** Traditional roguelike conventions vs. modern clarity?

### 3. Apply Aesthetic Principles

**Glyph Selection Strategy:**
- Prefer **unique glyphs for distinct entity types** when possible
- Use **selective Unicode for clarity** (e.g., `↑` for weapons is more intuitive than `)`)
- Prioritize immediate visual recognition and semantic appropriateness
- Aim for glyphs to be recognizable without relying solely on color
- Balance intuitive symbolism with roguelike tradition

**Color Coding Strategy:**
- Support **large bestiary through categorical color grouping**
- Same glyph can represent multiple creatures differentiated by color
- Group related creatures under shared glyphs (e.g., all dragons use `D`, differentiated by color)
- Color is primary differentiator within creature families
- Ensure sufficient color contrast between variants

**Traditional Conventions to Preserve:**
- `@` for player (universal)
- `.` for floor, `#` for walls, `<`/`>` for stairs, `+` for doors
- Common creatures: `r` (rat), `b` (bat), `D` (dragon), `L` (lich), `V` (vampire), `Z` (zombie)
- Common items: `!` (potion), `[` (armor)

**Intuitive Variations to Consider:**
- Weapon glyph: `↑` (intuitive upward-pointing) vs `)` (traditional curved blade)
- Scroll glyph: `♪` (distinctive musical note) vs `?` (traditional mystery)
- Floor glyph: `·` (middle dot, cleaner) vs `.` (period)
- Trap glyph: `◇` (distinctive diamond) vs `^` (traditional caret)

### 4. Evaluate Glyph Choices

**Criteria:**
- Visual distinction from entities outside its color-coded family
- Renders clearly at single-character size
- Semantic appropriateness (intuitive meaning)
- Follows established patterns or has good reason to diverge
- Serves as effective category marker when color-coding variants

### 5. Evaluate Color Choices

**Process:**
1. Determine **semantic category** (material, effect, creature type, UI function)
2. Review `Palette.cs` for available colors in that category
3. Check for existing patterns (material progressions, creature families)
4. Choose existing palette color by constant name OR justify new color addition

**Criteria:**
- High contrast against black background (readability first)
- Semantic meaning over appearance (`Steel` not `BlueGray3`)
- Distinct from same-glyph entities
- Fits material/progression patterns
- Reuses existing colors when semantically appropriate

### 6. Provide Structured Recommendations

**Format your response:**

**Recommendation:** [Specific glyph and/or color constant name]

**Rationale:**
- Semantic meaning: [Why this fits the concept]
- Visual hierarchy: [How this emphasizes/de-emphasizes appropriately]
- Distinctiveness: [How this differs from unrelated entities or stands out within family]
- Categorical fit: [How this supports glyph families and color-coding strategy]
- Palette coherence: [How this fits existing system]

**Implementation:**
- Glyph: `"X"` (if applicable)
- Color: `Palette.ColorName` (reference constant name)
- YAML: `color: ColorName` (for data files)

**Alternatives Considered:** [Other options and why not chosen]

**Notes:** [Trade-offs, future considerations, documentation updates needed]

## Core Design Principles

### High Contrast on Black
Every color must pop against pure black. No subtle shades. Readability trumps subtlety.

### Semantic Over Arbitrary
Use `Steel` not `BlueGray3`. Use `HealthCritical` not `Red`. Color names communicate purpose.

### Material-Driven Design
Colors represent physical materials (metals, stones, woods), creating intuitive consistency.

### Intentional Limitation
Constrained palette prevents color proliferation. Not every shade variation needs its own entry.

### Color as Functional Distinction
When glyphs repeat, color enables instant recognition. Multiple swords use same glyph but differentiate through Bronze/Iron/Steel.

### Distinct Items Need Distinct Colors
Same-glyph items must differ. Creature variants never share colors. Clarity over realism.

### Categorical Color Grouping for Creatures
Creature families share glyphs and differentiate through color. This supports large bestiaries while maintaining clear visual categories. Players learn "all dragons are `D`" and use color to identify specific variants.

## Common Scenarios

### Glyph Assignment for New Entity
1. Check `glyphs.md` for type-based patterns
2. Review `glyph-analysis.md` for proven design strategies and similar entity examples
3. Consider roguelike conventions vs. intuitive symbolic choices
4. Identify creature family for potential glyph sharing (support color-coding strategy)
5. Ensure visual distinction from unrelated entities
6. Recommend glyph with reasoning

### Color Assignment for Entity
1. Determine semantic category (material, effect, creature, UI)
2. Read `Palette.cs` for available colors in that category
3. Check for material/progression patterns in `color.md`
4. Recommend existing palette color by constant name
5. If no fit exists, propose new color with category/name/hex/justification

### Palette Expansion Decision
1. Review existing colors in relevant categories
2. Check if semantic reuse could work (different name, same hex)
3. Evaluate if one-off variation vs. reusable concept
4. Only recommend addition for new semantic categories or progression gaps
5. Suggest semantic name, hex value, category, and documentation updates

### Visual Balance Review
1. Read current `Palette.cs` implementation
2. Identify potential visual conflicts (similar colors, low contrast)
3. Check distribution across categories
4. Assess progression coherence (materials, health states, threat levels)
5. Provide specific improvements with rationale

---

**Remember:** Maintain visual consistency, semantic clarity, and aesthetic coherence. When in doubt, favor existing colors over creating new ones. Prioritize meaning over appearance. Balance roguelike tradition with intuitive symbolism. Support large bestiary through categorical color grouping while keeping glyphs semantically clear.
