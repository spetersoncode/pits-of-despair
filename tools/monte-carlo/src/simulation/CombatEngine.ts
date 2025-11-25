/**
 * CombatEngine - Full combat simulation loop.
 * Runs combat until one team is eliminated or max turns reached.
 */

import type {
  Combatant as CombatantState,
  SimulationResult,
  Position,
} from '../data/types.js';
import { type RandomGenerator, defaultRng } from '../data/DiceNotation.js';
import { isAlive } from './Combatant.js';
import { resolveAttack, applyDamage, type AttackResult } from './CombatResolver.js';
import { processRegeneration } from './Regeneration.js';
import { decideAction, applyMovement, type AIAction } from './AIController.js';
import {
  advanceTime,
  getNextReady,
  deductTime,
  STANDARD_ACTION_DELAY,
} from './TurnScheduler.js';

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
}

export const DEFAULT_CONFIG: CombatConfig = {
  maxTurns: 1000,
  startingDistance: 5,
  verbose: false,
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
 */
export function executeTurn(
  actor: CombatantState,
  state: CombatState,
  rng: RandomGenerator,
  verbose: boolean
): void {
  const action = decideAction(actor, state.combatants);

  switch (action.type) {
    case 'attack': {
      const result = resolveAttack(actor, action.target, action.attack, rng);

      if (result.hit) {
        const actualDamage = applyDamage(action.target, result.damage);

        // Track damage dealt
        if (actor.team === 'A') {
          state.teamADamageDealt += actualDamage;
        } else {
          state.teamBDamageDealt += actualDamage;
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
      } else if (verbose) {
        state.events.push({
          turn: state.turn,
          actor: actor.name,
          action: 'miss',
          details: `${actor.name} misses ${action.target.name} (${result.attackRoll} vs ${result.defenseRoll})`,
        });
      }
      break;
    }

    case 'move': {
      applyMovement(actor, action.direction);
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

  // Process regeneration after action
  const healed = processRegeneration(actor);
  if (healed > 0 && verbose) {
    state.events.push({
      turn: state.turn,
      actor: actor.name,
      action: 'regen',
      details: `${actor.name} regenerates ${healed} HP`,
    });
  }
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
  rng: RandomGenerator = defaultRng
): SimulationResult {
  const state = initializeCombat(combatants, config);

  // Main combat loop
  while (!isCombatOver(state, config.maxTurns)) {
    state.turn++;

    // Advance time for all combatants
    advanceTime(state.combatants, STANDARD_ACTION_DELAY);

    // Process all ready combatants this tick
    let ready = getNextReady(state.combatants, STANDARD_ACTION_DELAY, rng);
    while (ready && !isCombatOver(state, config.maxTurns)) {
      executeTurn(ready, state, rng, config.verbose);
      deductTime(ready, STANDARD_ACTION_DELAY, rng);
      ready = getNextReady(state.combatants, STANDARD_ACTION_DELAY, rng);
    }
  }

  // Calculate results
  const winner = getWinner(state);
  const aliveA = state.combatants.filter((c) => c.team === 'A' && isAlive(c));
  const aliveB = state.combatants.filter((c) => c.team === 'B' && isAlive(c));

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
