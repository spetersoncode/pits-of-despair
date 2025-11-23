# Skills System - Focused Brainstorming

**Status:** Active brainstorming - foundation for implementation
**Date:** 2025-11-22 (Updated)
**Context:** Drilling down from level-progression-system.md to flesh out the skills system specifically. This is the primary blocker for meaningful progression and utilizing the WIL stat.

---

## Core Parameters (Fixed Constraints)

### Progression Numbers
- **Level Cap:** 21 (start at level 1, cap at 21)
- **Stat Points:** 20 total (one per level, levels 2-21)
- **Stat Cap:** 12 per stat (maximum investment in any single stat)
- **Skill Points:** 15 total (weighted toward early game)
- **Stats:** STR, AGI, END, WIL (each can range 0-12)

### Skill Point Distribution
Skills are **front-loaded** to support early build definition:

| Phase | Levels | Stat Points | Skill Points | Feel |
|-------|--------|-------------|--------------|------|
| Foundation | 2-10 | 9 | 9 (every level) | Rapid growth, discovering build |
| Refinement | 11-20 | 10 | 5 (evens only: 12,14,16,18,20) | Stats matter more, deliberate picks |
| Capstone | 21 | 1 | 1 | Final form, signature ability |
| **Total** | | **20** | **15** | |

### Stat Distribution Possibilities
With 20 points and 12 cap:
- **Heavy Specialist:** 12-8-0-0 (one maxed, one strong)
- **Dual Focus:** 10-10-0-0 (two high stats)
- **Tri-Stat:** 8-6-6-0 or 7-7-6-0 (three invested)
- **Balanced:** 5-5-5-5 (true generalist)
- **Varied Hybrid:** 8-6-4-2 (declining investment)

### Level-Up Flow
1. Player accumulates XP from kills
2. XP threshold crossed → "Level Up Available!" indicator appears
3. Player presses **L** when ready (not forced mid-combat)
4. Modal appears - **choose stat to increase** (+1 to STR/AGI/END/WIL)
5. Stat applies immediately (can unlock new skill prereqs)
6. If this level grants a skill: modal transitions to **skill selection**
7. Skill acquired, modal closes
8. If multiple levels pending, must repeat for each (no banking)

**Critical Anti-Hoarding:** Each level processed discretely. Stat applies before skill selection. Cannot queue multiple stat choices to game prerequisites.

---

## Willpower Energy System

### The Core Concept
**Willpower** is a secondary resource pool derived from the WIL stat. It powers active skills - both martial techniques and magical abilities. Even 0-WIL characters have a small pool for basic maneuvers.

### Pool Size Formula

**Linear scaling (recommended for simplicity):**
```
Willpower = 10 + (WIL × 5)
```

| WIL | Willpower Pool |
|-----|----------------|
| 0 | 10 |
| 4 | 30 |
| 8 | 50 |
| 12 | 70 |

This ensures:
- Everyone can use cheap skills (10 base)
- Moderate WIL investment unlocks meaningful caster gameplay
- WIL 12 specialists have large pools for expensive abilities

### Willpower Regeneration

**Hybrid system (multiple levers to tune):**
- **Passive:** Regenerate 1 Willpower every 5 turns
- **On Kill:** +3 Willpower per enemy killed
- **Floor Descent:** Full Willpower restored
- **Items:** Potions, food, scrolls can restore Willpower

This creates:
- Aggressive play rewarded (kills restore WP)
- Natural attrition in long fights
- Strategic resource management
- Floor transitions as recovery points

### Skill Cost Ranges

| Tier | Cost | Description |
|------|------|-------------|
| Low | 0-5 WP | Basic techniques, minor buffs, utility. Usable by anyone. |
| Medium | 6-12 WP | Solid combat moves, meaningful spells. Moderate usage. |
| High | 13-20 WP | Powerful abilities. Strategic usage, rewards WIL investment. |
| Ultimate | 21-35 WP | Build-defining, fight-ending. Once or twice per floor. |

**Passive Skills:** 0 Willpower (always active)
**Auras:** Typically 0 cost but may have activation cost or WP/turn drain

---

## Skill Categories

### Active Skills (Cost Willpower)
Skills you consciously activate. Usually consume your turn.

**Sub-categories:**
- **Attacks:** Enhanced strikes, special maneuvers, damaging spells
- **Buffs:** Temporary self-enhancement (Haste, Giant Strength, Shield)
- **Debuffs:** Weaken enemies (Slow, Blind, Weaken)
- **Crowd Control:** Disable enemies (Stun, Fear, Paralyze, Confuse)
- **Utility:** Non-combat benefits (Blink, Detect, Heal, Light)
- **Summons:** Create allies or obstacles

### Passive Skills (Free, Always Active)
Permanent bonuses once acquired. No activation, no cost.

**Sub-categories:**
- **Stat Bonuses:** +X to derived values, +X% damage
- **Proc Effects:** "On hit, 10% chance to..." / "When damaged, gain..."
- **Resistances:** Reduce damage types or status conditions
- **Conditional Bonuses:** "While below 50% HP..." / "First attack each floor..."
- **Resource Bonuses:** +X Willpower, +X HP, improved regen

### Reactive Skills (Triggered)
Activate automatically when conditions are met. May or may not cost Willpower.

**Sub-categories:**
- **Counters:** Trigger on being attacked (Riposte, Reflect, Block)
- **Interrupt:** Trigger on enemy action (Counterspell, Preemptive Strike)
- **Survival:** Trigger on low HP (Last Stand, Second Wind, Die Hard)
- **Kill Effects:** Trigger on killing enemy (Soul Harvest, Momentum, Rampage)

### Auras (Persistent Area Effects) - NEW
Ongoing effects centered on the player affecting nearby tiles/entities.

**Sub-categories:**
- **Ally Buffs:** "Allies within 3 tiles gain +2 attack"
- **Enemy Debuffs:** "Enemies within 2 tiles have -1 defense"
- **Environmental:** "Tiles within 2 tiles are illuminated"
- **Protective:** "Allies within 2 tiles take -1 damage"

**Aura Mechanics:**
- Typically always active once learned (passive auras)
- Some may toggle on/off
- Some may drain Willpower while active (X WP/turn)
- Radius scales with stat investment or skill tier
- **Ally System Integration:** Auras become much more valuable with summons, companions, or recruited allies

---

## Ally System Considerations

The game has an ally system. Skills that affect allies gain significant value:

**Ally Types:**
- Summoned creatures (Raise Undead, Summon Elemental)
- Dominated enemies (Dominate spell)
- Companions (if implemented)
- Recruited NPCs (if implemented)

**Skills that benefit from allies:**
- **Buffs:** "Target ally gains +3 attack" → multiply effectiveness
- **Auras:** "Allies in radius gain..." → scales with ally count
- **Healing:** "Heal ally for 2d6" → keep summons alive
- **Positioning:** "Swap places with ally" → tactical options

**Design Note:** Summoner/commander builds should feel distinctly different. Heavy WIL + END (Warlock/Necromancer) should excel at this playstyle.

---

## Skill Prerequisite Design

### Tier Structure (Revised for 12-cap)

**Single-Stat Tiers:**

| Tier | Prereq | Skills/Stat | Description |
|------|--------|-------------|-------------|
| Entry | 1-2 | 3-4 | First investment, defining stat flavor |
| Developing | 3-4 | 3-4 | Committed to stat, solid abilities |
| Specialized | 5-7 | 2-3 | Serious investment, powerful options |
| Advanced | 8-10 | 2 | Deep commitment, build-defining |
| Capstone | 11-12 | 1 | Maximum investment, signature ability |

**Two-Stat Tiers:**

| Tier | Prereq | Skills/Combo | Description |
|------|--------|--------------|-------------|
| Low | 2/2 - 3/3 | 2 | Early hybrid options |
| Medium | 4/4 - 5/5 | 2 | Mid-game hybrid payoffs |
| High | 6/6+ | 1 | Deep dual investment |

**Multi-Stat:**
- Three-Stat (3/3/3+): 1 per combo = 4 total
- Four-Stat (3/3/3/3+): 2-3 total

### Skill Pool Targets (~90 skills)

| Category | Count | Notes |
|----------|-------|-------|
| **Universal** | 4-5 | Zero prereq, everyone can use |
| **STR Single** | 13-14 | Entry through Capstone |
| **AGI Single** | 13-14 | Entry through Capstone |
| **END Single** | 11-12 | Entry through Capstone (fewer offense options) |
| **WIL Single** | 14-15 | Entry through Capstone (spells are diverse) |
| **Two-Stat** | 30 | 5 per combo × 6 combos |
| **Three-Stat** | 4 | 1 per combo |
| **Four-Stat** | 2-3 | True generalists |
| **TOTAL** | **~90** | 6× coverage for 15 choices |

### Coverage Verification

**Pure STR player (12-8-0-0 final):**
- STR skills: ~14 options
- STR/END hybrids: ~5 options
- Universal: ~4 options
- Total: ~23 options for 15 picks ✓

**Balanced player (5-5-5-5 final):**
- Single stat at 5: ~8 per stat × 4 = ~32 options
- Low hybrids (all 6 combos): ~12 options
- Four-stat skills: ~2 options
- Universal: ~4 options
- Total: ~50 options for 15 picks ✓

**Early game (level 5, ~4 stats):**
- Universal: 4-5 options
- Tier 1 in chosen stats: ~6-8 options
- Maybe one 2/2 hybrid: 1-2 options
- Total: ~12-15 options for first 4 picks ✓

---

## Stat-Themed Skill Design

### Strength (STR) - Power & Impact
**Core Fantasy:** Physical dominance, overwhelming force, intimidation

**Mechanical Themes:**
- Bonus melee damage
- Armor penetration / reduction
- Knockback and positioning
- Cleaving/AoE melee attacks
- Intimidation effects
- Breaking objects/barriers

**Signature Capstone (STR 12):** *Worldbreaker* - Earth-shattering power

---

### Agility (AGI) - Speed & Precision
**Core Fantasy:** Untouchable, precise, mobile

**Mechanical Themes:**
- Evasion bonuses
- Extra movement
- Ranged accuracy and multi-shot
- Critical hit chance/damage
- First strike / initiative
- Stealth and ambush

**Signature Capstone (AGI 12):** *Phantom* - Untouchable speed demon

---

### Endurance (END) - Resilience & Attrition
**Core Fantasy:** Unkillable, outlast everything, steady

**Mechanical Themes:**
- HP bonuses
- Damage reduction
- Healing enhancement
- Status resistance
- Regeneration
- Death prevention
- Ally protection (with ally system)

**Signature Capstone (END 12):** *Eternal* - Simply cannot die

---

### Willpower (WIL) - Magic & Control
**Core Fantasy:** Bend reality, control the battlefield, arcane might

**Mechanical Themes:**
- Spell damage
- Larger Willpower pool
- Spell efficiency (reduced costs)
- Mental status resistance
- Crowd control
- Summoning and domination
- Reality manipulation

**Signature Capstone (WIL 12):** *Archmage* - Master of all magic

---

## Hybrid Stat Themes

### STR/AGI - The Weapon Master
**Fantasy:** Perfect martial technique, deadly with any weapon
**Themes:** Critical strikes, weapon combos, finisher moves, dueling

### STR/END - The Juggernaut
**Fantasy:** Unstoppable tank who hits back hard
**Themes:** HP-based damage, revenge damage, never retreat, warlord

### STR/WIL - The Battle Mage
**Fantasy:** Spell-enhanced melee, warrior-wizard
**Themes:** Weapon enchantments, spell strikes, arcane warrior

### AGI/END - The Survivor
**Fantasy:** Impossible to pin down, impossible to kill
**Themes:** Evasive recovery, safe kiting, second chances

### AGI/WIL - The Trickster
**Fantasy:** Mage who never gets hit, illusions
**Themes:** Blink, invisibility, illusory doubles, arcane archery

### END/WIL - The Warlock
**Fantasy:** Dark magic sustained by unnatural vitality, summoner
**Themes:** Life drain, curse mastery, summoning, blood magic, ally buffs

---

## Draft Skill Pool (Revised for ~90 skills)

### Universal Skills (No Prerequisites) - 5 skills

| Skill | Type | Cost | Description |
|-------|------|------|-------------|
| **Second Wind** | Active | 5 | Heal 20% max HP. Once per floor. |
| **Desperate Strike** | Active | 3 | +3 damage on next attack, -2 defense until next turn. |
| **Brace** | Active | 3 | +3 defense until next turn, cannot move. |
| **Focus** | Active | 2 | +2 accuracy on next attack. |
| **Disengage** | Active | 4 | Move 2 tiles without triggering reactions. |

---

### Strength Skills - 14 skills

#### STR Entry (1-2)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Power Attack** | STR 1 | Active | 3 | Next melee attack deals +3 damage. |
| **Shove** | STR 1 | Active | 2 | Push adjacent enemy 1 tile. |
| **Mighty Thews** | STR 2 | Passive | - | +1 melee damage permanently. |
| **War Cry** | STR 2 | Active | 4 | Enemies in 3 tiles: -1 attack for 3 turns. |

#### STR Developing (3-4)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Cleave** | STR 3 | Active | 6 | Attack hits two adjacent enemies. |
| **Sunder Armor** | STR 3 | Active | 5 | Attack reduces enemy armor by 2 for 5 turns. |
| **Execute** | STR 4 | Active | 8 | Double damage vs enemies below 25% HP. |
| **Relentless** | STR 4 | Passive | - | Kill grants +1 move this turn. |

#### STR Specialized (5-7)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Whirlwind** | STR 5 | Active | 10 | Attack ALL adjacent enemies. |
| **Rage** | STR 6 | Active | 12 | +50% melee damage for 5 turns. -2 defense. |
| **Rampage** | STR 7 | Reactive | 5 | On kill, immediately make another attack. |

#### STR Advanced (8-10)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Earthquake** | STR 8 | Active | 15 | All enemies within 2 tiles take STR×2 damage. |
| **Unstoppable** | STR 10 | Active | 18 | Immune to all CC for 5 turns. Double damage. |

#### STR Capstone (12)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Worldbreaker** | STR 12 | Active | 25 | Instantly kill target. Shockwave damages all nearby. |

---

### Agility Skills - 14 skills

#### AGI Entry (1-2)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Quick Step** | AGI 1 | Active | 2 | Move 1 extra tile this turn. |
| **Precise Shot** | AGI 1 | Active | 3 | +3 to ranged attack roll. |
| **Fleet of Foot** | AGI 2 | Passive | - | +1 movement range permanently. |
| **Sidestep** | AGI 2 | Reactive | 2 | When missed by melee, move 1 tile free. |

#### AGI Developing (3-4)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Double Shot** | AGI 3 | Active | 6 | Fire two ranged attacks. |
| **Evasion** | AGI 3 | Passive | - | +2 defense permanently. |
| **Riposte** | AGI 4 | Reactive | 3 | When enemy misses melee, counter-attack. |
| **Vanish** | AGI 4 | Active | 8 | Invisible for 3 turns or until you attack. |

#### AGI Specialized (5-7)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Shadow Step** | AGI 5 | Active | 6 | Teleport up to 4 tiles. |
| **Triple Shot** | AGI 6 | Active | 10 | Fire three ranged attacks. |
| **Assassinate** | AGI 7 | Active | 12 | Triple damage from stealth/surprise. |

#### AGI Advanced (8-10)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Perfect Dodge** | AGI 8 | Active | 15 | Automatically dodge all attacks for 2 turns. |
| **Death from Above** | AGI 10 | Active | 12 | Leap to enemy 5 tiles away, +8 damage. |

#### AGI Capstone (12)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Phantom** | AGI 12 | Passive | - | 30% chance attacks phase through you. +2 moves/turn. |

---

### Endurance Skills - 12 skills

#### END Entry (1-2)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Tough** | END 1 | Passive | - | +8 max HP. |
| **Grit** | END 1 | Passive | - | +1 to all saving throws. |
| **Thick Skinned** | END 2 | Passive | - | Reduce all damage by 1 (min 1). |
| **Recover** | END 2 | Active | 5 | Heal END×2 HP. |

#### END Developing (3-4)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Regeneration** | END 3 | Passive | - | Heal 1 HP every 3 turns. |
| **Die Hard** | END 3 | Reactive | 0 | First lethal hit per floor leaves you at 1 HP. |
| **Iron Skin** | END 4 | Active | 8 | +5 armor for 5 turns. |
| **Fortified** | END 4 | Passive | - | +2 armor permanently. |

#### END Specialized (5-7)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Indomitable** | END 5 | Passive | - | Immune to fear and stun. |
| **Regenerate** | END 6 | Passive | - | Heal 1 HP every turn. |
| **Undying** | END 7 | Reactive | 0 | Twice per floor, survive lethal at 1 HP. |

#### END Advanced (8-10)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Stone Body** | END 8 | Passive | - | Reduce all damage by 3 (min 1). Cannot be crit. |
| **Protective Aura** | END 10 | Aura | - | Allies within 2 tiles take -2 damage. |

#### END Capstone (12)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Eternal** | END 12 | Passive | - | Regenerate 3% max HP per turn. Cannot be reduced below 1 HP. |

---

### Willpower Skills - 15 skills

#### WIL Entry (1-2)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Magic Missile** | WIL 1 | Active | 3 | 2d4 damage, never misses, 6 range. |
| **Light** | WIL 1 | Active | 2 | Illuminate area for 30 turns. |
| **Arcane Focus** | WIL 2 | Passive | - | +5 Willpower pool. |
| **Minor Heal** | WIL 2 | Active | 4 | Heal 2d6 HP to self or adjacent ally. |

#### WIL Developing (3-4)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Fireball** | WIL 3 | Active | 8 | 3x3 area, 3d6 fire damage. |
| **Fear** | WIL 3 | Active | 6 | Target flees for 3 turns. |
| **Blink** | WIL 4 | Active | 5 | Teleport up to 5 tiles to visible space. |
| **Slow** | WIL 4 | Active | 6 | Target acts every other turn for 5 turns. |

#### WIL Specialized (5-7)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Chain Lightning** | WIL 5 | Active | 12 | 3d6 to target, jumps to 2 nearby enemies. |
| **Paralyze** | WIL 6 | Active | 10 | Target cannot act for 3 turns. |
| **Summon Elemental** | WIL 7 | Active | 15 | Create allied elemental for 10 turns. |

#### WIL Advanced (8-10)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Disintegrate** | WIL 8 | Active | 18 | Single target, 8d6 damage. |
| **Dominate** | WIL 9 | Active | 20 | Target enemy fights for you for 5 turns. |
| **Time Stop** | WIL 10 | Active | 25 | Take 2 turns while all enemies frozen. |

#### WIL Capstone (12)

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Archmage** | WIL 12 | Passive | - | All spell damage +50%. All spell costs -25%. |

---

### Two-Stat Hybrid Skills - 30 skills (5 per combo)

#### STR/AGI - Weapon Master

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Feint** | STR 2, AGI 2 | Active | 4 | Next attack cannot be dodged. |
| **Blade Dance** | STR 3, AGI 3 | Active | 8 | Attack all adjacent, then move 2 tiles. |
| **Perfect Strike** | STR 4, AGI 4 | Active | 10 | Guaranteed critical hit. |
| **Weapon Mastery** | STR 5, AGI 5 | Passive | - | +2 damage, +2 accuracy with all weapons. |
| **Thousand Cuts** | STR 6, AGI 6 | Active | 15 | Attack every enemy within 2 tiles. |

#### STR/END - Juggernaut

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Reckless Attack** | STR 2, END 2 | Active | 5 | +5 damage, take +3 damage until next turn. |
| **Revenge** | STR 3, END 3 | Reactive | 3 | When damaged, +3 damage on next attack. |
| **Shield Wall** | STR 4, END 4 | Active | 8 | +6 armor. Adjacent allies gain +3 armor. 3 turns. |
| **Warmaster** | STR 5, END 5 | Passive | - | +2 melee damage, +2 armor, +10 HP. |
| **Unbreakable** | STR 6, END 6 | Passive | - | Cannot be reduced below 1 HP by any single hit. |

#### STR/WIL - Battle Mage

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Flame Blade** | STR 2, WIL 2 | Active | 5 | Weapon deals +1d6 fire for 5 turns. |
| **Shocking Grasp** | STR 3, WIL 3 | Active | 6 | Melee touch: 2d6 lightning + stun 1 turn. |
| **Giant Strength** | STR 4, WIL 4 | Active | 10 | +4 STR for 10 turns. |
| **Spell Strike** | STR 5, WIL 5 | Active | 12 | Cast spell AND melee attack same turn. |
| **Arcane Warrior** | STR 6, WIL 6 | Passive | - | Melee kills restore 5 Willpower. |

#### AGI/END - Survivor

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Defensive Roll** | AGI 2, END 2 | Active | 4 | Move 2 tiles, heal 5 HP. |
| **Evasive Recovery** | AGI 3, END 3 | Passive | - | Dodging heals 2 HP. |
| **Mobile Shot** | AGI 4, END 4 | Active | 5 | Move + ranged attack, no penalty. |
| **Ranger's Resilience** | AGI 5, END 5 | Passive | - | +2 defense, +15 max HP. |
| **Slippery** | AGI 6, END 6 | Passive | - | Cannot be grappled, slowed, or rooted. |

#### AGI/WIL - Trickster

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Mirror Image** | AGI 2, WIL 2 | Active | 6 | Create 2 decoys that absorb attacks. |
| **Phase Arrow** | AGI 3, WIL 3 | Active | 5 | Ranged attack ignores armor and cover. |
| **Greater Invisibility** | AGI 4, WIL 4 | Active | 12 | Invisible for 5 turns, even while attacking. |
| **Arcane Archer** | AGI 5, WIL 5 | Passive | - | Ranged attacks deal +WIL bonus damage. |
| **Dimensional Step** | AGI 6, WIL 6 | Active | 8 | Teleport anywhere visible + attack. |

#### END/WIL - Warlock

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Life Drain** | END 2, WIL 2 | Active | 6 | 2d6 damage, heal same amount. |
| **Blood Magic** | END 3, WIL 3 | Passive | - | Can spend HP instead of WP (2 HP = 1 WP). |
| **Raise Undead** | END 4, WIL 4 | Active | 12 | Corpse becomes allied skeleton. |
| **Dark Pact** | END 5, WIL 5 | Passive | - | +30% spell damage. Healing received -30%. |
| **Command Aura** | END 6, WIL 6 | Aura | - | Allies within 3 tiles: +2 attack, +2 damage. |

---

### Three-Stat Hybrid Skills - 4 skills

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Combat Expertise** | STR 3, AGI 3, END 3 | Passive | - | +2 attack, +2 defense, +10 HP. |
| **Elemental Blade Dance** | STR 3, AGI 3, WIL 3 | Active | 15 | Attack all adjacent with fire, move 3. |
| **Dark Knight** | STR 3, END 3, WIL 3 | Passive | - | Melee kills restore 5 HP and 5 WP. |
| **Twilight Sentinel** | AGI 3, END 3, WIL 3 | Passive | - | +2 defense, +2 spell resist, regen 1 HP/turn. |

---

### Four-Stat Generalist Skills - 3 skills

| Skill | Prereq | Type | Cost | Description |
|-------|--------|------|------|-------------|
| **Jack of All Trades** | All 3 | Passive | - | +1 to all stats. Can use any item regardless of prereqs. |
| **Adaptability** | All 4 | Passive | - | After being hit by damage type, gain resistance to it. |
| **Transcendence** | All 5 | Active | 25 | Full heal, full WP, cure all status effects. |

---

## Skill Count Summary

| Category | Count |
|----------|-------|
| Universal | 5 |
| STR Single | 14 |
| AGI Single | 14 |
| END Single | 12 |
| WIL Single | 15 |
| Two-Stat (6 combos × 5) | 30 |
| Three-Stat | 4 |
| Four-Stat | 3 |
| **TOTAL** | **97** |

**Coverage Ratio:** 97 skills for 15 choices = 6.5× multiplier. Excellent variety and replayability.

---

## Level-Up Flow Design

### When Skills Are Granted

| Level | Stat | Skill? |
|-------|------|--------|
| 2 | Yes | Yes |
| 3 | Yes | Yes |
| 4 | Yes | Yes |
| 5 | Yes | Yes |
| 6 | Yes | Yes |
| 7 | Yes | Yes |
| 8 | Yes | Yes |
| 9 | Yes | Yes |
| 10 | Yes | Yes |
| 11 | Yes | No |
| 12 | Yes | Yes |
| 13 | Yes | No |
| 14 | Yes | Yes |
| 15 | Yes | No |
| 16 | Yes | Yes |
| 17 | Yes | No |
| 18 | Yes | Yes |
| 19 | Yes | No |
| 20 | Yes | Yes |
| 21 | Yes | Yes (Capstone) |

### Modal Flow (Skill Level)

```
┌────────────────────────────────────────────┐
│           LEVEL UP! (Level 6)              │
├────────────────────────────────────────────┤
│ Choose a stat to increase:                 │
│                                            │
│ [S] STR: 4 → 5  (+1 melee dmg, +1 acc)    │
│     Unlocks: Whirlwind, Weapon Mastery     │
│                                            │
│ [A] AGI: 2 → 3  (+1 defense, +1 ranged)   │
│     Unlocks: Double Shot, Evasion          │
│                                            │
│ [E] END: 1 → 2  (HP: 29 → 32)             │
│     Unlocks: Thick Skinned, Recover        │
│                                            │
│ [W] WIL: 0 → 1  (WP: 10 → 15)             │
│     Unlocks: Magic Missile, Light          │
└────────────────────────────────────────────┘
```

After stat choice:

```
┌────────────────────────────────────────────┐
│        Choose a Skill (5/15)               │
├────────────────────────────────────────────┤
│ [1] Whirlwind (STR 5)        Cost: 10 WP  │
│     Attack ALL adjacent enemies.           │
│                                            │
│ [2] Weapon Mastery (STR 5/AGI 5) Passive  │
│     +2 damage, +2 accuracy all weapons.    │
│     (Requires AGI 5 - you have AGI 2)      │
│                                            │
│ [3] Execute (STR 4)          Cost: 8 WP   │
│     Double damage vs enemies below 25% HP. │
│                                            │
│ [4] Cleave (STR 3)           Cost: 6 WP   │
│     Attack hits two adjacent enemies.      │
│                                            │
│ [5] Power Attack (STR 1)     Cost: 3 WP   │
│     Next melee attack deals +3 damage.     │
└────────────────────────────────────────────┘
```

### Modal Flow (Stat-Only Level)

```
┌────────────────────────────────────────────┐
│           LEVEL UP! (Level 13)             │
├────────────────────────────────────────────┤
│ Choose a stat to increase:                 │
│                                            │
│ [S] STR: 8 → 9  (+1 melee dmg, +1 acc)    │
│ [A] AGI: 3 → 4  (+1 defense, +1 ranged)   │
│ [E] END: 2 → 3  (HP: 38 → 46)             │
│ [W] WIL: 0 → 1  (WP: 10 → 15)             │
│                                            │
│ (No skill choice this level)               │
└────────────────────────────────────────────┘
```

---

## Skill Menu Design

### Recommended: Full Menu (Z key)

With 15 skills (mix of active, passive, reactive, aura), a hotbar isn't necessary:

- **Z** opens skill menu
- Skills grouped by type:
  - **Active** (require selection and targeting)
  - **Passive** (always on, shown for reference)
  - **Reactive** (trigger conditions shown)
  - **Auras** (toggle on/off if applicable)
- Select active skill, then target (if needed)
- Passives/Reactives don't need interaction

### Alternative: Limited Hotbar

If players want quick access:
- **1-8:** Bindable active skill slots
- **Z:** Full menu for rebinding and reference
- Passives/Reactives always active, no slots needed

---

## Open Questions

### Numbers to Playtest
1. **Willpower regen rate** - Is 1/5 turns + 3/kill right?
2. **Skill costs** - Are the tiers balanced?
3. **Damage scaling** - Do high-tier skills feel impactful enough?
4. **Aura radii** - How big should they be?

### Design Decisions
1. **Skill visibility** - Show all skills with prereqs, or only unlocked?
2. **Equipment skills** - Should gear grant bonus skills?
3. **Enemy skills** - Can monsters use the same skill system?

### Implementation
1. **Data format** - YAML skill definitions
2. **Targeting types** - Self, adjacent, ranged single, area, cone
3. **Effect composition** - Build from existing effect primitives

---

## Wild Ideas (Experimental)

### Skill Upgrades
Skills improve with use or investment:
- Fireball used 20 times → Empowered Fireball (+1d6 damage)
- Creates mastery feeling

### Equipment-Granted Skills
Items bypass prereqs:
- Staff of Fire: Grants Fireball while equipped
- Boots of Blinking: Grants Blink
- Expands options for non-specialist builds

### Forbidden Skills
Negative prerequisites reward extreme builds:
- **Glass Cannon** (STR 10+, END 0): +50% damage
- **Pure Arcane** (WIL 10+, STR 0, AGI 0): Spell costs halved

### Combo Skills
Synergies between specific skills:
- Rage + Whirlwind = Blood Storm (extra damage, lifesteal)
- Invisibility + Assassinate = auto-triggered triple damage

---

## Implementation Requirements

This section outlines everything needed to build the skills system from the ground up.

### 1. Willpower Resource System

**New Secondary Stat:**
- Add `Willpower` as a derived stat (like HP from END)
- Formula: `MaxWillpower = 10 + (WIL × 5)`
- Track `CurrentWillpower` on player entity
- Cap at 12 for WIL stat (max 70 Willpower pool)

**Regeneration Mechanics:**
- Per-turn passive regen (1 WP every 5 turns) - needs turn counter
- On-kill bonus (+3 WP) - hook into kill event
- Floor descent reset (full WP) - hook into floor transition
- Item restoration - new consumable effect type

**UI Requirements:**
- Willpower bar/display in HUD (alongside HP)
- Show current/max Willpower
- Visual feedback when spending/gaining WP

---

### 2. Skill Data Schema

**YAML Skill Definition Structure:**
```yaml
skills:
  - id: "power_attack"
    name: "Power Attack"
    description: "Next melee attack deals +3 damage."
    category: "active"          # active, passive, reactive, aura

    # Prerequisites
    prerequisites:
      str: 1
      agi: 0
      end: 0
      wil: 0

    # Costs
    willpower_cost: 3

    # Targeting
    targeting: "self"           # self, adjacent, tile, enemy, ally, area
    range: 0
    area_size: 0                # for AoE skills

    # Effects (composable)
    effects:
      - type: "apply_status"
        status: "power_attack_buff"
        duration: 1

    # For reactive skills
    trigger: null               # on_hit, on_miss, on_kill, on_damage, on_low_hp
    trigger_cost: 0             # WP cost when triggered

    # For auras
    aura_radius: 0
    aura_target: null           # allies, enemies, all

    # Metadata
    tier: 1                     # 1-5 for UI sorting
    tags: ["melee", "buff", "damage"]
```

**Skill Categories to Support:**
- `active` - Player-initiated, costs turn and WP
- `passive` - Always on, no cost, no activation
- `reactive` - Triggered by conditions, may cost WP
- `aura` - Persistent area effect, may drain WP/turn

---

### 3. Core Skill Framework

**SkillDefinition Resource:**
- Godot Resource class holding skill data
- Loaded from YAML at startup
- Indexed by skill ID for quick lookup

**SkillComponent (Player Component):**
- `List<string> LearnedSkills` - skills the player has acquired
- `int SkillPointsSpent` - track progression
- Methods:
  - `LearnSkill(skillId)` - add to learned list
  - `HasSkill(skillId)` - check if learned
  - `GetAvailableSkills()` - filter by current stats
  - `CanLearnSkill(skillId)` - prereq check
  - `GetLearnedSkillsByCategory()` - for UI grouping

**WillpowerComponent (Player Component):**
- `int MaxWillpower` - derived from WIL stat
- `int CurrentWillpower` - current pool
- `int RegenCounter` - turns since last passive regen
- Methods:
  - `SpendWillpower(amount)` - returns bool success
  - `RestoreWillpower(amount)` - clamp to max
  - `FullRestore()` - for floor transitions
  - `OnTurnProcessed()` - handle passive regen
  - `OnKill()` - handle kill bonus
- Signals:
  - `WillpowerChanged(current, max)`

**PrerequisiteChecker (Utility):**
- `MeetsPrerequisites(skillDef, statsComponent)` - bool check
- `GetMissingPrerequisites(skillDef, statsComponent)` - for UI display
- Handle multi-stat requirements
- Handle negative prerequisites (if implementing forbidden skills)

---

### 4. Skill Execution System

**SkillAction (extends Action):**
- Takes skill ID and target info
- `CanExecute()`:
  - Check skill is learned
  - Check Willpower available
  - Check targeting valid
  - Check cooldown (if implementing)
- `Execute()`:
  - Spend Willpower
  - Apply skill effects
  - Return ActionResult

**Targeting System:**
- `self` - no targeting needed
- `adjacent` - select from 8 adjacent tiles
- `tile` - select any tile in range
- `enemy` - select enemy in range
- `ally` - select ally in range
- `area` - select center point, affects radius
- `cone` - select direction, affects cone
- `line` - select direction, affects line

**Effect Composition:**
Skills compose effects from existing primitives:
- `DamageEffect` - deal damage to target
- `HealEffect` - restore HP
- `ApplyStatusEffect` - add status condition
- `TeleportEffect` - move to tile
- `SummonEffect` - create ally entity
- `KnockbackEffect` - push target
- `ModifyStatEffect` - temporary stat change

New effects needed:
- `MultiTargetEffect` - hit multiple targets
- `AreaEffect` - affect all in radius
- `ConditionalEffect` - effect based on condition (e.g., target HP < 25%)

---

### 5. Passive Skill System

**PassiveSkillProcessor:**
- On skill learn: register passive effects
- On skill forget (if ever): unregister effects
- Passive types:
  - **Stat Modifiers:** Add to multi-source modifier system
  - **Flat Bonuses:** +X HP, +X damage, +X defense
  - **Percentage Bonuses:** +X% damage, +X% HP
  - **Conditional:** Check condition each relevant event

**Integration Points:**
- Combat system: Check for damage modifiers, crit modifiers
- Movement system: Check for speed modifiers
- Status system: Check for immunities/resistances

---

### 6. Reactive Skill System

**ReactiveSkillProcessor:**
- Subscribe to relevant signals based on trigger type
- On trigger: check if player has reactive skill with that trigger
- Check Willpower cost (if any)
- Execute skill effect automatically or prompt player

**Trigger Types:**
- `on_attacked` - when targeted by attack
- `on_hit` - when attack hits player
- `on_miss` - when attack misses player (enemy missed)
- `on_dodge` - when player dodges attack
- `on_damage` - when player takes damage
- `on_kill` - when player kills enemy
- `on_low_hp` - when HP drops below threshold
- `on_ally_hit` - when ally takes damage (for protection skills)

**Design Decision:** Auto-trigger vs player choice?
- Some reactives should auto-fire (Die Hard, Rampage)
- Some might want player confirmation (spend WP to counter?)
- Consider: `auto_trigger: true/false` in skill definition

---

### 7. Aura System

**AuraProcessor:**
- Track active auras on player
- Each turn/movement: recalculate affected entities
- Apply/remove aura effects as entities enter/leave radius

**Aura Implementation:**
- Store aura as status effect on affected entities
- Source tracking: `aura_<skill_id>_<player_id>`
- On player move: update all aura targets
- On entity move: check if entering/leaving any auras

**Aura Types:**
- **Passive Auras:** Always active, no cost (e.g., Protective Aura)
- **Toggle Auras:** Player can turn on/off (future?)
- **Draining Auras:** Cost X WP per turn while active (future?)

---

### 8. Level-Up System Modifications

**Changes to Existing Level-Up:**
- Track which levels grant skills (2-10, 12, 14, 16, 18, 20, 21)
- After stat selection on skill levels: transition to skill selection
- Enforce discrete processing (no banking)

**Stat Cap Enforcement:**
- Check if stat already at 12 before offering as option
- Grey out or hide maxed stats in selection

**New Level-Up Modal Flow:**
1. Show stat options (with preview of what each unlocks)
2. Player selects stat → apply immediately
3. If skill level: show available skills
4. Player selects skill → add to learned skills
5. Close modal

**Skill Selection UI:**
- Filter skills by: meets prerequisites, not already learned
- Sort by: tier, category, stat requirement
- Show: name, description, cost, prereqs, type
- Highlight newly unlocked skills (from stat just chosen)

---

### 9. Skill Menu UI

**Skill Menu (Z key):**
- Full-screen or large modal
- Sections:
  - **Active Skills:** Selectable, shows WP cost
  - **Passive Skills:** Display only, shows "Always Active"
  - **Reactive Skills:** Display only, shows trigger condition
  - **Auras:** Display only (or toggle if implementing)

**Skill Activation Flow:**
1. Player presses Z → menu opens
2. Player selects active skill
3. If targeting needed: menu closes, targeting mode activates
4. Player selects target
5. Skill executes (or fails with message)

**Alternative: Hotbar (Optional)**
- Number keys 1-8 bound to favorite active skills
- Player assigns in skill menu
- Quick access without opening menu

---

### 10. HUD Updates

**New HUD Elements:**
- Willpower bar (blue? purple?) below or beside HP bar
- Current/Max display
- Possibly: active buff icons from skills

**Skill Feedback:**
- Floating text for skill activation
- Visual effects for spells (particles, screen effects)
- Sound effects for skill use

---

### 11. Stat Cap Implementation

**Changes to StatsComponent:**
- Add constant `STAT_CAP = 12`
- `CanIncreaseStat(stat)` - check if below cap
- Modify level-up to respect cap
- Update HP formula to cap END at 12: max HP from END ~= 98

**Formula Verification with Cap:**
- END 12: `20 + (144 + 108) / 2 = 20 + 126 = 146 HP` (need to verify formula)
- WIL 12: `10 + 60 = 70 Willpower`

---

### 12. Content Creation (The Big One)

**~97 Skills to Implement:**

| Category | Count | Complexity |
|----------|-------|------------|
| Universal | 5 | Simple |
| STR Single | 14 | Moderate |
| AGI Single | 14 | Moderate |
| END Single | 12 | Simple-Moderate |
| WIL Single | 15 | Complex (spells) |
| Two-Stat | 30 | Moderate-Complex |
| Three-Stat | 4 | Complex |
| Four-Stat | 3 | Complex |

**Implementation Phases:**
1. **Phase 1 (MVP):** 20-25 skills covering basic gameplay
   - 5 Universal
   - 3-4 per single stat at tiers 1-2
   - 1-2 per two-stat combo at low tier

2. **Phase 2:** Mid-tier expansion (40-50 total)
   - Fill out tiers 3-4 for single stats
   - Add medium two-stat hybrids

3. **Phase 3:** Complete pool (90+ total)
   - Capstone skills (12-point abilities)
   - High-tier hybrids
   - Three-stat and four-stat skills
   - Polish and balance pass

**Skill Implementation Checklist (per skill):**
- [ ] YAML definition
- [ ] Effect composition (or custom effect if needed)
- [ ] Targeting setup
- [ ] Visual feedback (animation/particles)
- [ ] Sound effect
- [ ] Balance testing

---

### 13. Integration Points Summary

**Systems That Need Modification:**
- `StatsComponent` - stat cap, WIL affecting Willpower
- `HealthComponent` - END cap consideration
- `TurnManager` - Willpower regen on turn
- `CombatSystem` - skill-based attacks, damage modifiers
- `InputHandler` - Z for skill menu, targeting mode
- `LevelUpModal` - stat→skill flow, skill selection
- `Player` - add SkillComponent, WillpowerComponent
- `EntityFactory` - wire up new components

**New Systems to Create:**
- `SkillSystem` - central skill management
- `SkillComponent` - player skill storage
- `WillpowerComponent` - WP resource management
- `SkillAction` - skill execution
- `SkillMenu` - UI for skill selection
- `TargetingSystem` - handle skill targeting modes
- `PassiveProcessor` - manage passive effects
- `ReactiveProcessor` - manage reactive triggers
- `AuraProcessor` - manage area effects

**Data Files to Create:**
- `skills.yaml` - all skill definitions
- Possibly split: `skills_str.yaml`, `skills_agi.yaml`, etc.

---

### 14. Testing Strategy

**Unit Tests:**
- Prerequisite checking
- Willpower spending/regeneration
- Skill learning/availability

**Integration Tests:**
- Skill execution in combat
- Passive effects applying correctly
- Reactive triggers firing
- Aura radius calculations

**Playtest Scenarios:**
- Pure STR build (levels 1-21)
- Pure WIL build (caster)
- Balanced 5-5-5-5 build
- Hybrid builds (STR/WIL battle mage, etc.)

**Balance Checkpoints:**
- Early game (levels 2-5): Is there enough variety?
- Mid game (levels 6-12): Do hybrids feel rewarding?
- Late game (levels 13-21): Are capstones worth the investment?

---

### 15. Implementation Order (Recommended)

**Foundation (Must Have First):**
1. Stat cap (12) enforcement
2. WillpowerComponent + WIL derivation
3. Skill data schema (YAML)
4. SkillComponent (learn/store skills)
5. Prerequisite checking

**Core Loop:**
6. Level-up modal modifications (stat→skill flow)
7. Skill selection UI in level-up
8. Basic SkillAction execution
9. Simple targeting (self, adjacent)

**Active Skills:**
10. First 10-15 active skills
11. Skill menu (Z key)
12. Advanced targeting (area, range)
13. Effect composition system

**Passive/Reactive:**
14. Passive skill processor
15. Reactive skill processor
16. Implement passive/reactive skills

**Polish:**
17. Aura system
18. Visual/audio feedback
19. Remaining skills
20. Balance pass

---

## Summary

**Total New Systems:** ~9 major systems/components
**Total Skills to Create:** ~97 (phased rollout)
**Estimated Scope:** Large feature, recommend phased approach

**MVP Definition:**
- Willpower system working
- 20-25 skills implemented (mix of types)
- Level-up grants skills on correct levels
- Skill menu functional
- Basic targeting working

**Full Feature:**
- All 97 skills
- All skill categories (active, passive, reactive, aura)
- Full targeting system
- Polished UI and feedback
- Balanced through playtesting

---

*"In the pits, power is earned with each descent. Choose wisely - every skill shapes your legend."*
