/**
 * Aggregate data loader with validation.
 * Loads all creature and item definitions from the game's Data directory.
 */

import { readdirSync } from 'fs';
import { join, resolve } from 'path';
import { parseCreatureFile, parseItemFile } from './YamlParser.js';
import type { CreatureDefinition, ItemDefinition } from './types.js';

// =============================================================================
// Data Store
// =============================================================================

export interface GameData {
  creatures: Map<string, CreatureDefinition>;
  items: Map<string, ItemDefinition>;
}

// =============================================================================
// Path Resolution
// =============================================================================

/**
 * Find the project root by looking for the Data directory.
 * Searches up from the current working directory.
 */
function findProjectRoot(startDir: string = process.cwd()): string {
  let current = resolve(startDir);
  const root = resolve('/');

  while (current !== root) {
    const dataPath = join(current, 'Data');
    try {
      const stat = readdirSync(dataPath);
      if (stat) {
        return current;
      }
    } catch {
      // Directory doesn't exist, continue searching
    }
    current = resolve(current, '..');
  }

  throw new Error(
    'Could not find project root (Data directory not found). ' +
      'Run from within the Pits of Despair project directory.'
  );
}

/**
 * Get the path to the Data directory.
 */
export function getDataPath(projectRoot?: string): string {
  const root = projectRoot ?? findProjectRoot();
  return join(root, 'Data');
}

// =============================================================================
// File Discovery
// =============================================================================

/**
 * Get all YAML files in a directory (excluding templates).
 */
function getYamlFiles(dirPath: string): string[] {
  const files = readdirSync(dirPath);
  return files
    .filter((f) => f.endsWith('.yaml') && !f.startsWith('_'))
    .map((f) => join(dirPath, f));
}

// =============================================================================
// Loading
// =============================================================================

/**
 * Load all creature definitions from the Data/Creatures directory.
 */
export function loadCreatures(dataPath: string): Map<string, CreatureDefinition> {
  const creaturesPath = join(dataPath, 'Creatures');
  const files = getYamlFiles(creaturesPath);
  const creatures = new Map<string, CreatureDefinition>();

  for (const file of files) {
    const parsed = parseCreatureFile(file);
    for (const creature of parsed) {
      if (creatures.has(creature.id)) {
        console.warn(`Duplicate creature ID: ${creature.id}`);
      }
      creatures.set(creature.id, creature);
    }
  }

  return creatures;
}

/**
 * Common prefixes used in creature equipment references.
 * Maps item type to the prefix used in creature YAML files.
 */
const ITEM_TYPE_PREFIXES: Record<string, string> = {
  weapon: 'weapon_',
  armor: 'armor_',
  ring: 'ring_',
  ammo: 'ammo_',
  potion: 'potion_',
  scroll: 'scroll_',
  wand: 'wand_',
  staff: 'staff_',
};

/**
 * Load all item definitions from the Data/Items directory.
 * Also creates prefixed aliases (e.g., "club" -> "weapon_club") for lookup.
 */
export function loadItems(dataPath: string): Map<string, ItemDefinition> {
  const itemsPath = join(dataPath, 'Items');
  const files = getYamlFiles(itemsPath);
  const items = new Map<string, ItemDefinition>();

  for (const file of files) {
    const parsed = parseItemFile(file);
    for (const item of parsed) {
      if (items.has(item.id)) {
        console.warn(`Duplicate item ID: ${item.id}`);
      }
      // Store with original ID
      items.set(item.id, item);

      // Also store with type prefix for creature equipment lookups
      const prefix = ITEM_TYPE_PREFIXES[item.type];
      if (prefix) {
        const prefixedId = `${prefix}${item.id}`;
        if (!items.has(prefixedId)) {
          items.set(prefixedId, item);
        }
      }
    }
  }

  return items;
}

/**
 * Load all game data (creatures and items).
 */
export function loadGameData(projectRoot?: string): GameData {
  const dataPath = getDataPath(projectRoot);

  console.log(`Loading game data from: ${dataPath}`);

  const creatures = loadCreatures(dataPath);
  const items = loadItems(dataPath);

  console.log(`Loaded ${creatures.size} creatures and ${items.size} items`);

  return { creatures, items };
}

// =============================================================================
// Validation
// =============================================================================

export interface ValidationResult {
  valid: boolean;
  errors: string[];
  warnings: string[];
}

/**
 * Validate loaded game data.
 */
export function validateGameData(data: GameData): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  // Validate creatures
  for (const [id, creature] of data.creatures) {
    // Check equipment references
    for (const equipment of creature.equipment) {
      const itemId = typeof equipment === 'string' ? equipment : equipment.id;
      if (!data.items.has(itemId)) {
        errors.push(
          `Creature "${id}" references unknown item: "${itemId}"`
        );
      }
    }

    // Check for creatures with no attacks and no equipment
    if (creature.attacks.length === 0 && creature.equipment.length === 0) {
      warnings.push(
        `Creature "${id}" has no attacks and no equipment`
      );
    }

    // Check for invalid stats
    if (creature.health <= 0) {
      errors.push(`Creature "${id}" has invalid health: ${creature.health}`);
    }
    if (creature.speed <= 0) {
      errors.push(`Creature "${id}" has invalid speed: ${creature.speed}`);
    }
  }

  // Validate items
  for (const [id, item] of data.items) {
    // Check weapons have attack definitions
    if (item.type === 'weapon' && !item.attack) {
      errors.push(`Weapon "${id}" has no attack definition`);
    }

    // Check armor values
    if (item.type === 'armor' && item.armor === undefined) {
      warnings.push(`Armor "${id}" has no armor value`);
    }
  }

  return {
    valid: errors.length === 0,
    errors,
    warnings,
  };
}

// =============================================================================
// Singleton Loader
// =============================================================================

let _gameData: GameData | null = null;

/**
 * Get loaded game data (loads on first call).
 */
export function getGameData(projectRoot?: string): GameData {
  if (!_gameData) {
    _gameData = loadGameData(projectRoot);
  }
  return _gameData;
}

/**
 * Clear cached game data (for reloading).
 */
export function clearGameData(): void {
  _gameData = null;
}

// =============================================================================
// Lookup Helpers
// =============================================================================

/**
 * Get a creature by ID.
 * @throws Error if creature not found
 */
export function getCreature(
  id: string,
  data?: GameData
): CreatureDefinition {
  const gameData = data ?? getGameData();
  const creature = gameData.creatures.get(id);
  if (!creature) {
    throw new Error(`Creature not found: "${id}"`);
  }
  return creature;
}

/**
 * Get an item by ID.
 * @throws Error if item not found
 */
export function getItem(id: string, data?: GameData): ItemDefinition {
  const gameData = data ?? getGameData();
  const item = gameData.items.get(id);
  if (!item) {
    throw new Error(`Item not found: "${id}"`);
  }
  return item;
}

/**
 * List all creature IDs.
 */
export function listCreatures(data?: GameData): string[] {
  const gameData = data ?? getGameData();
  return Array.from(gameData.creatures.keys()).sort();
}

/**
 * List all item IDs.
 */
export function listItems(data?: GameData): string[] {
  const gameData = data ?? getGameData();
  return Array.from(gameData.items.keys()).sort();
}

/**
 * Search creatures by name (case-insensitive partial match).
 */
export function searchCreatures(
  query: string,
  data?: GameData
): CreatureDefinition[] {
  const gameData = data ?? getGameData();
  const lowerQuery = query.toLowerCase();
  return Array.from(gameData.creatures.values()).filter(
    (c) =>
      c.id.toLowerCase().includes(lowerQuery) ||
      c.name.toLowerCase().includes(lowerQuery)
  );
}

/**
 * Search items by name (case-insensitive partial match).
 */
export function searchItems(
  query: string,
  data?: GameData
): ItemDefinition[] {
  const gameData = data ?? getGameData();
  const lowerQuery = query.toLowerCase();
  return Array.from(gameData.items.values()).filter(
    (i) =>
      i.id.toLowerCase().includes(lowerQuery) ||
      i.name.toLowerCase().includes(lowerQuery)
  );
}
