# Statistics System

The statistics system manages creature attributes and combat modifiers through multi-source tracking. Stats influence combat resolution via opposed 2d6 rolls and damage calculations.

## Core Architecture

**Multi-Source Modifiers**: Stats track base values and modifiers from multiple sources (equipment, status effects, buffs). Each source identified by unique string key; sources stack additively. Total stat = base + sum of all modifiers.

**Signal-Based Updates**: `StatsChanged` signal emitted on any modification. UI and dependent systems (health) subscribe for reactive updates without tight coupling.

**Stat Categories**: Four primary attributes (STR/AGI/END/WIL), two defensive values (Armor/Evasion Penalty), and derived combat values calculated on-demand.

## Primary Attributes

**Strength (STR)**: Melee combat effectiveness. Adds to melee attack rolls (2d6+STR vs 2d6+AGI) and melee damage output (weapon dice + STR).

**Agility (AGI)**: Ranged combat and defense. Adds to ranged attack rolls and all defense rolls (2d6+AGI). Higher AGI increases hit chance with ranged weapons and ability to evade incoming attacks.

**Endurance (END)**: Hit point capacity. Quadratic scaling formula: `MaxHP = BaseMaxHP + (END² + 9×END) / 2`. Provides increasing marginal returns (each END point grants more HP than the last). Negative END floors at BaseMaxHP. Level no longer affects HP scaling.

**Will (WIL)**: Reserved for future magic/ability systems. Currently tracked but unused in game mechanics.

## Combat Values

**Armor**: Flat damage reduction applied after successful hits. Multi-source (equipment, buffs). Final damage: `max(0, BaseDamage + DamageBonus - Armor)`.

**Evasion Penalty**: Negative modifier applied to defense rolls. Primary source: heavy armor restricting movement. Defense calculation: `2d6 + AGI + EvasionPenalty` (penalty typically negative).

**Attack Modifiers**: Melee uses STR, ranged uses AGI. Retrieved via `GetAttackModifier(isMelee)` for combat roll calculations.

**Damage Bonus**: Melee adds STR to weapon damage; ranged uses weapon dice only. Retrieved via `GetDamageBonus(isMelee)`.

## Combat Resolution

**Phase 1 - Attack Roll**: Attacker rolls 2d6 + attack stat (STR/AGI based on weapon). Defender rolls 2d6 + AGI + Evasion Penalty. Hit if attacker roll ≥ defender roll (ties favor attacker).

**Phase 2 - Damage**: On hit, roll weapon damage dice. Add damage bonus (STR for melee only). Subtract target Armor. Minimum 0 damage.

**Phase 3 - Outcome**: Deal damage if >0 (emit `AttackHit`). If 0 after armor, emit `AttackBlocked`. If attack roll failed, emit `AttackMissed`.

## Multi-Source Tracking

Each stat type maintains dictionary of modifiers keyed by source string. Sources represent origins: `"equipped_sword"`, `"strength_potion"`, `"curse_debuff"`. Adding modifier with existing source key replaces previous value from that source. Removing source eliminates its contribution.

**Example**: Equipping +2 STR ring adds `"ring_slot"` source. Swapping to +3 STR ring updates same source. Unequipping removes `"ring_slot"` entirely.

**Stat Change Flow**: Modifier added/removed → Dictionary updated → `StatsChanged` signal emitted → UI refreshes, HealthComponent recalculates MaxHP, combat systems use updated values on next calculation.

## Modifier Sources

**Equipment**: Primary source via equip/unequip actions. Weapons, armor, accessories contribute stats using unique slot/item identifiers.

**Status Effects**: Temporary buffs/debuffs (StrengthBuffStatus, AgilityBuffStatus, etc.). Applied on status activation, removed on expiration via GUID-based source tracking.

**Level-Based**: Level increases trigger `StatsChanged` for dependent recalculations. Note: Level no longer affects HP directly (see quadratic END formula above).

## See Also

- [progression.md](progression.md) - XP and leveling system
- [combat.md](combat.md) - Combat resolution
