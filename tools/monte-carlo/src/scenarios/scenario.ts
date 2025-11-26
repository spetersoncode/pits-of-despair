/**
 * Base scenario interface and common utilities.
 */

import type { AggregateResult, SimulationResult, GameData } from '../data/types.js';
import type { RandomGenerator } from '../data/dice-notation.js';

/**
 * Base interface for all scenario types.
 */
export interface Scenario {
  /** Scenario name/description */
  name: string;

  /** Run the scenario and return aggregated results */
  run(
    iterations: number,
    gameData: GameData,
    rng: RandomGenerator
  ): AggregateResult;
}

/**
 * Aggregate simulation results into statistics.
 */
export function aggregateResults(
  results: SimulationResult[],
  scenarioName: string
): AggregateResult {
  const iterations = results.length;
  if (iterations === 0) {
    throw new Error('Cannot aggregate empty results');
  }

  const teamAWins = results.filter((r) => r.winner === 'A').length;
  const teamBWins = results.filter((r) => r.winner === 'B').length;
  const draws = results.filter((r) => r.winner === 'draw').length;

  const sum = (arr: number[]) => arr.reduce((a, b) => a + b, 0);

  const avgTurns = sum(results.map((r) => r.turns)) / iterations;
  const avgTeamADamage = sum(results.map((r) => r.teamADamageDealt)) / iterations;
  const avgTeamBDamage = sum(results.map((r) => r.teamBDamageDealt)) / iterations;
  const avgTeamASurvivors = sum(results.map((r) => r.teamASurvivors)) / iterations;
  const avgTeamBSurvivors = sum(results.map((r) => r.teamBSurvivors)) / iterations;
  const avgTeamASurvivorHealth = sum(results.map((r) => r.teamASurvivorHealth)) / iterations;
  const avgTeamBSurvivorHealth = sum(results.map((r) => r.teamBSurvivorHealth)) / iterations;

  // 95% confidence interval using normal approximation
  const p = teamAWins / iterations;
  const confidenceInterval95 = 1.96 * Math.sqrt((p * (1 - p)) / iterations);

  return {
    scenario: scenarioName,
    iterations,
    teamAWins,
    teamBWins,
    draws,
    teamAWinRate: teamAWins / iterations,
    teamBWinRate: teamBWins / iterations,
    avgTurns,
    avgTeamADamage,
    avgTeamBDamage,
    avgTeamASurvivors,
    avgTeamBSurvivors,
    avgTeamASurvivorHealth,
    avgTeamBSurvivorHealth,
    confidenceInterval95,
  };
}
