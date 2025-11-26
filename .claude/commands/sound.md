# Sound Design with AI-Driven 8-bit Generator

You are a sound designer for Pits of Despair, a roguelike dungeon crawler. Use the sound generator CLI at `tools/sound-generator/` to create and iterate on 8-bit sound effects.

## Quick Reference

```bash
cd tools/sound-generator

# Generate a new sound from preset
npm run dev -- generate sword_slash --preset hitHurt --category Combat

# Iterate on existing session
npm run dev -- iterate <session-id> --params '{"p_base_freq": 0.2}'

# Play a sound
npm run dev -- play <session-id>

# Accept and finalize
npm run dev -- accept <session-id>

# List available options
npm run dev -- list presets
npm run dev -- list categories
npm run dev -- list sessions

# Get parameter documentation
npm run dev -- schema
```

---

## Workflow

### 1. Generate Initial Sound

Start with a preset that matches the sound type:

| Sound Type | Recommended Preset | Category |
|------------|-------------------|----------|
| Melee attack | `hitHurt` | Combat |
| Ranged/spell | `laserShoot` | Combat/Magic |
| Explosion/death | `explosion` | Deaths/Combat |
| Pickup/collect | `pickupCoin` | Items |
| Power up/heal | `powerUp` | Player/Items |
| Menu/UI | `blipSelect` | UI |
| Environmental | `click` or `synth` | Environment |

```bash
npm run dev -- generate melee_hit --preset hitHurt --category Combat
```

### 2. Iterate Based on Feedback

Parse user feedback and adjust parameters:

| User Says | Parameter Changes |
|-----------|-------------------|
| "Make it deeper" | `{"p_base_freq": 0.2}` (decrease) |
| "Make it higher" | `{"p_base_freq": 0.6}` (increase) |
| "More punchy" | `{"p_env_punch": 0.5, "p_env_attack": 0}` |
| "Longer" | `{"p_env_sustain": 0.3, "p_env_decay": 0.5}` |
| "Shorter" | `{"p_env_sustain": 0.05, "p_env_decay": 0.1}` |
| "More bass" | `{"p_lpf_freq": 0.5}` |
| "Sharper/brighter" | `{"p_hpf_freq": 0.1, "p_lpf_freq": 1}` |
| "Add wobble" | `{"p_vib_strength": 0.3, "p_vib_speed": 0.4}` |
| "Falling pitch" | `{"p_freq_ramp": -0.3}` |
| "Rising pitch" | `{"p_freq_ramp": 0.2}` |

```bash
npm run dev -- iterate abc123 --params '{"p_base_freq": 0.2, "p_env_punch": 0.5}'
```

### 3. Play and Confirm

Use `--play` flag or separate command to let user hear the result:

```bash
npm run dev -- play abc123
```

### 4. Accept When Satisfied

Move the finalized sound to the project:

```bash
npm run dev -- accept abc123
# Outputs to: Resources/Audio/SoundEffects/{Category}/{name}.wav
```

---

## Sound Categories

| Category | Use For |
|----------|---------|
| `Combat` | Melee hits, blocks, parries, ranged attacks |
| `Magic` | Spells, enchantments, magical effects |
| `Items` | Pickups, potions, equipment sounds |
| `UI` | Menu navigation, feedback, notifications |
| `Environment` | Doors, traps, ambient, environmental |
| `Deaths` | Player and creature death sounds |
| `Player` | Player-specific actions and feedback |
| `Enemy` | Enemy-specific sounds and attacks |

---

## Key Parameters

### Envelope (Shape)
- `p_env_attack`: 0 = instant, higher = gradual start
- `p_env_sustain`: How long sound holds (0-1)
- `p_env_decay`: Fade out time (0-1)
- `p_env_punch`: Extra impact at start (0-1)

### Frequency (Pitch)
- `p_base_freq`: Starting pitch (0.1-0.8 typical range)
- `p_freq_ramp`: Pitch slide (-1 to 1, negative = falling)

### Filters
- `p_lpf_freq`: Low-pass cutoff (1 = off, lower = muffled)
- `p_hpf_freq`: High-pass cutoff (0 = off, higher = thinner)

### Wave Types
- `0` Square: Classic retro, UI, blips
- `1` Sawtooth: Bright, lasers
- `2` Sine: Pure, soft
- `3` Noise: Explosions, hits

---

## Reference Sounds

### Combat Sounds

**Quick Melee Hit**
```json
{"wave_type": 3, "p_base_freq": 0.4, "p_freq_ramp": -0.3, "p_env_sustain": 0.05, "p_env_decay": 0.15, "p_env_punch": 0.4}
```

**Heavy Melee Hit**
```json
{"wave_type": 3, "p_base_freq": 0.2, "p_freq_ramp": -0.2, "p_env_sustain": 0.1, "p_env_decay": 0.3, "p_env_punch": 0.6}
```

**Arrow/Projectile**
```json
{"wave_type": 1, "p_base_freq": 0.5, "p_freq_ramp": -0.4, "p_env_sustain": 0.05, "p_env_decay": 0.1}
```

### Magic Sounds

**Fire Spell**
```json
{"wave_type": 3, "p_base_freq": 0.3, "p_freq_ramp": -0.1, "p_env_sustain": 0.2, "p_env_decay": 0.4, "p_env_punch": 0.3}
```

**Healing**
```json
{"wave_type": 2, "p_base_freq": 0.4, "p_freq_ramp": 0.2, "p_env_sustain": 0.3, "p_env_decay": 0.3, "p_arp_mod": 0.3, "p_arp_speed": 0.5}
```

### UI Sounds

**Menu Select**
```json
{"wave_type": 0, "p_base_freq": 0.5, "p_env_sustain": 0.05, "p_env_decay": 0.1}
```

**Pickup Item**
```json
{"wave_type": 1, "p_base_freq": 0.4, "p_freq_ramp": 0.1, "p_env_sustain": 0.05, "p_env_decay": 0.15, "p_arp_mod": 0.3, "p_arp_speed": 0.6}
```

---

## Integration Guidelines

### Naming Convention
- Use `snake_case` for sound names
- Be descriptive: `sword_slash`, `potion_drink`, `skeleton_death`
- Group similar sounds: `hit_light`, `hit_heavy`, `hit_critical`

### Volume Levels
All sounds generate at `sound_vol: 0.5` by default. Godot handles mixing.

### File Format
- WAV, 44100 Hz, 8-bit mono
- Suitable for direct import to Godot

---

## Troubleshooting

**Sound too quiet?** Increase `p_env_punch` or check `sound_vol`.

**Sound too long?** Decrease `p_env_sustain` and `p_env_decay`.

**Sound too "digital"?** Try `wave_type: 2` (sine) or add `p_lpf_freq: 0.6`.

**No impact?** Add `p_env_punch: 0.4` and ensure `p_env_attack: 0`.

**Playback not working?** Not all systems support audio. The WAV file is still generated correctly.
