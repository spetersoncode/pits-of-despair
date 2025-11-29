/**
 * AIController - Tactical AI for combat simulation.
 * Unified ranged/melee pools with weighted random selection.
 */

import type {
  Combatant as CombatantState,
  AttackDefinition,
  SkillDefinition,
  Position,
} from '../data/types.js';
import {
  isAlive,
  getMeleeAttack,
  getRangedAttack,
  distance,
  canAttack,
  canUseSkillOn,
  getUsableRangedSkills,
  getUsableMeleeSkills,
} from './combatant.js';
import { type RandomGenerator, defaultRng } from '../data/dice-notation.js';

// =============================================================================
// AI Decision Types
// =============================================================================

export type AIAction =
  | { type: 'attack'; target: CombatantState; attack: AttackDefinition }
  | { type: 'skill'; target: CombatantState; skill: SkillDefinition }
  | { type: 'move'; direction: Position }
  | { type: 'wait' };

// =============================================================================
// Combat Option Types
// =============================================================================

/**
 * A unified combat option that can be either an attack or a skill.
 */
type CombatOption =
  | { kind: 'attack'; attack: AttackDefinition; range: number }
  | { kind: 'skill'; skill: SkillDefinition; range: number };

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
// Combat Option Collection
// =============================================================================

/**
 * Get all usable ranged options (attacks + skills with range > 1).
 */
function getRangedOptions(combatant: CombatantState): CombatOption[] {
  const options: CombatOption[] = [];

  // Ranged weapon attacks (if has ammo)
  const rangedAttack = getRangedAttack(combatant);
  if (rangedAttack) {
    options.push({
      kind: 'attack',
      attack: rangedAttack,
      range: rangedAttack.range ?? 6,
    });
  }

  // Ranged skills (range > 1, active, targeting enemies, has WP)
  const rangedSkills = getUsableRangedSkills(combatant);
  for (const skill of rangedSkills) {
    options.push({
      kind: 'skill',
      skill,
      range: skill.range,
    });
  }

  return options;
}

/**
 * Get all usable melee options (attacks + skills with range <= 1).
 */
function getMeleeOptions(combatant: CombatantState): CombatOption[] {
  const options: CombatOption[] = [];

  // Melee weapon attacks (always available via getMeleeAttack fallback)
  const meleeAttack = getMeleeAttack(combatant);
  options.push({
    kind: 'attack',
    attack: meleeAttack,
    range: meleeAttack.range ?? 1,
  });

  // Melee skills (range <= 1, active, targeting enemies, has WP)
  const meleeSkills = getUsableMeleeSkills(combatant);
  for (const skill of meleeSkills) {
    options.push({
      kind: 'skill',
      skill,
      range: skill.range,
    });
  }

  return options;
}

/**
 * Pick a random option from a list (equal weighting).
 */
function pickRandomOption(
  options: CombatOption[],
  rng: RandomGenerator
): CombatOption {
  if (options.length === 1) {
    return options[0];
  }
  const index = Math.floor(rng.random() * options.length);
  return options[index];
}

/**
 * Check if a combat option can reach the target.
 */
function canReachTarget(
  combatant: CombatantState,
  target: CombatantState,
  option: CombatOption
): boolean {
  if (option.kind === 'attack') {
    return canAttack(combatant, target, option.attack);
  } else {
    return canUseSkillOn(combatant, target, option.skill);
  }
}

/**
 * Create an action from a combat option and target.
 */
function createActionFromOption(
  option: CombatOption,
  target: CombatantState
): AIAction {
  if (option.kind === 'attack') {
    return { type: 'attack', target, attack: option.attack };
  } else {
    return { type: 'skill', target, skill: option.skill };
  }
}

// =============================================================================
// AI Decision Making
// =============================================================================

/**
 * Decide action for a ranged combatant (has ranged options).
 * Priority: kite if adjacent > attack if in range > move toward
 */
function decideRangedBehavior(
  combatant: CombatantState,
  enemies: CombatantState[],
  rangedOptions: CombatOption[],
  rng: RandomGenerator
): AIAction {
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

  // Find the lowest HP enemy we can hit with any ranged option
  const lowestHP = getLowestHPEnemy(combatant, enemies);
  if (lowestHP) {
    // Filter options that can reach this target
    const viableOptions = rangedOptions.filter((opt) =>
      canReachTarget(combatant, lowestHP, opt)
    );

    if (viableOptions.length > 0) {
      // Randomly pick from viable options
      const chosen = pickRandomOption(viableOptions, rng);
      return createActionFromOption(chosen, lowestHP);
    }
  }

  // Can't hit lowest HP, try any enemy in range
  for (const enemy of enemies) {
    const viableOptions = rangedOptions.filter((opt) =>
      canReachTarget(combatant, enemy, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      return createActionFromOption(chosen, enemy);
    }
  }

  // No enemies in range, move toward nearest
  const direction = getMoveToward(combatant.position, nearest.position);
  return { type: 'move', direction };
}

/**
 * Decide action for a melee combatant (no ranged options or only melee).
 * Priority: attack if adjacent > move toward
 */
function decideMeleeBehavior(
  combatant: CombatantState,
  enemies: CombatantState[],
  meleeOptions: CombatOption[],
  rng: RandomGenerator
): AIAction {
  // Find the lowest HP enemy we can hit with any melee option
  const lowestHP = getLowestHPEnemy(combatant, enemies);
  if (lowestHP) {
    const viableOptions = meleeOptions.filter((opt) =>
      canReachTarget(combatant, lowestHP, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      return createActionFromOption(chosen, lowestHP);
    }
  }

  // Can't hit lowest HP, try any adjacent enemy
  for (const enemy of enemies) {
    const viableOptions = meleeOptions.filter((opt) =>
      canReachTarget(combatant, enemy, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      return createActionFromOption(chosen, enemy);
    }
  }

  // No enemies in range, move toward nearest
  const nearest = getNearestEnemy(combatant, enemies);
  if (nearest) {
    const direction = getMoveToward(combatant.position, nearest.position);
    return { type: 'move', direction };
  }

  return { type: 'wait' };
}

/**
 * Decide the best action for a combatant.
 * Uses unified ranged/melee pools with weighted random selection.
 */
export function decideAction(
  combatant: CombatantState,
  allCombatants: CombatantState[],
  rng: RandomGenerator = defaultRng
): AIAction {
  const enemies = getEnemies(combatant, allCombatants);
  if (enemies.length === 0) {
    return { type: 'wait' };
  }

  // Collect all combat options
  const rangedOptions = getRangedOptions(combatant);
  const meleeOptions = getMeleeOptions(combatant);

  // Priority order: ranged behavior > melee behavior
  // If we have ranged options, use ranged behavior (includes kiting)
  if (rangedOptions.length > 0) {
    return decideRangedBehavior(combatant, enemies, rangedOptions, rng);
  }

  // Otherwise, use melee behavior
  return decideMeleeBehavior(combatant, enemies, meleeOptions, rng);
}
