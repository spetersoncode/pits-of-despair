/**
 * CsvReporter - CSV file output.
 */

import { writeFileSync } from 'fs';
import type { AggregateResult } from '../data/types.js';

const CSV_HEADERS = [
  'scenario',
  'iterations',
  'teamAWins',
  'teamBWins',
  'draws',
  'teamAWinRate',
  'teamBWinRate',
  'confidenceInterval95',
  'avgTurns',
  'avgTeamADamage',
  'avgTeamBDamage',
  'avgTeamASurvivors',
  'avgTeamBSurvivors',
  'avgTeamASurvivorHealth',
  'avgTeamBSurvivorHealth',
];

/**
 * Escape a CSV field (handle commas and quotes).
 */
function escapeField(value: string | number): string {
  const str = String(value);
  if (str.includes(',') || str.includes('"') || str.includes('\n')) {
    return `"${str.replace(/"/g, '""')}"`;
  }
  return str;
}

/**
 * Format a single result as CSV row.
 */
export function toCsvRow(result: AggregateResult): string {
  const values = [
    result.scenario,
    result.iterations,
    result.teamAWins,
    result.teamBWins,
    result.draws,
    result.teamAWinRate.toFixed(4),
    result.teamBWinRate.toFixed(4),
    result.confidenceInterval95.toFixed(4),
    result.avgTurns.toFixed(2),
    result.avgTeamADamage.toFixed(2),
    result.avgTeamBDamage.toFixed(2),
    result.avgTeamASurvivors.toFixed(2),
    result.avgTeamBSurvivors.toFixed(2),
    result.avgTeamASurvivorHealth.toFixed(2),
    result.avgTeamBSurvivorHealth.toFixed(2),
  ];
  return values.map(escapeField).join(',');
}

/**
 * Format results as CSV string.
 */
export function toCsv(results: AggregateResult[]): string {
  const header = CSV_HEADERS.join(',');
  const rows = results.map(toCsvRow);
  return [header, ...rows].join('\n');
}

/**
 * Write results to CSV file.
 */
export function writeCsvFile(
  results: AggregateResult[],
  filePath: string
): void {
  const csv = toCsv(results);
  writeFileSync(filePath, csv);
  console.log(`Results written to: ${filePath}`);
}

/**
 * Print results as CSV to console.
 */
export function printCsv(results: AggregateResult[]): void {
  console.log(toCsv(results));
}
