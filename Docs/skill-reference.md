# Skill Reference

Reference document for approved, implemented, and tested skills available in the game.

## Skill Categories

- **Active** - Player-activated skills that cost Willpower
- **Passive** - Always-on skills providing permanent bonuses

---

## Strength Skills

### Power Attack

**Category:** Active | **Tier:** 1 | **Prerequisites:** STR 1 | **Willpower Cost:** 3

**Targeting:** Self | **Consumes Turn:** No

**Tags:** melee, damage, buff

**Effect:** +3 STR for the remainder of the turn.

**Mechanics:** Applies a `strength_modifier` condition with +3 amount for 1 turn. As a free action, this can be combined with a melee attack on the same turn. The STR bonus enhances both attack rolls and damage.

---

### Cleave

**Category:** Active | **Tier:** 2 | **Prerequisites:** STR 3 | **Willpower Cost:** 6

**Targeting:** Adjacent | **Consumes Turn:** Yes

**Tags:** melee, damage, multi-target

**Effect:** Attack hits two adjacent enemies.

**Mechanics:** Performs a `melee_attack` effect that targets up to 2 enemies adjacent to the caster. Requires a primary target selection, then automatically includes one additional adjacent hostile entity if available.

---

## Agility Skills

### Quick Step

**Category:** Active | **Tier:** 1 | **Prerequisites:** AGI 1 | **Willpower Cost:** 2

**Targeting:** Line | **Range:** 2 | **Consumes Turn:** Yes

**Tags:** movement, utility

**Effect:** Move 2 tiles in the selected direction.

**Mechanics:** Uses the `move_tiles` effect to move the caster up to 2 tiles in the chosen direction. Movement stops early if blocked by walls, impassable terrain, or hostile entities. Can swap positions with friendly entities on the first tile.

---

### Fleet of Foot

**Category:** Passive | **Tier:** 1 | **Prerequisites:** AGI 2 | **Willpower Cost:** 0

**Targeting:** Self

**Tags:** defense, evasion

**Effect:** +1 evasion.

**Mechanics:** Applies a permanent `evasion_modifier` condition when learned. The evasion bonus improves the entity's defense roll in combat, making them harder to hit.

---

## Endurance Skills

### Thick Skin

**Category:** Passive | **Tier:** 1 | **Prerequisites:** END 1 | **Willpower Cost:** 0

**Targeting:** Self

**Tags:** defense

**Effect:** +1 armor.

**Mechanics:** Applies a permanent `armor_modifier` condition when learned. Armor directly reduces incoming physical damage after a successful hit.

---

## Willpower Skills

### Magic Missile

**Category:** Active | **Tier:** 1 | **Prerequisites:** WIL 1 | **Willpower Cost:** 3

**Targeting:** Enemy | **Range:** 6 | **Consumes Turn:** Yes

**Projectile:** magic_missile

**Tags:** spell, damage, ranged

**Effect:** 2d4 damage, never misses, 6 range.

**Mechanics:** Spawns a `magic_missile` projectile that travels to the target. On impact, applies `damage` with dice="2d4" and autoHit=true, bypassing the normal attack roll resolution. The projectile visual is a cyan arcane diamond with a trailing effect.

---

### Attunement

**Category:** Passive | **Tier:** 1 | **Prerequisites:** WIL 2 | **Willpower Cost:** 0

**Targeting:** Self

**Tags:** magic, utility

**Effect:** Sense charge levels on wands and staves.

**Mechanics:** When learned, chargeable items display approximate charge levels in inventory and item details: Full (75%+), Half (50-74%), Low (25-49%), or Almost Empty (<25%). Without this skill, charge levels are hidden.

---

## Quick Reference

| Skill | Category | Prereq | WP | Effect Summary |
|-------|----------|--------|-----|----------------|
| Power Attack | Active | STR 1 | 3 | +3 STR this turn (free action) |
| Cleave | Active | STR 3 | 6 | Hit 2 adjacent enemies |
| Quick Step | Active | AGI 1 | 2 | Move 2 tiles in a direction |
| Fleet of Foot | Passive | AGI 2 | 0 | +1 evasion |
| Thick Skin | Passive | END 1 | 0 | +1 armor |
| Magic Missile | Active | WIL 1 | 3 | 2d4 damage, auto-hit, range 6 |
| Attunement | Passive | WIL 2 | 0 | Sense wand/staff charge levels |

---

## Implementation Files

**Data:**
- Skill Definitions: `Data/Skills/*.yaml`

**Core Systems:**
- Skill Executor: `Scripts/Skills/SkillExecutor.cs`
- Passive Processor: `Scripts/Skills/PassiveSkillProcessor.cs`
- Condition System: `Scripts/Conditions/ConditionFactory.cs`

**Effects:**
- ApplyConditionEffect: `Scripts/Effects/ApplyConditionEffect.cs` (Power Attack buff)
- MeleeAttackEffect: `Scripts/Effects/MeleeAttackEffect.cs` (Cleave)
- MoveTilesEffect: `Scripts/Effects/MoveTilesEffect.cs` (Quick Step)
- DamageEffect: `Scripts/Effects/DamageEffect.cs` (Magic Missile)

**Visuals:**
- Projectile Definitions: `Scripts/Systems/VisualEffects/VisualEffectDefinitions.cs`
