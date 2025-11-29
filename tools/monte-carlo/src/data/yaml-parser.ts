/**
 * YAML parser for creature and item definitions.
 * Reads the game's YAML data files and converts them to TypeScript types.
 */

import { readFileSync } from 'fs';
import { parse as parseYaml } from 'yaml';
import type {
  CreatureDefinition,
  CreatureYamlFile,
  ItemDefinition,
  ItemYamlFile,
  AttackDefinition,
  DamageType,
  EquipmentEntry,
  ItemType,
  SkillDefinition,
  SkillYamlFile,
  SkillEffect,
  EffectStep,
} from './types.js';

// =============================================================================
// Default Values
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

const ITEM_DEFAULTS: Omit<ItemDefinition, 'id' | 'name' | 'type'> = {
  description: '',
  glyph: '?',
  color: 'Palette.Default',
};

// =============================================================================
// Parsing Utilities
// =============================================================================

/**
 * Parse an attack definition from YAML.
 */
function parseAttack(
  data: Record<string, unknown>,
  name?: string
): AttackDefinition {
  return {
    name: (data.name as string) ?? name ?? 'attack',
    type: (data.type as 'Melee' | 'Ranged') ?? 'Melee',
    dice: (data.dice as string) ?? '1d4',
    damageType: (data.damageType as DamageType) ?? 'Bludgeoning',
    range: (data.range as number) ?? 1,
    delay: (data.delay as number) ?? 1.0,
    ammoType: data.ammoType as string | undefined,
  };
}

/**
 * Parse an array of attacks from YAML.
 */
function parseAttacks(data: unknown): AttackDefinition[] {
  if (!Array.isArray(data)) {
    return [];
  }

  return data.map((attack, index) => {
    if (typeof attack === 'object' && attack !== null) {
      return parseAttack(attack as Record<string, unknown>);
    }
    return {
      name: `attack_${index}`,
      type: 'Melee' as const,
      dice: '1d4',
      damageType: 'Bludgeoning' as DamageType,
      range: 1,
      delay: 1.0,
    };
  });
}

/**
 * Parse damage type array from YAML.
 */
function parseDamageTypes(data: unknown): DamageType[] {
  if (!Array.isArray(data)) {
    return [];
  }
  return data.filter(
    (item): item is DamageType => typeof item === 'string'
  );
}

/**
 * Parse equipment entries from YAML.
 */
function parseEquipment(data: unknown): EquipmentEntry[] {
  if (!Array.isArray(data)) {
    return [];
  }

  return data.map((entry) => {
    if (typeof entry === 'string') {
      return entry;
    }
    if (typeof entry === 'object' && entry !== null) {
      const obj = entry as Record<string, unknown>;
      return {
        id: (obj.id as string) ?? '',
        quantity: (obj.quantity as number) ?? 1,
      };
    }
    return '';
  });
}

/**
 * Parse skill IDs from YAML (creature references skills by ID).
 */
function parseSkillIds(data: unknown): string[] {
  if (!Array.isArray(data)) {
    return [];
  }
  return data.filter((item): item is string => typeof item === 'string');
}

// =============================================================================
// Creature Parsing
// =============================================================================

/**
 * Parse a creature YAML file.
 * @param filePath Path to the YAML file
 * @returns Array of creature definitions
 */
export function parseCreatureFile(filePath: string): CreatureDefinition[] {
  const content = readFileSync(filePath, 'utf-8');
  const yaml = parseYaml(content) as CreatureYamlFile;

  const creatures: CreatureDefinition[] = [];
  const fileDefaults = yaml.defaults ?? {};
  const creatureType = yaml.type;

  for (const [id, entry] of Object.entries(yaml.entries)) {
    // Skip template entries
    if (id.startsWith('_')) {
      continue;
    }

    // Merge defaults: global defaults < file defaults < entry
    const merged = {
      ...CREATURE_DEFAULTS,
      ...fileDefaults,
      ...entry,
    };

    const creature: CreatureDefinition = {
      id,
      name: (merged.name as string) ?? id,
      description: merged.description as string,
      type: creatureType,
      glyph: merged.glyph as string,
      color: merged.color as string,
      threat: merged.threat as number,
      strength: merged.strength as number,
      agility: merged.agility as number,
      endurance: merged.endurance as number,
      will: merged.will as number,
      health: merged.health as number,
      speed: merged.speed as number,
      equipment: parseEquipment(merged.equipment),
      attacks: parseAttacks(merged.attacks),
      skills: parseSkillIds(merged.skills),
      immunities: parseDamageTypes(merged.immunities),
      resistances: parseDamageTypes(merged.resistances),
      vulnerabilities: parseDamageTypes(merged.vulnerabilities),
      ai: merged.ai as { type: string }[] | undefined,
      visionRange: merged.visionRange as number | undefined,
    };

    creatures.push(creature);
  }

  return creatures;
}

// =============================================================================
// Item Parsing
// =============================================================================

/**
 * Parse an item YAML file.
 * @param filePath Path to the YAML file
 * @returns Array of item definitions
 */
export function parseItemFile(filePath: string): ItemDefinition[] {
  const content = readFileSync(filePath, 'utf-8');
  const yaml = parseYaml(content) as ItemYamlFile;

  const items: ItemDefinition[] = [];
  const fileDefaults = yaml.defaults ?? {};
  const itemType = yaml.type;

  for (const [id, entry] of Object.entries(yaml.entries)) {
    // Skip template entries
    if (id.startsWith('_')) {
      continue;
    }

    // Merge defaults: global defaults < file defaults < entry
    const merged = {
      ...ITEM_DEFAULTS,
      ...fileDefaults,
      ...entry,
    };

    const item: ItemDefinition = {
      id,
      name: (merged.name as string) ?? id,
      description: merged.description as string,
      type: itemType,
      glyph: merged.glyph as string,
      color: merged.color as string,
    };

    // Optional properties
    if (merged.attack) {
      item.attack = parseAttack(
        merged.attack as unknown as Record<string, unknown>,
        item.name
      );
    }
    if (typeof merged.armor === 'number') {
      item.armor = merged.armor;
    }
    if (typeof merged.evasion === 'number') {
      item.evasion = merged.evasion;
    }
    if (typeof merged.strength === 'number') {
      item.strength = merged.strength;
    }
    if (typeof merged.agility === 'number') {
      item.agility = merged.agility;
    }
    if (typeof merged.endurance === 'number') {
      item.endurance = merged.endurance;
    }
    if (typeof merged.will === 'number') {
      item.will = merged.will;
    }
    if (typeof merged.regen === 'number') {
      item.regen = merged.regen;
    }
    if (typeof merged.speed === 'number') {
      item.speed = merged.speed;
    }

    items.push(item);
  }

  return items;
}

/**
 * Parse multiple creature files.
 */
export function parseCreatureFiles(filePaths: string[]): CreatureDefinition[] {
  const creatures: CreatureDefinition[] = [];
  for (const filePath of filePaths) {
    creatures.push(...parseCreatureFile(filePath));
  }
  return creatures;
}

/**
 * Parse multiple item files.
 */
export function parseItemFiles(filePaths: string[]): ItemDefinition[] {
  const items: ItemDefinition[] = [];
  for (const filePath of filePaths) {
    items.push(...parseItemFile(filePath));
  }
  return items;
}

// =============================================================================
// Skill Parsing
// =============================================================================

/**
 * Parse an effect step from YAML.
 */
function parseEffectStep(data: Record<string, unknown>): EffectStep {
  return {
    type: (data.type as EffectStep['type']) ?? 'damage',
    dice: data.dice as string | undefined,
    damageType: data.damageType as DamageType | undefined,
    scalingStat: data.scalingStat as string | undefined,
    scalingMultiplier: data.scalingMultiplier as number | undefined,
    attackStat: data.attackStat as string | undefined,
    stopOnMiss: data.stopOnMiss as boolean | undefined,
  };
}

/**
 * Parse an effect block from YAML.
 */
function parseSkillEffect(data: Record<string, unknown>): SkillEffect {
  const stepsData = data.steps;
  const steps: EffectStep[] = [];

  if (Array.isArray(stepsData)) {
    for (const step of stepsData) {
      if (typeof step === 'object' && step !== null) {
        steps.push(parseEffectStep(step as Record<string, unknown>));
      }
    }
  }

  return { steps };
}

/**
 * Parse effects array from YAML.
 */
function parseSkillEffects(data: unknown): SkillEffect[] {
  if (!Array.isArray(data)) {
    return [];
  }

  const effects: SkillEffect[] = [];
  for (const effect of data) {
    if (typeof effect === 'object' && effect !== null) {
      effects.push(parseSkillEffect(effect as Record<string, unknown>));
    }
  }
  return effects;
}

/**
 * Parse a skill YAML file.
 * @param filePath Path to the YAML file
 * @returns Array of skill definitions
 */
export function parseSkillFile(filePath: string): SkillDefinition[] {
  const content = readFileSync(filePath, 'utf-8');
  const yaml = parseYaml(content) as SkillYamlFile;

  const skills: SkillDefinition[] = [];

  for (const [id, entry] of Object.entries(yaml.entries)) {
    // Skip template entries
    if (id.startsWith('_')) {
      continue;
    }

    // Skip non-active skills (passive, reactive) for now
    const category = (entry.category as string) ?? 'active';
    if (category !== 'active') {
      continue;
    }

    const skill: SkillDefinition = {
      id,
      name: (entry.name as string) ?? id,
      description: entry.description as string | undefined,
      category: category as 'active' | 'passive' | 'reactive',
      targeting: (entry.targeting as string) ?? 'enemy',
      range: (entry.range as number) ?? 1,
      willpowerCost: (entry.willpowerCost as number) ?? 0,
      effects: parseSkillEffects(entry.effects),
    };

    skills.push(skill);
  }

  return skills;
}

/**
 * Parse multiple skill files.
 */
export function parseSkillFiles(filePaths: string[]): SkillDefinition[] {
  const skills: SkillDefinition[] = [];
  for (const filePath of filePaths) {
    skills.push(...parseSkillFile(filePath));
  }
  return skills;
}
