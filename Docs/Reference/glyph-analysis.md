# Roguelike Glyph Systems: Comparative Analysis

A comparative analysis of glyph (display character) usage in two major roguelikes: **Dungeon Crawl Stone Soup (DCSS)** and **Brogue: Community Edition (Brogue CE)**.

**Purpose:** This document provides structured guidance for selecting glyphs for your own roguelike game by analyzing proven design patterns from two successful games.

**Last Updated:** 2025-11-19

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [System Philosophy Comparison](#system-philosophy-comparison)
3. [Category-by-Category Analysis](#category-by-category-analysis)
4. [Design Principles](#design-principles)
5. [Glyph Selection Strategies](#glyph-selection-strategies)
6. [Quick Reference Tables](#quick-reference-tables)
7. [Recommendations by Game Type](#recommendations-by-game-type)

---

## Executive Summary

### DCSS Approach
- **Total glyphs:** 64+ unique creature glyphs, 17 item types
- **Total entities:** 661 monster types
- **Strategy:** Heavy glyph reuse differentiated by color
- **Character set:** CP437/WGL4 extended characters with ASCII fallback
- **Design goal:** Maximize variety within ASCII constraints

### Brogue CE Approach
- **Total glyphs:** ~50 unique creature glyphs
- **Total entities:** ~80 monster types
- **Strategy:** More unique glyphs, less reuse, selective Unicode
- **Character set:** Mix of ASCII and carefully chosen Unicode symbols
- **Design goal:** Visual clarity and intuitive symbolism

### Key Difference
**DCSS** uses the same glyph for many creatures (e.g., 11 beetle types all use `B`), relying on color. **Brogue** prefers giving different creature types distinct glyphs when possible (e.g., `O` for Ogre, `T` for Troll, not both using a "large humanoid" glyph).

---

## System Philosophy Comparison

### DCSS Philosophy: Categorical Grouping
```
Glyph = Creature Category
Color = Specific Variant
```

**Example:**
- `D` = All dragons (21 types)
  - Bone dragon (white)
  - Gold dragon (yellow)
  - Shadow dragon (dark)
  - Steam dragon (light gray)

**Advantages:**
- Players learn categories quickly
- Easy to remember (all dragons are `D`)
- Supports large bestiary (661 monsters)

**Disadvantages:**
- Requires good color differentiation
- Can be confusing when colors are similar
- Harder to distinguish at a glance

---

### Brogue CE Philosophy: Semantic Clarity
```
Glyph = Specific Creature or Small Group
Color = Secondary differentiation
```

**Example:**
- `D` = Dragon (only 1 creature)
- `g` = Goblins (4 types, all goblins)
- `O` = Ogres (2 types, both ogres)

**Advantages:**
- Immediate visual recognition
- Less reliance on color
- Clearer for colorblind players

**Disadvantages:**
- Limited scalability (runs out of glyphs)
- Less systematic categorization
- Harder to extend with new creatures

---

## Category-by-Category Analysis

### Player Character

| Game | Glyph | Notes |
|------|-------|-------|
| **DCSS** | `@` | Standard roguelike convention |
| **Brogue** | `@` | Standard roguelike convention |
| **Recommendation** | `@` | Universal roguelike standard - don't change this |

---

### Items

#### Weapons

| Game | Glyph | Philosophy |
|------|-------|------------|
| **DCSS** | `)` | Traditional roguelike symbol (looks like a scimitar) |
| **Brogue** | `↑` (U+2191) | Arrow symbol (more intuitive) |

**Options for your game:**
1. **Traditional:** `)` - Familiar to roguelike veterans
2. **Modern/Intuitive:** `↑` - Clear "weapon pointing up" symbolism
3. **Alternative:** `/` or `|` - Simple stick-like weapons
4. **Category-specific:** `)` swords, `/` axes, `|` spears (DCSS approach but more granular)

#### Armor

| Game | Glyph | Philosophy |
|------|-------|------------|
| **DCSS** | `[` | Traditional (looks like armor piece) |
| **Brogue** | `[` | Traditional |

**Consensus:** `[` is the standard. Consider keeping it.

#### Potions

| Game | Glyph | Philosophy |
|------|-------|------------|
| **DCSS** | `!` | Traditional (potion bottle) |
| **Brogue** | `!` | Traditional |

**Consensus:** `!` is universal.

#### Scrolls

| Game | Glyph | Philosophy |
|------|-------|------------|
| **DCSS** | `?` | Traditional (mysterious writing) |
| **Brogue** | `♪` (U+266A) | Musical note (creative choice) |

**Options for your game:**
1. **Traditional:** `?` - Unknown/mysterious content
2. **Symbolic:** `♪` - Unique and distinctive (Brogue's choice)
3. **Alternative:** `%` - Paper/document symbol
4. **Literal:** `"` - Looks like a scroll unfurled

#### Staves/Wands

| Game | Staves | Wands | Notes |
|------|--------|-------|-------|
| **DCSS** | `|` | `/` | Vertical vs angled |
| **Brogue** | `/` | `~` | Angled vs wavy |

**Options for your game:**
1. **Differentiate by angle:** `|` (staff) vs `/` (wand)
2. **Differentiate by symbol:** `/` (staff) vs `~` (wand)
3. **Use one category:** `/` for all magical implements
4. **Symbolic:** `τ` or `♪` for magical items (if you want Unicode)

#### Rings & Amulets

| Game | Rings | Amulets | Notes |
|------|-------|---------|-------|
| **DCSS** | `=` | `"` | Horizontal = ring, " = necklace |
| **Brogue** | `⚪` (U+26AA) | `♀` (U+2640) | Circle = ring, Venus = amulet |

**Options for your game:**
1. **ASCII traditional:** `=` (ring), `"` (amulet)
2. **Unicode symbolic:** `⚪` (ring), `♀` (amulet)
3. **Simplified:** Use `=` for all jewelry
4. **Alternative:** `o` (ring), `O` (amulet) - size difference

#### Gold

| Game | Glyph | Philosophy |
|------|-------|------------|
| **DCSS** | `$` | Dollar sign (universal money) |
| **Brogue** | `*` | Sparkle/shine |

**Options for your game:**
1. **Universal:** `$` - Everyone recognizes this
2. **Symbolic:** `*` - Represents shininess
3. **Alternative:** `¤` (U+00A4) - Currency symbol
4. **Letter-based:** `G` or `g` for "gold"

#### Special Items

| Game | Books | Misc | Gems | Food |
|------|-------|------|------|------|
| **DCSS** | `:` | `}` | `♦`/`$` | N/A |
| **Brogue** | N/A | N/A | `●` (U+25CF) | `;` |

---

### Monster Categories

#### Humanoids (Common)

| Category | DCSS | Brogue | Notes |
|----------|------|--------|-------|
| **Humans** | `@` | `@` (rare) | DCSS: humanoids, Brogue: player only |
| **Kobolds** | `K` | `k` | DCSS: uppercase category, Brogue: lowercase common |
| **Goblins** | `g` | `g` | Both use lowercase |
| **Orcs** | `o` | N/A | DCSS only |
| **Gnolls** | `j` | N/A | DCSS only |

**Design Pattern:**
- **DCSS:** Lowercase = common humanoids (`g`, `o`, `k`)
- **Brogue:** Lowercase = small/weak creatures (`k`, `g`, `j`)

**Options for your game:**
1. **Alphabetic by name:** `k` = kobold, `g` = goblin, `o` = orc
2. **Size-based:** Lowercase = small, Uppercase = large
3. **Threat-based:** Lowercase = weak, Uppercase = dangerous
4. **Hybrid:** Use letter of race name, case by size/threat

#### Dragons

| Game | Glyph | Count | Differentiation |
|------|-------|-------|-----------------|
| **DCSS** | `D` | 21 types | Color only |
| **Brogue** | `D` | 1 type | Single dragon |

**Design Pattern:**
- **DCSS:** One glyph for entire category, heavy color use
- **Brogue:** Limited creature types, less need for variants

**Options for your game:**
1. **Single glyph:** `D` for all dragons (DCSS style)
2. **Size-based:** `d` = drake, `D` = dragon, `W` = wyrm
3. **Element-based:** `D` = dragon, `@` with color for type
4. **Limited roster:** One or two dragon types only (Brogue style)

#### Undead

| Type | DCSS | Brogue | Notes |
|------|------|--------|-------|
| **Zombie** | `Z` | `Z` | Both uppercase |
| **Skeleton** | `z` | N/A | DCSS: lowercase |
| **Wraith** | `W` | `W` | Both uppercase |
| **Lich** | `L` | `L` | Both uppercase |
| **Vampire** | `V` | `V` | Both uppercase |
| **Ghost** | `W` | N/A | DCSS: shares with wraith |
| **Mummy** | `M` | N/A | DCSS only |

**Design Pattern:**
- Clear consensus: Major undead get uppercase letters
- `L` = Lich, `V` = Vampire, `W` = Wraith, `Z` = Zombie

**Recommendation:** Use uppercase letters for undead types, one letter per major category.

#### Demons

| Tier | DCSS | Brogue | Notes |
|------|------|--------|-------|
| **Demon Lords** | `&` | N/A | DCSS: special symbol |
| **Greater Demons** | `1` | N/A | DCSS: numbered tiers |
| **Major Demons** | `2` | N/A | DCSS: numbered tiers |
| **Lesser Demons** | `3` | N/A | DCSS: numbered tiers |
| **Minor Demons** | `4` | N/A | DCSS: numbered tiers |
| **Imps** | `5` | `i` | DCSS: number, Brogue: letter |

**Design Pattern:**
- **DCSS:** Numeric hierarchy (`1` = strongest, `5` = weakest)
- **Brogue:** Alphabetic naming (`i` = imp)

**Options for your game:**
1. **Numeric tiers:** `1-5` for demon ranks (very clear hierarchy)
2. **Alphabetic:** `&` = demon lord, `d` = demon, `i` = imp
3. **Mixed:** `&` for lords, `1-4` for tiered demons, `i` for imps
4. **Symbolic:** `&` for all demons, differentiate by color

#### Animals

| Type | DCSS | Brogue | Notes |
|------|------|--------|-------|
| **Rat** | `r` | `r` | Universal |
| **Bat** | `b` | `b` | Universal |
| **Dog/Wolf** | `h` | `h` | "Hound" category |
| **Snake** | `S` | N/A | DCSS only |
| **Spider** | `s` | `s` | Both lowercase |
| **Frog** | `F` | N/A | DCSS uppercase |
| **Yak** | `Y` | N/A | DCSS only |

**Design Pattern:**
- Small animals: lowercase (`r`, `b`)
- Large animals: uppercase (`Y`, `F`, `S`)

**Recommendation:** This is a clear, intuitive pattern to follow.

#### Jellies/Slimes

| Game | Glyph | Count | Notes |
|------|-------|-------|-------|
| **DCSS** | `J` | 11 types | Color-differentiated |
| **Brogue** | `J` | 3 types | Pink, Acidic, Black |

**Consensus:** `J` is the standard for jellies/oozes across roguelikes.

#### Unique/Special Monsters

| Type | DCSS | Brogue | Notes |
|------|------|--------|-------|
| **Animated weapons** | `(` | `↑` | DCSS: parenthesis, Brogue: weapon glyph |
| **Orbs/Energy** | `*` | N/A | DCSS: asterisk |
| **Eyes** | `G` | N/A | DCSS: "Gaze" category |
| **Constructs** | `9` | `G` | DCSS: number, Brogue: Golem |
| **Angels** | `A` | N/A | DCSS only |
| **Trees** | `7` | N/A | DCSS: number (looks like tree) |

**Brogue-specific:**
- `ß` (U+00DF) = Guardians, statues (reused glyph)
- `⚲` (U+26B2) = Totems
- `Y` = Warden (unique boss)

---

### Environment & Terrain

#### Basic Terrain

| Feature | DCSS | Brogue | Notes |
|---------|------|--------|-------|
| **Floor** | `.` | `·` (U+00B7) | DCSS: period, Brogue: middle dot |
| **Wall** | `#` | `#` | Universal |
| **Door (closed)** | `+` | `+` | Universal |
| **Door (open)** | `'` | `'` | Universal |
| **Stairs up** | `<` | `<` | Universal |
| **Stairs down** | `>` | `>` | Universal |

**Consensus:** These are universal roguelike standards. Keep them.

#### Water & Liquids

| Feature | DCSS | Brogue | Notes |
|---------|------|--------|-------|
| **Shallow water** | `~` | `~` | Universal |
| **Deep water** | `≈` (U+2248) / `~` | `~` | DCSS: Unicode for deep, ASCII fallback |

**Options for your game:**
1. **Single glyph:** `~` for all water (simpler)
2. **Differentiated:** `~` shallow, `≈` deep (more informative)
3. **Alternative:** Use color only to distinguish depth

#### Traps

| Game | Glyph | Notes |
|------|-------|-------|
| **DCSS** | `^` | Traditional "pointy trap" |
| **Brogue** | `◇` (U+25C7) | Diamond symbol |

**Options for your game:**
1. **Traditional:** `^` - Universal roguelike standard
2. **Symbolic:** `◇` - More distinctive diamond
3. **Hidden:** Don't show traps until triggered/detected
4. **Alternative:** `*` or `x` for trapped tiles

#### Special Features

| Feature | DCSS | Brogue | Notes |
|---------|------|--------|-------|
| **Altar** | `_` | `|` | DCSS: underscore, Brogue: vertical |
| **Fountain** | `⌠` (U+2320) / `-` | N/A | DCSS Unicode |
| **Statue** | `ß` (U+00DF) / `8` | `ß` (U+00DF) | Both use German eszett |
| **Tree** | `♣` (U+2663) / `7` | N/A | DCSS: club suit or lucky 7 |
| **Grass** | N/A | `"` | Brogue only |
| **Chasm** | N/A | `☍` (U+2237) | Brogue only |

---

## Design Principles

### From DCSS

1. **Categorical grouping** - Same glyph for related creatures
2. **Heavy color use** - Color is primary differentiator
3. **Numeric hierarchies** - Use `1-9` for power tiers
4. **ASCII fallback** - Always provide ASCII alternative
5. **Maximize reuse** - Support large bestiary with limited glyphs

### From Brogue

1. **Semantic clarity** - Glyph should suggest creature type
2. **Limited roster** - Fewer creature types, more distinct glyphs
3. **Selective Unicode** - Use Unicode where it adds clarity
4. **Visual distinction** - Minimize reliance on color alone
5. **Intuitive symbols** - Choose glyphs that make sense (↑ for weapon)

---

## Glyph Selection Strategies

### Strategy 1: DCSS-Style (High Reuse, Color-Based)

**Best for:** Games with 200+ creature types

**Approach:**
1. Assign one glyph per broad category
2. Use color as primary differentiator
3. Use numeric glyphs for hierarchies
4. Stick to ASCII for maximum compatibility

**Example Mapping:**
```
D = All dragons (red, blue, green, gold, shadow, etc.)
d = All drakes (fire, ice, wind, etc.)
O = All ogres (normal, mage, warrior, etc.)
o = All orcs (warrior, shaman, archer, etc.)
1-5 = Demon hierarchy (1=strongest, 5=weakest)
```

**Pros:** Scales to very large bestiaries, consistent categorization
**Cons:** Requires good color support, harder to distinguish at a glance

---

### Strategy 2: Brogue-Style (Low Reuse, Glyph-Based)

**Best for:** Games with <100 creature types

**Approach:**
1. Assign unique glyph to each creature or small group
2. Use color as secondary enhancement
3. Choose Unicode symbols that add clarity
4. Prioritize immediate visual recognition

**Example Mapping:**
```
D = Dragon (single creature, red)
T = Troll (green, regenerates)
O = Ogre (brown, big humanoid)
g = Goblin family (4-5 variants, all green-ish)
k = Kobold (small red humanoid)
```

**Pros:** Clear visual distinction, less color-dependent, intuitive
**Cons:** Limited scalability, runs out of meaningful glyphs

---

### Strategy 3: Hybrid Approach (Recommended)

**Best for:** Games with 100-200 creature types

**Approach:**
1. Use unique glyphs for major creature types
2. Allow controlled reuse within families
3. Reserve numbers for hierarchies or special uses
4. Use ASCII with selective Unicode enhancements

**Example Mapping:**
```
Major uniques:
  D = Dragon
  L = Lich
  V = Vampire
  T = Troll

Controlled families:
  g/G = Goblin family (g=common, G=boss)
  o/O = Orc family (o=common, O=elite)

Hierarchies:
  1-5 = Demon ranks

Items:
  ) = Weapons
  [ = Armor
  ! = Potions
  ? = Scrolls
```

**Pros:** Balances scalability and clarity, flexible
**Cons:** Requires careful planning to avoid confusion

---

### Strategy 4: Thematic/Symbolic Approach

**Best for:** Games with unique setting/theme

**Approach:**
1. Choose glyphs based on thematic meaning
2. Break from traditional roguelike conventions where appropriate
3. Use Unicode liberally for atmosphere
4. Prioritize aesthetic consistency over tradition

**Example Mapping (Sci-Fi):**
```
Creatures:
  ⚛ = Robot
  ◈ = Drone
  ⚡ = Energy being
  ◐ = Cyborg

Items:
  ⚙ = Tech item
  ◈ = Data chip
  ⚡ = Energy cell
  ⚕ = Medical supply
```

**Pros:** Unique identity, atmospheric, memorable
**Cons:** Less familiar to roguelike veterans, Unicode required

---

## Quick Reference Tables

### Universal Standards (Don't Change These)

| Glyph | Meaning | Status |
|-------|---------|--------|
| `@` | Player | Absolute standard |
| `.` or `·` | Floor | Near-universal |
| `#` | Wall | Near-universal |
| `<` | Stairs up | Universal |
| `>` | Stairs down | Universal |
| `+` | Closed door | Universal |
| `'` | Open door | Universal |
| `~` | Water | Near-universal |

### Strong Conventions (Safe to Use)

| Glyph | Meaning | Usage |
|-------|---------|-------|
| `)` | Weapon | DCSS + many others |
| `[` | Armor | Both games |
| `!` | Potion | Both games |
| `?` | Scroll | DCSS + many others |
| `$` | Gold | DCSS + many others |
| `=` | Ring | DCSS + many others |
| `J` | Jelly/Ooze | Both games |
| `r` | Rat | Both games |
| `b` | Bat | Both games |
| `h` | Hound/Dog | Both games |
| `D` | Dragon | Both games |
| `L` | Lich | Both games |
| `V` | Vampire | Both games |
| `Z` | Zombie | Both games |
| `^` | Trap | DCSS + traditional |

### Creative Variations (Your Choice)

| Glyph | DCSS | Brogue | Notes |
|-------|------|--------|-------|
| Weapon | `)` | `↑` | Traditional vs intuitive |
| Scroll | `?` | `♪` | Mystery vs symbolic |
| Wand | `/` | `~` | Angled vs wavy |
| Gold | `$` | `*` | Currency vs sparkle |
| Floor | `.` | `·` | Period vs middle dot |
| Altar | `_` | `|` | Horizontal vs vertical |

---

## Recommendations by Game Type

### For a Traditional Dungeon Crawler
**Recommended approach:** Strategy 1 (DCSS-Style) or Strategy 3 (Hybrid)

**Core glyph set:**
```
Creatures (alphabetic):
  @ = Player
  a-z = Common creatures (lowercase by name or category)
  A-Z = Dangerous creatures (uppercase variants)
  1-9 = Special hierarchies (demons, elementals, etc.)
  &, *, ; = Ultra-special (demon lords, orbs, etc.)

Items (symbolic):
  ) = Weapons
  [ = Armor
  ! = Potions
  ? = Scrolls
  / = Staves
  ~ = Wands
  = = Rings
  " = Amulets
  $ = Gold
  % = Food/Corpses

Environment (structural):
  . = Floor
  # = Wall
  + = Closed door
  ' = Open door
  < = Stairs up
  > = Stairs down
  ~ = Water
  ^ = Trap
  _ = Altar
```

---

### For a Compact/Short Roguelike
**Recommended approach:** Strategy 2 (Brogue-Style)

**Core glyph set:**
```
~20 creature types with unique glyphs:
  r = Rat
  k = Kobold
  g = Goblin
  s = Spider
  O = Ogre
  T = Troll
  D = Dragon
  L = Lich
  V = Vampire
  Z = Zombie

~8 item categories:
  ↑ = Weapon (intuitive)
  [ = Armor
  ! = Potion
  ♪ = Scroll (distinctive)
  / = Staff
  ~ = Wand
  ● = Ring (Unicode)
  $ = Gold

Environment (minimal):
  · = Floor (Unicode middle dot)
  # = Wall
  + = Door
  < / > = Stairs
  ~ = Water
  ◇ = Trap (Unicode)
```

---

### For a Modern/Graphical-Hybrid Game
**Recommended approach:** Strategy 4 (Thematic)

**Core glyph set:**
```
Mix ASCII and Unicode freely:
  Creatures: Choose thematic symbols
    ⚔ = Knight
    🏹 = Archer
    🧙 = Mage
    👹 = Monster

  Items: Use clear Unicode symbols
    ⚔ = Weapon
    🛡 = Shield
    ⚗ = Potion
    📜 = Scroll

  Environment: Enhanced symbols
    ░ = Floor
    ▓ = Wall
    ⬆ = Stairs up
    ⬇ = Stairs down
```

**Note:** Requires full Unicode support and good font rendering.

---

### For a Terminal-Only ASCII Game
**Recommended approach:** Strategy 1 (DCSS-Style) with strict ASCII

**Core glyph set:**
```
Stick to ASCII 32-126 only:
  A-Z, a-z = Creatures
  0-9 = Special creatures or hierarchies
  !, @, #, $, %, ^, &, *, ( = Special creatures/items
  ), [, ], {, }, <, >, / = Items and environment
  +, -, =, _, ~, `, ', " = Environment and items
  :, ;, , , . = Environment

Avoid:
  | (often rendered poorly)
  \ (escape character issues)
  Any Unicode
```

---

## Glyph Conflict Resolution

### Common Conflicts

| Glyph | Possible Use 1 | Possible Use 2 | Resolution |
|-------|----------------|----------------|------------|
| `*` | Gold (sparkle) | Orb creature | Use `$` for gold (standard) |
| `|` | Staff/Wand | Altar | Use `/` for staff, `|` for altar |
| `&` | Demon lord | Dewar/container | Context-dependent, prefer demon |
| `●` | Turret | Gem | Color differentiation |
| `0` | Cloud | Orb | Context-dependent (clouds move) |

### Glyph Priority (Most to Least Important)

1. **Player (`@`)** - Never change
2. **Basic terrain (`.`, `#`, `<`, `>`)** - Standard
3. **Common items (`!`, `?`, `[`, `)`)** - Highly conventional
4. **Doors (`+`, `'`)** - Universal
5. **Common creatures (`r`, `b`, `J`)** - Strong convention
6. **Boss creatures (`D`, `L`, `V`)** - Recognizable
7. **Special features** - More flexible
8. **Decorative elements** - Most flexible

---

## Testing Your Glyph Choices

### Readability Checklist

- [ ] Can you distinguish all glyphs in a 80x24 terminal?
- [ ] Do similar glyphs have different categories? (avoid `O` ogre and `0` orc)
- [ ] Can colorblind players distinguish creatures without color?
- [ ] Do glyphs make intuitive sense? (weapon looks weapon-like?)
- [ ] Are universal standards preserved? (`@`, `.`, `#`, `<`, `>`, `+`)

### Color-Dependence Test

List creatures that share glyphs and ONLY differ by color:
- If list is >30% of total creatures → High color dependence (DCSS-style)
- If list is 10-30% → Moderate dependence (Hybrid)
- If list is <10% → Low color dependence (Brogue-style)

### Scalability Test

- Can you add 50 more creatures without running out of meaningful glyphs?
- Can you add 10 more item types without conflicts?
- Do you have room for future expansion?

### Player Feedback Questions

1. "Without looking it up, what do you think `X` represents?"
2. "Can you spot the dragon in this room?" (test visual search speed)
3. "What's the difference between these two creatures?" (test color distinction)

---

## Implementation Recommendations

### Start Simple, Expand Later

**Phase 1: Core Set (20-30 glyphs)**
- Player, basic terrain, 5-10 items, 10-15 creatures
- Stick to universal standards
- Pure ASCII

**Phase 2: Expansion (50-60 glyphs)**
- Add more creature types
- Introduce hierarchies or families
- Consider selective Unicode

**Phase 3: Polish (100+ glyphs)**
- Add special effects
- Enhance with Unicode where beneficial
- Create comprehensive reference

### Maintain a Glyph Registry

Keep a master table:
```markdown
| Glyph | Category | Entity | Color | ASCII? | Notes |
|-------|----------|--------|-------|--------|-------|
| @ | Player | Player | White | Yes | Universal standard |
| r | Creature | Rat | Brown | Yes | Common enemy |
| ↑ | Item | Sword | Gray | No | U+2191, ASCII fallback ) |
```

### Provide Configuration Options

Allow players to customize:
1. **Glyph set:** ASCII vs Unicode
2. **Color mode:** Full color vs 16-color vs monochrome
3. **Custom glyphs:** Let players override specific glyphs
4. **Glyph reference:** In-game help showing all glyphs

---

## Conclusion

### Quick Decision Matrix

**Your game has 200+ creatures?** → Use DCSS-style categorical grouping
**Your game has <100 creatures?** → Use Brogue-style unique glyphs
**Your game has 100-200 creatures?** → Use Hybrid approach
**Your game has unique theme?** → Use Thematic approach

### Final Recommendations

1. **Always preserve universal standards** (`@`, basic terrain, stairs, doors)
2. **Choose an approach and stay consistent**
3. **Prioritize readability over cleverness**
4. **Test with actual players early**
5. **Provide ASCII fallback for Unicode glyphs**
6. **Document your choices** (create a glyph atlas like these)
7. **Allow player customization when possible**

### Remember

The best glyph system is the one that:
- Makes sense to YOUR players
- Fits YOUR game's scope
- Works with YOUR technical constraints
- Feels consistent and intuitive

Don't copy either DCSS or Brogue blindly - learn from both and create what works for your game.

---

## Appendix: Glyph Availability Reference

### Safe ASCII Glyphs (Always Available)

**Letters:** `A-Z` (26), `a-z` (26) = 52 total
**Numbers:** `0-9` (10)
**Symbols:** `! @ # $ % ^ & * ( ) - _ = + [ ] { } \ | ; : ' " , . < > / ? ~` (32)

**Total ASCII pool:** ~94 printable characters

### Common Unicode Enhancements

**Box Drawing:** `─ │ ┌ ┐ └ ┘ ├ ┤ ┬ ┴ ┼` (borders, frames)
**Blocks:** `░ ▒ ▓ █ ▄ ▀` (shading, terrain)
**Geometric:** `● ○ ◐ ◑ ◆ ◇ ◈ ■ □ ▲ △` (special markers)
**Arrows:** `← → ↑ ↓ ↔ ↕` (directions, missiles)
**Special:** `⚔ ⚡ ⚙ ⚛ ⚕` (thematic symbols)
**Greek:** `α β γ δ ε Ω Σ` (magical/special entities)

**Caution:** Test Unicode rendering in target terminals/fonts before committing.

---

*This comparative analysis is designed to be both human-readable and machine-parseable for AI agents assisting with roguelike development.*
