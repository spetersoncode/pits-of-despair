/**
 * Type definitions for the Monte Carlo combat simulator.
 * These types mirror the YAML schemas used by Pits of Despair.
 */

// Re-export GameData for convenience (defined in data-loader.ts)
export type { GameData } from './data-loader.js';

// =============================================================================
// Damage Types
// =============================================================================

export type DamageType =
  | 'Bludgeoning'
  | 'Slashing'
  | 'Piercing'
  | 'Poison'
  | 'Fire'
  | 'Cold'
  | 'Necrotic';

export const DAMAGE_TYPES: readonly DamageType[] = [
  'Bludgeoning',
  'Slashing',
  'Piercing',
  'Poison',
  'Fire',
  'Cold',
  'Necrotic',
] as const;

// =============================================================================
// Attack Types
// =============================================================================

export type AttackType = 'Melee' | 'Ranged';

// =============================================================================
// Equipment Slots
// =============================================================================

export type EquipmentSlot =
  | 'MeleeWeapon'
  | 'RangedWeapon'
  | 'Armor'
  | 'Ring1'
  | 'Ring2'
  | 'Ammo';

// =============================================================================
// Item Types (from YAML type field)
// =============================================================================

export type ItemType =
  | 'weapon'
  | 'armor'
  | 'ring'
  | 'ammo'
  | 'potion'
  | 'scroll'
  | 'wand'
  | 'staff';

// =============================================================================
// Attack Definition
// =============================================================================

/**
 * Defines an attack (natural or from a weapon).
 */
export interface AttackDefinition {
  name: string;
  type: AttackType;
  dice: string; // Dice notation, e.g., "1d6", "2d4+2"
  damageType: DamageType;
  range: number; // 1 for standard melee, 2 for reach, higher for ranged
  ammoType?: string; // Required ammo type for ranged weapons
}

// =============================================================================
// Skill Definitions
// =============================================================================

/**
 * Defines a skill (active ability, spell, etc.).
 */
export interface SkillDefinition {
  id: string;
  name: string;
  description?: string;
  category: 'active' | 'passive' | 'reactive';
  targeting: string; // 'enemy', 'self', 'ally', 'line', 'area', etc.
  range: number;
  willpowerCost: number;
  effects: SkillEffect[];
}

/**
 * A single effect block within a skill (may have multiple steps).
 */
export interface SkillEffect {
  steps: EffectStep[];
}

/**
 * A step within an effect (damage, attack_roll, heal, etc.).
 * Only modeling what's needed for simple damage skills like magic_missile.
 */
export interface EffectStep {
  type: 'damage' | 'attack_roll' | 'heal'; // Expand as needed
  dice?: string;
  damageType?: DamageType;
  scalingStat?: string; // 'str', 'agi', 'end', 'wil'
  scalingMultiplier?: number;
  attackStat?: string; // For attack_roll steps
  stopOnMiss?: boolean; // For attack_roll steps
}

/**
 * Structure of skill YAML files.
 */
export interface SkillYamlFile {
  type: string; // Category name (e.g., 'willpower')
  entries: Record<string, Partial<Omit<SkillDefinition, 'id'>>>;
}

// =============================================================================
// Equipment Entry (in creature definitions)
// =============================================================================

/**
 * Equipment can be a simple string ID or an object with quantity (for ammo).
 */
export type EquipmentEntry = string | { id: string; quantity: number };

// =============================================================================
// Creature Definition (from YAML)
// =============================================================================

/**
 * Raw creature definition as parsed from YAML.
 */
export interface CreatureDefinition {
  id: string;
  name: string;
  description?: string;
  type: string; // Category (goblinoid, undead, rodent, etc.)
  glyph: string;
  color: string;

  // Power level
  threat: number;

  // Base stats (all default to 0)
  strength: number;
  agility: number;
  endurance: number;
  will: number;

  // Health and speed
  health: number; // Base HP before END bonus
  speed: number; // Default 10

  // Combat
  equipment: EquipmentEntry[];
  attacks: AttackDefinition[]; // Natural attacks
  skills: string[]; // Skill IDs (resolved at combatant creation)

  // Damage modifiers
  immunities: DamageType[];
  resistances: DamageType[];
  vulnerabilities: DamageType[];

  // AI behaviors (for reference, not used in simulation)
  ai?: { type: string }[];

  // Vision (for reference)
  visionRange?: number;
}

// =============================================================================
// Item Definition (from YAML)
// =============================================================================

/**
 * Raw item definition as parsed from YAML.
 */
export interface ItemDefinition {
  id: string;
  name: string;
  description?: string;
  type: ItemType;
  glyph: string;
  color: string;

  // Weapon properties
  attack?: AttackDefinition;

  // Armor properties
  armor?: number;
  evasion?: number;

  // Stat bonuses (from rings, etc.)
  strength?: number;
  agility?: number;
  endurance?: number;
  will?: number;

  // Regen bonus (rings)
  regen?: number;

  // Speed modifier
  speed?: number;
}

// =============================================================================
// YAML File Structure
// =============================================================================

/**
 * Structure of creature YAML files.
 */
export interface CreatureYamlFile {
  type: string;
  defaults?: Partial<Omit<CreatureDefinition, 'id'>>;
  entries: Record<string, Partial<Omit<CreatureDefinition, 'id'>>>;
}

/**
 * Structure of item YAML files.
 */
export interface ItemYamlFile {
  type: ItemType;
  defaults?: Partial<Omit<ItemDefinition, 'id'>>;
  entries: Record<string, Partial<Omit<ItemDefinition, 'id'>>>;
}

// =============================================================================
// Runtime Types (for simulation)
// =============================================================================

/**
 * Position on the combat arena.
 */
export interface Position {
  x: number;
  y: number;
}

/**
 * Runtime combatant state during simulation.
 */
export interface Combatant {
  id: string;
  name: string;
  team: 'A' | 'B';

  // Effective stats (after equipment)
  strength: number;
  agility: number;
  endurance: number;
  will: number;
  armor: number;
  evasion: number;
  speed: number;
  regenBonus: number;

  // Health
  maxHealth: number;
  currentHealth: number;

  // Willpower
  maxWillpower: number;
  currentWillpower: number;

  // Position
  position: Position;

  // Combat capabilities
  attacks: AttackDefinition[];
  skills: SkillDefinition[];

  // Damage modifiers
  immunities: Set<DamageType>;
  resistances: Set<DamageType>;
  vulnerabilities: Set<DamageType>;

  // Turn tracking
  accumulatedTime: number;
  regenPoints: number;
  wpRegenPoints: number;

  // Equipment reference (for ammo tracking)
  ammo: Map<string, number>; // ammoType -> count
}

// =============================================================================
// Simulation Results
// =============================================================================

/**
 * Result of a single combat simulation.
 */
export interface SimulationResult {
  winner: 'A' | 'B' | 'draw';
  turns: number;
  teamADamageDealt: number;
  teamBDamageDealt: number;
  teamASurvivors: number;
  teamBSurvivors: number;
  teamASurvivorHealth: number; // Total remaining HP
  teamBSurvivorHealth: number;
}

/**
 * Aggregated results from multiple simulations.
 */
export interface AggregateResult {
  scenario: string;
  iterations: number;
  teamAWins: number;
  teamBWins: number;
  draws: number;
  teamAWinRate: number;
  teamBWinRate: number;
  avgTurns: number;
  avgTeamADamage: number;
  avgTeamBDamage: number;
  avgTeamASurvivors: number;
  avgTeamBSurvivors: number;
  avgTeamASurvivorHealth: number;
  avgTeamBSurvivorHealth: number;
  confidenceInterval95: number; // For win rate
}

// =============================================================================
// Scenario Configuration
// =============================================================================

/**
 * Configuration for a duel scenario.
 */
export interface DuelConfig {
  creatureA: string;
  creatureB: string;
  equipmentOverridesA?: string[];
  equipmentOverridesB?: string[];
  iterations: number;
  // Optional inline creature definitions (takes precedence over ID lookup)
  inlineCreatureA?: CreatureDefinition;
  inlineCreatureB?: CreatureDefinition;
}

/**
 * Team member specification.
 */
export interface TeamMember {
  creatureId: string;
  count: number;
  equipmentOverrides?: string[];
}

/**
 * Configuration for a group battle scenario.
 */
export interface GroupConfig {
  teamA: TeamMember[];
  teamB: TeamMember[];
  iterations: number;
}

/**
 * Variation for testing different loadouts.
 */
export interface Variation {
  name: string;
  equipmentOverrides?: string[];
  statOverrides?: Partial<{
    strength: number;
    agility: number;
    endurance: number;
    will: number;
    health: number;
    speed: number;
  }>;
}

// =============================================================================
// Inline Creature Definition (for CLI JSON input)
// =============================================================================

/**
 * Inline creature definition for JSON CLI input.
 * Supports two modes:
 * 1. Layer on base creature: { base: "goblin", strength: 4 }
 * 2. Complete definition: { name: "custom", strength: 2, health: 10, ... }
 */
export interface InlineCreature {
  // Mode 1: Inherit from existing creature
  base?: string;

  // Mode 2: Define from scratch (name required if no base)
  name?: string;

  // Stat overrides (applied on top of base, or used directly)
  strength?: number;
  agility?: number;
  endurance?: number;
  will?: number;
  health?: number;
  speed?: number;

  // Equipment (replaces base equipment if specified)
  equipment?: string[];

  // Natural attacks (replaces base attacks if specified)
  attacks?: InlineAttack[];

  // Damage modifiers (replaces base if specified)
  resistances?: DamageType[];
  vulnerabilities?: DamageType[];
  immunities?: DamageType[];
}

/**
 * Inline attack definition for JSON input.
 */
export interface InlineAttack {
  name: string;
  type: AttackType;
  dice: string;
  damageType: DamageType;
  range?: number; // Defaults to 1 for melee, 6 for ranged
  ammoType?: string;
}

/**
 * Configuration for variation testing scenario.
 */
export interface VariationConfig {
  baseCreature: string;
  opponent: string;
  variations: Variation[];
  iterations: number;
}

/**
 * Factor to test in isolation.
 */
export interface Factor {
  target: 'A' | 'B';
  stat?: string;
  delta?: number;
  equipment?: string;
}

/**
 * Configuration for factor testing scenario.
 */
export interface FactorConfig {
  creatureA: string;
  creatureB: string;
  factor: Factor;
  iterations: number;
}
