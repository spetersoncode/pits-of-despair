/**
 * Monte Carlo Combat Simulator for Pits of Despair
 *
 * Entry point for the CLI application.
 */

import { loadGameData, validateGameData } from './data/DataLoader.js';

// Temporary: Just load and validate data to verify setup
async function main() {
  console.log('Monte Carlo Combat Simulator');
  console.log('============================\n');

  try {
    const data = loadGameData();
    const validation = validateGameData(data);

    if (validation.warnings.length > 0) {
      console.log('\nWarnings:');
      for (const warning of validation.warnings) {
        console.log(`  - ${warning}`);
      }
    }

    if (validation.errors.length > 0) {
      console.log('\nErrors:');
      for (const error of validation.errors) {
        console.log(`  - ${error}`);
      }
    }

    if (validation.valid) {
      console.log('\nData validation passed!');
    } else {
      console.log('\nData validation failed.');
      process.exit(1);
    }

    // List some creatures and items
    console.log('\nSample creatures:');
    const creatures = Array.from(data.creatures.values()).slice(0, 5);
    for (const creature of creatures) {
      console.log(`  - ${creature.id}: ${creature.name} (threat ${creature.threat})`);
    }

    console.log('\nSample items:');
    const items = Array.from(data.items.values()).slice(0, 5);
    for (const item of items) {
      console.log(`  - ${item.id}: ${item.name} (${item.type})`);
    }
  } catch (error) {
    console.error('Error:', error);
    process.exit(1);
  }
}

main();
