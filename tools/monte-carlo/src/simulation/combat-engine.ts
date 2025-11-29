/**
 * CombatEngine - Full combat simulation loop.
 * Runs combat until one team is eliminated or max turns reached.
 */

import type {
  Combatant as CombatantState,
  SimulationResult,
  Position,
} from '../data/types.js';
import { type RandomGenerator, defaultRng } from '../data/dice-notation.js';
import { isAlive, consumeWillpower } from './combatant.js';
import {
  resolveAttack,
  applyDamage,
  getAttackModifier,
  getDefenseModifier,
  type AttackResult,
} from './combat-resolver.js';
import { resolveSkill } from './skill-resolver.js';
import { processRegeneration, processWillpowerRegeneration } from './regeneration.js';
import { decideAction, decideActionWithReasoning, applyMovement, type AIAction } from './ai-controller.js';
import {
  advanceTime,
  getNextReady,
  deductTime,
  calculateDelay,
  STANDARD_ACTION_DELAY,
} from './turn-scheduler.js';
import type { VerboseLogger } from '../output/verbose-logger.js';

// =============================================================================
// Combat Configuration
// =============================================================================

export interface CombatConfig {
  /** Maximum turns before declaring a draw. */
  maxTurns: number;
  /** Starting distance between teams. */
  startingDistance: number;
  /** Enable verbose logging. */
  verbose: boolean;
  /** Arena size (combatants can't move outside -arenaSize to +arenaSize). */
  arenaSize: number;
}

export const DEFAULT_CONFIG: CombatConfig = {
  maxTurns: 1000,
  startingDistance: 5,
  verbose: false,
  arenaSize: 20,
};

// =============================================================================
// Combat Events (for logging/debugging)
// =============================================================================

export interface CombatEvent {
  turn: number;
  actor: string;
  action: string;
  details: string;
}

// =============================================================================
// Combat State
// =============================================================================

export interface CombatState {
  combatants: CombatantState[];
  turn: number;
  events: CombatEvent[];
  teamADamageDealt: number;
  teamBDamageDealt: number;
  arenaSize: number;
}

// =============================================================================
// Setup
// =============================================================================

/**
 * Position combatants for battle.
 * Team A starts on the left, Team B on the right.
 */
export function positionCombatants(
  combatants: CombatantState[],
  startingDistance: number
): void {
  const teamA = combatants.filter((c) => c.team === 'A');
  const teamB = combatants.filter((c) => c.team === 'B');

  // Position Team A on the left (x=0)
  teamA.forEach((c, i) => {
    c.position = { x: 0, y: i };
  });

  // Position Team B on the right
  teamB.forEach((c, i) => {
    c.position = { x: startingDistance, y: i };
  });
}

/**
 * Initialize combat state.
 */
export function initializeCombat(
  combatants: CombatantState[],
  config: CombatConfig = DEFAULT_CONFIG
): CombatState {
  positionCombatants(combatants, config.startingDistance);

  return {
    combatants,
    turn: 0,
    events: [],
    teamADamageDealt: 0,
    teamBDamageDealt: 0,
    arenaSize: config.arenaSize,
  };
}

/**
 * Clamp a position to arena bounds.
 */
export function clampToArena(pos: Position, arenaSize: number): Position {
  return {
    x: Math.max(-arenaSize, Math.min(arenaSize, pos.x)),
    y: Math.max(-arenaSize, Math.min(arenaSize, pos.y)),
  };
}

// =============================================================================
// Combat Resolution
// =============================================================================

/**
 * Check if combat should end.
 */
export function isCombatOver(state: CombatState, maxTurns: number): boolean {
  // Check for max turns
  if (state.turn >= maxTurns) {
    return true;
  }

  // Check if one team is eliminated
  const aliveTeams = new Set(
    state.combatants.filter(isAlive).map((c) => c.team)
  );
  return aliveTeams.size <= 1;
}

/**
 * Get the winning team (or null for draw).
 */
export function getWinner(state: CombatState): 'A' | 'B' | 'draw' {
  const aliveA = state.combatants.filter((c) => c.team === 'A' && isAlive(c));
  const aliveB = state.combatants.filter((c) => c.team === 'B' && isAlive(c));

  if (aliveA.length > 0 && aliveB.length === 0) return 'A';
  if (aliveB.length > 0 && aliveA.length === 0) return 'B';
  return 'draw';
}

/**
 * Execute a single combatant's turn.
 * @returns The action cost (delay multiplier for deductTime)
 */
export function executeTurn(
  actor: CombatantState,
  state: CombatState,
  rng: RandomGenerator,
  verbose: boolean,
  logger?: VerboseLogger
): number {
  // Get action with reasoning if we have a logger
  const { action, reasoning } = logger
    ? decideActionWithReasoning(actor, state.combatants, rng)
    : { action: decideAction(actor, state.combatants, rng), reasoning: '' };

  let actionCost = STANDARD_ACTION_DELAY; // Default cost for move/wait

  // Log AI decision
  if (logger) {
    logger.logAIDecision({ action: action.type, reasoning });
  }

  switch (action.type) {
    case 'attack': {
      // Weapon delay affects action cost
      actionCost = Math.round(STANDARD_ACTION_DELAY * action.attack.delay);
      const isMelee = action.attack.type === 'Melee';
      const attackMod = getAttackModifier(actor, isMelee);
      const defenseMod = getDefenseModifier(action.target);

      const result = resolveAttack(actor, action.target, action.attack, rng);
      const oldHp = action.target.currentHealth;

      if (result.hit) {
        const actualDamage = applyDamage(action.target, result.damage);
        const newHp = action.target.currentHealth;

        // Track damage dealt
        if (actor.team === 'A') {
          state.teamADamageDealt += actualDamage;
        } else {
          state.teamBDamageDealt += actualDamage;
        }

        if (logger) {
          // Calculate damage bonus for logging
          const damageBonus = isMelee ? actor.strength : 0;
          logger.logAttack({
            result,
            attackMod,
            defenseMod,
            weaponDice: action.attack.dice,
            damageRolled: result.damageBeforeModifiers - damageBonus + action.target.armor,
            damageBonus,
            armor: action.target.armor,
          });
          logger.logDamageApplied(
            action.target.name,
            actualDamage,
            oldHp,
            newHp,
            action.target.maxHealth,
            !isAlive(action.target)
          );
        }

        if (verbose) {
          let msg = `${actor.name} hits ${action.target.name} with ${action.attack.name} for ${actualDamage} damage`;
          if (result.modifier !== 'none') {
            msg += ` (${result.modifier})`;
          }
          if (!isAlive(action.target)) {
            msg += ` - ${action.target.name} dies!`;
          }
          state.events.push({
            turn: state.turn,
            actor: actor.name,
            action: 'attack',
            details: msg,
          });
        }
      } else {
        if (logger) {
          const damageBonus = isMelee ? actor.strength : 0;
          logger.logAttack({
            result,
            attackMod,
            defenseMod,
            weaponDice: action.attack.dice,
            damageRolled: 0,
            damageBonus,
            armor: action.target.armor,
          });
        }

        if (verbose) {
          state.events.push({
            turn: state.turn,
            actor: actor.name,
            action: 'miss',
            details: `${actor.name} misses ${action.target.name} (${result.attackRoll} vs ${result.defenseRoll})`,
          });
        }
      }
      break;
    }

    case 'skill': {
      const wpBefore = actor.currentWillpower;
      // Consume willpower first
      consumeWillpower(actor, action.skill);
      const wpAfter = actor.currentWillpower;

      // Resolve skill effects
      const result = resolveSkill(actor, action.target, action.skill, rng);
      const oldHp = action.target.currentHealth;

      if (result.damage > 0) {
        const actualDamage = applyDamage(action.target, result.damage);
        const newHp = action.target.currentHealth;

        // Track damage dealt
        if (actor.team === 'A') {
          state.teamADamageDealt += actualDamage;
        } else {
          state.teamBDamageDealt += actualDamage;
        }

        if (logger) {
          logger.logSkill({
            skillName: action.skill.name,
            targetName: action.target.name,
            wpCost: action.skill.willpowerCost,
            wpRemaining: wpAfter,
            damage: actualDamage,
            damageType: result.damageType ?? 'unknown',
            modifier: result.modifier,
          });
          logger.logDamageApplied(
            action.target.name,
            actualDamage,
            oldHp,
            newHp,
            action.target.maxHealth,
            !isAlive(action.target)
          );
        }

        if (verbose) {
          let msg = `${actor.name} casts ${action.skill.name} on ${action.target.name} for ${actualDamage} damage`;
          if (result.modifier !== 'none') {
            msg += ` (${result.modifier})`;
          }
          if (!isAlive(action.target)) {
            msg += ` - ${action.target.name} dies!`;
          }
          state.events.push({
            turn: state.turn,
            actor: actor.name,
            action: 'skill',
            details: msg,
          });
        }
      } else {
        if (logger) {
          logger.logSkill({
            skillName: action.skill.name,
            targetName: action.target.name,
            wpCost: action.skill.willpowerCost,
            wpRemaining: wpAfter,
            damage: 0,
            damageType: 'none',
            modifier: 'none',
          });
        }

        if (verbose) {
          state.events.push({
            turn: state.turn,
            actor: actor.name,
            action: 'skill',
            details: `${actor.name} casts ${action.skill.name} on ${action.target.name} (no damage)`,
          });
        }
      }
      break;
    }

    case 'move': {
      const oldPos = { x: actor.position.x, y: actor.position.y };
      applyMovement(actor, action.direction);
      // Clamp to arena bounds
      const clamped = clampToArena(actor.position, state.arenaSize);
      actor.position.x = clamped.x;
      actor.position.y = clamped.y;

      if (logger) {
        logger.logMovement(actor.name, oldPos, actor.position);
      }

      if (verbose) {
        state.events.push({
          turn: state.turn,
          actor: actor.name,
          action: 'move',
          details: `${actor.name} moves to (${actor.position.x}, ${actor.position.y})`,
        });
      }
      break;
    }

    case 'wait': {
      if (logger) {
        logger.logWait(actor.name);
      }

      if (verbose) {
        state.events.push({
          turn: state.turn,
          actor: actor.name,
          action: 'wait',
          details: `${actor.name} waits`,
        });
      }
      break;
    }
  }

  // Process HP regeneration after action
  const healed = processRegeneration(actor);
  if (healed > 0) {
    if (logger) {
      logger.logRegeneration(actor.name, healed, actor.currentHealth, actor.maxHealth);
    }
    if (verbose) {
      state.events.push({
        turn: state.turn,
        actor: actor.name,
        action: 'regen',
        details: `${actor.name} regenerates ${healed} HP`,
      });
    }
  }

  // Process WP regeneration after action
  const restored = processWillpowerRegeneration(actor);
  if (restored > 0) {
    if (logger) {
      logger.logWillpowerRegeneration(actor.name, restored, actor.currentWillpower, actor.maxWillpower);
    }
    if (verbose) {
      state.events.push({
        turn: state.turn,
        actor: actor.name,
        action: 'wp_regen',
        details: `${actor.name} restores ${restored} WP`,
      });
    }
  }

  // Log action cost
  if (logger) {
    const weaponDelay = action.type === 'attack' ? action.attack.delay : undefined;
    logger.logActionCost(actor.name, actionCost, actor.speed, weaponDelay);
  }

  return actionCost;
}

// =============================================================================
// Main Combat Loop
// =============================================================================

/**
 * Run a complete combat simulation.
 */
export function runCombat(
  combatants: CombatantState[],
  config: CombatConfig = DEFAULT_CONFIG,
  rng: RandomGenerator = defaultRng,
  logger?: VerboseLogger
): SimulationResult {
  const state = initializeCombat(combatants, config);

  // Log combat start
  if (logger) {
    logger.logCombatStart(state.combatants);
  }

  // Main combat loop
  while (!isCombatOver(state, config.maxTurns)) {
    state.turn++;

    // Advance time for all combatants
    advanceTime(state.combatants, STANDARD_ACTION_DELAY);

    // Log turn start
    if (logger) {
      logger.logTurnStart(state.turn);
    }

    // Process all ready combatants this tick
    let ready = getNextReady(state.combatants, STANDARD_ACTION_DELAY, rng);
    while (ready && !isCombatOver(state, config.maxTurns)) {
      // Log actor ready
      if (logger) {
        const delay = calculateDelay(ready.speed, STANDARD_ACTION_DELAY, rng);
        logger.logActorReady(ready, ready.accumulatedTime, delay);
      }

      const actionCost = executeTurn(ready, state, rng, config.verbose, logger);
      deductTime(ready, actionCost, rng);
      ready = getNextReady(state.combatants, STANDARD_ACTION_DELAY, rng);
    }
  }

  // Calculate results
  const winner = getWinner(state);
  const aliveA = state.combatants.filter((c) => c.team === 'A' && isAlive(c));
  const aliveB = state.combatants.filter((c) => c.team === 'B' && isAlive(c));

  // Log combat end
  if (logger) {
    logger.logCombatEnd(winner, state.turn, state.combatants);
  }

  return {
    winner,
    turns: state.turn,
    teamADamageDealt: state.teamADamageDealt,
    teamBDamageDealt: state.teamBDamageDealt,
    teamASurvivors: aliveA.length,
    teamBSurvivors: aliveB.length,
    teamASurvivorHealth: aliveA.reduce((sum, c) => sum + c.currentHealth, 0),
    teamBSurvivorHealth: aliveB.reduce((sum, c) => sum + c.currentHealth, 0),
  };
}

/**
 * Run a simple duel between two combatants (no movement, just attack).
 * Simplified version for quick testing.
 */
export function runSimpleDuel(
  combatantA: CombatantState,
  combatantB: CombatantState,
  config: Partial<CombatConfig> = {},
  rng: RandomGenerator = defaultRng
): SimulationResult {
  // Position them adjacent for immediate combat
  combatantA.position = { x: 0, y: 0 };
  combatantB.position = { x: 1, y: 0 };

  return runCombat(
    [combatantA, combatantB],
    { ...DEFAULT_CONFIG, ...config, startingDistance: 1 },
    rng
  );
}
