/**
 * Regeneration - DCSS-style regeneration system.
 * Matches the C# HealthComponent regeneration implementation.
 */

import type { Combatant as CombatantState } from '../data/types.js';
import { isAlive } from './combatant.js';

// =============================================================================
// Constants
// =============================================================================

/**
 * Points required to heal 1 HP.
 */
export const REGEN_THRESHOLD = 100;

/**
 * Base regen rate before MaxHP scaling.
 */
export const BASE_REGEN_RATE = 20;

// =============================================================================
// Regeneration
// =============================================================================

/**
 * Calculate the regeneration rate for a combatant.
 * Formula: 20 + (maxHP / 6) + regenBonus
 */
export function calculateRegenRate(combatant: CombatantState): number {
  return BASE_REGEN_RATE + Math.floor(combatant.maxHealth / 6) + combatant.regenBonus;
}

/**
 * Process regeneration for a combatant.
 * Called after each action. Accumulates regen points and heals when threshold reached.
 * @returns The amount of HP healed (can be 0 or more).
 */
export function processRegeneration(combatant: CombatantState): number {
  // Don't regenerate if dead or at full health
  if (!isAlive(combatant) || combatant.currentHealth >= combatant.maxHealth) {
    // Reset regen points when at full health to avoid stockpiling
    combatant.regenPoints = 0;
    return 0;
  }

  // Accumulate regeneration points
  const regenRate = calculateRegenRate(combatant);
  combatant.regenPoints += regenRate;

  // Heal 1 HP for every 100 points accumulated
  let healed = 0;
  while (
    combatant.regenPoints >= REGEN_THRESHOLD &&
    combatant.currentHealth < combatant.maxHealth
  ) {
    combatant.currentHealth += 1;
    combatant.regenPoints -= REGEN_THRESHOLD;
    healed += 1;
  }

  return healed;
}

/**
 * Calculate approximate turns to heal 1 HP.
 */
export function turnsToHealOne(combatant: CombatantState): number {
  const regenRate = calculateRegenRate(combatant);
  if (regenRate <= 0) return Infinity;
  return Math.ceil(REGEN_THRESHOLD / regenRate);
}

/**
 * Calculate approximate turns to full heal.
 */
export function turnsToFullHeal(combatant: CombatantState): number {
  const missing = combatant.maxHealth - combatant.currentHealth;
  if (missing <= 0) return 0;
  return turnsToHealOne(combatant) * missing;
}
