/**
 * CombatResolver - Handles attack resolution and damage calculation.
 * Implements the 3-phase combat system matching the C# implementation.
 */

import type {
  Combatant as CombatantState,
  AttackDefinition,
  DamageType,
} from '../data/types.js';
import { rollDice, rollTotal, type RandomGenerator, defaultRng } from '../data/dice-notation.js';
import { consumeAmmo } from './combatant.js';

// =============================================================================
// Attack Result Types
// =============================================================================

export interface AttackResult {
  hit: boolean;
  attackRoll: number;
  defenseRoll: number;
  damage: number;
  damageBeforeModifiers: number;
  damageType: DamageType;
  modifier: 'none' | 'immune' | 'resistant' | 'vulnerable';
  attackerName: string;
  targetName: string;
  attackName: string;
}

// =============================================================================
// Phase 1: Attack Roll
// =============================================================================

/**
 * Get the attack modifier for a combatant.
 * Melee: STR, Ranged: AGI
 */
export function getAttackModifier(
  attacker: CombatantState,
  isMelee: boolean
): number {
  return isMelee ? attacker.strength : attacker.agility;
}

/**
 * Get the defense modifier for a combatant.
 * AGI + evasion modifier
 */
export function getDefenseModifier(target: CombatantState): number {
  return target.agility + target.evasion;
}

/**
 * Perform an attack roll.
 * @returns true if attack hits (attacker >= defender, ties favor attacker)
 */
export function rollAttack(
  attacker: CombatantState,
  target: CombatantState,
  isMelee: boolean,
  rng: RandomGenerator = defaultRng
): { hit: boolean; attackRoll: number; defenseRoll: number } {
  const attackMod = getAttackModifier(attacker, isMelee);
  const defenseMod = getDefenseModifier(target);

  const attackRoll = rollDice(2, 6, attackMod, rng);
  const defenseRoll = rollDice(2, 6, defenseMod, rng);

  // Ties favor attacker
  const hit = attackRoll >= defenseRoll;

  return { hit, attackRoll, defenseRoll };
}

// =============================================================================
// Phase 2: Damage Calculation
// =============================================================================

/**
 * Get the damage bonus for a combatant.
 * Melee: STR, Ranged: 0
 */
export function getDamageBonus(
  attacker: CombatantState,
  isMelee: boolean
): number {
  return isMelee ? attacker.strength : 0;
}

/**
 * Calculate raw damage from an attack.
 * Formula: weapon_dice + damageBonus - armor, minimum 0
 */
export function calculateRawDamage(
  attacker: CombatantState,
  target: CombatantState,
  attack: AttackDefinition,
  rng: RandomGenerator = defaultRng
): number {
  const isMelee = attack.type === 'Melee';

  // Roll weapon damage
  const baseDamage = rollTotal(attack.dice, rng);

  // Add damage bonus
  const damageBonus = getDamageBonus(attacker, isMelee);

  // Subtract armor
  const armor = target.armor;

  // Calculate final damage (minimum 0)
  return Math.max(0, baseDamage + damageBonus - armor);
}

// =============================================================================
// Phase 3: Damage Modifiers
// =============================================================================

export type DamageModifier = 'none' | 'immune' | 'resistant' | 'vulnerable';

/**
 * Get the damage modifier for a target against a damage type.
 */
export function getDamageModifier(
  target: CombatantState,
  damageType: DamageType
): DamageModifier {
  if (target.immunities.has(damageType)) {
    return 'immune';
  }
  if (target.vulnerabilities.has(damageType)) {
    return 'vulnerable';
  }
  if (target.resistances.has(damageType)) {
    return 'resistant';
  }
  return 'none';
}

/**
 * Apply damage modifier to raw damage.
 * Immunity: 0x, Vulnerability: 2x, Resistance: 0.5x (floor)
 */
export function applyDamageModifier(
  damage: number,
  modifier: DamageModifier
): number {
  switch (modifier) {
    case 'immune':
      return 0;
    case 'vulnerable':
      return damage * 2;
    case 'resistant':
      return Math.floor(damage / 2);
    case 'none':
    default:
      return damage;
  }
}

/**
 * Calculate final damage including type modifiers.
 */
export function calculateFinalDamage(
  rawDamage: number,
  target: CombatantState,
  damageType: DamageType
): { damage: number; modifier: DamageModifier } {
  const modifier = getDamageModifier(target, damageType);
  const damage = applyDamageModifier(rawDamage, modifier);
  return { damage, modifier };
}

// =============================================================================
// Full Attack Resolution
// =============================================================================

/**
 * Resolve a complete attack from attacker to target.
 * Returns the result including whether it hit and damage dealt.
 */
export function resolveAttack(
  attacker: CombatantState,
  target: CombatantState,
  attack: AttackDefinition,
  rng: RandomGenerator = defaultRng
): AttackResult {
  const isMelee = attack.type === 'Melee';

  // Phase 1: Attack roll
  const { hit, attackRoll, defenseRoll } = rollAttack(
    attacker,
    target,
    isMelee,
    rng
  );

  if (!hit) {
    return {
      hit: false,
      attackRoll,
      defenseRoll,
      damage: 0,
      damageBeforeModifiers: 0,
      damageType: attack.damageType,
      modifier: 'none',
      attackerName: attacker.name,
      targetName: target.name,
      attackName: attack.name,
    };
  }

  // Phase 2: Damage calculation
  const rawDamage = calculateRawDamage(attacker, target, attack, rng);

  // Phase 3: Apply damage type modifiers
  const { damage, modifier } = calculateFinalDamage(
    rawDamage,
    target,
    attack.damageType
  );

  // Consume ammo for ranged attacks
  if (attack.type === 'Ranged') {
    consumeAmmo(attacker, attack);
  }

  return {
    hit: true,
    attackRoll,
    defenseRoll,
    damage,
    damageBeforeModifiers: rawDamage,
    damageType: attack.damageType,
    modifier,
    attackerName: attacker.name,
    targetName: target.name,
    attackName: attack.name,
  };
}

/**
 * Apply damage to a target combatant.
 * @returns The actual damage dealt (after capping at current health).
 */
export function applyDamage(
  target: CombatantState,
  damage: number
): number {
  const actualDamage = Math.min(damage, target.currentHealth);
  target.currentHealth -= actualDamage;
  return actualDamage;
}
