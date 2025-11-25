/**
 * Monte Carlo Combat Simulator for Pits of Despair
 *
 * Entry point for the CLI application.
 */

import { loadGameData, getCreature } from './data/DataLoader.js';
import { SeededRng } from './data/DiceNotation.js';
import { createCombatant, resetCombatantIdCounter } from './simulation/Combatant.js';
import { runCombat, DEFAULT_CONFIG } from './simulation/CombatEngine.js';
import type { SimulationResult, AggregateResult } from './data/types.js';

// =============================================================================
// Simulation Runner
// =============================================================================

/**
 * Run multiple combat simulations and aggregate results.
 */
function runSimulations(
  creatureIdA: string,
  creatureIdB: string,
  iterations: number,
  seed?: number
): AggregateResult {
  const data = loadGameData();
  const creatureA = getCreature(creatureIdA, data);
  const creatureB = getCreature(creatureIdB, data);

  const results: SimulationResult[] = [];
  const rng = seed !== undefined ? new SeededRng(seed) : new SeededRng(Date.now());

  for (let i = 0; i < iterations; i++) {
    // Reset combatant IDs for determinism
    resetCombatantIdCounter();

    // Create fresh combatants for each simulation
    const combatantA = createCombatant(creatureA, 'A', data, { x: 0, y: 0 });
    const combatantB = createCombatant(creatureB, 'B', data, { x: 5, y: 0 });

    const result = runCombat([combatantA, combatantB], DEFAULT_CONFIG, rng);
    results.push(result);
  }

  // Aggregate results
  const teamAWins = results.filter((r) => r.winner === 'A').length;
  const teamBWins = results.filter((r) => r.winner === 'B').length;
  const draws = results.filter((r) => r.winner === 'draw').length;

  const avgTurns = results.reduce((sum, r) => sum + r.turns, 0) / iterations;
  const avgTeamADamage =
    results.reduce((sum, r) => sum + r.teamADamageDealt, 0) / iterations;
  const avgTeamBDamage =
    results.reduce((sum, r) => sum + r.teamBDamageDealt, 0) / iterations;
  const avgTeamASurvivors =
    results.reduce((sum, r) => sum + r.teamASurvivors, 0) / iterations;
  const avgTeamBSurvivors =
    results.reduce((sum, r) => sum + r.teamBSurvivors, 0) / iterations;
  const avgTeamASurvivorHealth =
    results.reduce((sum, r) => sum + r.teamASurvivorHealth, 0) / iterations;
  const avgTeamBSurvivorHealth =
    results.reduce((sum, r) => sum + r.teamBSurvivorHealth, 0) / iterations;

  // Calculate 95% confidence interval for win rate
  // Using normal approximation: p ± 1.96 * sqrt(p(1-p)/n)
  const p = teamAWins / iterations;
  const confidenceInterval95 = 1.96 * Math.sqrt((p * (1 - p)) / iterations);

  return {
    scenario: `${creatureIdA} vs ${creatureIdB}`,
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

// =============================================================================
// Output Formatting
// =============================================================================

function formatPercent(value: number): string {
  return `${(value * 100).toFixed(1)}%`;
}

function formatNumber(value: number, decimals: number = 1): string {
  return value.toFixed(decimals);
}

function printResults(result: AggregateResult): void {
  const [nameA, nameB] = result.scenario.split(' vs ');

  console.log('\n' + '='.repeat(60));
  console.log(`  DUEL: ${nameA} vs ${nameB} (n=${result.iterations})`);
  console.log('='.repeat(60));

  console.log('\n  Results:');
  console.log(`    ${nameA} wins: ${formatPercent(result.teamAWinRate)} ± ${formatPercent(result.confidenceInterval95)}`);
  console.log(`    ${nameB} wins: ${formatPercent(result.teamBWinRate)} ± ${formatPercent(result.confidenceInterval95)}`);
  if (result.draws > 0) {
    console.log(`    Draws: ${formatPercent(result.draws / result.iterations)}`);
  }

  console.log('\n  Combat Statistics:');
  console.log(`    Average turns: ${formatNumber(result.avgTurns)}`);
  console.log(`    ${nameA} avg damage dealt: ${formatNumber(result.avgTeamADamage)}`);
  console.log(`    ${nameB} avg damage dealt: ${formatNumber(result.avgTeamBDamage)}`);

  console.log('\n  Survivor Statistics (when winning):');
  const aWinHealth = result.teamAWins > 0
    ? result.avgTeamASurvivorHealth / result.teamAWinRate
    : 0;
  const bWinHealth = result.teamBWins > 0
    ? result.avgTeamBSurvivorHealth / result.teamBWinRate
    : 0;
  console.log(`    ${nameA} avg remaining HP: ${formatNumber(aWinHealth)}`);
  console.log(`    ${nameB} avg remaining HP: ${formatNumber(bWinHealth)}`);

  console.log('\n' + '='.repeat(60));
}

// =============================================================================
// Main
// =============================================================================

async function main(): Promise<void> {
  console.log('Monte Carlo Combat Simulator');
  console.log('============================\n');

  // Run some sample duels
  const duels = [
    ['goblin', 'rat'],
    ['goblin', 'skeleton'],
    ['goblin_ruffian', 'skeleton'],
    ['zombie', 'goblin'],
  ];

  const iterations = 1000;
  const seed = 42; // For reproducibility

  for (const [a, b] of duels) {
    try {
      const result = runSimulations(a, b, iterations, seed);
      printResults(result);
    } catch (error) {
      console.error(`Error running ${a} vs ${b}:`, error);
    }
  }
}

main().catch(console.error);
