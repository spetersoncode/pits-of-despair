# Magic School Color System

Brainstorming document for organizing scroll/spell colors by magical school. The goal is to create intuitive color families where players can instantly recognize what "type" of magic they're looking at.

---

## Design Philosophy

### Color as Language
Colors should communicate meaning before the player reads text. A glance at a scroll's color should hint at its function:
- "That's a purple scroll - mind magic"
- "Orange glow - something's about to explode"
- "Sickly green - necromancy, stay away"

### School Spectrums
Each school gets a color range, not just a single color. This allows for:
- **Intensity progression**: Minor → Major effects (darker → brighter)
- **Sub-school differentiation**: Fire vs Lightning within Destruction
- **Future expansion**: Room to add scrolls without color collision

### Thematic Resonance
Colors should feel "right" for their school:
- Fire magic shouldn't be blue (unless it's special "cold fire")
- Healing shouldn't be angry red
- Death magic shouldn't be cheerful yellow

But also: **subvert expectations occasionally** for memorable items. A scroll of "Soothing Flames" in calming blue would be distinctive.

---

## The Schools

### 1. TRANSLOCATION (Spatial Magic)
*Moving through space, bending distance*

**Color Range**: Cyan → Teal → Greenish-blue (cool, dimensional, "between spaces")

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Short blink | ScrollBlink | #55DDEE | Light cyan, quick flash |
| Phase step | ScrollPhase | #44CCCC | Medium cyan, short phase |
| Teleport | ScrollTeleport | #228877 | Deep teal-green, heavier magic |
| Dimension door | ScrollDimension | #227766 | Darker teal, crossing boundaries |
| Recall | ScrollRecall | #33AA88 | Greenish-teal, anchored magic |
| Plane shift | ScrollPlaneShift | #226655 | Deepest green-teal, between worlds |

**Progression Logic**:
- Brighter cyan = shorter range, quicker, more controlled
- Deeper teal/greenish = longer range, more significant spatial distortion
- The spectrum represents "depth" of dimensional travel

**Why this palette**: Cyan/teal feels like portal energy, dimensional rifts, the void between spaces. Cool and otherworldly without being elemental (ice is different blues).

---

### 2. MIND-AFFECTING (Enchantment/Psychic)
*Controlling thoughts, emotions, perceptions*

**Color Range**: Purple family (historically associated with royalty, mystery, the mind)

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Confusion | ScrollConfusion | #9966DD | Blue-violet, scrambled thoughts |
| Charm | ScrollCharm | #BB66AA | Pink-violet, warmth, friendship |
| Fear | ScrollFear | #8855AA | Darker purple, dread |
| Sleep | ScrollSleep | #9988CC | Soft lavender, peaceful |
| Dominate | ScrollDominate | #CC44AA | Aggressive pink-purple, control |
| Madness | ScrollMadness | #AA33DD | Bright, unstable purple |
| Calm | ScrollCalm | #AA99CC | Muted, gentle violet |

**Sub-schools**:
- **Debuffs** (confusion, fear, madness): Blue-purples, more "cold" feeling
- **Control** (charm, dominate): Pink-purples, more "warm" and insidious
- **Peaceful** (sleep, calm): Soft lavenders, muted and gentle

**The Idea**: Blue-violet = attacking the mind. Pink-violet = seducing the mind.

---

### 3. DESTRUCTION (Evocation/Damage)
*Raw elemental damage, explosions, death rays*

**Color Range**: Warm spectrum + element-specific

This is the most complex school because elemental damage types each have strong color associations.

#### Fire Sub-school
| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Burning hands | ScrollFireMinor | #DD6633 | Orange, contained |
| Fireball | ScrollFire | #EE5522 | Classic fireball orange-red |
| Inferno | ScrollFireMajor | #FF4411 | Intense, approaching white-hot |
| Hellfire | ScrollHellfire | #CC2211 | Darker, more sinister red |

#### Lightning Sub-school
| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Shock | ScrollLightningMinor | #DDDD44 | Pale yellow spark |
| Lightning bolt | ScrollLightning | #EEEE22 | Bright electric yellow |
| Chain lightning | ScrollLightningMajor | #FFFF44 | Brilliant, almost white |
| Storm | ScrollStorm | #CCDD66 | Yellow-green, ozone feeling |

#### Ice Sub-school
| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Frost | ScrollIceMinor | #66AACC | Soft blue |
| Ice bolt | ScrollIce | #55CCEE | Bright cyan-blue |
| Blizzard | ScrollIceMajor | #88DDFF | Pale, almost white-blue |

#### Acid Sub-school
| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Acid splash | ScrollAcidMinor | #88CC44 | Yellow-green |
| Acid arrow | ScrollAcid | #77EE33 | Bright toxic green |
| Dissolve | ScrollAcidMajor | #99FF55 | Brilliant, caustic |

#### Force/Pure Magic
| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Magic missile | ScrollForce | #DDDDFF | Pale blue-white, pure energy |
| Disintegrate | ScrollDisintegrate | #FFDDDD | Pale red-white, unmaking |

**The Idea**: Each element owns a color region. Intensity increases brightness toward white.

---

### 4. CONJURATION (Summoning)
*Calling creatures, creating matter*

**Color Range**: Teals and unusual greens (otherworldly, not-quite-natural)

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Summon vermin | ScrollSummonMinor | #44AA88 | Muted teal |
| Summon beast | ScrollSummon | #33CC99 | Medium teal-green |
| Summon demon | ScrollSummonDemon | #33AAAA | Darker, more ominous teal |
| Summon elemental | ScrollSummonElemental | #55DDAA | Bright, powerful |
| Create food | ScrollCreate | #66BB88 | Softer, more natural green |

**The Idea**: Teal represents "between worlds" - not quite blue (elemental), not quite green (natural). Things being pulled from elsewhere.

**Alternative Direction**: Could use portal-like colors - make summoning share the magenta range with translocation since both involve crossing dimensional boundaries.

---

### 5. NECROMANCY (Death Magic)
*Undeath, life drain, corpse manipulation*

**Color Range**: Sickly greens, bone whites, grave grays, dried-blood reds

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Chill touch | ScrollNecroChill | #88AA99 | Cold gray-green |
| Drain life | ScrollDrainLife | #AA5566 | Dried blood, faded red |
| Animate dead | ScrollAnimate | #99AA88 | Corpse green-gray |
| Raise skeleton | ScrollRaiseSkeleton | #CCBB99 | Bone color |
| Raise zombie | ScrollRaiseZombie | #889977 | Rotting green-gray |
| Death bolt | ScrollDeath | #775566 | Dark purple-gray |
| Wither | ScrollWither | #998866 | Decayed brown |

**The Idea**: Necromancy colors should feel *wrong* - sickly, faded, desaturated. Life drained out.

**Contrast with Healing**: Healing greens are vibrant (#66DD66). Necromancy greens are muted, gray-tinged.

---

### 6. PROTECTION (Abjuration/Warding)
*Shields, barriers, dispelling*

**Color Range**: Golds, silvers, clean whites (purity, defense, light)

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Shield | ScrollShield | #CCAA66 | Warm gold |
| Armor | ScrollArmor | #AABBCC | Cool silver-blue |
| Resist elements | ScrollResist | #DDCC77 | Pale gold |
| Sanctuary | ScrollSanctuary | #EEDDAA | Soft warm white |
| Dispel magic | ScrollDispel | #DDDDEE | Clean silver-white |
| Banish | ScrollBanish | #FFEECC | Bright, holy white-gold |
| Ward | ScrollWard | #BBAA77 | Bronze, sturdy |

**The Idea**: Protection colors feel *safe* - warm golds like sunlight, clean silvers like polished armor.

---

### 7. DIVINATION (Knowledge/Seeing)
*Detection, identification, foresight*

**Color Range**: Silvers, pale blues, crystal whites (clarity, sight, truth)

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Detect magic | ScrollDetect | #AACCDD | Pale blue, seeing |
| Identify | ScrollIdentify | #CCDDEE | Silver-white, revealing |
| True sight | ScrollTrueSight | #DDEEFF | Almost white, pure clarity |
| Foresight | ScrollForesight | #99BBCC | Deeper blue, peering ahead |
| Scry | ScrollScry | #88AACC | Medium blue, distant seeing |
| Reveal | ScrollReveal | #BBDDDD | Pale cyan, unveiling |

**The Idea**: Divination is about *clarity* - colors should feel transparent, crystalline, like looking through clear water or glass.

---

### 8. TRANSMUTATION (Alteration)
*Changing form, enhancing, polymorphing*

**Color Range**: Earthy browns and greens + mercurial silvers (transformation, alchemy)

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Enlarge | ScrollEnlarge | #CC9966 | Earthy tan |
| Shrink | ScrollShrink | #AA8855 | Darker tan |
| Haste | ScrollHaste | #DDAA44 | Quicksilver gold |
| Slow | ScrollSlow | #887755 | Heavy brown |
| Polymorph | ScrollPolymorph | #99AA77 | Natural green-brown |
| Stone to flesh | ScrollStoneFlesh | #BBAA88 | Flesh-stone hybrid |
| Strengthen | ScrollStrengthen | #BB8844 | Bronze, solid |

**The Idea**: Transmutation deals with the physical world - earthy, material colors. Quicksilver/mercury references for speed effects.

---

### 9. HEALING (Restoration)
*Hit point recovery, cure conditions, purification*

**Color Range**: Vibrant greens, warm whites (life, vitality, renewal)

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Minor heal | ScrollHealMinor | #66BB66 | Soft green |
| Heal | ScrollHeal | #66DD66 | Vibrant life green |
| Major heal | ScrollHealMajor | #88EE88 | Brilliant green |
| Cure poison | ScrollCurePoison | #77DD99 | Teal-green, antidote |
| Cure disease | ScrollCureDisease | #88DDAA | Cyan-green, purifying |
| Restoration | ScrollRestoration | #AAEEBB | Pale green-white |
| Regeneration | ScrollRegeneration | #55CC55 | Deep vital green |

**The Idea**: Healing greens should feel ALIVE - saturated, vibrant. Contrast with necromancy's sickly desaturated greens.

---

### 10. NATURE (Druidic/Primal)
*Plants, animals, weather, earth*

**Color Range**: Forest greens, earth browns, sky blues

| Effect Type | Color Name | Hex | Notes |
|-------------|------------|-----|-------|
| Entangle | ScrollEntangle | #558844 | Forest green |
| Barkskin | ScrollBarkskin | #776644 | Bark brown |
| Call lightning | ScrollCallLightning | #AACC66 | Yellow-green, storm |
| Earthquake | ScrollEarthquake | #886644 | Earth brown |
| Animal ally | ScrollAnimalAlly | #77AA66 | Natural green |
| Insect swarm | ScrollInsectSwarm | #666633 | Dark olive |
| Thornwall | ScrollThornwall | #447733 | Dark forest green |

**The Idea**: Nature magic uses *natural* colors - nothing neon or artificial. Muted compared to pure elemental magic.

---

## Color Collision Avoidance

### Danger Zones
Some color ranges are already claimed or have strong meaning:

| Color Range | Already Used For | Avoid Using For |
|-------------|-----------------|-----------------|
| Bright Yellow #FFFF00 | Player | Everything else |
| Red #DD6655 range | Damage taken, danger | Healing, safety |
| Gray #888888 | Disabled UI, iron | Active magic |
| Pure White #FFFFFF | Default text | Colored scrolls |

### School Boundaries

```
COOL ←————————————————————————————————→ WARM

Cyan        Teal       Green      Yellow     Orange      Red        Purple/Pink
|           |          |          |          |           |          |
Blink       Teleport   Heal       Lightning  Fire        Necro      Mind
Divination  Summon     Nature     Transmute  Destruction (drain)    Charm
Ice                    Necro(rot)                                   Confusion
```

### The Purple Problem
Purple is doing a lot of work:
- Mind-affecting (enchantment)
- Necromancy (death)
- Will potions
- Possibly shadow magic

**Solution**:
- Mind = pink-purples and blue-purples (vibrant)
- Necro = gray-purples (desaturated, dead feeling)
- Will potions = already distinct at #9966CC
- Shadow = dark blue-purples, almost black

---

## Wild Ideas

### Scroll Rarity Colors?
What if scroll color also indicated rarity/power?
- Common scrolls: Normal saturation
- Rare scrolls: Slight glow effect (lighter variant)
- Legendary scrolls: Inverted or unusual color for the school

*Probably overcomplicating things. School color is enough.*

### Cursed Scroll Variants
Cursed scrolls could be the school color but *wrong* somehow:
- Desaturated (drained of power)
- Shifted toward sickly green
- Darker, more ominous variant

Example: Cursed Scroll of Healing = #66DD66 shifts to #88AA77 (gray-green, rotting life)

### School Mixing
Multi-school scrolls could blend colors:
- Teleport + Fire = Flaming teleport, orange-magenta #EE6688
- Heal + Necro = Life transfer, green-gray #77BB88
- Mind + Divination = Read thoughts, blue-purple #8888DD

### The "No School" Color
Some scrolls might be "pure magic" with no school:
- Identify (could be divination OR no school)
- Scroll of wonder (random effect)
- Antimagic

Color: Pure white (#EEEEFF) or silver (#BBBBCC) - magic without flavor

---

## Implementation Priority

### Phase 1: Current Scrolls (DONE)
- Translocation: Blink, Teleport
- Mind: Confusion, Charm

### Phase 2: Core Expansion
- Healing: Heal (minor/regular/major)
- Destruction: Fireball, Lightning bolt
- Divination: Identify

### Phase 3: School Completion
- Fill out remaining schools
- Add palette regions for each

### Phase 4: Polish
- Review color collisions
- Ensure contrast on black background
- Document in color.md

---

## Questions to Resolve

1. **Should potions share color schools with scrolls?**
   - Healing potion vs Scroll of healing - same green?
   - Might cause confusion (is it a potion or scroll?)
   - Or might reinforce "green = healing" association

2. **How to handle multi-effect scrolls?**
   - Scroll of fire shield (destruction + protection)
   - Use primary school color? Blend? New color?

3. **Should wands follow the same system?**
   - Wand of fireballs = same orange as scroll?
   - Probably yes for consistency

4. **NPC spell effects?**
   - When enemies cast spells, use same colors?
   - Helps player recognize "that's a fireball incoming"

5. **Elemental vs School priority?**
   - Ice magic: Is it Destruction (damage) or Nature (cold weather)?
   - Fire magic: Destruction or could be Nature (forest fire)?
   - Probably Destruction owns direct damage, Nature owns environmental

---

## References

- Existing `Palette.cs` scroll region structure
- Potion organization pattern (PotionStrength, PotionAgility, etc.)
- Traditional D&D school colors (if any)
- Other roguelikes: DCSS spell schools, Brogue colors
