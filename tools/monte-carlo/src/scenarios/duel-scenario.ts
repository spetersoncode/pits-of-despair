/**
 * DuelScenario - 1v1 combat simulation.
 */

import type {
  DuelConfig,
  GameData,
  SimulationResult,
  AggregateResult,
} from '../data/types.js';
import type { RandomGenerator } from '../data/dice-notation.js';
import { getCreature } from '../data/data-loader.js';
import { createCombatant, resetCombatantIdCounter } from '../simulation/combatant.js';
import { runCombat, DEFAULT_CONFIG } from '../simulation/combat-engine.js';
import { aggregateResults, type Scenario } from './scenario.js';
import { createVerboseLogger } from '../output/verbose-logger.js';

/**
 * Run a duel scenario.
 */
export function runDuel(
  config: DuelConfig,
  gameData: GameData,
  rng: RandomGenerator
): AggregateResult {
  // Use inline creature if provided, otherwise look up by ID
  const creatureA = config.inlineCreatureA ?? getCreature(config.creatureA, gameData);
  const creatureB = config.inlineCreatureB ?? getCreature(config.creatureB, gameData);

  const results: SimulationResult[] = [];

  // Create logger for verbose mode
  const logger = config.verbose ? createVerboseLogger() : undefined;

  for (let i = 0; i < config.iterations; i++) {
    resetCombatantIdCounter();

    const combatantA = createCombatant(
      creatureA,
      'A',
      gameData,
      { x: 0, y: 0 },
      config.equipmentOverridesA
    );
    const combatantB = createCombatant(
      creatureB,
      'B',
      gameData,
      { x: 5, y: 0 },
      config.equipmentOverridesB
    );

    // Start new fight in logger
    if (logger) {
      logger.startFight();
    }

    const result = runCombat([combatantA, combatantB], DEFAULT_CONFIG, rng, logger);
    results.push(result);
  }

  // Use creature names for scenario label
  const nameA = creatureA.name;
  const nameB = creatureB.name;

  return aggregateResults(
    results,
    `${nameA} vs ${nameB}`
  );
}

/**
 * DuelScenario class implementing Scenario interface.
 */
export class DuelScenario implements Scenario {
  public readonly name: string;
  private readonly config: Omit<DuelConfig, 'iterations'>;

  constructor(
    creatureA: string,
    creatureB: string,
    equipmentOverridesA?: string[],
    equipmentOverridesB?: string[]
  ) {
    this.name = `${creatureA} vs ${creatureB}`;
    this.config = {
      creatureA,
      creatureB,
      equipmentOverridesA,
      equipmentOverridesB,
    };
  }

  run(
    iterations: number,
    gameData: GameData,
    rng: RandomGenerator
  ): AggregateResult {
    return runDuel({ ...this.config, iterations }, gameData, rng);
  }
}
