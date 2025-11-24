# Visual Effects System

The visual effects system provides shader-based animated visuals for game events. Purely presentational—effects don't modify game state. Two parallel systems exist: projectiles (traveling visuals with game logic) and impact effects (stationary animations).

## Core Architecture

**Projectile System**: Manages traveling shader-based visuals from origin to target. Handles position interpolation, shader uniform updates, and impact callbacks. Projectiles can trigger game logic on impact (damage, AOE effects) or be purely visual.

**Visual Effect System**: Manages stationary shader-based animations at fixed positions. Handles radial effects (explosions, bursts) and beam effects (lines between two points). Effects animate via progress uniform (0→1) and clean up automatically on completion.

**Definition Pattern**: Both systems use data-driven definitions separating visual configuration from runtime state. Definitions specify shader paths, colors, timing, and parameters. Runtime data tracks position, progress, and shader node references.

**Shader Rendering**: All visuals render via GPU shaders on ColorRect nodes. Systems create shader materials, set uniforms from definitions, and update progress during animation. Nodes are children of TextRenderer for coordinate space consistency.

## Shader Organization

Shaders organized by visual category in `Resources/Shaders/`:

- **Projectiles/**: Traveling visuals (fireball, arrow, magic_missile, ice_shard, lightning_bolt, poison_bolt)
- **Impacts/**: Radial burst effects at target locations (fireball)
- **Beams/**: Line effects between two points (tunneling)
- **Auras/**: Persistent entity-attached effects (future)
- **Environment/**: Tile/world effects (future)

Naming convention: `{effect_name}.gdshader` within category folder. Same name can appear in multiple folders (Projectiles/fireball vs Impacts/fireball) as they serve different purposes.

## Projectile System

**Definition Properties**: ID, shader path, head color, trail color, speed (tiles/second), trail length, size multiplier, custom shader parameters.

**Runtime Flow**: Spawn creates ProjectileData with definition reference → creates ColorRect with shader material → sets uniforms from definition → animates via tween (progress 0→1) → updates position and shader each frame → executes impact callback → cleans up shader node.

**Impact Types**: Attack projectiles apply weapon damage via AttackComponent. Skill projectiles apply deferred effects. Callback projectiles execute custom logic (AOE explosions). Visual projectiles have no impact behavior.

**Shader Interface**: All projectile shaders expect uniforms: `progress` (0-1), `head_color`, `trail_color`, `size`, `trail_length`, `direction` (vec2). Shaders handle their own visual style—flames, crystals, lightning, etc.

## Visual Effect System

**Definition Properties**: ID, effect type (Explosion/Beam), shader path, duration, inner/mid/outer colors, custom shader parameters.

**Runtime Flow**: Spawn determines effect type from definition → creates appropriate shader node → sets uniforms → animates progress → cleans up on completion → emits AllEffectsCompleted signal when empty.

**Effect Types**: Radial effects (explosions) render centered on position with configurable radius. Beam effects render rotated line from origin to target with calculated length and angle.

**Shader Interface**: Impact shaders expect `progress`, `radius`, `inner_color`, `mid_color`, `outer_color`. Beam shaders expect `progress`, `beam_length`, `beam_width`, `core_color`, `mid_color`, `outer_color`.

## System Integration

**Game Effects → Projectiles**: FireballEffect spawns projectile with callback. On impact, callback triggers damage application and spawns impact visual effect. Projectile handles travel animation; effect system handles explosion.

**Turn System**: Both systems emit completion signals. TurnManager waits for AllProjectilesCompleted and AllEffectsCompleted before advancing turn. Ensures animations complete before next action.

**Coordinate System**: Both systems use TextRenderer methods for position conversion. Grid positions convert to screen coordinates accounting for camera offset. Shader nodes positioned relative to current render offset.

## Adding New Projectile Types

**Create Shader**: Add `{name}.gdshader` to `Resources/Shaders/Projectiles/`. Implement fragment function using standard uniforms. Consider reusing similar shaders with color variations.

**Register Definition**: Add static ProjectileDefinition to ProjectileDefinitions class. Specify shader path, colors from Palette, speed, trail length, size. Add case to GetById switch.

**Usage**: Reference definition when spawning projectiles via SpawnSkillProjectile, SpawnAttackProjectile, or SpawnProjectileWithCallback.

## Adding New Impact Effects

**Create Shader**: Add `{name}.gdshader` to appropriate folder (Impacts/ for radial, Beams/ for linear). Implement fragment function with appropriate uniforms.

**Register Definition**: Add static VisualEffectDefinition to VisualEffectDefinitions. Specify type (Explosion/Beam), shader path, duration, colors. Add case to GetById and yield to GetAll.

**Convenience Method**: Optionally add named spawn method to VisualEffectSystem (SpawnFireball, SpawnTunneling pattern).

## Design Patterns

**Definition/Data Split**: Definitions are immutable configuration shared across instances. Data is mutable runtime state. Enables data-driven content without per-instance overhead.

**Shader Caching**: Both systems cache loaded shaders by path. Prevents redundant resource loading when spawning multiple effects of same type.

**Tween Animation**: Progress animated via Godot tween system. Provides consistent timing without manual delta tracking. Callbacks fire on completion.

**Signal Completion**: Systems emit signals when all active effects complete. Enables turn system coordination without polling.

## Design Considerations

**Separation of Concerns**: Projectiles have game logic (damage, callbacks). Visual effects are purely presentational. Systems remain separate despite similar rendering approach. Future unification possible via shared delivery abstraction.

**Shader Flexibility**: Each projectile/effect type can have completely custom shader. No constraint on visual style. Trade-off: more shaders to maintain but maximum visual variety.

**Performance**: ColorRect per active effect/projectile. Shaders run on GPU. No pooling currently—nodes created and destroyed per effect. Acceptable for roguelike turn-based context.

---

*See [effects.md](effects.md) for game effects triggering visuals, [actions.md](actions.md) for action system integration.*
