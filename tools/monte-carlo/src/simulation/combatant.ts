/**
 * Combatant - Runtime combat entity with aggregated stats.
 * Creates a combatant from a creature definition and resolves equipment.
 */

import type {
  CreatureDefinition,
  ItemDefinition,
  AttackDefinition,
  SkillDefinition,
  DamageType,
  Combatant as CombatantState,
  Position,
  EquipmentEntry,
  GameData,
} from '../data/types.js';
import { getItem, getSkill } from '../data/data-loader.js';

// =============================================================================
// HP Formula
// =============================================================================

/**
 * Calculate health bonus from Endurance using quadratic scaling.
 * Formula: (END² + 9×END) / 2
 * Matches StatsComponent.GetHealthBonus() in C#.
 */
export function calculateHealthBonus(endurance: number): number {
  if (endurance <= 0) {
    return 0;
  }
  return Math.floor((endurance * endurance + 9 * endurance) / 2);
}

/**
 * Calculate max HP from base health and endurance.
 * Formula: baseHealth + healthBonus
 * MaxHealth floors at baseHealth (negative END doesn't reduce below base).
 */
export function calculateMaxHealth(baseHealth: number, endurance: number): number {
  const bonus = calculateHealthBonus(endurance);
  return Math.max(baseHealth, baseHealth + bonus);
}

// =============================================================================
// Willpower Formula
// =============================================================================

/**
 * Calculate max willpower from will stat.
 * Formula: 10 + (WIL × 5)
 * Matches WillpowerComponent in C#.
 */
export function calculateMaxWillpower(will: number): number {
  return 10 + Math.max(0, will * 5);
}

// =============================================================================
// Skill Resolution
// =============================================================================

/**
 * Resolve skill IDs to skill definitions.
 */
function resolveSkills(
  skillIds: string[],
  gameData: GameData
): SkillDefinition[] {
  const skills: SkillDefinition[] = [];

  for (const skillId of skillIds) {
    try {
      const skill = getSkill(skillId, gameData);
      skills.push(skill);
    } catch {
      // Skill not found - skip (validation should catch this)
      console.warn(`Skill not found: ${skillId}`);
    }
  }

  return skills;
}

// =============================================================================
// Equipment Resolution
// =============================================================================

/**
 * Resolve equipment entries to item definitions.
 */
function resolveEquipment(
  entries: EquipmentEntry[],
  gameData: GameData
): { items: ItemDefinition[]; ammo: Map<string, number> } {
  const items: ItemDefinition[] = [];
  const ammo = new Map<string, number>();

  for (const entry of entries) {
    const itemId = typeof entry === 'string' ? entry : entry.id;
    const quantity = typeof entry === 'string' ? 1 : entry.quantity;

    try {
      const item = getItem(itemId, gameData);
      items.push(item);

      // Track ammo quantities
      if (item.type === 'ammo') {
        const current = ammo.get(item.id) ?? 0;
        ammo.set(item.id, current + quantity);
      }
    } catch {
      // Item not found - skip (validation should catch this)
      console.warn(`Equipment item not found: ${itemId}`);
    }
  }

  return { items, ammo };
}

/**
 * Aggregate stat bonuses from equipment.
 */
interface EquipmentStats {
  strength: number;
  agility: number;
  endurance: number;
  will: number;
  armor: number;
  evasion: number;
  speed: number;
  regen: number;
  attacks: AttackDefinition[];
}

function aggregateEquipmentStats(items: ItemDefinition[]): EquipmentStats {
  const stats: EquipmentStats = {
    strength: 0,
    agility: 0,
    endurance: 0,
    will: 0,
    armor: 0,
    evasion: 0,
    speed: 0,
    regen: 0,
    attacks: [],
  };

  for (const item of items) {
    stats.strength += item.strength ?? 0;
    stats.agility += item.agility ?? 0;
    stats.endurance += item.endurance ?? 0;
    stats.will += item.will ?? 0;
    stats.armor += item.armor ?? 0;
    stats.evasion += item.evasion ?? 0;
    stats.speed += item.speed ?? 0;
    stats.regen += item.regen ?? 0;

    // Collect weapon attacks
    if (item.attack) {
      stats.attacks.push({
        ...item.attack,
        name: item.attack.name ?? item.name,
      });
    }
  }

  return stats;
}

// =============================================================================
// Combatant Factory
// =============================================================================

let _combatantIdCounter = 0;

/**
 * Create a combatant from a creature definition.
 */
export function createCombatant(
  creature: CreatureDefinition,
  team: 'A' | 'B',
  gameData: GameData,
  position: Position = { x: 0, y: 0 },
  equipmentOverrides?: string[]
): CombatantState {
  // Resolve equipment
  const equipmentEntries = equipmentOverrides
    ? equipmentOverrides.map((id) => id as EquipmentEntry)
    : creature.equipment;
  const { items, ammo } = resolveEquipment(equipmentEntries, gameData);
  const equipStats = aggregateEquipmentStats(items);

  // Calculate effective stats
  const strength = creature.strength + equipStats.strength;
  const agility = creature.agility + equipStats.agility;
  const endurance = creature.endurance + equipStats.endurance;
  const will = creature.will + equipStats.will;
  const armor = equipStats.armor;
  const evasion = equipStats.evasion;
  const speed = Math.max(1, creature.speed + equipStats.speed);
  const regenBonus = equipStats.regen;

  // Calculate max HP with effective endurance
  const maxHealth = calculateMaxHealth(creature.health, endurance);

  // Calculate max willpower with effective will
  const maxWillpower = calculateMaxWillpower(will);

  // Determine available attacks (equipment weapons or natural attacks)
  const attacks =
    equipStats.attacks.length > 0 ? equipStats.attacks : creature.attacks;

  // Resolve skills
  const skills = resolveSkills(creature.skills, gameData);

  // Generate unique ID
  const id = `${creature.id}_${++_combatantIdCounter}`;

  return {
    id,
    name: creature.name,
    team,

    // Stats
    strength,
    agility,
    endurance,
    will,
    armor,
    evasion,
    speed,
    regenBonus,

    // Health
    maxHealth,
    currentHealth: maxHealth,

    // Willpower
    maxWillpower,
    currentWillpower: maxWillpower,

    // Position
    position: { ...position },

    // Combat
    attacks,
    skills,
    immunities: new Set(creature.immunities),
    resistances: new Set(creature.resistances),
    vulnerabilities: new Set(creature.vulnerabilities),

    // Turn tracking
    accumulatedTime: 0,
    regenPoints: 0,
    wpRegenPoints: 0,

    // Ammo
    ammo,
  };
}

/**
 * Reset the combatant ID counter (for deterministic testing).
 */
export function resetCombatantIdCounter(): void {
  _combatantIdCounter = 0;
}

// =============================================================================
// Combatant Queries
// =============================================================================

/**
 * Check if a combatant is alive.
 */
export function isAlive(combatant: CombatantState): boolean {
  return combatant.currentHealth > 0;
}

/**
 * Check if combatant has a melee attack.
 */
export function hasMeleeAttack(combatant: CombatantState): boolean {
  return combatant.attacks.some((a) => a.type === 'Melee');
}

/**
 * Check if combatant has a ranged attack.
 */
export function hasRangedAttack(combatant: CombatantState): boolean {
  return combatant.attacks.some((a) => a.type === 'Ranged');
}

/**
 * Default fallback melee attack for all creatures.
 */
const DEFAULT_PUNCH: AttackDefinition = {
  name: 'punch',
  dice: '1d2',
  damageType: 'Bludgeoning',
  type: 'Melee',
  range: 1,
};

/**
 * Get the best melee attack (falls back to default punch).
 */
export function getMeleeAttack(
  combatant: CombatantState
): AttackDefinition {
  return combatant.attacks.find((a) => a.type === 'Melee') ?? DEFAULT_PUNCH;
}

/**
 * Get the best ranged attack (with ammo if required).
 */
export function getRangedAttack(
  combatant: CombatantState
): AttackDefinition | undefined {
  return combatant.attacks.find((a) => {
    if (a.type !== 'Ranged') return false;
    // Check ammo if required
    if (a.ammoType) {
      const ammoCount = combatant.ammo.get(a.ammoType) ?? 0;
      return ammoCount > 0;
    }
    return true;
  });
}

/**
 * Consume ammo for a ranged attack.
 */
export function consumeAmmo(
  combatant: CombatantState,
  attack: AttackDefinition
): void {
  if (attack.ammoType) {
    const current = combatant.ammo.get(attack.ammoType) ?? 0;
    if (current > 0) {
      combatant.ammo.set(attack.ammoType, current - 1);
    }
  }
}

/**
 * Calculate Chebyshev distance between two positions.
 */
export function distance(a: Position, b: Position): number {
  return Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y));
}

/**
 * Check if a combatant can attack a target with a given attack.
 */
export function canAttack(
  attacker: CombatantState,
  target: CombatantState,
  attack: AttackDefinition
): boolean {
  const dist = distance(attacker.position, target.position);
  const range = attack.range ?? 1;

  if (attack.type === 'Melee') {
    return dist <= range;
  } else {
    // Ranged: check range and ammo
    if (dist > range) return false;
    if (attack.ammoType) {
      const ammoCount = attacker.ammo.get(attack.ammoType) ?? 0;
      return ammoCount > 0;
    }
    return true;
  }
}

// =============================================================================
// Skill Queries
// =============================================================================

/**
 * Check if a combatant has enough willpower to use a skill.
 */
export function canUseSkill(
  combatant: CombatantState,
  skill: SkillDefinition
): boolean {
  return combatant.currentWillpower >= skill.willpowerCost;
}

/**
 * Check if a skill is a ranged offensive skill.
 * Ranged skills have range > 1 and target enemies.
 */
export function isRangedSkill(skill: SkillDefinition): boolean {
  const targeting = skill.targeting?.toLowerCase() ?? 'self';
  const isOffensive = targeting === 'enemy' || targeting === 'creature';
  return isOffensive && skill.range > 1;
}

/**
 * Check if a skill is a melee offensive skill.
 * Melee skills have range <= 1 and target enemies.
 */
export function isMeleeSkill(skill: SkillDefinition): boolean {
  const targeting = skill.targeting?.toLowerCase() ?? 'self';
  const isOffensive = targeting === 'enemy' || targeting === 'creature';
  return isOffensive && skill.range <= 1;
}

/**
 * Get all usable ranged skills (active, targeting enemies, range > 1, enough WP).
 */
export function getUsableRangedSkills(
  combatant: CombatantState
): SkillDefinition[] {
  return combatant.skills.filter(
    (s) =>
      s.category === 'active' &&
      isRangedSkill(s) &&
      canUseSkill(combatant, s)
  );
}

/**
 * Get all usable melee skills (active, targeting enemies, range <= 1, enough WP).
 */
export function getUsableMeleeSkills(
  combatant: CombatantState
): SkillDefinition[] {
  return combatant.skills.filter(
    (s) =>
      s.category === 'active' &&
      isMeleeSkill(s) &&
      canUseSkill(combatant, s)
  );
}

/**
 * Check if a combatant can use a skill on a target (in range, has WP).
 */
export function canUseSkillOn(
  attacker: CombatantState,
  target: CombatantState,
  skill: SkillDefinition
): boolean {
  if (!canUseSkill(attacker, skill)) {
    return false;
  }

  const dist = distance(attacker.position, target.position);
  return dist <= skill.range;
}

/**
 * Consume willpower for using a skill.
 */
export function consumeWillpower(
  combatant: CombatantState,
  skill: SkillDefinition
): void {
  combatant.currentWillpower = Math.max(
    0,
    combatant.currentWillpower - skill.willpowerCost
  );
}
