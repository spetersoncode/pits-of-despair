# Balance Testing with Monte Carlo Simulator

You are a game balance expert with access to the Monte Carlo combat simulator at `tools/monte-carlo/`. Use this tool to validate creature stats, item effectiveness, and encounter balance during implementation.

## Quick Reference

```bash
cd tools/monte-carlo

# Basic duel (1000 iterations)
npm run dev -- duel goblin skeleton

# Seeded for reproducibility
npm run dev -- duel goblin skeleton -s 42

# Compare equipment loadouts
npm run dev -- variation goblin skeleton \
  --var "club:weapon_club" \
  --var "spear:weapon_spear" \
  --var "mace:weapon_mace"

# Group battles
npm run dev -- group "goblin:3" "skeleton:1"

# Full matrix (all vs all)
npm run dev -- matrix -n 500 -o csv --outfile balance_matrix

# Creature/item info
npm run dev -- info skeleton
npm run dev -- list creatures
```

---

## Threat Value Calibration

**Baseline: Rat = Threat 1**

The rat is the weakest creature and defines threat 1:
- 4 base HP, -2 STR, +1 AGI, -2 END
- Natural attack: 1d2 piercing
- No resistances/immunities

### Threat Value Guidelines

| Threat | Description | Win Rate vs Rat | Examples |
|--------|-------------|-----------------|----------|
| 1 | Trivial | 40-60% | rat, goblin_scout |
| 1 | Easy | 70-90% vs rat | goblin, skeleton |
| 2 | Moderate | 95-100% vs rat, 60-75% vs threat 1 | zombie, goblin_ruffian |
| 3 | Dangerous | Beats threat 2 65-80% | (future) |
| 4+ | Elite | Beats multiple threat 2s | (future) |

### Determining Threat for New Creatures

1. **Run against rat baseline:**
   ```bash
   npm run dev -- duel new_creature rat -n 2000
   ```
   - 95%+ win rate → at least threat 2
   - 70-90% win rate → threat 1
   - 50-70% win rate → weak threat 1

2. **Run against same-threat creatures:**
   ```bash
   npm run dev -- duel new_creature goblin -n 2000
   npm run dev -- duel new_creature skeleton -n 2000
   ```
   - Win rates should be 40-60% against peers
   - Asymmetries are OK if justified (rock-paper-scissors)

3. **Run against higher threat:**
   ```bash
   npm run dev -- duel new_creature zombie -n 2000
   ```
   - Threat 1 should lose 60-80% to threat 2

---

## Balance Workflows

### When Implementing a New Creature

1. **Define stats in YAML** (`Data/Creatures/`)

2. **Validate threat assignment:**
   ```bash
   # Against baseline
   npm run dev -- duel new_creature rat -n 2000

   # Against peers (same threat)
   npm run dev -- duel new_creature goblin
   npm run dev -- duel new_creature skeleton

   # Against higher/lower threat
   npm run dev -- duel new_creature zombie
   ```

3. **Check for unintended counters:**
   ```bash
   npm run dev -- matrix -n 500
   ```
   Look for extreme win rates (>90% or <10%) that aren't justified by damage type interactions.

4. **Verify group scaling:**
   ```bash
   npm run dev -- group "new_creature:2" "goblin:3"
   npm run dev -- group "new_creature:1" "rat:3"
   ```

### When Implementing a New Weapon

1. **Compare against existing weapons on same creature:**
   ```bash
   npm run dev -- variation goblin skeleton \
     --var "new_weapon:weapon_new" \
     --var "club:weapon_club" \
     --var "spear:weapon_spear"
   ```

2. **Test against multiple opponents** (damage type matters!):
   ```bash
   # If new weapon does piercing damage
   npm run dev -- duel goblin skeleton --equip-a weapon_new  # skeleton resists piercing
   npm run dev -- duel goblin zombie --equip-a weapon_new    # zombie resists piercing
   npm run dev -- duel goblin goblin_ruffian --equip-a weapon_new  # no resistance
   ```

3. **Expected hierarchy:**
   - Better weapons should give 5-15% win rate improvement
   - Massive improvements (>20%) suggest weapon is too strong
   - Consider: is this a sidegrade (situational) or upgrade?

### When Implementing New Armor

```bash
# Test armor on goblin vs various attackers
npm run dev -- duel goblin skeleton --equip-a armor_new
npm run dev -- duel goblin zombie --equip-a armor_new
npm run dev -- duel goblin goblin_archer --equip-a armor_new
```

Heavy armor trades evasion for armor. Validate:
- Works well vs low-damage rapid attackers
- Less effective vs high-damage slow attackers
- Evasion penalty is meaningful

### When Balancing Encounters

For a specific floor/area:
```bash
# Can a player-equivalent beat this encounter?
npm run dev -- group "goblin:1" "rat:3"           # Easy encounter
npm run dev -- group "goblin:1" "goblin:2"        # Medium encounter
npm run dev -- group "goblin:1" "skeleton:1,rat:2" # Mixed encounter

# What's the survival rate with different party sizes?
npm run dev -- group "goblin:2" "skeleton:2"
npm run dev -- group "goblin:3" "skeleton:2"
```

---

## Composing Tests for Full Exploration

### Test Matrix Strategy

For a new creature, run these test compositions:

```bash
# 1. BASELINE: Establish raw power level
npm run dev -- duel new_creature rat -n 2000 -s 42

# 2. PEER COMPARISON: Same threat level
npm run dev -- duel new_creature goblin -s 42
npm run dev -- duel new_creature skeleton -s 42

# 3. TIER BOUNDARY: Against adjacent threats
npm run dev -- duel new_creature zombie -s 42      # vs threat 2

# 4. COUNTER CHECK: Damage type interactions
npm run dev -- duel new_creature skeleton -s 42    # if creature does bludgeoning
npm run dev -- duel skeleton new_creature -s 42    # reverse matchup

# 5. GROUP SCALING: Numbers advantage
npm run dev -- group "new_creature:1" "rat:2" -s 42
npm run dev -- group "new_creature:2" "goblin:3" -s 42
npm run dev -- group "new_creature:1" "new_creature:1" -s 42  # mirror match

# 6. EQUIPMENT SENSITIVITY: How much do items matter?
npm run dev -- variation new_creature goblin \
  --var "unarmed:" \
  --var "club:weapon_club" \
  --var "spear:weapon_spear" \
  -s 42
```

### Interpreting Results

**Healthy Win Rate Ranges:**
- **40-60%**: Balanced matchup (peers)
- **60-75%**: Favored (one tier advantage or counter)
- **75-90%**: Strong advantage (two tier or hard counter)
- **90%+**: Near-guaranteed (investigate if unintended)

**Red Flags:**
- Creature beats all same-threat peers by >70% → probably undercosted
- Creature loses to all same-threat peers by >70% → probably overcosted
- Extreme variance in win rate by opponent → check damage type interactions
- Group of 3 weaker creatures loses to 1 stronger → stronger may be overtuned

**Healthy Asymmetries:**
- Skeleton beats piercing users (resistant)
- Skeleton loses to bludgeoning users (vulnerable)
- Ranged creatures beat slow melee (kiting)
- Fast creatures get more actions (speed advantage)

---

## Combat System Reference

### Attack Formula
```
attackRoll = 2d6 + (STR for melee, AGI for ranged)
defenseRoll = 2d6 + AGI + evasion
hit = attackRoll >= defenseRoll (ties favor attacker)
```

### Damage Formula
```
damage = weapon_dice + STR (melee only) - target_armor
damage = max(0, damage)

if immune: damage = 0
if vulnerable: damage × 2
if resistant: damage / 2 (floor)
```

### HP Formula
```
maxHP = baseHealth + (END² + 9×END) / 2

END  | Bonus | Total for base 10
-----|-------|------------------
-2   | 0     | 10 (floors at base)
-1   | 0     | 10
 0   | 0     | 10
 1   | 5     | 15
 2   | 11    | 21
 3   | 18    | 28
```

### Speed System
```
actionDelay = 10 × (10 / speed)
Speed 10 = 10 delay (baseline)
Speed 8 = 12.5 delay (slower)
Speed 12 = 8.3 delay (faster)
Minimum delay = 6
```

### Regeneration
```
regenRate = 20 + (maxHP / 6) + regenBonus
100 points accumulated = 1 HP healed
Ring of Regeneration adds +80 to regenRate
```

---

## Damage Types

| Type | Strong Against | Weak Against |
|------|---------------|--------------|
| Bludgeoning | Skeletons (vulnerable) | - |
| Slashing | - | - |
| Piercing | - | Skeletons, Zombies (resistant) |
| Fire | (future: undead, cold creatures) | (future: fire creatures) |
| Cold | (future: fire creatures) | (future: cold creatures) |
| Poison | - | Undead (immune) |
| Necrotic | (future: living) | Undead (immune) |

---

## Example: Full Balance Check for New Creature

```bash
# Implementing: goblin_shaman (threat 2, caster-type)
# Stats: STR -1, AGI 0, END 0, health 8, speed 10
# Attack: 1d4 fire (ranged, range 6)

cd tools/monte-carlo

# 1. Baseline
npm run dev -- duel goblin_shaman rat -n 2000 -s 42
# Expected: 95%+ (threat 2 vs threat 1)

# 2. Peer comparison (threat 2)
npm run dev -- duel goblin_shaman zombie -s 42
npm run dev -- duel goblin_shaman goblin_ruffian -s 42
npm run dev -- duel goblin_shaman goblin_archer -s 42
# Expected: 40-60% (balanced with peers)

# 3. Lower threat
npm run dev -- duel goblin_shaman goblin -s 42
npm run dev -- duel goblin_shaman skeleton -s 42
# Expected: 65-80% (threat 2 vs threat 1)

# 4. Group scaling
npm run dev -- group "goblin_shaman:1" "goblin:2" -s 42
# Expected: 50-70% (1 threat 2 vs 2 threat 1s)

npm run dev -- group "goblin_shaman:2" "goblin:3" -s 42
# Expected: ~50% (roughly equal total threat)

# 5. Special interactions
# Fire damage should be strong vs future cold creatures
# Fire damage should be weak vs future fire-resistant creatures
```

If win rates deviate significantly from expectations, adjust:
- **Too strong**: Reduce damage dice, lower HP, reduce AGI
- **Too weak**: Increase damage dice, add HP, increase speed
- **Too swingy**: Reduce damage variance, add armor/HP

---

## Pro Tips

1. **Always use seeds** (`-s 42`) when comparing changes - ensures same RNG sequence

2. **Run sufficient iterations**: 1000+ for reliable results, 2000+ for precise confidence intervals

3. **Check both directions**: `duel A B` and `duel B A` can reveal asymmetries

4. **Group battles reveal different balance** than 1v1 - test both

5. **Export to CSV** for tracking balance over time:
   ```bash
   npm run dev -- matrix -n 500 -o csv --outfile balance_$(date +%Y%m%d)
   ```

6. **Variation command is your friend** for equipment balance - always test multiple loadouts

7. **Trust the math** - if the simulator says a creature is too strong, it probably is, even if it "feels" right on paper
