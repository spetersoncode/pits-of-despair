/**
 * Inline creature parser for JSON CLI input.
 * Parses inline JSON creature definitions and converts them to CreatureDefinition.
 */

import { getCreature, searchCreatures, type GameData } from './data-loader.js';
import type {
  CreatureDefinition,
  InlineCreature,
  InlineAttack,
  AttackDefinition,
  DamageType,
} from './types.js';
import { DAMAGE_TYPES } from './types.js';

// =============================================================================
// Default Values (matches yaml-parser.ts)
// =============================================================================

const CREATURE_DEFAULTS: Omit<CreatureDefinition, 'id' | 'name' | 'type'> = {
  description: '',
  glyph: '?',
  color: 'Palette.Default',
  threat: 1,
  strength: 0,
  agility: 0,
  endurance: 0,
  will: 0,
  health: 10,
  speed: 10,
  equipment: [],
  attacks: [],
  skills: [],
  immunities: [],
  resistances: [],
  vulnerabilities: [],
};

// =============================================================================
// Known Fields (for typo detection)
// =============================================================================

const KNOWN_FIELDS = new Set([
  'base',
  'name',
  'strength',
  'agility',
  'endurance',
  'will',
  'health',
  'speed',
  'equipment',
  'attacks',
  'resistances',
  'vulnerabilities',
  'immunities',
]);

// =============================================================================
// Validation & Parsing
// =============================================================================

/**
 * Find similar creature IDs for "did you mean" suggestions.
 */
function findSimilarCreatures(id: string, gameData: GameData): string[] {
  const results = searchCreatures(id.substring(0, 3), gameData);
  return results.slice(0, 3).map((c) => c.id);
}

/**
 * Validate damage types and return any invalid ones.
 */
function validateDamageTypes(types: unknown[]): { valid: DamageType[]; invalid: string[] } {
  const valid: DamageType[] = [];
  const invalid: string[] = [];

  for (const t of types) {
    if (typeof t === 'string' && DAMAGE_TYPES.includes(t as DamageType)) {
      valid.push(t as DamageType);
    } else {
      invalid.push(String(t));
    }
  }

  return { valid, invalid };
}

/**
 * Parse inline attack definition to full AttackDefinition.
 */
function parseInlineAttack(attack: InlineAttack): AttackDefinition {
  return {
    name: attack.name,
    type: attack.type,
    dice: attack.dice,
    damageType: attack.damageType,
    range: attack.range ?? (attack.type === 'Ranged' ? 6 : 1),
    delay: attack.delay ?? 1.0,
    ammoType: attack.ammoType,
  };
}

/**
 * Check for unknown fields and warn about potential typos.
 */
function checkUnknownFields(obj: Record<string, unknown>): string[] {
  const warnings: string[] = [];

  for (const key of Object.keys(obj)) {
    if (!KNOWN_FIELDS.has(key)) {
      // Check for common typos
      const suggestions: string[] = [];
      if (key.toLowerCase().includes('str')) suggestions.push('strength');
      if (key.toLowerCase().includes('agi')) suggestions.push('agility');
      if (key.toLowerCase().includes('end')) suggestions.push('endurance');
      if (key.toLowerCase().includes('hp') || key.toLowerCase().includes('heal')) suggestions.push('health');
      if (key.toLowerCase().includes('spd')) suggestions.push('speed');
      if (key.toLowerCase().includes('resist')) suggestions.push('resistances');
      if (key.toLowerCase().includes('vuln')) suggestions.push('vulnerabilities');
      if (key.toLowerCase().includes('immun')) suggestions.push('immunities');
      if (key.toLowerCase().includes('equip') || key.toLowerCase().includes('gear')) suggestions.push('equipment');
      if (key.toLowerCase().includes('atk') || key.toLowerCase().includes('attack')) suggestions.push('attacks');

      if (suggestions.length > 0) {
        warnings.push(`Unknown field '${key}' (did you mean '${suggestions[0]}'?)`);
      } else {
        warnings.push(`Unknown field '${key}' in inline creature`);
      }
    }
  }

  return warnings;
}

// =============================================================================
// Main Parser
// =============================================================================

export interface ParseResult {
  creature: CreatureDefinition;
  warnings: string[];
}

/**
 * Parse an inline creature JSON string into a CreatureDefinition.
 *
 * Supports two modes:
 * 1. Layer on base: { base: "goblin", strength: 4 } - inherits from goblin, overrides strength
 * 2. Complete definition: { name: "custom", health: 15, ... } - creates from scratch
 *
 * @param json The JSON string to parse
 * @param gameData Game data for resolving base creatures
 * @returns ParseResult with creature and any warnings
 * @throws Error if JSON is invalid or required fields are missing
 */
export function parseInlineCreature(json: string, gameData: GameData): ParseResult {
  const warnings: string[] = [];

  // Parse JSON
  let parsed: unknown;
  try {
    parsed = JSON.parse(json);
  } catch (e) {
    throw new Error(`Invalid JSON: ${(e as Error).message}`);
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    throw new Error('Inline creature must be a JSON object');
  }

  const inline = parsed as InlineCreature & Record<string, unknown>;

  // Check for unknown fields
  warnings.push(...checkUnknownFields(inline));

  // Determine mode: base creature or from scratch
  let baseCreature: CreatureDefinition | null = null;

  if (inline.base) {
    try {
      baseCreature = getCreature(inline.base, gameData);
    } catch {
      const similar = findSimilarCreatures(inline.base, gameData);
      const suggestion = similar.length > 0 ? ` (did you mean '${similar[0]}'?)` : '';
      throw new Error(`Base creature '${inline.base}' not found${suggestion}`);
    }
  }

  // Validate required fields
  if (!baseCreature && !inline.name) {
    throw new Error("Inline creature requires 'name' when no 'base' is specified");
  }

  // Build the creature definition
  const creature: CreatureDefinition = baseCreature
    ? { ...baseCreature }
    : {
        id: `inline_${inline.name}`,
        name: inline.name!,
        type: 'inline',
        ...CREATURE_DEFAULTS,
      };

  // Apply name override (for labeling in results)
  if (inline.name) {
    creature.name = inline.name;
    creature.id = `inline_${inline.name}`;
  }

  // Apply stat overrides
  if (inline.strength !== undefined) creature.strength = inline.strength;
  if (inline.agility !== undefined) creature.agility = inline.agility;
  if (inline.endurance !== undefined) creature.endurance = inline.endurance;
  if (inline.will !== undefined) creature.will = inline.will;
  if (inline.health !== undefined) creature.health = inline.health;
  if (inline.speed !== undefined) creature.speed = inline.speed;

  // Apply equipment override (replaces, doesn't merge)
  if (inline.equipment !== undefined) {
    creature.equipment = inline.equipment;
  }

  // Apply attacks override (replaces, doesn't merge)
  if (inline.attacks !== undefined) {
    creature.attacks = inline.attacks.map(parseInlineAttack);
  }

  // Apply damage modifiers (replaces, doesn't merge)
  if (inline.resistances !== undefined) {
    const { valid, invalid } = validateDamageTypes(inline.resistances);
    creature.resistances = valid;
    if (invalid.length > 0) {
      throw new Error(`Invalid damage type(s) in resistances: ${invalid.join(', ')}`);
    }
  }

  if (inline.vulnerabilities !== undefined) {
    const { valid, invalid } = validateDamageTypes(inline.vulnerabilities);
    creature.vulnerabilities = valid;
    if (invalid.length > 0) {
      throw new Error(`Invalid damage type(s) in vulnerabilities: ${invalid.join(', ')}`);
    }
  }

  if (inline.immunities !== undefined) {
    const { valid, invalid } = validateDamageTypes(inline.immunities);
    creature.immunities = valid;
    if (invalid.length > 0) {
      throw new Error(`Invalid damage type(s) in immunities: ${invalid.join(', ')}`);
    }
  }

  // Validate final creature
  if (creature.health <= 0) {
    throw new Error(`Invalid health value: ${creature.health} (must be > 0)`);
  }
  if (creature.speed <= 0) {
    throw new Error(`Invalid speed value: ${creature.speed} (must be > 0)`);
  }

  return { creature, warnings };
}

/**
 * Parse an array of inline creatures from a JSON array string.
 *
 * @param json JSON array string of inline creatures
 * @param gameData Game data for resolving base creatures
 * @returns Array of ParseResults
 */
export function parseInlineCreatures(json: string, gameData: GameData): ParseResult[] {
  let parsed: unknown;
  try {
    parsed = JSON.parse(json);
  } catch (e) {
    throw new Error(`Invalid JSON array: ${(e as Error).message}`);
  }

  if (!Array.isArray(parsed)) {
    throw new Error('Expected a JSON array of inline creatures');
  }

  return parsed.map((item, index) => {
    try {
      return parseInlineCreature(JSON.stringify(item), gameData);
    } catch (e) {
      throw new Error(`Invalid inline creature at index ${index}: ${(e as Error).message}`);
    }
  });
}
