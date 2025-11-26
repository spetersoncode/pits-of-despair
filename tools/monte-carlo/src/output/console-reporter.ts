/**
 * ConsoleReporter - Terminal output formatting.
 */

import type { AggregateResult } from '../data/types.js';

/**
 * Format a number as percentage.
 */
export function formatPercent(value: number, decimals: number = 1): string {
  return `${(value * 100).toFixed(decimals)}%`;
}

/**
 * Format a number with fixed decimals.
 */
export function formatNumber(value: number, decimals: number = 1): string {
  return value.toFixed(decimals);
}

/**
 * Print a horizontal divider.
 */
export function printDivider(char: string = '=', width: number = 60): void {
  console.log(char.repeat(width));
}

/**
 * Print aggregated results in a formatted table.
 */
export function printResults(result: AggregateResult): void {
  const parts = result.scenario.split(' vs ');
  const nameA = parts[0]?.replace(/^\[/, '').trim() ?? 'Team A';
  const nameB = parts[1]?.replace(/\]$/, '').trim() ?? 'Team B';

  console.log('');
  printDivider();
  console.log(`  ${result.scenario} (n=${result.iterations})`);
  printDivider();

  console.log('\n  Win Rates:');
  console.log(
    `    ${nameA}: ${formatPercent(result.teamAWinRate)} ± ${formatPercent(result.confidenceInterval95)}`
  );
  console.log(
    `    ${nameB}: ${formatPercent(result.teamBWinRate)} ± ${formatPercent(result.confidenceInterval95)}`
  );
  if (result.draws > 0) {
    console.log(`    Draws: ${formatPercent(result.draws / result.iterations)}`);
  }

  console.log('\n  Combat Statistics:');
  console.log(`    Average turns: ${formatNumber(result.avgTurns)}`);
  console.log(`    ${nameA} avg damage: ${formatNumber(result.avgTeamADamage)}`);
  console.log(`    ${nameB} avg damage: ${formatNumber(result.avgTeamBDamage)}`);

  if (result.avgTeamASurvivors > 1 || result.avgTeamBSurvivors > 1) {
    console.log('\n  Survivors:');
    console.log(`    ${nameA} avg survivors: ${formatNumber(result.avgTeamASurvivors)}`);
    console.log(`    ${nameB} avg survivors: ${formatNumber(result.avgTeamBSurvivors)}`);
  }

  console.log('\n  Remaining HP (when winning):');
  const aWinHealth =
    result.teamAWinRate > 0
      ? result.avgTeamASurvivorHealth / result.teamAWinRate
      : 0;
  const bWinHealth =
    result.teamBWinRate > 0
      ? result.avgTeamBSurvivorHealth / result.teamBWinRate
      : 0;
  console.log(`    ${nameA}: ${formatNumber(aWinHealth)}`);
  console.log(`    ${nameB}: ${formatNumber(bWinHealth)}`);

  printDivider();
}

/**
 * Print a compact one-line result.
 */
export function printCompactResult(result: AggregateResult): void {
  console.log(
    `${result.scenario}: ` +
      `${formatPercent(result.teamAWinRate)} vs ${formatPercent(result.teamBWinRate)} ` +
      `(±${formatPercent(result.confidenceInterval95)}, n=${result.iterations})`
  );
}

/**
 * Print multiple results as a comparison table.
 */
export function printComparison(results: AggregateResult[]): void {
  console.log('');
  printDivider();
  console.log('  Comparison Results');
  printDivider();
  console.log('');

  // Header
  console.log('  %-40s  %8s  %8s  %6s', 'Scenario', 'Win A', 'Win B', 'Turns');
  console.log('  ' + '-'.repeat(66));

  for (const result of results) {
    const scenario =
      result.scenario.length > 40
        ? result.scenario.slice(0, 37) + '...'
        : result.scenario;
    console.log(
      `  %-40s  %8s  %8s  %6s`,
      scenario,
      formatPercent(result.teamAWinRate),
      formatPercent(result.teamBWinRate),
      formatNumber(result.avgTurns)
    );
  }

  console.log('');
  printDivider();
}
