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

### Matrix
Run all creatures against each other:
```bash
npm run dev -- matrix -n 500 -o csv --outfile results
```

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

## Development

```bash
npm run build    # Compile TypeScript
npm run dev      # Run with tsx (no build needed)
npm test         # Run tests
```
