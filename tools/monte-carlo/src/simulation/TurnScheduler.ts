/**
 * TurnScheduler - Energy-based turn ordering system.
 * Matches the C# SpeedComponent and TimeSystem implementation.
 */

import type { Combatant as CombatantState } from '../data/types.js';
import { weightedRound, type RandomGenerator, defaultRng } from '../data/DiceNotation.js';
import { isAlive } from './Combatant.js';

// =============================================================================
// Constants
// =============================================================================

export const AVERAGE_SPEED = 10;
export const MIN_SPEED = 1;
export const MIN_DELAY = 6;
export const STANDARD_ACTION_DELAY = 10;

// =============================================================================
// Speed System
// =============================================================================

/**
 * Calculate action delay based on speed.
 * Formula: baseCost × (10 / speed), with weighted random rounding.
 * Minimum delay is 6 (unless hasted, which we don't simulate).
 */
export function calculateDelay(
  speed: number,
  baseCost: number = STANDARD_ACTION_DELAY,
  rng: RandomGenerator = defaultRng
): number {
  const effectiveSpeed = Math.max(MIN_SPEED, speed);
  const rawDelay = baseCost * (AVERAGE_SPEED / effectiveSpeed);
  const delay = weightedRound(rawDelay, rng);
  return Math.max(MIN_DELAY, delay);
}

// =============================================================================
// Turn Scheduler
// =============================================================================

/**
 * Advance time for all combatants by the given amount.
 */
export function advanceTime(
  combatants: CombatantState[],
  amount: number
): void {
  for (const combatant of combatants) {
    if (isAlive(combatant)) {
      combatant.accumulatedTime += amount;
    }
  }
}

/**
 * Get the next combatant ready to act.
 * A combatant is ready when accumulatedTime >= their action delay.
 * Ties are broken by higher speed (faster acts first).
 */
export function getNextReady(
  combatants: CombatantState[],
  baseCost: number = STANDARD_ACTION_DELAY,
  rng: RandomGenerator = defaultRng
): CombatantState | null {
  let best: CombatantState | null = null;
  let bestSpeed = -1;

  for (const combatant of combatants) {
    if (!isAlive(combatant)) continue;

    const delay = calculateDelay(combatant.speed, baseCost, rng);
    if (combatant.accumulatedTime >= delay) {
      // Prefer faster combatants
      if (combatant.speed > bestSpeed) {
        best = combatant;
        bestSpeed = combatant.speed;
      }
    }
  }

  return best;
}

/**
 * Deduct action cost from a combatant's accumulated time.
 */
export function deductTime(
  combatant: CombatantState,
  baseCost: number = STANDARD_ACTION_DELAY,
  rng: RandomGenerator = defaultRng
): void {
  const delay = calculateDelay(combatant.speed, baseCost, rng);
  combatant.accumulatedTime -= delay;
}

/**
 * Simple turn-based scheduler for duels.
 * Returns combatants in turn order (cycles through until combat ends).
 */
export function* turnOrder(
  combatants: CombatantState[],
  rng: RandomGenerator = defaultRng
): Generator<CombatantState, void, void> {
  // Sort by speed (descending) for initial order
  const sorted = [...combatants].sort((a, b) => b.speed - a.speed);

  while (true) {
    // Give all combatants time
    advanceTime(sorted, STANDARD_ACTION_DELAY);

    // Process all ready combatants
    let ready = getNextReady(sorted, STANDARD_ACTION_DELAY, rng);
    while (ready) {
      yield ready;
      deductTime(ready, STANDARD_ACTION_DELAY, rng);

      // Check if combat should end
      const aliveTeams = new Set(
        sorted.filter(isAlive).map((c) => c.team)
      );
      if (aliveTeams.size <= 1) {
        return;
      }

      ready = getNextReady(sorted, STANDARD_ACTION_DELAY, rng);
    }
  }
}
