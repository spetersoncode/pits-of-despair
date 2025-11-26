/**
 * CLI command definitions using Commander.
 */

import { Command } from 'commander';
import { SeededRng } from '../data/dice-notation.js';
import {
  loadGameData,
  listCreatures,
  listItems,
  getCreature,
  getItem,
} from '../data/data-loader.js';
import { runDuel } from '../scenarios/duel-scenario.js';
import { runGroupBattle, parseTeamString } from '../scenarios/group-scenario.js';
import { runVariations, printVariationResults } from '../scenarios/variation-scenario.js';
import { printResults, printCompactResult } from '../output/console-reporter.js';
import { writeJsonFile, printJson } from '../output/json-reporter.js';
import { writeCsvFile, printCsv } from '../output/csv-reporter.js';
import { calculateMaxHealth } from '../simulation/combatant.js';
import type { AggregateResult } from '../data/types.js';

type OutputFormat = 'console' | 'json' | 'csv';

/**
 * Handle output based on format and file options.
 */
function handleOutput(
  result: AggregateResult,
  format: OutputFormat,
  outfile?: string,
  compact?: boolean
): void {
  switch (format) {
    case 'json':
      if (outfile) {
        writeJsonFile(result, outfile.endsWith('.json') ? outfile : `${outfile}.json`);
      } else {
        printJson(result);
      }
      break;
    case 'csv':
      if (outfile) {
        writeCsvFile([result], outfile.endsWith('.csv') ? outfile : `${outfile}.csv`);
      } else {
        printCsv([result]);
      }
      break;
    case 'console':
    default:
      if (compact) {
        printCompactResult(result);
      } else {
        printResults(result);
      }
      break;
  }
}

/**
 * Create the CLI program.
 */
export function createProgram(): Command {
  const program = new Command();

  program
    .name('monte-carlo')
    .description('Monte Carlo combat simulator for Pits of Despair')
    .version('1.0.0');

  // Duel command
  program
    .command('duel <creatureA> <creatureB>')
    .description('Run a 1v1 duel simulation')
    .option('-n, --iterations <number>', 'Number of iterations', '1000')
    .option('-s, --seed <number>', 'Random seed for reproducibility')
    .option('--equip-a <items>', 'Equipment overrides for creature A (comma-separated)')
    .option('--equip-b <items>', 'Equipment overrides for creature B (comma-separated)')
    .option('-o, --output <format>', 'Output format: console, json, csv', 'console')
    .option('--outfile <path>', 'Output file path (for json/csv)')
    .option('-c, --compact', 'Compact console output')
    .action((creatureA, creatureB, options) => {
      const iterations = parseInt(options.iterations, 10);
      const seed = options.seed ? parseInt(options.seed, 10) : undefined;
      const rng = seed !== undefined ? new SeededRng(seed) : new SeededRng(Date.now());

      const gameData = loadGameData();

      const equipA = options.equipA?.split(',').map((s: string) => s.trim());
      const equipB = options.equipB?.split(',').map((s: string) => s.trim());

      const result = runDuel(
        {
          creatureA,
          creatureB,
          iterations,
          equipmentOverridesA: equipA,
          equipmentOverridesB: equipB,
        },
        gameData,
        rng
      );

      handleOutput(result, options.output as OutputFormat, options.outfile, options.compact);
    });

  // Group battle command
  program
    .command('group <teamA> <teamB>')
    .description('Run a group battle simulation (e.g., "goblin:3" "skeleton:2")')
    .option('-n, --iterations <number>', 'Number of iterations', '1000')
    .option('-s, --seed <number>', 'Random seed for reproducibility')
    .option('-o, --output <format>', 'Output format: console, json, csv', 'console')
    .option('--outfile <path>', 'Output file path (for json/csv)')
    .option('-c, --compact', 'Compact console output')
    .action((teamAStr, teamBStr, options) => {
      const iterations = parseInt(options.iterations, 10);
      const seed = options.seed ? parseInt(options.seed, 10) : undefined;
      const rng = seed !== undefined ? new SeededRng(seed) : new SeededRng(Date.now());

      const gameData = loadGameData();

      const teamA = parseTeamString(teamAStr);
      const teamB = parseTeamString(teamBStr);

      const result = runGroupBattle({ teamA, teamB, iterations }, gameData, rng);

      handleOutput(result, options.output as OutputFormat, options.outfile, options.compact);
    });

  // Variation command
  program
    .command('variation <creature> <opponent>')
    .description('Test different equipment loadouts (e.g., variation goblin skeleton --var "club:weapon_club" --var "spear:weapon_spear")')
    .option('-n, --iterations <number>', 'Number of iterations per variation', '1000')
    .option('-s, --seed <number>', 'Random seed for reproducibility')
    .option('--var <spec>', 'Variation spec "name:item1,item2" (can repeat)', (val, prev: string[]) => [...prev, val], [])
    .action((creature, opponent, options) => {
      const iterations = parseInt(options.iterations, 10);
      const seed = options.seed ? parseInt(options.seed, 10) : undefined;
      const rng = seed !== undefined ? new SeededRng(seed) : new SeededRng(Date.now());

      if (options.var.length === 0) {
        console.error('At least one --var option required');
        process.exit(1);
      }

      const variations = options.var.map((spec: string) => {
        const [name, ...equipParts] = spec.split(':');
        const equipment = equipParts.join(':').split(',').map((s: string) => s.trim()).filter(Boolean);
        return {
          name: name.trim(),
          equipmentOverrides: equipment.length > 0 ? equipment : undefined,
        };
      });

      const gameData = loadGameData();
      const results = runVariations(
        { baseCreature: creature, opponent, variations, iterations },
        gameData,
        rng
      );

      printVariationResults(results);
    });

  // Matrix command - run all creatures vs all creatures
  program
    .command('matrix')
    .description('Run all creatures against each other')
    .option('-n, --iterations <number>', 'Number of iterations per matchup', '500')
    .option('-s, --seed <number>', 'Random seed')
    .option('-o, --output <format>', 'Output format: console, csv', 'console')
    .option('--outfile <path>', 'Output file path')
    .action((options) => {
      const iterations = parseInt(options.iterations, 10);
      const seed = options.seed ? parseInt(options.seed, 10) : undefined;
      const rng = seed !== undefined ? new SeededRng(seed) : new SeededRng(Date.now());

      const gameData = loadGameData();
      const creatures = listCreatures(gameData);
      const results: AggregateResult[] = [];

      console.log(`\nRunning ${creatures.length}x${creatures.length} matrix (${creatures.length * creatures.length} matchups)...\n`);

      for (const a of creatures) {
        for (const b of creatures) {
          if (a === b) continue;
          const result = runDuel({ creatureA: a, creatureB: b, iterations }, gameData, rng);
          results.push(result);
          printCompactResult(result);
        }
      }

      if (options.output === 'csv' && options.outfile) {
        writeCsvFile(results, options.outfile.endsWith('.csv') ? options.outfile : `${options.outfile}.csv`);
      }
    });

  // List creatures command
  program
    .command('list <type>')
    .description('List available creatures or items')
    .action((type) => {
      const gameData = loadGameData();

      if (type === 'creatures' || type === 'creature') {
        const creatures = listCreatures(gameData);
        console.log('\nAvailable creatures:');
        for (const id of creatures) {
          const creature = getCreature(id, gameData);
          console.log(`  ${id}: ${creature.name} (threat ${creature.threat})`);
        }
        console.log(`\nTotal: ${creatures.length} creatures`);
      } else if (type === 'items' || type === 'item') {
        const items = listItems(gameData);
        // Filter out prefixed duplicates
        const uniqueItems = items.filter(
          (id) =>
            !id.startsWith('weapon_') &&
            !id.startsWith('armor_') &&
            !id.startsWith('ring_') &&
            !id.startsWith('ammo_')
        );
        console.log('\nAvailable items:');
        for (const id of uniqueItems) {
          const item = getItem(id, gameData);
          console.log(`  ${id}: ${item.name} (${item.type})`);
        }
        console.log(`\nTotal: ${uniqueItems.length} items`);
      } else {
        console.error(`Unknown type: ${type}. Use 'creatures' or 'items'.`);
        process.exit(1);
      }
    });

  // Info command
  program
    .command('info <id>')
    .description('Show details about a creature or item')
    .action((id) => {
      const gameData = loadGameData();

      // Try creature first
      try {
        const creature = getCreature(id, gameData);
        console.log('\n=== Creature ===');
        console.log(`ID: ${creature.id}`);
        console.log(`Name: ${creature.name}`);
        console.log(`Type: ${creature.type}`);
        console.log(`Threat: ${creature.threat}`);
        console.log(`\nStats:`);
        console.log(`  STR: ${creature.strength}`);
        console.log(`  AGI: ${creature.agility}`);
        console.log(`  END: ${creature.endurance}`);
        console.log(`  WIL: ${creature.will}`);
        console.log(`  Base HP: ${creature.health}`);
        console.log(`  Max HP: ${calculateMaxHealth(creature.health, creature.endurance)}`);
        console.log(`  Speed: ${creature.speed}`);

        if (creature.attacks.length > 0) {
          console.log(`\nNatural Attacks:`);
          for (const attack of creature.attacks) {
            console.log(`  ${attack.name}: ${attack.dice} ${attack.damageType} (${attack.type}, range ${attack.range})`);
          }
        }

        if (creature.equipment.length > 0) {
          console.log(`\nEquipment:`);
          for (const equip of creature.equipment) {
            const equipId = typeof equip === 'string' ? equip : `${equip.id} x${equip.quantity}`;
            console.log(`  - ${equipId}`);
          }
        }

        if (creature.immunities.length > 0) {
          console.log(`\nImmunities: ${creature.immunities.join(', ')}`);
        }
        if (creature.resistances.length > 0) {
          console.log(`Resistances: ${creature.resistances.join(', ')}`);
        }
        if (creature.vulnerabilities.length > 0) {
          console.log(`Vulnerabilities: ${creature.vulnerabilities.join(', ')}`);
        }

        console.log('');
        return;
      } catch {
        // Not a creature, try item
      }

      try {
        const item = getItem(id, gameData);
        console.log('\n=== Item ===');
        console.log(`ID: ${item.id}`);
        console.log(`Name: ${item.name}`);
        console.log(`Type: ${item.type}`);

        if (item.attack) {
          console.log(`\nAttack:`);
          console.log(`  Dice: ${item.attack.dice}`);
          console.log(`  Damage: ${item.attack.damageType}`);
          console.log(`  Type: ${item.attack.type}`);
          console.log(`  Range: ${item.attack.range}`);
          if (item.attack.ammoType) {
            console.log(`  Ammo: ${item.attack.ammoType}`);
          }
        }

        const stats: string[] = [];
        if (item.armor) stats.push(`Armor: ${item.armor}`);
        if (item.evasion) stats.push(`Evasion: ${item.evasion}`);
        if (item.strength) stats.push(`STR: ${item.strength}`);
        if (item.agility) stats.push(`AGI: ${item.agility}`);
        if (item.endurance) stats.push(`END: ${item.endurance}`);
        if (item.regen) stats.push(`Regen: +${item.regen}`);

        if (stats.length > 0) {
          console.log(`\nStats: ${stats.join(', ')}`);
        }

        console.log('');
        return;
      } catch {
        // Not found
      }

      console.error(`Not found: ${id}`);
      process.exit(1);
    });

  return program;
}
