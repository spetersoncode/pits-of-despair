/**
 * DuelScenario - 1v1 combat simulation.
 */

import type {
  DuelConfig,
  GameData,
  SimulationResult,
  AggregateResult,
} from '../data/types.js';
import type { RandomGenerator } from '../data/DiceNotation.js';
import { getCreature } from '../data/DataLoader.js';
import { createCombatant, resetCombatantIdCounter } from '../simulation/Combatant.js';
import { runCombat, DEFAULT_CONFIG } from '../simulation/CombatEngine.js';
import { aggregateResults, type Scenario } from './Scenario.js';

/**
 * Run a duel scenario.
 */
export function runDuel(
  config: DuelConfig,
  gameData: GameData,
  rng: RandomGenerator
): AggregateResult {
  const creatureA = getCreature(config.creatureA, gameData);
  const creatureB = getCreature(config.creatureB, gameData);

  const results: SimulationResult[] = [];

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

    const result = runCombat([combatantA, combatantB], DEFAULT_CONFIG, rng);
    results.push(result);
  }

  return aggregateResults(
    results,
    `${config.creatureA} vs ${config.creatureB}`
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
