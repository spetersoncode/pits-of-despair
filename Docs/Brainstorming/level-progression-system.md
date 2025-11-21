# Level Progression System - Brainstorming & Exploration

**Status:** Exploratory brainstorming - foundation for future implementation
**Date:** 2025-11-21
**Context:** This document captures a comprehensive discussion about stat-based progression, archetype design, and skill unlocking systems. This is NOT a formal design doc - it's a creative exploration of possibilities.

---

## Core Vision

**The Dream:** A progression system where new players can "level up stats they want and pick skills that look cool" while experienced players can theorycraft exact stat distributions to unlock specific skill combinations. Stats naturally guide you toward appropriate skills. Your build emerges organically from your choices.

**The Elegance:** Not a "magic system" or "skill tree" - it's a **stat-gated skill system** where your attribute investments unlock abilities that synergize with those attributes. Kitchen sink design that works because natural filtering prevents overwhelm.

---

## The Four Core Stats

### Strength (STR)
**Identity:** Proactive offense - kill threats before they kill you

**Mechanical Benefits:**
- Melee weapon damage scaling
- Melee accuracy/hit chance
- Heavy armor proficiency
- Carrying capacity (?)
- Intimidation checks (?)

**Playstyle:** Aggressive, frontloaded, decisive. End fights in 3-5 turns. Works best when you can close distance and deliver devastating blows.

**Build Archetypes:**
- Pure STR: Berserker (glass cannon damage)
- STR/END: Juggernaut (tank and spank)
- STR/AGI: Duelist (balanced offense/defense)
- STR/WIL: Battle Mage (spell-enhanced melee)

---

### Agility (AGI)
**Identity:** Reactive defense - don't get hit in the first place

**Mechanical Benefits:**
- Defense/evasion rating
- Ranged weapon accuracy
- Movement speed/range
- Initiative/turn order (?)
- Stealth checks (?)

**Playstyle:** Evasive, positional, RNG-dependent. Maintain distance, strike from range, dance around threats. When it works, you're untouchable. When it fails, you're in trouble.

**Build Archetypes:**
- Pure AGI: Shadow (perfect evasion specialist)
- AGI/END: Ranger (sustainable ranged)
- AGI/STR: Duelist (weapon master)
- AGI/WIL: Arcane Archer (magic + arrows)

---

### Endurance (END)
**Identity:** Passive survivability - outlast and absorb damage

**Mechanical Benefits:**
- MaxHP (20 base + quadratic scaling: `(END² + 9×END) / 2`)
- Physical saving throws (poison, paralysis, disease, etc.)
- Healing effectiveness (cascading: higher MaxHP = more HP from potions)
- Future: Regeneration rate (percentage-based)

**Playstyle:** Durable, forgiving, attrition-based. Mistakes are survivable. Fights are longer but safer. The "foundation stat" most builds want 3-5 points in.

**Build Archetypes:**
- Pure END: Fortress (unkillable challenge run)
- END/STR: Juggernaut (frontline tank)
- END/AGI: Ranger (safe skirmisher)
- END/WIL: Necromancer/Tank Mage (durable caster)

**Special Note:** END is unique - it's the "foundation" stat rather than a "build around" stat. You CAN go pure END, but most builds use it as a safety net (3-5 points) rather than primary focus.

---

### Willpower (WIL)
**Identity:** Active control - deny enemies the ability to threaten you

**Theoretical Mechanical Benefits:**
- Spell/ability effectiveness (damage, duration, potency)
- Mental saving throws (confusion, fear, charm, sleep)
- Mana pool or spell charges (?)
- Magic resistance (?)
- Perception checks (?)

**Playstyle:** Control-focused, resource-management, high skill ceiling. Can completely shut down encounters with proper spell usage. Can replace END foundation if you have defensive spells.

**Build Archetypes:**
- Pure WIL: Archmage/Controller (glass cannon mage)
- WIL/STR: Battle Mage (melee + magic hybrid)
- WIL/AGI: Arcane Archer/Trickster (ranged + utility magic)
- WIL/END: Necromancer/Venom Mage (tanky debuffer)

**Design Note:** WIL is the only stat that can potentially REPLACE the END foundation (via defensive spells) rather than complement it. This makes pure WIL builds the "ultimate challenge" archetype.

---

## Hypothetical Progression Structure

**Working Example (subject to change):**
- 21 levels total
- Alternates: Stat point → Skill point → Stat point → Skill point
- Final build: 10 stat points distributed + 10 skills chosen
- Choices made immediately at level up (no banking points)

**Level 1:** Start (base stats? or first stat point?)
**Level 2:** +1 Stat
**Level 3:** +1 Skill (choose from available based on stats)
**Level 4:** +1 Stat
**Level 5:** +1 Skill
...
**Level 21:** Final skill choice

**Why Immediate Choices?**
- Prevents banking points to "game" the system
- Forces commitment to build direction
- Each level feels impactful
- Can't save 5 stat points then dump them all at once

**Hotbar Constraints:**
- Number keys (1-9, 0?) = max 10 active skills
- Perfect fit for 10 skill choices
- Forces meaningful selection even within available pool

---

## The Complete Archetype Gallery

### TIER 1: PURE MARTIALS (No WIL Investment)

#### 1. THE BERSERKER
*"Everything dies or I die trying."*

**Stats:** STR 10, END 5, AGI 0, WIL 0
**HP:** 55
**Role:** Pure Offense Specialist

**Core Fantasy:** You are the biggest, meanest thing in the dungeon. Everything you touch dies. You solve problems by hitting them harder.

**Strengths:**
- Highest melee damage in the game
- Perfect accuracy with heavy weapons
- Can cleave through weak enemies in one hit
- Works with plate armor, two-handed weapons, brutal tactics
- Intimidates enemies just by existing

**Weaknesses:**
- No CC resistance - confusion/fear are death sentences
- No magic utility - doors, traps, puzzles require items
- One-dimensional gameplay - every problem is a nail
- Vulnerable to kiting, ranged enemies, magic users
- Long dungeons wear you down (no sustain without END potions)

**Skill Unlocks:** Cleave, Power Attack, Sunder Armor, Execute, Earthquake, Unstoppable, Intimidate, Crushing Blow

**Tactical Flow:**
- See enemy → charge → obliterate
- Every fight is a DPS race
- Use STR consumables (rage potions, spiked armor)
- Pray you kill them before they kill you

**Ideal For:** Players who want simple, aggressive, high-damage gameplay. "I cast fist."

---

#### 2. THE DUELIST
*"They can't kill what they can't touch."*

**Stats:** STR 7, AGI 7, END 0, WIL 0
**HP:** 20
**Role:** High-Skill Glass Cannon

**Core Fantasy:** Master swordsman. Dance between enemy attacks, strike when openings appear. Every move is precise. One mistake ends the run.

**Strengths:**
- High damage AND high evasion (double threat)
- Works with finesse weapons (rapiers, katanas, dual-wield)
- Counter-attack mechanics (riposte)
- Fastest movement speed
- Versatile (melee and ranged options)

**Weaknesses:**
- 20 HP = 1-2 hits to death
- No CC resistance (still vulnerable to magic)
- RNG-dependent (bad dodge streak = instant death)
- No sustain (no healing, every hit is precious)
- High stress gameplay (no room for error)

**Skill Unlocks:** Blade Dance, Riposte, Feint, Whirlwind, Dodge Roll, Quick Shot, Perfect Dodge, Parry

**Tactical Flow:**
- Position perfectly
- Strike when enemies miss
- Dodge, counterattack, reposition
- Never trade hits (you can't afford to)
- Kite, poke, dance

**Ideal For:** Skilled players who want high-risk, high-reward gameplay. Dark Souls veterans.

---

#### 3. THE JUGGERNAUT
*"I am the wall upon which your attacks break."*

**Stats:** STR 8, END 8, AGI 0, WIL 0
**HP:** 88
**Role:** Frontline Tank

**Core Fantasy:** Unstoppable warrior in full plate. Walk up, trade hits, win through superior durability. Simple, reliable, forgiving.

**Strengths:**
- Massive HP pool (88!) + high damage
- Can facetank most enemies
- Best armor utilization (plate, shields)
- Very forgiving of mistakes
- Resource-efficient (HP pool lasts long)
- Good physical saves (END 8)

**Weaknesses:**
- Slow movement (heavy armor penalty?)
- No CC resistance (mental saves still weak)
- No ranged options (must close distance)
- Predictable (enemies know what you'll do)
- Boring for some players (low tactical variety)

**Skill Unlocks:** Shield Wall, Reckless Attack, Devastating Blow, Berserker Rage, Last Stand, Iron Skin, Second Wind, Taunt

**Tactical Flow:**
- Walk up to enemy
- Trade hits (you have more HP)
- Heal with END potions when low
- Repeat
- Win by attrition

**Ideal For:** New players, low-stress runs, "easy mode" builds. Warriors who want straightforward gameplay.

---

#### 4. THE SHADOW
*"You cannot kill what you cannot see."*

**Stats:** AGI 10, END 5, STR 0, WIL 0
**HP:** 55
**Role:** Evasion Specialist

**Core Fantasy:** Phantom archer. Enemies swing at air while arrows rain from the darkness. Perfect positioning, untouchable when played correctly.

**Strengths:**
- Highest evasion in game (80%+ dodge rate?)
- Perfect ranged accuracy
- Fast movement, superior positioning
- Excellent kiting potential
- Can dictate engagement range

**Weaknesses:**
- Low damage output (no STR scaling)
- When hit, it hurts (half the damage of STR builds)
- Long fights = more dodge rolls = more chances to fail
- No CC resistance
- Struggles vs teleporting/charging enemies

**Skill Unlocks:** Perfect Dodge, Triple Shot, Stealth, Smoke Bomb, Evasive Recovery, Quick Shot, Riposte, Acrobatics

**Tactical Flow:**
- Maintain max distance at all times
- Shoot, reposition, shoot
- Kite in circles
- Use terrain (doorways, corridors)
- Never enter melee range

**Ideal For:** Ranged specialists, players who love positioning gameplay, kiting enthusiasts.

---

#### 5. THE RANGER
*"Steady hands, steady heart."*

**Stats:** AGI 8, END 6, STR 0, WIL 0
**HP:** 65
**Role:** Sustainable Skirmisher

**Core Fantasy:** Experienced adventurer. High evasion, decent HP pool. Doesn't take unnecessary risks. Reliable, consistent, safe.

**Strengths:**
- High evasion + meaningful HP buffer
- Good ranged damage
- Survives mistakes (unlike pure AGI)
- Resource-efficient
- Comfortable, low-stress gameplay

**Weaknesses:**
- Mediocre at everything (jack-of-all-trades)
- Lower evasion than Shadow
- Lower HP than Juggernaut
- Still no CC resistance
- Can't burst down major threats

**Skill Unlocks:** Mobile Shot, Hunter's Mark, Evasive Recovery, Defensive Roll, Quick Shot, Second Wind

**Tactical Flow:**
- Shoot from range
- Dodge when possible
- Tank hits when necessary (you have HP for it)
- Heal occasionally
- Steady, methodical gameplay

**Ideal For:** Players who want reliable ranged builds without the stress of glass cannon gameplay.

---

#### 6. THE FORTRESS
*"They will break before I do."*

**Stats:** END 14, STR 0, AGI 0, WIL 0
**HP:** 143
**Role:** Unkillable Wall (Challenge Run)

**Core Fantasy:** You literally cannot die. Unfortunately, you also can't kill anything. The ultimate pacifist run or comedy build.

**Strengths:**
- Absurd HP pool (143 HP!)
- Best physical saves in game (END 14)
- Ultimate resource efficiency (takes 50+ turns to die)
- END potions heal for massive amounts (+35+ HP per potion)
- Can tank literally anything

**Weaknesses:**
- No damage output (base weapon damage only)
- Fights take 30-50+ turns each
- No CC resistance (still vulnerable to confusion)
- Incredibly tedious gameplay
- Questionable viability

**Skill Unlocks:** Immortality, Fortress Stance, Martyrdom, Iron Skin, Last Stand, Second Wind, Regeneration

**Tactical Flow:**
- Hit enemy for 3 damage
- Tank 50 hits
- Heal occasionally
- Wait
- Eventually they die
- Question life choices

**Ideal For:** "Can you beat the game with 0 STR/AGI?" challenge runs. Masochists. Comedians.

---

### TIER 2: MAGE SPECIALISTS (WIL Primary, 8-10 points)

#### 7. THE ARCHMAGE (Nuker Variant)
*"Behold, the power of the cosmos."*

**Stats:** WIL 10, END 0, STR 0, AGI 4
**HP:** 20
**Role:** Glass Cannon Blaster

**Core Fantasy:** You wield ultimate magical power. Enemies are deleted from existence with a word. Unfortunately, you're made of tissue paper.

**Strengths:**
- Highest spell damage in game
- Can one-shot dangerous enemies
- Ends fights in 1-3 casts
- Some AGI provides dodge chance (20% evasion?)
- Perfect mental saves (WIL 10)

**Weaknesses:**
- 20 HP = death in 1-2 hits
- Resource-limited (can't spam top-tier spells)
- No defensive tools (pure offense)
- Vulnerable to fast enemies who close before you cast
- If spell charges run out, you're useless

**Skill Unlocks:** Fireball, Meteor, Lightning Bolt, Disintegrate, Chain Lightning, Magic Missile, Frost Nova, Time Stop

**Tactical Flow:**
- Identify biggest threat
- Delete it with maximum-power spell
- Next target
- Run away if enemies close distance
- Pray your spells last the floor

**Ideal For:** Players who want big flashy damage numbers. "I cast Gun."

---

#### 8. THE CONTROLLER (CC Variant)
*"You will dance to my tune."*

**Stats:** WIL 10, END 0, AGI 4, STR 0
**HP:** 20
**Role:** Crowd Control God

**Core Fantasy:** Enemies never get to act. Perfect CC chains. Chess grandmaster. One slip = death, but you don't slip.

**Strengths:**
- Can lock down entire encounters permanently
- Enemies die without taking a single turn
- Ultimate skill ceiling
- Perfect mental saves (immune to enemy CC)
- Incredibly satisfying when mastered

**Weaknesses:**
- 20 HP = instant death if CC chain breaks
- Requires perfect execution every fight
- Cooldown management is mentally taxing
- Low damage output (CC doesn't kill, just disables)
- One mistake ends the run

**Skill Unlocks:** Confusion, Paralysis, Fear, Slow, Sleep, Petrify, Dominate, Mass Confusion, Time Stop

**Tactical Flow:**
- Cast CC on enemy #1
- Cast CC on enemy #2
- Recast CC on #1 before it expires
- Cast CC on enemy #3
- Juggle all cooldowns perfectly
- Enemies never act
- Eventually they die somehow

**Ideal For:** Master tacticians. Players who love puzzle-like combat. Control freaks (literally).

---

#### 9. THE NECROMANCER
*"Death is not the end - it is merely a career change."*

**Stats:** WIL 8, END 8, STR 0, AGI 0
**HP:** 88
**Role:** Tanky Summoner

**Core Fantasy:** Lich lord. Command the dead. Tank damage while your minions do the work. You are the raid boss.

**Strengths:**
- Huge HP pool (88) + summons = massive HP advantage
- Action economy (your turn + summon turns)
- Can literally AFK and let summons fight
- Excellent physical AND mental saves
- Very safe, very forgiving

**Weaknesses:**
- Weak personal damage (summons do everything)
- Slow fights (summons aren't super strong individually)
- Many skill slots dedicated to summoning
- Can feel passive/boring
- Tedious if summons are too weak, broken if too strong

**Skill Unlocks:** Raise Undead, Summon Demon, Animate Objects, Skeletal Warrior, Bone Wall, Death Bolt, Life Drain, Corpse Explosion

**Tactical Flow:**
- Summon skeleton
- Summon another skeleton
- Debuff enemies (Weakness, Slow)
- Tank hits with your 88 HP
- Watch skeletons slowly win
- Summon replacements when they die
- Win eventually

**Ideal For:** Pet class enjoyers. Players who want safe, relaxed gameplay. Overlord fans.

---

#### 10. THE BATTLE MAGE
*"My blade burns with arcane fire."*

**Stats:** WIL 7, STR 7, END 0, AGI 0
**HP:** 20
**Role:** Melee Spellcaster

**Core Fantasy:** Warrior who channels magic through their weapon. Hit like a truck, but made of glass. High risk, explosive reward.

**Strengths:**
- High physical damage + high spell damage
- Spell-enhanced combos (Haste → double attacks, Flame Weapon → bonus fire damage)
- Versatile (magic for range, melee for close)
- Can adapt tactics mid-fight
- Incredibly satisfying when it works

**Weaknesses:**
- 20 HP = fragile in melee range (where you need to be)
- Resource-intensive (spells + HP consumables)
- Needs to be in danger to deal damage
- Falls off hard if spell charges run out
- Stressful gameplay (high risk constantly)

**Skill Unlocks:** Flame Weapon, Shocking Grasp, Giant Strength, Haste, Spell Strike, Arcane Blade, Stone Fist, Vampiric Touch

**Tactical Flow:**
- Buff yourself (Flame Weapon, Giant Strength)
- Charge into melee
- Unleash devastating enhanced attacks
- Take damage (you're at 20 HP in melee)
- Retreat, heal, rebuff
- Repeat

**Ideal For:** Hybrid enthusiasts. Players who want best of both worlds. Risk-takers.

---

#### 11. THE ARCANE ARCHER
*"My arrows are guided by the cosmos."*

**Stats:** WIL 7, AGI 7, END 0, STR 0
**HP:** 20
**Role:** Ranged Magic Hybrid

**Core Fantasy:** Elven mage-archer. Arrows and spells both rain from distance. Perfect kiting. Extremely squishy, extremely deadly.

**Strengths:**
- Dual ranged options (physical + magical)
- Perfect kiting (AGI 7 + Blink spell)
- High evasion when positioned well
- Can handle resistant enemies (magic-immune? use arrows. Physical-immune? use spells)
- Incredible tactical flexibility

**Weaknesses:**
- 20 HP = one mistake = death
- Resource-intensive (arrows + spell charges)
- No close-range options
- Vulnerable to teleporting/charging enemies
- Requires perfect positioning constantly

**Skill Unlocks:** Homing Arrow, Phase Shot, Magic Missile, Blink, Shadow Step, Lightning Bolt, Quick Shot, Arcane Arrows

**Tactical Flow:**
- Maintain maximum range
- Shoot arrows at weak enemies (conserve spells)
- Cast spells at dangerous enemies
- Blink when cornered
- Reposition constantly
- Never let them reach you

**Ideal For:** Kiting masters. Players who love ranged combat. Positioning enthusiasts.

---

#### 12. THE TRANSMUTER
*"What is flesh but clay waiting to be molded?"*

**Stats:** WIL 8, STR 6, END 0, AGI 0
**HP:** 20 (but can gain temp HP through transformations)
**Role:** Shapeshifter

**Core Fantasy:** Transform into monsters, dragons, elementals. Ultra-versatile. Every fight is different. Incredibly complex to master.

**Strengths:**
- Massive versatility (become tank, assassin, mage, etc.)
- Transformations grant bonus HP (effective HP way above 20)
- Can adapt to any encounter on the fly
- Transformations have unique abilities
- Endless tactical creativity

**Weaknesses:**
- Weak in human form (20 HP, okay-ish STR)
- Transformation duration limited
- Complex to master (many forms, many abilities)
- Caught between transformations = extremely vulnerable
- Requires encyclopedic knowledge of forms

**Skill Unlocks:** Dragon Form, Bear Form, Shadow Form, Elemental Form, Giant Form, Swarm Form, Stone Form, Gaseous Form

**Tactical Flow:**
- Assess encounter
- Choose appropriate form (Bear for tank, Shadow for stealth, Dragon for DPS)
- Transform
- Dominate with form's abilities
- Revert when duration expires
- Assess next threat
- Transform again

**Ideal For:** Players who love versatility. Shapeshifter fantasy enjoyers. System masters who know all the forms.

---

### TIER 3: BALANCED HYBRIDS (3-Stat Spreads)

#### 13. THE SPELLSWORD
*"Sword and spell, in perfect harmony."*

**Stats:** STR 5, WIL 5, END 4, AGI 0
**HP:** 46
**Role:** Durable Hybrid

**Core Fantasy:** Balanced warrior-mage. Can fight, can cast, can take hits. Jack-of-all-trades, master of survival.

**Strengths:**
- Versatile (melee, magic, tanky)
- 46 HP allows trading hits (forgiving)
- Can solve problems martially OR magically
- Good at everything, great at nothing
- Adaptable to any situation

**Weaknesses:**
- Lower damage than specialists (STR 5, WIL 5 both mediocre)
- Fewer spell slots than pure mages
- Less HP than pure tanks
- Can feel bland (no extreme strengths)

**Skill Unlocks:** Flame Weapon, Magic Missile, Second Wind, Spell Strike, Shield, Haste, Power Attack

**Tactical Flow:**
- Assess situation
- Choose best tool for job
- Melee when safe
- Cast when threatened
- Tank when necessary
- Consistent, reliable, safe

**Ideal For:** New players exploring hybrids. Players who want safety and options.

---

#### 14. THE TRICKSTER
*"Now you see me..."*

**Stats:** AGI 8, WIL 6, END 0, STR 0
**HP:** 20
**Role:** Evasive Utility Mage

**Core Fantasy:** Rogue who learned magic. Illusions, stealth, tricks. Slippery as hell. Infuriating to fight. Weak when caught.

**Strengths:**
- High natural evasion + spell evasion (Mirror Image, Blur)
- Can disengage at will (Blink, Invisibility)
- Tricks create unexpected advantages
- High skill ceiling, satisfying mastery
- Never fight fair

**Weaknesses:**
- 20 HP = death if caught
- Low damage output (no STR, WIL for utility not nukes)
- Long fights (can't burst)
- Resource-intensive

**Skill Unlocks:** Invisibility, Blink, Mirror Image, Smoke Bomb, Shadow Step, Silence, Blur, Disguise

**Tactical Flow:**
- Engage only on your terms
- Turn invisible when threatened
- Blink to safety
- Create illusions to confuse
- Poke from stealth
- Never trade hits

**Ideal For:** Stealth game fans. Players who love utility magic. Tricksters (duh).

---

#### 15. THE WITCH HUNTER
*"Your magic is useless against me, warlock."*

**Stats:** STR 5, AGI 5, WIL 4, END 0
**HP:** 20
**Role:** Anti-Mage Specialist

**Core Fantasy:** Built to kill casters. Shut down enemy magic, then kill them with martial prowess. Hard counters mages.

**Strengths:**
- Perfect mental saves (WIL 4 + counterspells)
- Shuts down enemy casters completely
- Balanced STR/AGI (versatile combat)
- Excels in magic-heavy floors

**Weaknesses:**
- 20 HP = fragile vs martials
- Spell slots for defense, not offense
- Niche (great vs mages, mediocre vs beasts)
- Complex spell management

**Skill Unlocks:** Dispel Magic, Silence, Counterspell, Spell Reflection, Antimagic Field, Feint, Riposte

**Tactical Flow:**
- Identify enemy casters
- Shut them down (Silence, Counterspell)
- Mop up with physical attacks
- Ignore martial enemies (they're easy)

**Ideal For:** Players who love hard countering specific threats. Anti-meta builds.

---

#### 16. THE CRUSADER
*"In faith, I endure."*

**Stats:** STR 6, END 6, WIL 2, AGI 0
**HP:** 65
**Role:** Holy Warrior

**Core Fantasy:** Paladin. Melee fighter with support magic. Healing, cleansing, righteousness. Durable, supportive, steady.

**Strengths:**
- High HP + healing magic (very sustaining)
- Good melee damage (STR 6)
- Support utility (Cleanse, Ward, Bless)
- Decent mental saves (WIL 2)
- Very forgiving gameplay

**Weaknesses:**
- Slow fights (healing prolongs everything)
- Mediocre offense (not enough STR)
- Support-focused (limited damage spells)
- Can be out-sustained by strong enemies

**Skill Unlocks:** Lay on Hands, Smite, Bless, Cleanse, Ward, Holy Light, Shield of Faith, Aura of Protection

**Tactical Flow:**
- Fight in melee
- Take damage
- Heal yourself
- Cleanse debuffs
- Continue fighting
- Wars of attrition (you always win)

**Ideal For:** Paladin fantasy fans. Players who want sustain. Healers in denial.

---

### TIER 4: UNCONVENTIONAL SPECIALISTS

#### 17. THE ALCHEMIST
*"Chemistry is just magic that works."*

**Stats:** END 8, WIL 6, STR 0, AGI 0
**HP:** 88
**Role:** Consumable Specialist

**Core Fantasy:** Mad scientist. Spells enhance consumables. Throw bombs, chug potions, survive through massive HP pool and alchemy.

**Strengths:**
- Massive HP pool (88) for experimentation
- Spells enhance consumables (double duration, AoE splash, etc.)
- Can afford to throw expensive items (you survive mistakes)
- Unique tactical options

**Weaknesses:**
- No direct damage stats
- Relies on consumable availability (RNG-dependent)
- Slow fights (damage from items + spells, not stats)
- Very niche build

**Skill Unlocks:** Alchemical Bomb, Transmute, Potion Mastery, Poison Cloud, Acid Splash, Stone to Gold, Create Potion

**Tactical Flow:**
- Throw alchemical bomb (spell-enhanced for AoE)
- Chug potion (spell-enhanced for double duration)
- Cast Poison Cloud
- Tank hits with 88 HP
- Repeat with different consumables

**Ideal For:** Players who love consumables. Alchemy enthusiasts. Mad scientists.

---

#### 18. THE WARDANCER
*"I am the wind."*

**Stats:** AGI 6, WIL 5, END 3, STR 0
**HP:** 38
**Role:** Mobile Debuffer

**Core Fantasy:** Dance around enemies, touching them with debuff spells. Never stop moving. Graceful, flowing combat.

**Strengths:**
- High mobility + debuffs (apply and retreat)
- Decent evasion (AGI 6)
- Foundation END (38 HP for mistakes)
- Can handle multiple enemies (debuff all of them)

**Weaknesses:**
- Low damage (debuffs take time to kill)
- Touch-range is risky (must close to melee)
- Complex (tracking many debuffs on many enemies)
- Long fights

**Skill Unlocks:** Touch of Weakness, Slow, Curse, Drain, Spring Attack, Haste, Debilitating Touch, Dance of Blades

**Tactical Flow:**
- Dash into melee
- Touch enemy with debuff spell
- Dash out
- Move to next enemy
- Apply different debuff
- Reposition
- Watch enemies weaken and die slowly

**Ideal For:** Mobile combat lovers. Debuff enjoyers. Dancers.

---

#### 19. THE ELEMENTALIST
*"I do not fight you. I fight the world itself."*

**Stats:** WIL 10, END 0, AGI 4, STR 0
**HP:** 20
**Role:** Environmental Controller

**Core Fantasy:** Manipulate the battlefield. Walls, terrain, elements. Chess grandmaster who controls space, not pieces.

**Strengths:**
- Reshape battlefield to your advantage
- AoE control (affect entire rooms)
- Block/funnel enemies with walls
- Creative solutions to every problem
- Incredibly satisfying when mastered

**Weaknesses:**
- 20 HP = death if caught
- Indirect damage (environmental hazards, not direct spells)
- Requires spatial planning and awareness
- Some encounters may not support tactics (empty rooms)

**Skill Unlocks:** Wall of Fire, Ice Floor, Stone Wall, Fog Cloud, Earthquake, Pit Trap, Gust of Wind, Lava Flow

**Tactical Flow:**
- Cast Wall of Fire across room
- Enemies must path around or through (taking damage)
- Cast Ice Floor on their path (they slip)
- Cast Stone Wall to funnel them
- Watch them die to environment
- Reposition if necessary

**Ideal For:** Environmental puzzle lovers. Creative tacticians. Control freaks.

---

#### 20. THE BLOOD MAGE (Theoretical)
*"Power demands sacrifice. My power demands MY sacrifice."*

**Stats:** WIL 10, END 4, STR 0, AGI 0
**HP:** 46
**Role:** HP-Casting Specialist

**Core Fantasy:** Cast spells using HP instead of charges/mana. More HP = more casts. Every spell risks death. High risk, high power.

**Note:** Requires HP-casting mechanic to be implemented. May not be viable depending on magic system design.

**Strengths:**
- Cast far more spells than any other mage
- High WIL = powerful spells
- Foundation END = more HP = more casts
- Can drain enemies to recover HP

**Weaknesses:**
- Every spell risks death
- Must balance offense and survival constantly
- Traditional healing is counterproductive (want to spend HP on spells)
- Incredibly stressful

**Skill Unlocks:** Blood Bolt (cost HP, deal damage), Life Drain, Vampiric Touch, Sacrifice, Blood Shield, Hemorrhage, Crimson Chains

**Tactical Flow:**
- Cast powerful spell (costs 10 HP)
- Cast another (costs 10 HP, now at 26 HP)
- Drain enemy (recover 15 HP)
- Cast again
- Balance HP constantly
- Every decision: "Is this spell worth the HP?"

**Ideal For:** Risk-takers. Players who love resource tension. Masochists.

---

## The Stat-Gated Skill System

### Core Concept

**The Problem with Traditional Systems:**
- Skill trees: Overwhelming, complex, front-loads choices
- Free selection: Analysis paralysis (100 skills, pick 10)
- Class-based: Restrictive, limits experimentation

**Our Solution: Natural Filtering**

Skills have stat requirements (often multiple). Your stat investments unlock skills that synergize with those stats. The game shows you what's available based on YOUR build.

**Player Experience:**
- New player: "I leveled STR, now I can pick from 5 cool melee skills!"
- Veteran: "I need STR 6 + AGI 4 + WIL 2 to unlock Blade Dance for my hybrid build"

**Benefits:**
1. No overwhelming choice paralysis (only see ~15-25 skills at a time)
2. Natural build guidance (shown skills work with your stats)
3. Replayability through discovery (new stat combos = new skills)
4. Depth for veterans (optimize stat breakpoints)
5. Simplicity for newbies (just pick what looks cool)

---

### Skill Categories

#### Active Skills
Abilities you trigger with hotkeys (1-0 number keys).

**Types:**
- Instant cast (Cleave, Power Attack)
- Channeled (some spells?)
- Reactive (Riposte triggers on dodge)
- Toggleable stances (Defensive Stance on/off)

#### Passive Skills
Always-on bonuses or modifiers.

**Types:**
- Stat boosts (+10% STR)
- Mechanical changes (critical hits restore HP)
- Conditional effects (killing blow grants temp buff)
- Synergy enablers (fire spells also burn ground)

**Design Tension:** Should passives count toward the 10-skill limit? Or separate pools?
- If same pool: Forces tough choices (active versatility vs passive power)
- If separate: Might allow too much power stacking
- Leaning toward: Same pool, forces meaningful tradeoffs

---

### Skill Requirement Examples

#### Single-Stat Requirements (Specialists)

**STR 6+:**
- Power Attack
- Cleave
- Sunder Armor
- Intimidate

**STR 8+:**
- Execute (finish wounded enemies)
- Earthquake (AoE ground slam)
- Unstoppable (ignore CC)

**AGI 6+:**
- Dodge Roll
- Quick Shot
- Riposte
- Stealth

**AGI 8+:**
- Perfect Dodge (guarantee dodge for 3 turns)
- Triple Shot
- Smoke Bomb

**END 6+:**
- Second Wind (heal % HP)
- Iron Skin (damage reduction)
- Last Stand (survive lethal hit)
- Taunt

**END 8+:**
- Immortality (literally unkillable for 5 turns)
- Fortress Stance (perfect defense, no offense)

**WIL 6+:**
- Fireball
- Confusion
- Blink
- Magic Missile

**WIL 8+:**
- Meteor (room-wide damage)
- Time Stop (take multiple turns)
- Mass Confusion (CC all enemies)

---

#### Dual-Stat Requirements (Hybrids)

**STR 5 + WIL 5 (Battle Mage):**
- Flame Weapon (add fire damage to weapon)
- Shocking Grasp (melee touch attack stun)
- Giant Strength (temporary STR boost)
- Spell Strike (cast + melee combo)

**AGI 5 + WIL 5 (Arcane Archer):**
- Homing Arrow (never misses)
- Phase Shot (ignore armor)
- Shadow Step (blink + attack)
- Mirror Image (create decoy)

**STR 5 + AGI 5 (Duelist):**
- Blade Dance (attack all adjacent + move)
- Riposte Stance (counter all melee this turn)
- Feint (guarantee crit)
- Whirlwind (spin AoE attack)

**STR 5 + END 5 (Juggernaut):**
- Reckless Attack (more damage, take more damage)
- Shield Wall (massive defense, can't move)
- Devastating Blow (huge single-target)
- Berserker Rage (trade HP for damage)

**AGI 5 + END 5 (Ranger):**
- Evasive Recovery (heal when dodge)
- Mobile Shot (move + shoot without penalty)
- Defensive Roll (reduce damage when hit)
- Hunter's Mark (track + bonus damage)

**END 5 + WIL 5 (Tank Mage):**
- Poison Cloud (AoE DOT)
- Slow (reduce enemy actions)
- Vampiric Touch (damage + heal)
- Stone Skin (massive temp HP)

---

#### Triple-Stat Requirements (Advanced)

**STR 4 + AGI 4 + WIL 4:**
- Elemental Blade Dance (Whirlwind + fire/ice/lightning)
- Arcane Riposte (counter with spell instead of attack)
- Battle Trance (buff all three stats)

**AGI 4 + END 4 + WIL 4:**
- Debuff Aura (mobile debuff field)
- Spring Attack (move, attack, debuff, move again)
- Evasive Casting (cast while dodging)

**STR 4 + END 4 + WIL 4:**
- Holy Smite (massive damage to evil enemies)
- Lay on Hands (massive heal)
- Aura of Protection (allies gain defense - if co-op?)

**STR 3 + AGI 3 + END 3 + WIL 3 (True Generalist):**
- Adaptability (passive: gain small bonus to all stats)
- Jack of All Trades (can use any item/weapon without penalty)
- Master of None (passive: no penalties for hybrid builds)

---

### Passive Skill Examples

**STR Passives:**
- Mighty Blows: +10% melee damage
- Heavy Hitter: Killing blows knock back adjacent enemies
- Armor Breaker: Attacks reduce enemy armor permanently
- Brutal Critical: Critical hits deal triple damage instead of double

**AGI Passives:**
- Nimble: +10% evasion
- Acrobat: Can dodge while moving
- Fleet-Footed: +1 movement range
- Reflexes: First attack each combat automatically dodged

**END Passives:**
- Thick Skin: +10% max HP
- Regeneration: Heal small amount each turn
- Survivor: Heal when dropping below 25% HP (once per floor)
- Tough: Reduce all damage by 1 (massive!)

**WIL Passives:**
- Arcane Mastery: +10% spell effectiveness
- Mana Efficient: Spells cost less (or have longer duration)
- Mental Fortress: Immune to confusion and fear
- Spell Synergy: Casting fire spell leaves burning ground, etc.

**Hybrid Passives:**
- Spellsword (STR 3 + WIL 3): Melee attacks restore spell charges/mana
- Shadow Dancer (AGI 3 + WIL 3): Casting makes you harder to hit next turn
- Battle Mage (STR 3 + END 3): Taking damage increases melee damage
- War Caster (WIL 3 + END 3): Can cast while in melee without penalty

---

### Skill Pool Size

**Target: 50-80 total skills**

**Rough Distribution:**
- STR-focused: 10-12 skills
- AGI-focused: 10-12 skills
- END-focused: 8-10 skills (fewer offensive options)
- WIL-focused: 15-20 skills (spells are diverse)
- Hybrid STR/AGI: 5-6 skills
- Hybrid STR/END: 5-6 skills
- Hybrid STR/WIL: 6-8 skills
- Hybrid AGI/END: 4-5 skills
- Hybrid AGI/WIL: 6-8 skills
- Hybrid END/WIL: 5-6 skills
- Triple-stat: 3-5 skills
- Universal/generalist: 2-3 skills

**Total: ~75-85 skills** (in the target range)

**Why This Works:**
- Each build sees ~20-25 available skills max
- Creates distinct skill "pools" for different archetypes
- Massive replayability (new stat combos = new skill sets)
- Doesn't overwhelm (natural filtering by stats)

---

## Progression Feel & Flow

### Early Game (Levels 1-5)

**Level 1:** Character creation (starting stats? or first stat point?)
**Level 2:** First stat point - pick primary stat (STR, AGI, END, or WIL)
**Level 3:** First skill point - choose from 3-5 basic skills in that stat
**Level 4:** Second stat point - commit to primary or start secondary
**Level 5:** Second skill point - build is starting to take shape

**Player Experience:**
- Simple, guided choices
- Learning game systems
- Build direction emerging naturally
- Not overwhelming (few skills available)

**Example - STR Focus:**
- Lvl 2: +1 STR (now STR 1)
- Lvl 3: Choose between Power Attack, Cleave, Heavy Strike
- Lvl 4: +1 STR (now STR 2) OR +1 END (safety)
- Lvl 5: New skills unlocked based on choice

---

### Mid Game (Levels 6-14)

**Player Experience:**
- Second stat is being invested in
- Hybrid skills start unlocking
- Build identity crystallizes
- "Oh, I'm becoming a Battle Mage!"
- Tactical complexity increases
- More skills available (now seeing 10-15 options)

**Key Milestones:**
- Hit STR 5 or WIL 5 → hybrid skills unlock
- Get 5th or 6th skill → hotbar filling up, meaningful choices
- Stat breakpoints matter → "If I get one more AGI, I unlock Dodge Roll!"

**Example - Battle Mage:**
- Lvl 6: +1 WIL (now STR 3, WIL 1)
- Lvl 7: Unlock Magic Missile
- Lvl 10: Hit STR 5, WIL 5 → Flame Weapon unlocks!
- Lvl 11: Choose Flame Weapon → build comes together

---

### Late Game (Levels 15-21)

**Player Experience:**
- High-threshold specialist skills unlock
- OR triple-stat hybrid skills available
- Full build expression
- Powerful, satisfying abilities
- Build is DONE, now mastering it

**Key Milestones:**
- Hit WIL 8 → Meteor unlocks
- Hit STR 4 + AGI 4 + WIL 4 → Blade Dance unlocks
- 10th skill chosen → build complete
- Now it's about execution, not choices

**Example - Archmage:**
- Lvl 18: +1 WIL (now WIL 9)
- Lvl 19: Meteor available! Choose it.
- Lvl 20: +1 WIL (now WIL 10) → Time Stop unlocks
- Lvl 21: Choose Time Stop → god mode achieved

---

### Moment-to-Moment Feel

**Level Up (Stat):**
- "You gained a level!"
- Assign stat point immediately
- See tooltip: "STR 6 → 7: +2 melee damage, unlocks Execute"
- Anticipation for next skill level

**Level Up (Skill):**
- "Choose a new skill!"
- Browse available skills (filtered by your stats)
- Read descriptions, see requirements
- "Oh, Flame Weapon looks cool! And I meet the requirements!"
- Choose
- Hotkey immediately available
- Try it out in next fight

**No Banking:**
- Forces commitment
- Each level feels impactful
- Can't save up and dump points
- Prevents gaming the system

---

## Design Principles & Guidelines

### For Skill Design

**Every Skill Should:**
1. Have clear mechanical identity (what does it DO)
2. Fit thematically with stat requirements (STR = hitting hard, AGI = mobility, etc.)
3. Be useful in multiple situations (not too niche)
4. Have appropriate power level for stat investment
5. Create interesting decisions (not just "always use")

**Avoid:**
- Trap skills (skills that sound good but are useless)
- Mandatory skills (if everyone takes it, it shouldn't be a choice)
- Complex interactions that are hard to understand
- Passive stat sticks (+1 STR is boring)

**Embrace:**
- Skill synergies (Haste + Cleave = lots of AoE damage)
- Build-defining skills (Raise Undead changes entire playstyle)
- Flashy, satisfying abilities (Meteor feels GOOD)
- Tactical depth (Riposte requires timing and positioning)

---

### For Stat Requirements

**Guidelines:**
- Single stat 6+: Basic specialist skills
- Single stat 8+: Advanced specialist skills, very powerful
- Dual stat 5/5: Core hybrid identity skills
- Dual stat 4/4: Accessible hybrid skills
- Triple stat 4/4/4: Advanced hybrid skills, build-around-able
- Triple stat 3/3/3/3: Generalist skills (rare)

**Tuning Breakpoints:**
- Make sure every stat investment feels rewarding
- Avoid dead levels (STR 7 unlocks nothing? bad!)
- Some skills at STR 6, some at STR 7, some at STR 8
- Create interesting breakpoint decisions ("Do I go STR 8 or spread to AGI 4?")

---

### Balance Philosophy

**Not All Builds Are Equal (And That's OK):**
- Pure STR (Berserker) should be simple and powerful
- Pure WIL (Controller) should be complex and powerful
- Hybrid builds trade power for versatility
- Glass cannons trade survivability for damage
- Tanks trade damage for survivability

**Power Budget:**
- High damage = low survivability (Duelist, Battle Mage)
- High survivability = low damage (Fortress, Tank Mage)
- High versatility = medium everything (Spellsword, Ranger)

**Skill Ceiling:**
- Easy builds: Juggernaut, Berserker (forgiving, straightforward)
- Medium builds: Ranger, Spellsword (balanced, adaptable)
- Hard builds: Controller, Arcane Archer (unforgiving, complex)

**All Should Be Viable:**
- Even Fortress (0 STR/AGI) should be able to win (eventually)
- Even Duelist (0 END) should be able to win (with skill)
- Viability ≠ optimal, viability = possible

---

## Open Questions & Considerations

### Progression Structure

**How many levels total?**
- 21 levels = 10 stats + 10 skills (current hypothesis)
- Could be more, could be fewer
- Needs playtesting to find sweet spot

**Starting stats?**
- Do you start at 0-0-0-0 and first level gives +1?
- Or start at 1-1-1-1 baseline?
- Or distribute starting points (like 3 points to assign)?

**Level curve?**
- How much XP per level?
- Linear (always 100 XP) or exponential (100, 200, 400...)?
- Floor-based (each floor = 1-2 levels) or kill-based?

---

### Skill System Details

**How many skills can you have active?**
- 10 seems right (1-0 hotkeys)
- But maybe you have 10 chosen, and can only have 6 *equipped* at a time?
- Forces loadout choices (swap skills between floors?)

**Can you respec?**
- Probably not (roguelike permadeath philosophy)
- Your build is committed for the run
- Replayability through trying new builds

**Passive skill balance?**
- Should they count toward 10-skill limit? (Yes, probably)
- How powerful should they be? (Competitive with actives)
- Example: Is "Regenerate 2 HP/turn" worth a skill slot? Maybe!

**Skill discovery?**
- First time you meet requirements, do you see the skill?
- Or do you need to find a skill book first?
- Leaning toward: Auto-unlock when requirements met (less RNG)

---

### Magic System Integration

**What resources do spells use?**
- Charges per floor (roguelike)
- Cooldowns (simple, tactical)
- Mana pool (traditional)
- HP (blood magic)
- Mix of above (different spells, different resources)

**If charge-based:**
- How many charges per spell per floor?
- Can you find more? (scrolls, mana potions?)
- Different spells, different charge counts?

**If cooldown-based:**
- How long? (Fireball every 5 turns?)
- Does WIL reduce cooldowns?
- Easy to balance, hard to make feel "magical"

**If mana-based:**
- Does WIL increase mana pool?
- Regenerating or fixed per floor?
- Requires whole mana system implementation

**Current Lean:** Probably cooldown-based for simplicity, but open to alternatives.

---

### Stat Scaling Details

**Exactly how much does each stat do?**

**STR:**
- Melee damage: +1 damage per STR? +2? Multiplier?
- Accuracy: +5% per STR? Flat bonus to hit roll?

**AGI:**
- Evasion: +5% per AGI? Flat bonus to defense roll?
- Ranged accuracy: Same as STR for ranged?
- Movement: +1 tile per 5 AGI?

**END:**
- HP: Already defined (20 base + quadratic)
- Physical saves: +1 to save roll per END?
- Regen: Future system, percentage-based

**WIL:**
- Spell damage: +10% per WIL? +2 flat per WIL?
- Spell duration: +1 turn per 3 WIL?
- Mental saves: +1 to save roll per WIL?
- Mana pool: +10 mana per WIL? (if mana system)

**Needs:** Extensive playtesting to find satisfying scaling.

---

### Edge Cases & Weird Builds

**What if someone goes 5-5-5-5 (generalist)?**
- Should be viable but not optimal
- Unlocks some skills from each tree
- Jack-of-all-trades, master of none
- Maybe unlock special "generalist" skills?

**What if someone goes 10-10-0-0 (extreme dual-focus)?**
- Should be very powerful in their niche
- Extremely vulnerable in weak areas
- Example: STR 10 + AGI 10 = untouchable damage dealer with 20 HP
- High risk, high reward

**What if someone goes 3-3-3-3 early then focuses?**
- Versatile early game, specialist late game
- Might unlock some low-threshold hybrid skills early
- Delayed power spike but broad options

**What if someone never invests in a stat?**
- 0 STR: Can't deal melee damage (but spells/ranged work)
- 0 AGI: Terrible evasion (tank builds fine with this)
- 0 END: 20 HP (glass cannon, extremely risky)
- 0 WIL: No spells (pure martial, totally viable)

All of these should be possible and interesting!

---

## Wild Ideas & Experimental Concepts

### Skill Mutations
What if skills could evolve or mutate based on usage?
- Fireball used 50 times → unlocks Meteor (upgraded version)
- Cleave used 30 times → unlocks Whirlwind (360° version)
- Mastery through practice
- Rewards favorite skills

**Pros:** Rewarding progression, skill mastery fantasy
**Cons:** Complex to track, might encourage grinding

---

### Skill Combos
What if certain skill combinations created special effects?
- Flame Weapon + Whirlwind = Fire Tornado
- Blink + Power Attack = Teleport Strike
- Haste + Cleave = Blender Mode
- Discovered through experimentation

**Pros:** Massive depth, rewarding discovery, creativity
**Cons:** Complex to design, hard to balance, might be hidden

---

### Forbidden Skills
What if some skills had NEGATIVE requirements?
- "Berserker Rage" requires STR 8, END 0 (glass cannon only)
- "Perfect Dodge" requires AGI 10, STR 0 (pure evasion only)
- "Lich Form" requires WIL 10, END 0 (undead transformation)
- Rewards extreme specialization

**Pros:** Incentivizes weird builds, flavorful restrictions
**Cons:** Might feel punishing, complex to explain

---

### Equipment-Granted Skills
What if some equipment gave temporary skill access?
- Staff of Fireballs: Grants "Fireball" skill while equipped
- Boots of Blinking: Grants "Blink" skill while worn
- Doesn't count toward 10-skill limit
- Lost when unequipped

**Pros:** Equipment feels more interesting, build flexibility
**Cons:** Might dilute skill system, makes permanent choices less meaningful

---

### Skill Loadouts
What if you could prepare different skill loadouts?
- Choose 10 skills from your learned skills
- Equip 6 for combat (1-6 hotkeys)
- Swap loadouts between floors
- "Boss loadout" vs "exploration loadout"

**Pros:** Tactical depth, adaptation to situations
**Cons:** Adds complexity, might be unnecessary

---

### Corruption/Dark Skills
What if some powerful skills had drawbacks?
- "Life Drain": Steal HP, but your max HP decreases permanently by 1
- "Demonic Pact": +5 to all stats, but you're always burning
- "Lich Transformation": Immune to death, but can't heal
- High risk, high reward
- Permanent character alterations

**Pros:** Interesting decisions, risk/reward fantasy
**Cons:** Might feel bad, hard to balance, could be trap choices

---

### Synergy Bonuses
What if having certain stat distributions gave bonuses?
- STR 7 + AGI 7 = "Weapon Master" passive (+1 to both in combat)
- WIL 10 + END 10 = "Arcane Titan" passive (spells cost HP but deal more damage)
- AGI 10 + WIL 10 = "Shadow Mage" passive (casting doesn't break stealth)

**Pros:** Rewards specific builds, creates goals
**Cons:** Might create "trap" stat distributions, complex

---

### Skill Rarity & Discovery
What if some skills were hidden until discovered?
- Most skills: Auto-unlock when requirements met
- Rare skills: Require finding a tome/scroll/trainer
- Legendary skills: Unlocked by quests or achievements
- Creates sense of discovery and progression

**Pros:** Exciting discoveries, meta-progression feel
**Cons:** RNG might screw builds, could feel bad

---

## Implementation Priorities

### Phase 1: Core Framework
1. Stat system (STR, AGI, END - WIL comes later)
2. Level-up system (alternating stat/skill points)
3. Skill requirement checking (show/hide based on stats)
4. Hotkey system (1-0 keys for skills)
5. Basic skill framework (cooldowns, execution)

### Phase 2: Initial Skill Pool
1. 5-10 skills per primary stat (STR, AGI, END)
2. Simple, clear abilities
3. No complex interactions yet
4. Test balance and feel

### Phase 3: Hybrid Skills
1. Dual-stat requirements (STR/AGI, STR/END, AGI/END)
2. 3-5 skills per hybrid combo
3. Test build diversity

### Phase 4: Magic & WIL
1. Implement WIL stat
2. Magic resource system (cooldowns? charges? mana?)
3. 10-15 WIL-based skills (spells)
4. Magic/martial hybrid skills

### Phase 5: Advanced Skills
1. Triple-stat requirements
2. High-threshold specialist skills (STR 8+, WIL 8+, etc.)
3. Passive skills
4. Experimental/weird skills

### Phase 6: Polish & Expansion
1. Reach 50-80 total skills
2. Balance pass
3. Add skill descriptions, flavor text
4. Playtesting and iteration

---

## Flavor & Presentation

### Skill Names Should Be Evocative
- "Cleave" > "Multi-target Attack"
- "Whirlwind" > "360° Slash"
- "Time Stop" > "Temporal Manipulation"
- "Shadow Step" > "Teleport + Attack Combo"

### Skill Descriptions Should Include:
- Flavor text (1 sentence, evocative)
- Mechanical description (what it does)
- Stat requirements (STR 6, AGI 4)
- Resource cost (if applicable)
- Cooldown (if applicable)

**Example:**
```
BLADE DANCE
"The sword becomes an extension of your will, flowing like water through enemies."

Strike all adjacent enemies, then move up to 3 tiles.

Requirements: STR 5, AGI 5
Cooldown: 5 turns
```

### Stat Descriptions Should Be Clear
- STR: Melee damage, accuracy, heavy weapons
- AGI: Evasion, ranged accuracy, movement
- END: Hit points, physical saves, survivability
- WIL: Magic power, mental saves, spells

### Level-Up Screen Should Show:
- Current stats
- Available stat to increase
- Skills that would unlock with different choices
- Preview of next skill point

**Example:**
```
LEVEL UP!

Choose a stat to increase:

[STR: 5 → 6]
  +2 melee damage, +5% accuracy
  Unlocks: Sunder Armor, Power Attack

[AGI: 4 → 5]
  +5% evasion, +5% ranged accuracy
  Unlocks: Riposte, Dodge Roll
  With STR 5: Unlocks Blade Dance!

[END: 3 → 4]
  +26 → 34 HP, +1 physical save
  Unlocks: Second Wind

[WIL: 2 → 3]
  +10% spell power, +1 mental save
  Unlocks: Magic Missile
```

---

## Closing Thoughts

This system has potential to be incredibly elegant:

**For New Players:**
- "Level up what you want"
- "Pick skills that look cool"
- Game guides you naturally
- Can't make truly bad choices

**For Veterans:**
- Optimize stat breakpoints for specific skills
- Discover new combinations
- Theorycraft perfect builds
- Master complex archetypes (Controller, Transmuter)

**For Everyone:**
- Massive replayability (every run different)
- Clear build identity emerges naturally
- Satisfying progression (frequent rewards)
- Tactical depth without overwhelming complexity

The key insight: **Natural filtering through stat requirements** solves the "kitchen sink" problem. You CAN have 80 skills without overwhelming players because they only ever see ~20 at a time.

---

**Next Steps:**
1. Finalize WIL stat design (magic system dependencies)
2. Design first 20-30 core skills (representatives of each category)
3. Implement stat-gating system
4. Prototype level-up flow
5. Playtest and iterate

This is a strong foundation. Let's build something special.

---

*"In the depths of despair, power awaits those bold enough to grasp it."*
