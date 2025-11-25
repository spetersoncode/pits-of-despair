/**
 * AIController - Basic tactical AI for combat simulation.
 * Simple heuristics without pathfinding (open arena assumption).
 */

import type {
  Combatant as CombatantState,
  AttackDefinition,
  Position,
} from '../data/types.js';
import {
  isAlive,
  hasMeleeAttack,
  hasRangedAttack,
  getMeleeAttack,
  getRangedAttack,
  distance,
  canAttack,
} from './Combatant.js';

// =============================================================================
// AI Decision Types
// =============================================================================

export type AIAction =
  | { type: 'attack'; target: CombatantState; attack: AttackDefinition }
  | { type: 'move'; direction: Position }
  | { type: 'wait' };

// =============================================================================
// Target Selection
// =============================================================================

/**
 * Get all living enemies of a combatant.
 */
export function getEnemies(
  combatant: CombatantState,
  allCombatants: CombatantState[]
): CombatantState[] {
  return allCombatants.filter(
    (c) => c.team !== combatant.team && isAlive(c)
  );
}

/**
 * Get the nearest enemy.
 */
export function getNearestEnemy(
  combatant: CombatantState,
  enemies: CombatantState[]
): CombatantState | null {
  if (enemies.length === 0) return null;

  let nearest = enemies[0];
  let nearestDist = distance(combatant.position, nearest.position);

  for (const enemy of enemies.slice(1)) {
    const dist = distance(combatant.position, enemy.position);
    if (dist < nearestDist) {
      nearest = enemy;
      nearestDist = dist;
    }
  }

  return nearest;
}

/**
 * Get the enemy with lowest HP (focus fire).
 * Tiebreaker: nearest.
 */
export function getLowestHPEnemy(
  combatant: CombatantState,
  enemies: CombatantState[]
): CombatantState | null {
  if (enemies.length === 0) return null;

  let best = enemies[0];
  let bestHP = best.currentHealth;
  let bestDist = distance(combatant.position, best.position);

  for (const enemy of enemies.slice(1)) {
    const hp = enemy.currentHealth;
    const dist = distance(combatant.position, enemy.position);

    if (hp < bestHP || (hp === bestHP && dist < bestDist)) {
      best = enemy;
      bestHP = hp;
      bestDist = dist;
    }
  }

  return best;
}

// =============================================================================
// Movement
// =============================================================================

/**
 * Get direction to move toward a target (Chebyshev movement).
 */
export function getMoveToward(from: Position, to: Position): Position {
  const dx = Math.sign(to.x - from.x);
  const dy = Math.sign(to.y - from.y);
  return { x: dx, y: dy };
}

/**
 * Get direction to move away from a target.
 */
export function getMoveAway(from: Position, to: Position): Position {
  const toward = getMoveToward(from, to);
  return { x: -toward.x, y: -toward.y };
}

/**
 * Apply movement to a combatant.
 */
export function applyMovement(
  combatant: CombatantState,
  direction: Position
): void {
  combatant.position.x += direction.x;
  combatant.position.y += direction.y;
}

// =============================================================================
// AI Decision Making
// =============================================================================

/**
 * Decide action for a melee combatant.
 * 1. If adjacent to enemy: attack lowest HP
 * 2. Else: move toward nearest enemy
 */
function decideMeleeAction(
  combatant: CombatantState,
  enemies: CombatantState[]
): AIAction {
  const meleeAttack = getMeleeAttack(combatant);
  if (!meleeAttack) {
    return { type: 'wait' };
  }

  // Find enemies we can attack
  const attackableEnemies = enemies.filter((e) =>
    canAttack(combatant, e, meleeAttack)
  );

  if (attackableEnemies.length > 0) {
    // Attack lowest HP enemy in range
    const target = getLowestHPEnemy(combatant, attackableEnemies);
    if (target) {
      return { type: 'attack', target, attack: meleeAttack };
    }
  }

  // Move toward nearest enemy
  const nearest = getNearestEnemy(combatant, enemies);
  if (nearest) {
    const direction = getMoveToward(combatant.position, nearest.position);
    return { type: 'move', direction };
  }

  return { type: 'wait' };
}

/**
 * Decide action for a ranged combatant.
 * 1. If enemy adjacent: move away (kite)
 * 2. If enemy in range: attack lowest HP
 * 3. Else: move to get enemy in range
 */
function decideRangedAction(
  combatant: CombatantState,
  enemies: CombatantState[]
): AIAction {
  const rangedAttack = getRangedAttack(combatant);
  if (!rangedAttack) {
    // Fall back to melee if no ranged attack available (out of ammo?)
    return decideMeleeAction(combatant, enemies);
  }

  const nearest = getNearestEnemy(combatant, enemies);
  if (!nearest) {
    return { type: 'wait' };
  }

  const dist = distance(combatant.position, nearest.position);

  // If enemy is adjacent (dist <= 1), kite away
  if (dist <= 1) {
    const direction = getMoveAway(combatant.position, nearest.position);
    return { type: 'move', direction };
  }

  // Find enemies we can attack at range
  const attackableEnemies = enemies.filter((e) =>
    canAttack(combatant, e, rangedAttack)
  );

  if (attackableEnemies.length > 0) {
    // Attack lowest HP enemy in range
    const target = getLowestHPEnemy(combatant, attackableEnemies);
    if (target) {
      return { type: 'attack', target, attack: rangedAttack };
    }
  }

  // Move toward nearest enemy to get in range
  const direction = getMoveToward(combatant.position, nearest.position);
  return { type: 'move', direction };
}

/**
 * Decide the best action for a combatant.
 */
export function decideAction(
  combatant: CombatantState,
  allCombatants: CombatantState[]
): AIAction {
  const enemies = getEnemies(combatant, allCombatants);
  if (enemies.length === 0) {
    return { type: 'wait' };
  }

  // Determine if this is a ranged or melee combatant
  // Prefer ranged if available and has ammo
  if (hasRangedAttack(combatant) && getRangedAttack(combatant)) {
    return decideRangedAction(combatant, enemies);
  }

  if (hasMeleeAttack(combatant)) {
    return decideMeleeAction(combatant, enemies);
  }

  // No attacks available - just move toward enemies
  const nearest = getNearestEnemy(combatant, enemies);
  if (nearest) {
    const direction = getMoveToward(combatant.position, nearest.position);
    return { type: 'move', direction };
  }

  return { type: 'wait' };
}
