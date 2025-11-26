# Sound Generator

AI-driven 8-bit sound effect generator for Pits of Despair, built on jsfxr.

## Quick Start

```bash
cd tools/sound-generator
npm install
npm run dev -- generate test_sound --preset hitHurt --category Combat
```

## Commands

| Command | Description |
|---------|-------------|
| `generate <name>` | Create a new sound from preset |
| `iterate <session-id>` | Modify existing sound parameters |
| `play <session-id>` | Play the working sound |
| `accept <session-id>` | Finalize and move to project |
| `list presets` | Show available jsfxr presets |
| `list categories` | Show sound categories |
| `list sessions` | Show active working sessions |
| `schema` | Show parameter documentation |
| `info <session-id>` | Show session details |

## Workflow

1. **Generate** - Create initial sound from a preset
2. **Iterate** - Adjust parameters based on feedback
3. **Play** - Preview the sound
4. **Accept** - Move to `Resources/Audio/SoundEffects/{Category}/`

## Options

### Generate
- `-p, --preset <preset>` - jsfxr preset (default: random)
- `-c, --category <category>` - Sound category (default: Combat)
- `--params <json>` - Parameter overrides
- `--play` - Play after generating

### Iterate
- `--params <json>` - Parameters to modify
- `--play` - Play after iterating

### Accept
- `--force` - Overwrite existing file

## Presets

| Preset | Best For |
|--------|----------|
| `hitHurt` | Melee attacks, damage |
| `laserShoot` | Ranged attacks, spells |
| `explosion` | Deaths, heavy impacts |
| `pickupCoin` | Item pickups |
| `powerUp` | Buffs, healing |
| `blipSelect` | UI interactions |
| `synth` | Ambient, atmosphere |
| `click` | Footsteps, mechanical |
| `random` | Experimentation |

## Categories

- `Combat` - Melee, ranged, blocks
- `Magic` - Spells, enchantments
- `Items` - Pickups, equipment
- `UI` - Menu, feedback
- `Environment` - Doors, traps
- `Deaths` - Death sounds
- `Player` - Player actions
- `Enemy` - Enemy sounds

## Output Format

All commands output JSON for AI parsing. Success responses include `next_actions` with suggested commands.

## Development

```bash
npm run dev -- <command>   # Run with tsx
npm run build              # Compile TypeScript
npm start                  # Run compiled version
npm test                   # Run tests
```

## See Also

- `.claude/commands/sound.md` - AI guidance for sound design
- `tools/sound-generator/src/data/schemas.ts` - Full parameter documentation
