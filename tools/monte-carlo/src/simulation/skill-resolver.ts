/**
 * SkillResolver - Handles skill resolution and damage calculation.
 * Processes effect steps to determine skill outcomes.
 */

import type {
  Combatant as CombatantState,
  SkillDefinition,
  DamageType,
} from '../data/types.js';
import { rollTotal, type RandomGenerator, defaultRng } from '../data/dice-notation.js';
import {
  getDamageModifier,
  applyDamageModifier,
} from './combat-resolver.js';

// =============================================================================
// Skill Result Types
// =============================================================================

export interface SkillResult {
  success: boolean;
  damage: number;
  damageBeforeModifiers: number;
  damageType: DamageType;
  modifier: 'none' | 'immune' | 'resistant' | 'vulnerable';
  skillName: string;
  attackerName: string;
  targetName: string;
}

// =============================================================================
// Stat Lookup
// =============================================================================

/**
 * Get a stat value from a combatant by stat name.
 */
function getStatValue(combatant: CombatantState, statName: string): number {
  switch (statName.toLowerCase()) {
    case 'str':
    case 'strength':
      return combatant.strength;
    case 'agi':
    case 'agility':
      return combatant.agility;
    case 'end':
    case 'endurance':
      return combatant.endurance;
    case 'wil':
    case 'will':
      return combatant.will;
    default:
      return 0;
  }
}

// =============================================================================
// Skill Resolution
// =============================================================================

/**
 * Resolve a skill's damage output.
 * Processes all effect steps and calculates total damage.
 *
 * For magic_missile: no attack roll, just damage with stat scaling.
 */
export function resolveSkill(
  attacker: CombatantState,
  target: CombatantState,
  skill: SkillDefinition,
  rng: RandomGenerator = defaultRng
): SkillResult {
  let totalDamage = 0;
  let damageType: DamageType = 'Bludgeoning'; // Default damage type

  // Process all effects and their steps
  for (const effect of skill.effects) {
    for (const step of effect.steps) {
      if (step.type === 'damage') {
        // Roll base damage
        let damage = 0;
        if (step.dice) {
          damage = rollTotal(step.dice, rng);
        }

        // Apply stat scaling (e.g., +WIL for magic_missile)
        if (step.scalingStat) {
          const statValue = getStatValue(attacker, step.scalingStat);
          const multiplier = step.scalingMultiplier ?? 1;
          damage += Math.floor(statValue * multiplier);
        }

        totalDamage += damage;

        // Use damage type from step if specified
        if (step.damageType) {
          damageType = step.damageType;
        }
      }
      // Note: attack_roll steps are not implemented yet
      // For magic_missile, there's no attack roll (always hits)
    }
  }

  // Apply damage type modifiers (resistance, vulnerability, immunity)
  const modifier = getDamageModifier(target, damageType);
  const finalDamage = applyDamageModifier(totalDamage, modifier);

  // Apply armor reduction
  const damageAfterArmor = Math.max(0, finalDamage - target.armor);

  return {
    success: true, // Skills like magic_missile always succeed
    damage: damageAfterArmor,
    damageBeforeModifiers: totalDamage,
    damageType,
    modifier,
    skillName: skill.name,
    attackerName: attacker.name,
    targetName: target.name,
  };
}
