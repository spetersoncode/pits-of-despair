/**
 * VariationScenario - Test same creature with different loadouts.
 * Answers: "Which weapon is best for a goblin?"
 */

import type {
  VariationConfig,
  Variation,
  GameData,
  AggregateResult,
} from '../data/types.js';
import type { RandomGenerator } from '../data/dice-notation.js';
import { runDuel } from './duel-scenario.js';

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
