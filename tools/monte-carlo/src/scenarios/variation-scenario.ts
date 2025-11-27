/**
 * VariationScenario - Test same creature with different loadouts.
 * Answers: "Which weapon is best for a goblin?"
 */

import type {
  VariationConfig,
  Variation,
  GameData,
  AggregateResult,
  CreatureDefinition,
} from '../data/types.js';
import type { RandomGenerator } from '../data/dice-notation.js';
import { runDuel } from './duel-scenario.js';
import { getCreature } from '../data/data-loader.js';

/**
 * Result for a single variation test.
 */
export interface VariationResult {
  variation: string;
  result: AggregateResult;
}

/**
 * Run variation scenario - test multiple loadouts against same opponent.
 */
export function runVariations(
  config: VariationConfig,
  gameData: GameData,
  rng: RandomGenerator
): VariationResult[] {
  const results: VariationResult[] = [];

  for (const variation of config.variations) {
    const duelResult = runDuel(
      {
        creatureA: config.baseCreature,
        creatureB: config.opponent,
        iterations: config.iterations,
        equipmentOverridesA: variation.equipmentOverrides,
      },
      gameData,
      rng
    );

    results.push({
      variation: variation.name,
      result: {
        ...duelResult,
        scenario: `${config.baseCreature} (${variation.name}) vs ${config.opponent}`,
      },
    });
  }

  return results;
}

/**
 * Print variation results as comparison.
 */
export function printVariationResults(results: VariationResult[]): void {
  console.log('\n' + '='.repeat(70));
  console.log('  VARIATION COMPARISON');
  console.log('='.repeat(70));
  console.log('');
  console.log('  Variation                   Win Rate      ± CI     Turns');
  console.log('  ' + '-'.repeat(60));

  // Sort by win rate descending
  const sorted = [...results].sort(
    (a, b) => b.result.teamAWinRate - a.result.teamAWinRate
  );

  for (const { variation, result } of sorted) {
    const name = variation.padEnd(25);
    const winRate = ((result.teamAWinRate * 100).toFixed(1) + '%').padStart(10);
    const ci = ('±' + (result.confidenceInterval95 * 100).toFixed(1) + '%').padStart(10);
    const turns = result.avgTurns.toFixed(1).padStart(8);
    console.log(`  ${name} ${winRate} ${ci} ${turns}`);
  }

  console.log('');
  console.log('='.repeat(70));
}

/**
 * Run inline variation scenario - test multiple inline creatures against same opponent.
 * Each inline creature is a complete creature definition (or based on a base creature).
 */
export function runInlineVariations(
  inlineCreatures: CreatureDefinition[],
  opponent: string,
  iterations: number,
  gameData: GameData,
  rng: RandomGenerator
): VariationResult[] {
  const results: VariationResult[] = [];
  const opponentCreature = getCreature(opponent, gameData);

  for (const inlineCreature of inlineCreatures) {
    const duelResult = runDuel(
      {
        creatureA: inlineCreature.id,
        creatureB: opponent,
        iterations,
        inlineCreatureA: inlineCreature,
      },
      gameData,
      rng
    );

    results.push({
      variation: inlineCreature.name,
      result: {
        ...duelResult,
        scenario: `${inlineCreature.name} vs ${opponentCreature.name}`,
      },
    });
  }

  return results;
}
