# Monte Carlo Combat Simulator

A TypeScript-based combat simulator for balance testing **Pits of Despair**. Runs thousands of combat simulations to generate statistical data for game balance analysis.

## Quick Start

```bash
cd tools/monte-carlo
npm install
npm run dev -- duel goblin skeleton -n 1000
```

## Commands

### Duel
Run a 1v1 combat simulation:
```bash
npm run dev -- duel <creatureA> <creatureB> [options]

# Examples
npm run dev -- duel goblin skeleton -n 1000
npm run dev -- duel goblin skeleton -s 42          # Seeded for reproducibility
npm run dev -- duel goblin skeleton -o json        # JSON output
npm run dev -- duel goblin skeleton --equip-a weapon_spear  # Override equipment
```

### Group Battle
Run team vs team combat:
```bash
npm run dev -- group <teamA> <teamB> [options]

# Examples
npm run dev -- group "goblin:3" "skeleton:1"       # 3 goblins vs 1 skeleton
npm run dev -- group "goblin:2,goblin_archer:1" "zombie:1"
```

### Variation
Compare different equipment loadouts:
```bash
npm run dev -- variation <creature> <opponent> --var "name:equipment" [--var ...]

# Example: Which weapon is best for a goblin vs skeleton?
npm run dev -- variation goblin skeleton \
  --var "club:weapon_club" \
  --var "spear:weapon_spear" \
  --var "mace:weapon_mace"
```

### Inline Creatures
Test hypothetical creature configurations without modifying YAML files.

**Note:** When using inline JSON, use a placeholder `_` for positional args that will be replaced by inline definitions:
```bash
# Inline A vs regular creature B
npm run dev -- duel _ skeleton --inline-a '{"base":"goblin","strength":4}'

# Regular A vs inline B
npm run dev -- duel goblin _ --inline-b '{"base":"skeleton","health":15}'

# Both inline
npm run dev -- duel _ _ --inline-a '{"base":"goblin","strength":4}' --inline-b '{"base":"skeleton","health":15}'
```

**Shell Escaping:**
JSON requires proper escaping depending on your shell:

```bash
# Bash/Unix - use single quotes around JSON
npm run dev -- duel _ skeleton --inline-a '{"base":"goblin","strength":4}'

# PowerShell/Windows - escape inner quotes with backslash
npm run dev -- duel _ skeleton --inline-a "{\"base\":\"goblin\",\"strength\":4}"

# CMD - escape inner quotes with backslash
npm run dev -- duel _ skeleton --inline-a "{\"base\":\"goblin\",\"strength\":4}"
```

**Examples:**
```bash
# Batch variation testing
npm run dev -- variation-inline skeleton \
  --vars '[{"base":"goblin","name":"baseline"},{"base":"goblin","name":"+2 STR","strength":2}]'

# Complete custom creature (no base)
npm run dev -- duel _ rat --inline-a '{"name":"tank","strength":2,"health":20,"equipment":["weapon_club"]}'
```

**Inline Creature Schema:**
```json
{
  "base": "goblin",           // Optional: inherit from existing creature
  "name": "my-variant",       // Required if no base
  "strength": 2,              // Stat overrides
  "agility": 0,
  "endurance": 1,
  "health": 12,
  "speed": 10,
  "equipment": ["weapon_club"],
  "resistances": ["Piercing"],
  "vulnerabilities": ["Fire"],
  "immunities": ["Poison"]
}
```

### Matrix
Run all creatures against each other:
```bash
npm run dev -- matrix -n 500 -o csv --outfile results
```
Note: Creatures with non-combat AI behaviors (e.g., cowardly) are excluded from the matrix.

### List & Info
```bash
npm run dev -- list creatures    # List all creatures
npm run dev -- list items        # List all items
npm run dev -- info skeleton     # Show creature details
npm run dev -- info club         # Show item details
```

## Options

| Option | Description |
|--------|-------------|
| `-n, --iterations <n>` | Number of simulations (default: 1000) |
| `-s, --seed <n>` | Random seed for reproducibility |
| `-o, --output <fmt>` | Output format: console, json, csv |
| `--outfile <path>` | Write output to file |
| `-c, --compact` | Compact console output |
| `-v, --verbose` | Debug mode: full combat logging (limits to 3 iterations) |
| `--inline-a <json>` | Inline JSON creature definition for creature A (duel) |
| `--inline-b <json>` | Inline JSON creature definition for creature B (duel) |
| `--vars <json>` | JSON array of inline creatures (variation-inline) |

## Combat System

The simulator faithfully replicates the game's combat mechanics:

### Attack Resolution (3-Phase)
1. **Attack Roll**: `2d6 + modifier` vs `2d6 + defense`
   - Melee: STR vs AGI + evasion
   - Ranged: AGI vs AGI + evasion
   - Ties favor attacker

2. **Damage Calculation**: `weapon_dice + STR (melee only) - armor`

3. **Damage Modifiers**:
   - Immune: 0 damage
   - Vulnerable: 2x damage
   - Resistant: 0.5x damage (floor)

### HP Formula
```
maxHP = baseHealth + (END² + 9×END) / 2
```

### Speed System
```
actionDelay = baseCost × (10 / speed)
minimumDelay = 6
```

### Regeneration (DCSS-style)
```
regenRate = 20 + (maxHP / 6) + regenBonus
100 points = 1 HP healed
```

### Willpower System
Creatures with skills use willpower (WP) to cast them:
```
maxWP = 10 + (WIL × 5)
```
WP regenerates like HP using the same DCSS-style accumulator system.

### Weapon Delay
Weapons affect action timing via their delay multiplier:
```
Fast weapons (delay: 0.7) = 7 action cost
Normal weapons (delay: 1.0) = 10 action cost
Slow weapons (delay: 1.3) = 13 action cost
```
Movement and skills use standard delay (10).

## Output Formats

### Console (default)
```
============================================================
  goblin vs skeleton (n=1000)
============================================================

  Win Rates:
    goblin: 82.1% ± 2.4%
    skeleton: 17.9% ± 2.4%
...
```

### JSON
```json
{
  "scenario": "goblin vs skeleton",
  "results": {
    "teamAWinRate": 0.821,
    "teamBWinRate": 0.179,
    "confidenceInterval95": 0.024
  }
}
```

### CSV
Includes all statistics for spreadsheet analysis.

## Verbose Debug Mode

Use `-v` or `--verbose` for detailed combat logging when debugging or troubleshooting:

```bash
npm run dev -- duel goblin skeleton -v
npm run dev -- group "goblin:2" "rat:3" -v
```

Verbose mode automatically limits iterations to 3 to prevent runaway output. It displays:

- **Combat initialization**: All combatant stats, equipment, skills, and positions
- **AI decision reasoning**: Why each creature chose its action
- **Dice roll breakdowns**: Attack/defense rolls with modifiers, damage calculations
- **Turn-by-turn state**: HP/WP changes, regeneration, movement, deaths
- **Combat summary**: Winner, survivors, and remaining HP

Example output:
```
=== FIGHT 1 ===
-- Combat Start --
Team A:
  Goblin
    HP: 10/10, WP: 10/10
    STR: 0, AGI: 1, END: 0, WIL: 0
    Speed: 10, Armor: 0, Evasion: 0
    Position: (0, 0)
    Attacks: Club (1d6 Bludgeoning)

-- Turn 1 --
[Goblin] Ready (accTime: 0, delay: 10)
  AI: Moving toward enemy (distance: 10 > attack range: 1)
  → Moves from (0, 0) to (1, 0)
  Action cost: 10 (speed 10)
...
```

## Development

```bash
npm run build    # Compile TypeScript
npm run dev      # Run with tsx (no build needed)
npm test         # Run tests
```
