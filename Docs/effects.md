# Effects System

The effects system provides instantaneous and time-based modifications to entities through a data-driven, component-based architecture. Effects are cleanly separated into two complementary mechanisms: instantaneous effects (healing, damage, teleportation) and status effects (buffs, debuffs with duration).

## Core Architecture

**Dual Architecture**: The system supports both legacy monolithic effects and modern composed effects. New effects should use the composable step-based pipeline.

**Legacy Effects**: Stateless, pure-function style operations that apply modifications and return results. Each effect is a C# class implementing the `Apply(EffectContext)` method.

**Composed Effects**: Step-based pipelines defined in YAML. Effects are sequences of reusable steps (damage, save check, apply condition) that share state and accumulate messages.

**Effect Results**: Structured return values encapsulate success/failure, human-readable messages, and display colors.

**Action Context**: Provides effects with access to game systems (map, entities, combat) without tight coupling.

## Composable Effects Architecture

```
CompositeEffect
├── EffectState (mutable shared state between steps)
├── MessageCollector (accumulates messages for batch emission)
└── Steps: List<IEffectStep>
    ├── Prechecks (save_check, attack_roll)
    ├── Core Effects (damage, heal, apply_condition)
    └── Triggers (conditional on save failed, damage dealt)
```

### Core Infrastructure (`Scripts/Effects/Composition/`)

| Class | Purpose |
|-------|---------|
| `EffectState` | Mutable state passed between steps: Continue, Success, DamageDealt, SaveSucceeded/Failed, AttackHit/Missed |
| `MessageCollector` | Accumulates messages for batch emission via CombatSystem |
| `IEffectStep` | Interface: `Execute(context, state, messages)` |
| `CompositeEffect` | Pipeline effect that executes steps in sequence |
| `StepDefinition` | YAML step configuration (all step properties) |
| `CompositeEffectBuilder` | Factory that builds CompositeEffect from definitions |

### Standard Steps (`Scripts/Effects/Steps/`)

| Step | Purpose | Key Properties |
|------|---------|----------------|
| `SaveCheckStep` | Opposed save roll | saveStat, attackStat, saveModifier, stopOnSuccess, halfOnSuccess |
| `AttackRollStep` | Attack vs defense | attackStat, stopOnMiss |
| `DamageStep` | Deal damage | dice, amount, damageType, scalingStat, armorPiercing, halfOnSave |
| `HealStep` | Restore HP | dice, amount, percent, scalingStat |
| `HealCasterStep` | Life drain | fraction (e.g., 0.5) |
| `ApplyConditionStep` | Add condition | conditionType, duration, dotDamage, requireSaveFailed, requireDamageDealt |
| `KnockbackStep` | Push target | distance, scalingStat |

## YAML Schema for Composed Effects

### Basic Structure

```yaml
effects:
  - name: Effect Name
    sound: sound_id          # Maps to effect_sounds.yaml
    steps:
      - type: step_type
        # step-specific properties
```

### Example: Vampiric Touch

```yaml
effects:
  - name: Vampiric Touch
    steps:
      - type: damage
        dice: 2d6
        damageType: Necrotic
      - type: heal_caster
        fraction: 0.5
```

### Example: Acid Blast (with attack roll and conditional DoT)

```yaml
effects:
  - name: Acid Blast
    sound: acid_blast
    steps:
      - type: attack_roll
        attackStat: wil
      - type: damage
        dice: 2d6
        damageType: Acid
        armorPiercing: 3
      - type: apply_condition
        conditionType: acid
        durationDice: 2d3
        dotDamage: 1d3
        armorPiercing: 3
        requireDamageDealt: true
```

### Example: Magic Missile (scaling damage)

```yaml
effects:
  - name: Magic Missile
    sound: magic_missile
    steps:
      - type: damage
        dice: 1d6
        scalingStat: wil
        scalingMultiplier: 1.0
```

### Example: Acid Arrow (with save for half damage)

```yaml
effects:
  - name: Acid Arrow
    steps:
      - type: save_check
        saveStat: end
        attackStat: wil
        halfOnSuccess: true
      - type: damage
        dice: 1d6
        damageType: Acid
        halfOnSave: true
      - type: apply_condition
        conditionType: acid
        duration: 3
        dotDamage: 1d3
        requireSaveFailed: true
```

## Audio in YAML

Sounds are specified directly on effect definitions via the `sound:` field:
- The `effect_sounds.yaml` file maps sound IDs to file paths
- `CompositeEffect` plays the sound once at the start of execution
- Legacy effects that need sounds should be migrated to composed effects

## Effect Application Flow

**Legacy Path**: Effect → Apply(EffectContext) → EffectResult

**Composed Path**:
1. `CompositeEffect.Apply()` or `ApplyToTargets()` called
2. Sound plays (if specified)
3. Steps execute sequentially, sharing `EffectState`
4. Each step may modify state (DamageDealt, SaveSucceeded, etc.)
5. Steps check preconditions (requireSaveFailed, requireDamageDealt)
6. `MessageCollector.Emit()` sends all accumulated messages

**Factory Pattern**: `Effect.CreateFromDefinition()` checks for Steps first:
- If Steps present → `CompositeEffectBuilder.Build()`
- Otherwise → legacy type-based switch

## Adding New Effect Steps

1. Create class in `Scripts/Effects/Steps/` implementing `IEffectStep`
2. Implement `Execute(EffectContext context, EffectState state, MessageCollector messages)`
3. Register in `CompositeEffectBuilder.CreateStep()` switch statement
4. Add properties to `StepDefinition` if needed

### Example Step Implementation

```csharp
public class MyStep : IEffectStep
{
    private readonly int _amount;

    public MyStep(StepDefinition definition)
    {
        _amount = definition.Amount;
    }

    public void Execute(EffectContext context, EffectState state, MessageCollector messages)
    {
        // Check preconditions
        if (!state.Continue) return;

        // Do the effect
        // ...

        // Update state
        state.Success = true;

        // Add messages
        messages.Add("Something happened!", Palette.ToHex(Palette.Default));
    }
}
```

## Legacy Effects

Legacy effects remain supported for backwards compatibility. They can be gradually migrated to composed effects as needed.

**When to use legacy effects:**
- Complex spatial logic (FireballEffect, ConeOfColdEffect)
- Special targeting patterns (ChainLightningEffect)
- Effects with visual effect spawning logic

**When to use composed effects:**
- Simple damage/heal/condition sequences
- Effects that follow the precheck → effect → condition pattern
- New effects that don't need custom C# logic

## System Integration

**Item Integration**: `UseItemAction` retrieves effects from item data and calls `ApplyToTargets()`.

**Skill Integration**: `SkillExecutor` creates effects from skill definitions using the same factory.

**Component Integration**: Effects query targets for required components (HealthComponent, StatsComponent).

**Turn System Integration**: Conditions process automatically via entity turn signals.

## See Also

- [conditions.md](conditions.md) - Condition system architecture
- [components.md](components.md) - Component architecture and composition
- [actions.md](actions.md) - Action system integration
