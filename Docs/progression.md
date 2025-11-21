# Progression System

The progression system manages player advancement through experience points, leveling, and stat growth. Designed for roguelike pacing with meaningful choices at each level-up.

## Experience Points

**XP Formula**: Cumulative XP threshold for each level: `50 × (level - 1) × level`. Quadratic progression creates increasing gaps between levels.

| Level | Cumulative XP | Gap from Previous |
|-------|---------------|-------------------|
| 1     | 0             | —                 |
| 2     | 100           | 100               |
| 3     | 300           | 200               |
| 4     | 600           | 300               |
| 5     | 1000          | 400               |
| 10    | 4500          | 900               |

**Delta-Based Rewards**: Creatures award XP based on their defined XP value in creature YAML. Higher-level creatures grant more XP. XP values tuned per creature type to reflect threat and challenge.

**XP Tracking**: Current XP tracks progress toward next level. On level-up, threshold subtracted from current XP (overflow preserved). Large XP gains can trigger multiple consecutive level-ups.

## Level-Up System

**Signal Flow**: `StatsComponent.GainExperience()` → threshold check → `LevelUp` signal emitted → `LevelUpSystem` receives signal → modal UI displayed → player selects stat → base stat increased.

**Stat Selection**: Each level-up grants +1 to one primary stat (STR, AGI, END, or WIL). Modal UI presents all four options with current values. Player must choose immediately—no banking or deferring. Selection applies via base stat increase, triggering `StatsChanged` signal cascade.

**UI Presentation**: LevelUpModal blocks gameplay until selection made. Displays current stat values and explains each stat's purpose. Keybindings (S/A/E/W) provide quick selection. Modal closes after selection, game resumes.

## Stat Growth Impact

**Strength**: +1 melee attack accuracy, +1 melee damage per point.

**Agility**: +1 ranged attack accuracy, +1 defense (evasion) per point.

**Endurance**: Quadratic HP scaling. Each point grants more HP than the last. See [statistics.md](statistics.md) for formula.

**Will**: Reserved for future magic systems. Currently tracked but unused.

## Signals

**ExperienceGained(amount, current, toNext)**: Emitted on any XP gain. UI updates XP display. Parameters: amount gained, current XP after gain, XP needed for next level.

**LevelUp(newLevel)**: Emitted when level threshold crossed. Triggers level-up modal. Parameter: new level reached.

**StatsChanged**: Emitted after stat selection. Cascades to HealthComponent (HP recalculation), UI (stat display refresh), and any systems depending on stats.

## Design Decisions

**Immediate Choice**: No point banking prevents analysis paralysis and ensures character builds evolve organically through play.

**Four-Stat Simplicity**: Limited options make each choice meaningful. No dump stats—all four have clear value propositions.

**Quadratic XP Curve**: Early levels come quickly for engagement. Later levels require more investment, extending endgame.

**No Level Cap**: System supports indefinite progression. Balance maintained through spawn table difficulty scaling rather than artificial caps.

## See Also

- [statistics.md](statistics.md) - Stat mechanics and combat integration
