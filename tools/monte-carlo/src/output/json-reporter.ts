/**
 * JsonReporter - JSON file output.
 */

import { writeFileSync } from 'fs';
import type { AggregateResult } from '../data/types.js';

/**
 * Format results as JSON object.
 */
export function toJson(result: AggregateResult): object {
  return {
    scenario: result.scenario,
    config: {
      iterations: result.iterations,
    },
    results: {
      teamAWins: result.teamAWins,
      teamBWins: result.teamBWins,
      draws: result.draws,
      teamAWinRate: result.teamAWinRate,
      teamBWinRate: result.teamBWinRate,
      confidenceInterval95: result.confidenceInterval95,
    },
    statistics: {
      avgTurns: result.avgTurns,
      teamA: {
        avgDamageDealt: result.avgTeamADamage,
        avgSurvivors: result.avgTeamASurvivors,
        avgSurvivorHealth: result.avgTeamASurvivorHealth,
      },
      teamB: {
        avgDamageDealt: result.avgTeamBDamage,
        avgSurvivors: result.avgTeamBSurvivors,
        avgSurvivorHealth: result.avgTeamBSurvivorHealth,
      },
    },
  };
}

/**
 * Write results to JSON file.
 */
export function writeJsonFile(result: AggregateResult, filePath: string): void {
  const json = toJson(result);
  writeFileSync(filePath, JSON.stringify(json, null, 2));
  console.log(`Results written to: ${filePath}`);
}

/**
 * Write multiple results to JSON file.
 */
export function writeJsonFileMultiple(
  results: AggregateResult[],
  filePath: string
): void {
  const json = results.map(toJson);
  writeFileSync(filePath, JSON.stringify(json, null, 2));
  console.log(`Results written to: ${filePath}`);
}

/**
 * Print results as JSON to console.
 */
export function printJson(result: AggregateResult): void {
  console.log(JSON.stringify(toJson(result), null, 2));
}
