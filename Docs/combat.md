# Combat System

The combat system resolves attacks through opposed dice rolls, damage calculation, and health management. Combat flows through a signal-driven pipeline decoupling action initiation from resolution.

## Combat Resolution Flow

Combat follows a three-phase resolution process:

**Phase 1 - Attack Roll**: Opposed 2d6 rolls determine hit/miss. Attacker rolls 2d6 plus attack modifier (STR for melee, AGI for ranged). Defender rolls 2d6 plus defense modifier (AGI + evasion penalty). Ties favor attacker.

**Phase 2 - Damage Calculation**: On hit, roll weapon dice notation for base damage. Add damage bonus (STR for melee only). Subtract target armor. Final damage = max(0, base + bonus - armor).

**Phase 3 - Outcome Application**: Apply final damage to HealthComponent if > 0. Emit appropriate signal (AttackHit, AttackBlocked, or AttackMissed).

## Attack Types

Three distinct attack categories with different mechanics:

**Melee**: Adjacent attacks with range = 1. Uses STR for attack roll and damage bonus. No line-of-sight required. Resolved via `AttackAction`.

**Reach**: Extended melee attacks with range > 1. Mechanically identical to melee (STR for attack/damage) but requires line-of-sight validation. Used by weapons like spears. Resolved via `ReachAttackAction`.

**Ranged**: Projectile attacks with range ≥ 3. Uses AGI for attack roll with no damage bonus. Requires line-of-sight. Spawns animated projectile via `ProjectileSystem`. Resolved via `RangedAttackAction`.

All range and line-of-sight calculations use Chebyshev distance for clean grid-based tactics.

## Attack Data Structure

Weapons define attacks via `AttackData` resource:

- **Name**: Display name for combat messages
- **Type**: `AttackType.Melee` or `AttackType.Ranged`
- **DiceNotation**: Damage formula (e.g., "1d6+2", "2d4", "5")
- **Range**: Attack distance in tiles (defaults to 1 for melee)

Equipment YAML specifies attack configuration. Natural attacks (unarmed) defined similarly for creatures without weapons.

## Combat Components

**HealthComponent**: Manages hit points and damage application. `MaxHP` calculated as `BaseMaxHP + (Endurance × Level)`. `TakeDamage()` reduces CurrentHP and emits signals. Emits `Died` when HP reaches 0, triggering entity removal.

**AttackComponent**: Interface between actions and combat resolution. Holds `Attacks` array (weapon or natural attacks). `RequestAttack(target, attackIndex)` emits signal to CombatSystem for processing. Updated automatically when equipment changes.

**StatsComponent**: Provides combat modifiers via multi-source tracking. `GetAttackModifier(isMelee)` returns STR/AGI. `GetDefenseModifier()` returns AGI + evasion penalty. `GetDamageBonus(isMelee)` returns STR for melee only. `TotalArmor` sums all armor sources.

## Signal-Driven Architecture

Combat uses signals to decouple action initiation from resolution:

```
AttackAction.Execute()
    ↓
AttackComponent.RequestAttack() [emits AttackRequested]
    ↓
CombatSystem.OnAttackRequested() [validates, rolls, calculates]
    ↓
CombatSystem emits AttackHit/AttackBlocked/AttackMissed
    ↓
HealthComponent.TakeDamage() [emits DamageTaken, Died]
    ↓
UI/MessageLog subscribe to signals
```

This enables independent UI updates, AI evaluation, and system testing without cross-dependencies.

## Combat Stats & Modifiers

**Strength (STR)**: Added to melee attack rolls and melee damage.

**Agility (AGI)**: Added to all defense rolls and ranged attack rolls. Reduced by armor evasion penalties.

**Endurance (END)**: Multiplied by level and added to BaseMaxHP.

**Armor**: Flat damage reduction from all sources.

**Evasion Penalty**: Negative modifier applied to defense rolls from heavy armor. Represents mobility restriction. Added as negative value to AGI in defense calculation.

Multi-source tracking uses unique source strings (e.g., "equipped_helmet", "strength_buff_guid") enabling add/remove operations without tracking all modifiers.

## Ranged Combat & Projectiles

Ranged attacks spawn animated projectiles for visual feedback. `RangedAttackAction.Execute()` emits `RangedAttackRequested` to player. `ProjectileSystem` spawns projectile entity and tweens position from origin to target. Projectile glyph updates direction during animation. On impact, projectile emits `ImpactReached`, triggering `AttackComponent.RequestAttack()` for damage resolution. Projectile removed after processing.

Projectiles are visual only—combat resolution happens on impact, not during flight.

## Distance Calculation

All combat targeting uses **Chebyshev distance** (max of absolute coordinate differences). Provides square-shaped ranges matching grid-based movement. Range checks: `distance <= weapon.Range`.

Line-of-sight for reach and ranged attacks uses `FOVCalculator` with Chebyshev distance, respecting walls and blocking tiles.

Creatures use Euclidean distance for vision/perception to provide realistic circular FOV.

## Action Integration

Combat actions implement two-phase validation pattern:

**CanExecute()**: Validates without side effects. Checks: actor has AttackComponent, target has HealthComponent, target alive, distance ≤ range, line-of-sight (for reach/ranged), valid attack index. Returns false to prevent turn consumption. Used by AI planning and UI feedback.

**Execute()**: Assumes validation passed. Calls `AttackComponent.RequestAttack()` emitting signal. Returns `ActionResult.CreateSuccess()` consuming turn. Actual combat resolution occurs asynchronously in CombatSystem.

Failed validation returns `ConsumesTurn = false` enabling free retries. Successful execution consumes turn regardless of hit/miss outcome.

## Turn Consumption Strategy

Actions consume turns on successful execution, not outcome:

- Validation failure: No turn consumed (free retry)
- Execution success: Turn consumed immediately
- Combat miss/block: Turn already consumed during Execute()

This prevents "fishing" for favorable RNG by treating attack attempts as committed actions.

## AI Combat Integration

`MeleeAttackGoal` evaluates player visibility and adjacency. Selects `AttackAction` with player as target when conditions met. Uses same validation and resolution pipeline as player actions. No special AI combat logic—unified system handles all attackers.

## Equipment & Attack Management

`EquipComponent` updates `AttackComponent.Attacks` when weapons equipped/unequipped. Equipped weapons replace natural attacks. Unequipping restores natural attacks (unarmed). Multi-source stat modifiers automatically add/remove armor, evasion penalties, and stat bonuses.

## Combat Feedback Signals

**CombatSystem Signals**:
- `AttackHit(attacker, target, damage, attackName)`: Successful damage dealt
- `AttackBlocked(attacker, target, attackName)`: Hit but armor absorbed all damage
- `AttackMissed(attacker, target, attackName)`: Attack roll failed
- `ActionMessage(actor, message, color)`: Generic combat messages

**HealthComponent Signals**:
- `HealthChanged(current, max)`: Any HP change
- `DamageTaken(amount)`: Damage applied
- `Died`: HP reached 0

**StatsComponent Signals**:
- `StatsChanged`: Any modifier change, triggers HP recalculation

MessageLog and UI components subscribe to these signals for player feedback without coupling to combat logic.

## Debug Logging

CombatSystem emits descriptive logs during resolution:

```
Combat: {Attacker} {AttackType} attack vs {Target}:
  Attack {Roll} (2d6+{Modifier}) vs Defense {Roll} (2d6+{Modifier})
Combat: HIT - Damage: {Base} ({Dice}) + {Bonus} bonus - {Armor} armor = {Final} final damage
```

Logs include both roll outcomes and damage breakdown for balance tuning and debugging.

## Design Decisions

**Opposed Rolls vs Fixed Defense**: Opposed 2d6 rolls create dynamic tension and emergent variance. Both attacker and defender roll, making each combat interaction uncertain.

**Flat Armor Reduction**: Simpler than percentage-based or damage resistance systems. Easier to balance and understand. Weak attacks can be completely negated by heavy armor.

**Damage Bonus Melee-Only**: Ranged attacks gain accuracy (AGI) without damage scaling. Encourages STR for melee builds, AGI for ranged builds.

**Turn Consumed on Execution**: Committed actions prevent save-scumming or RNG fishing. Missing is part of tactical risk.

## Adding New Attack Types

To create new attack mechanics beyond melee/reach/ranged:

1. Define new `AttackData` with appropriate Type, DiceNotation, and Range
2. Create action class implementing `Action` interface with combat-specific validation
3. In `CanExecute()`: Validate range, line-of-sight, and special requirements
4. In `Execute()`: Call `AttackComponent.RequestAttack()` or emit custom signal
5. If using custom resolution, subscribe to signal in CombatSystem and implement specialized logic
6. Consider whether to use existing damage formula or create new combat resolution path
7. Add UI support for targeting/feedback if needed (see `targeting.md`)

Most attacks should use existing pipeline via `RequestAttack()` for consistency.

---

*See also: [actions.md](actions.md) for action system integration, [statistics.md](statistics.md) for combat formula details, [targeting.md](targeting.md) for ranged/reach targeting UI, [components.md](components.md) for component patterns*
