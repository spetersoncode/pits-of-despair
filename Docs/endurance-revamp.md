# Endurance Stat Revamp

**Status:** Design Phase - Not Yet Implemented

## Problem Statement

Current Endurance implementation (`END × Level` HP bonus) creates scaling issues:

- **Weak early game:** 5 END at Level 1 = +5 HP (minimal impact)
- **Overwhelming late game:** 5 END at Level 10 = +50 HP (potentially trivializing damage)
- **Passive and boring:** No interesting decisions, just "bigger HP bar"
- **No mechanical identity:** Doesn't change how you play, just how long you survive

## Design Goals

1. **Smooth scaling:** Valuable early, grows steadily, doesn't dominate late
2. **Simple, digestible numbers:** Players can easily evaluate stat choices
3. **Distinct identity:** END feels different from STR (offense) and AGI (evasion)
4. **Build diversity:** Makes END compelling but not mandatory
5. **Multiple utility points:** HP + other benefits create interesting trade-offs

## New Endurance Formula

**Base HP:** 20 (at END 0)

**Total HP:** `20 + (END² + 9×END) / 2`

**HP Gain Per Point:** `(4 + new END value)` additional MaxHP

### Scaling Table

| END | Total HP | HP from END | Marginal Gain |
|-----|----------|-------------|---------------|
| 0   | 20       | 0           | —             |
| 1   | 25       | 5           | +5            |
| 2   | 31       | 11          | +6            |
| 3   | 38       | 18          | +7            |
| 4   | 46       | 26          | +8            |
| 5   | 55       | 35          | +9            |
| 6   | 65       | 45          | +10           |
| 8   | 88       | 68          | +12 (from 7)  |
| 10  | 115      | 95          | +14 (from 9)  |

### Rationale

- **Quadratic progression:** Each point gives more than the last, encouraging investment
- **Front-loaded value:** First point gives 5 HP (meaningful at Level 1)
- **Diminishing extremes:** Doesn't explode at high values like multiplicative scaling
- **Between flat and multiplicative:** Balances "always useful" with "scales with character"

## Key Mechanical Changes

### Level-Up HP Gain

**Current:** +4 base HP per level (automatic) + END scaling
**New:** +0 base HP per level - **all HP comes from END stat choices**

**Impact:** Makes stat choices on level-up matter significantly. Skipping END = no HP gains (high-risk glass cannon builds possible).

### Endurance Benefits

1. **Hit Points:** Quadratic scaling per formula above (20 base + END bonus)
2. **Physical Saving Throws:** Primary stat for opposed 2d6 + END rolls
   - Resist poison, disease, exhaustion, paralysis, petrification, stunning
   - Complements Will (mental saves) for complete save coverage

## Negative Endurance

**Design Decision:** Negative END floors at base HP (20 for player), never reduces below base.

### Rationale

- **Player:** Temporary debuffs reduce MaxHP to minimum of 20 (never lethal from debuff alone)
- **Creatures:** HP set explicitly in YAML (designer control, negative END just affects saves)
- **Simpler mental model:** "END adds HP if positive, floors at base if negative"
- **No instadeath:** Debuffs are punishing but not instantly lethal

### Use Cases

- **Weak creatures (Floor 1):** Rats, small enemies (END -1 to -2)
- **Fragile but evasive:** Skeletons, ghosts (END -2 to -3, compensated by high AGI or immunities)
- **Temporary debuffs:** Disease, exhaustion reducing player END (rare, punishing)

**Physical saves:** Negative END provides penalty (e.g., -2 END = 2d6-2 on saves)

## Healing Mechanics Innovation

### Endurance Potions as Healing

**Core Concept:** Temporary +END buffs provide effective healing through MaxHP increases.

**How it works:**
1. Drink Potion of Endurance (+2 END for 10 turns)
2. MaxHP increases by formula amount (e.g., END 3→5 = +17 MaxHP)
3. CurrentHP increases by same amount (healing effect)
4. After duration, MaxHP returns to normal
5. **CurrentHP stays elevated** (capped to new max if needed)

**Net effect:** Permanent healing from temporary buff.

### Example

```
Player: 20/35 HP (END 3)
Drink +2 END potion: 37/52 HP (healed 17)
Combat damage: 37 → 25 HP
Buff expires: 25/35 HP (kept the healing)
```

### Design Benefits

- **Thematic:** "Fortify yourself" → toughness closes wounds
- **Simple:** Just +END buff, healing is emergent property
- **Scales naturally:** Higher END characters get more healing
- **Tactical depth:** Duration creates HP buffer during fights
- **Elegant:** No traditional healing potions needed

### Consumable Examples

- **Minor:** +1 END (2-5 turns) → 5-9 HP healing
- **Standard:** +2 END (10 turns) → 11-17 HP healing
- **Major:** +3 END (15 turns) → 18-25 HP healing

*Healing amount varies with current END (higher END = more benefit).*

**IMPORTANT:** Potion effects cannot stack with same type (see Equipment Restrictions below).

## Equipment Restrictions

### Critical Rule: No +END on Equippable Items

**Problem:** Equip/unequip cycles provide infinite healing exploit.

**Solution:** +END bonuses **only** allowed on:
- Consumable potions (temporary duration-based buffs)
- Permanent level-up stat increases
- Possibly: Very rare consumed-on-use permanent items ("Constitution Tome")

**Not allowed on:**
- Rings, amulets, accessories
- Armor, weapons
- Any freely swappable equipment

### No Potion Stacking Rule

**Design Decision:** Cannot consume multiple potions of the same stat simultaneously.

**Problem Avoided:** Players spam-chugging END potions for invincibility (boring, one-dimensional).

**Solution:** Force tactical variety through consumable diversity.

**Example - Correct Usage:**
- ✓ Drink END potion + Quaff Barkskin + Read Confusion scroll = layered defense
- ✗ Drink 3 END potions in a row = blocked, must wait for duration

**Result:** Players must engage with full consumable toolkit (different buff types, crowd control, utility) rather than solving problems with resource spam.

### Alternative Equipment Bonuses

Equipment can still provide:
- **+MaxHP directly** (doesn't heal on equip, just increases cap)
- **+Armor** (damage reduction)
- **+STR/AGI/WIL** (other stats)
- **Resistances, immunities**
- **Special abilities**

This preserves equipment diversity without healing scumming.

## Stat Identity Summary

### Strength - "Aggressive Eliminator"
- High melee damage and accuracy
- Proactive defense: kill threats before they kill you
- Needs foundation END (3-5) to survive trades
- Build focus: Offense as best defense

### Agility - "Evasive Skirmisher"
- High defense, avoid getting hit entirely
- Ranged weapon accuracy
- Needs foundation END (3-5) for RNG protection
- Build focus: Don't get hit, but survive when you do

### Endurance - "Durable Survivor"
- Large HP pool through quadratic scaling (20 base + investment)
- Resists physical status effects
- Better healing/regen through MaxHP scaling
- Build focus: Foundation for all builds (3-5 typical), higher investment for tank playstyle

### Will - "Mental Fortitude" (future)
- Magic/abilities
- Mental saving throws (confusion, fear, charm)
- Complements END for complete save coverage

## Risk Profiles

**High STR, Low END (STR 8, END 2, AGI 2):** Aggressive glass cannon - 31 HP, high damage, kill fast or die
**High AGI, Low END (AGI 8, END 2, STR 2):** Evasive skirmisher - 31 HP, high evasion, dodge or die on bad luck
**High END, Balanced (END 8, STR 3, AGI 3):** Durable generalist - 88 HP, survive mistakes, outlast enemies, slower kills
**Balanced Stats (STR 5, END 5, AGI 4):** Jack-of-all-trades - 55 HP, consistent but no specialization
**Extreme Glass Cannon (STR 10, END 0, AGI 4):** Challenge run - 20 HP, maximum offense, one mistake = death

## Future Considerations

### Regeneration System

**Status:** Designed but not being implemented at this time.

**Design Concept (for future):** Percentage-based regeneration that scales with MaxHP (e.g., 5% per turn). This creates emergent value for END investment - higher MaxHP = more absolute HP regenerated per turn, while maintaining consistent pacing across all builds (~18-20 turns to full heal).

**Current State:** 1 HP per wait action (temporary placeholder).

### Saving Throw System

**Status:** Design approved, not implemented.

**Physical saves (END):** Poison, disease, exhaustion, paralysis, petrification, stunning, knockback
**Mental saves (WIL):** Confusion, fear, charm, sleep, mind control

**Mechanic:** Opposed 2d6 rolls (attacker + DC vs defender + stat)

### Open Questions

1. **Minimum END at character creation?** Can player start at END 0 or require minimum investment?
   - 20 HP is survivable but extremely risky - is this allowed from level 1?

2. **END potion rarity?** If they're primary healing, how common should they be?
   - Needs playtesting to balance survival vs scarcity
   - Must account for inability to stack (can't spam-heal)

## Design Philosophy

This revamp aligns with core design principles:

- **Simple numbers:** Formula is complex, but player sees clear marginal gains
- **Meaningful choices:** Skipping END is viable but risky
- **Emergent mechanics:** Healing through END buffs wasn't designed explicitly, emerged from system
- **Build diversity:** Three distinct stat identities create different playstyles
- **Roguelike tension:** Limited healing resources, permanent consequences for mistakes
- **Tactical variety:** No stacking forces engagement with full consumable toolkit

## Balance Summary

**Three Distinct Playstyles:**
- **STR:** Proactive defense (kill fast) - needs foundation END to trade hits
- **AGI:** Reactive defense (don't get hit) - needs foundation END for bad luck
- **END:** Attrition defense (outlast) - foundation for all, focus for tanks

**Cascading Benefits (Controlled):**
- END → MaxHP (quadratic scaling)
- MaxHP → Potion healing (can't stack, limited by duration/availability)
- MaxHP → Future regen system (not implemented yet)

**Foundation Stat Design:**
- 20 base HP (END 0) = viable but extremely risky
- 3-5 END (38-55 HP) = typical foundation for focused builds
- 6-10 END = tank specialist, slower gameplay

**Each build has a dump stat.** No dominant strategy. Risk profiles differ meaningfully.

## Implementation Status

**Current Phase:** Design finalized - ready for implementation
**Resolved Decisions:**
- Base HP: 20 (END 0)
- Quadratic HP scaling formula: `20 + (END² + 9×END) / 2`
- Negative END floors at base HP (never lethal)
- No potion stacking (force consumable variety)
- No +END on equippable items (prevent healing exploits)

**Not Implemented (Future):**
- Regeneration system (designed as percentage-based, waiting for later implementation)

**Remaining for Playtesting:**
- END potion rarity/availability tuning
- Minimum END at character creation policy

---

*See also: [combat.md](combat.md) for combat stats integration, [status.md](status.md) for status effect mechanics*
