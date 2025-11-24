# Visual Effects System

The visual effects system provides shader-based animated visuals for game events. A unified system handles all visual effects: stationary effects (explosions, beams) and moving effects (projectiles). Effects are purely visual—game logic is triggered via completion callbacks.

## Core Architecture

**VisualEffectSystem**: Single system managing all shader-based visuals. Handles stationary effects (radial explosions, beams) and moving effects (projectiles from origin to target). Effects animate via progress uniform (0→1), execute optional completion callbacks, and clean up automatically.

**Definition Pattern**: Data-driven definitions separate visual configuration from runtime state. VisualEffectDefinition specifies shader paths, colors, timing, speed, and parameters. VisualEffectData tracks position, progress, and shader node references.

**Effect Types** (VisualEffectType enum):
- **Explosion**: Radial effects centered at a position (fireball impact)
- **Beam**: Line effects between two points (tunneling)
- **Projectile**: Moving effects from origin to target (fireballs, arrows, magic missiles)

**Shader Rendering**: All visuals render via GPU shaders on ColorRect nodes. System creates shader materials, sets uniforms from definitions, and updates progress during animation. Nodes are children of TextRenderer for coordinate space consistency.

## Shader Organization

Shaders organized by visual category in `Resources/Shaders/`:

- **Projectiles/**: Moving visuals (fireball, arrow, magic_missile, ice_shard, lightning_bolt, poison_bolt)
- **Impacts/**: Radial burst effects at target locations (fireball)
- **Beams/**: Line effects between two points (tunneling)
- **Auras/**: Persistent entity-attached effects (future)
- **Environment/**: Tile/world effects (future)

Naming convention: `{effect_name}.gdshader` within category folder.

## Projectile Effects

**Definition Properties**: ID, shader path, head color, trail color, speed (tiles/second), trail length, size multiplier, custom shader parameters. Created via projectile-specific constructor.

**Runtime Flow**: `SpawnProjectile()` creates VisualEffectData with definition → creates ColorRect with shader material → sets uniforms → calculates duration from distance/speed → animates via tween (progress 0→1) → updates position each frame → executes completion callback → cleans up shader node.

**Completion Callbacks**: Callers provide `Action onComplete` to execute game logic when projectile arrives. Common patterns:
- Ranged attacks: Request attack via AttackComponent
- Skill projectiles: Apply effect and emit damage signal
- AOE projectiles: Apply area damage and spawn impact visual

**Shader Interface**: Projectile shaders expect uniforms: `progress` (0-1), `head_color`, `trail_color`, `size`, `trail_length`, `direction` (vec2).

**Predefined Projectiles** (in VisualEffectDefinitions):
- Physical: Arrow, Bolt
- Fire: FireballProjectile, FireBolt
- Ice: IceShard, FrostBolt
- Lightning: LightningBolt, Spark
- Poison/Acid: PoisonBolt, AcidSplash
- Arcane: MagicMissile, DarkBolt

## Stationary Effects

**Definition Properties**: ID, effect type (Explosion/Beam), shader path, duration, inner/mid/outer colors, custom shader parameters.

**Runtime Flow**: `SpawnEffect()` determines effect type → creates appropriate shader node → sets uniforms → animates progress → cleans up on completion → emits AllEffectsCompleted signal when queue empty.

**Radial Effects**: Render centered on position with configurable radius. Use inner/mid/outer color scheme.

**Beam Effects**: Render rotated line from origin to target. Calculate length and rotation from positions.

**Shader Interface**: Impact shaders expect `progress`, `radius`, `inner_color`, `mid_color`, `outer_color`. Beam shaders expect `progress`, `beam_length`, `beam_width`, `core_color`, `mid_color`, `outer_color`.

## System Integration

**ActionContext**: VisualEffectSystem available via context for actions and effects to spawn visuals.

**Turn System**: TurnManager connects to `AllEffectsCompleted` signal. Waits for all effects (including projectiles) to complete before advancing turn. Ensures animations finish before next action.

**Combat System**: Skill damage emitted via `CombatSystem.EmitSkillDamageDealt()` for message log display. Called from completion callbacks.

**Coordinate System**: Uses TextRenderer methods for position conversion. Grid positions convert to screen coordinates accounting for camera offset.

## Adding New Projectile Types

**Create Shader**: Add `{name}.gdshader` to `Resources/Shaders/Projectiles/`. Implement fragment function using standard uniforms (progress, head_color, trail_color, etc.).

**Register Definition**: Add static VisualEffectDefinition to VisualEffectDefinitions using projectile constructor:
```csharp
public static readonly VisualEffectDefinition MyProjectile = new(
    id: "my_projectile",
    shaderPath: MyProjectileShader,
    headColor: Palette.Fire,
    trailColor: WithAlpha(Palette.Fire, 0.4f),
    speed: 25.0f,
    trailLength: 3,
    size: 1.0f
);
```

**Add to GetById/GetAll**: Add case to switch statement and yield statement.

**Usage**: Call `VisualEffectSystem.SpawnProjectile(definition, origin, target, onComplete)`.

## Adding New Impact Effects

**Create Shader**: Add `{name}.gdshader` to appropriate folder (Impacts/ for radial, Beams/ for linear).

**Register Definition**: Add static VisualEffectDefinition using stationary constructor:
```csharp
public static readonly VisualEffectDefinition MyEffect = new(
    id: "my_effect",
    type: VisualEffectType.Explosion,
    shaderPath: MyEffectShader,
    duration: 0.5f,
    innerColor: Palette.Fire,
    midColor: Palette.Fire.Darkened(0.3f),
    outerColor: Palette.Fire.Darkened(0.6f)
);
```

**Convenience Method**: Optionally add named spawn method (SpawnMyEffect pattern).

## Design Patterns

**Unified System**: Single VisualEffectSystem handles all visual effects. Projectiles are just another effect type with movement behavior. Simplifies turn coordination and reduces code duplication.

**Definition/Data Split**: Definitions are immutable configuration shared across instances. Data is mutable runtime state. Enables data-driven content without per-instance overhead.

**Completion Callbacks**: Game logic triggered via callbacks, not embedded in VFX system. System remains purely visual; callers handle damage, effects, messages.

**Shader Caching**: System caches loaded shaders by path. Prevents redundant resource loading when spawning multiple effects of same type.

**Tween Animation**: Progress animated via Godot tween system. Provides consistent timing without manual delta tracking.

**Signal Completion**: AllEffectsCompleted signal emitted when all active effects finish. Enables turn system coordination without polling.

## Design Considerations

**Separation of Game Logic**: Visual effects are purely presentational. All game logic (damage, effects) executed via completion callbacks. This keeps the VFX system simple and reusable.

**Shader Flexibility**: Each effect type can have completely custom shader. No constraint on visual style. Trade-off: more shaders to maintain but maximum visual variety.

**Performance**: ColorRect per active effect. Shaders run on GPU. No pooling currently—nodes created and destroyed per effect. Acceptable for roguelike turn-based context.

---

*See [effects.md](effects.md) for game effects triggering visuals, [actions.md](actions.md) for action system integration.*
