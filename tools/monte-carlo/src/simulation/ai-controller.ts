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

export interface AIDecisionResult {
  action: AIAction;
  reasoning: string;
}

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
 * Get the name of a combat option for logging.
 */
function getOptionName(option: CombatOption): string {
  return option.kind === 'attack' ? option.attack.name : option.skill.name;
}

// =============================================================================
// Shoot-and-Scoot Constants
// =============================================================================

const FLEE_TURNS_AFTER_SHOT = 3;
const FLEE_TARGET_DISTANCE = 4;
const FLEE_TRIGGER_DISTANCE = 3; // Only trigger flee if enemy is this close
const FINISH_THRESHOLD = 8; // If target HP <= this, try to finish them off

/**
 * Estimate average damage for an option (rough heuristic for "can I kill?").
 */
function estimateOptionDamage(option: CombatOption, combatant: CombatantState): number {
  // Rough estimate: assume average dice roll + STR for melee
  // This is a heuristic, not exact
  if (option.kind === 'skill') {
    // Magic missile: 1d6 + WIL, average ~5.5 + WIL
    return 3.5 + Math.max(0, combatant.will);
  }
  // Weapon: assume average of 4 + STR for melee
  const strBonus = option.attack.type === 'Melee' ? combatant.strength : 0;
  return 4 + strBonus;
}

/**
 * Try to move away from a position. Returns null if cornered.
 */
function tryGetFleeDirection(
  combatant: CombatantState,
  fleeFrom: Position
): Position | null {
  const direction = getMoveAway(combatant.position, fleeFrom);
  // In this simple sim, movement is always valid (no walls)
  // But check if we'd actually move (not already at edge in a bounded arena)
  if (direction.x === 0 && direction.y === 0) {
    return null; // Can't flee (same position as threat)
  }
  return direction;
}

/**
 * Decide action for a ranged combatant (has ranged options).
 * Uses shoot-and-scoot behavior: shoot, then flee for several turns.
 * Priority: flee if fleeing > finish low HP target > kite if adjacent > shoot > move toward
 */
function decideRangedBehavior(
  combatant: CombatantState,
  enemies: CombatantState[],
  rangedOptions: CombatOption[],
  rng: RandomGenerator
): AIDecisionResult {
  const nearest = getNearestEnemy(combatant, enemies);
  if (!nearest) {
    return { action: { type: 'wait' }, reasoning: 'Wait (no enemies)' };
  }

  const dist = distance(combatant.position, nearest.position);
  const lowestHP = getLowestHPEnemy(combatant, enemies);

  // === PHASE 1: Handle active flee state ===
  if (combatant.fleeTurnsRemaining > 0) {
    combatant.fleeTurnsRemaining--;

    // Check if we've reached safe distance
    if (dist >= combatant.fleeTargetDistance) {
      // Safe distance reached, can stop fleeing early
      combatant.fleeTurnsRemaining = 0;
    } else {
      // Try to flee
      const fleeDir = tryGetFleeDirection(combatant, nearest.position);
      if (fleeDir) {
        return {
          action: { type: 'move', direction: fleeDir },
          reasoning: `Flee from ${nearest.name} (${combatant.fleeTurnsRemaining + 1} turns left, distance ${dist})`,
        };
      }
      // Cornered! Fall through to shoot instead
    }
  }

  // === PHASE 2: Check for finishing blow opportunity ===
  if (lowestHP && lowestHP.currentHealth <= FINISH_THRESHOLD) {
    const viableOptions = rangedOptions.filter((opt) =>
      canReachTarget(combatant, lowestHP, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      const estDamage = estimateOptionDamage(chosen, combatant);

      // If we can likely kill them, take the shot even at close range
      if (lowestHP.currentHealth <= estDamage * 1.5) {
        const action = createActionFromOption(chosen, lowestHP);
        // Don't trigger flee after finishing blow - target should be dead
        return {
          action,
          reasoning: `${getOptionName(chosen)} to finish ${lowestHP.name} (${lowestHP.currentHealth} HP)`,
        };
      }
    }
  }

  // === PHASE 3: If enemy is adjacent, kite away ===
  if (dist <= 1) {
    const fleeDir = tryGetFleeDirection(combatant, nearest.position);
    if (fleeDir) {
      return {
        action: { type: 'move', direction: fleeDir },
        reasoning: `Kite away from ${nearest.name} (distance ${dist})`,
      };
    }
    // Cornered at melee range - fall through to shoot
  }

  // === PHASE 4: Shoot if in range ===
  if (lowestHP) {
    const viableOptions = rangedOptions.filter((opt) =>
      canReachTarget(combatant, lowestHP, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      const action = createActionFromOption(chosen, lowestHP);

      // Trigger flee mode after shooting only if enemy is close (shoot-and-scoot)
      if (dist <= FLEE_TRIGGER_DISTANCE) {
        combatant.fleeTurnsRemaining = FLEE_TURNS_AFTER_SHOT;
        combatant.fleeTargetDistance = FLEE_TARGET_DISTANCE;
        return {
          action,
          reasoning: `${getOptionName(chosen)} ${lowestHP.name} (${lowestHP.currentHealth} HP), then scoot`,
        };
      }

      return {
        action,
        reasoning: `${getOptionName(chosen)} lowest HP target ${lowestHP.name} (${lowestHP.currentHealth} HP)`,
      };
    }
  }

  // Can't hit lowest HP, try any enemy in range
  for (const enemy of enemies) {
    const viableOptions = rangedOptions.filter((opt) =>
      canReachTarget(combatant, enemy, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      const action = createActionFromOption(chosen, enemy);
      const enemyDist = distance(combatant.position, enemy.position);

      // Trigger flee mode only if enemy is close
      if (enemyDist <= FLEE_TRIGGER_DISTANCE) {
        combatant.fleeTurnsRemaining = FLEE_TURNS_AFTER_SHOT;
        combatant.fleeTargetDistance = FLEE_TARGET_DISTANCE;
        return {
          action,
          reasoning: `${getOptionName(chosen)} ${enemy.name} (in range), then scoot`,
        };
      }

      return {
        action,
        reasoning: `${getOptionName(chosen)} ${enemy.name} (in range)`,
      };
    }
  }

  // No enemies in range, move toward nearest
  const direction = getMoveToward(combatant.position, nearest.position);
  return {
    action: { type: 'move', direction },
    reasoning: `Move toward ${nearest.name} (distance ${dist})`,
  };
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
): AIDecisionResult {
  // Find the lowest HP enemy we can hit with any melee option
  const lowestHP = getLowestHPEnemy(combatant, enemies);
  if (lowestHP) {
    const viableOptions = meleeOptions.filter((opt) =>
      canReachTarget(combatant, lowestHP, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      const action = createActionFromOption(chosen, lowestHP);
      return {
        action,
        reasoning: `${getOptionName(chosen)} lowest HP target ${lowestHP.name} (${lowestHP.currentHealth} HP)`,
      };
    }
  }

  // Can't hit lowest HP, try any adjacent enemy
  for (const enemy of enemies) {
    const viableOptions = meleeOptions.filter((opt) =>
      canReachTarget(combatant, enemy, opt)
    );

    if (viableOptions.length > 0) {
      const chosen = pickRandomOption(viableOptions, rng);
      const action = createActionFromOption(chosen, enemy);
      return {
        action,
        reasoning: `${getOptionName(chosen)} ${enemy.name} (adjacent)`,
      };
    }
  }

  // No enemies in range, move toward nearest
  const nearest = getNearestEnemy(combatant, enemies);
  if (nearest) {
    const dist = distance(combatant.position, nearest.position);
    const direction = getMoveToward(combatant.position, nearest.position);
    return {
      action: { type: 'move', direction },
      reasoning: `Move toward ${nearest.name} (distance ${dist})`,
    };
  }

  return { action: { type: 'wait' }, reasoning: 'Wait (no enemies)' };
}

/**
 * Decide the best action for a combatant with reasoning.
 * Uses unified ranged/melee pools with weighted random selection.
 */
export function decideActionWithReasoning(
  combatant: CombatantState,
  allCombatants: CombatantState[],
  rng: RandomGenerator = defaultRng
): AIDecisionResult {
  const enemies = getEnemies(combatant, allCombatants);
  if (enemies.length === 0) {
    return { action: { type: 'wait' }, reasoning: 'Wait (no enemies)' };
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

/**
 * Decide the best action for a combatant (without reasoning).
 * Convenience wrapper for non-verbose mode.
 */
export function decideAction(
  combatant: CombatantState,
  allCombatants: CombatantState[],
  rng: RandomGenerator = defaultRng
): AIAction {
  return decideActionWithReasoning(combatant, allCombatants, rng).action;
}
