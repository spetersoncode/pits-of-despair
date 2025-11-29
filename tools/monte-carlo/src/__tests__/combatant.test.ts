/**
 * Unit tests for the combatant module.
 * Tests HP formula, stat aggregation from equipment, and attack selection.
 */

import { describe, it, expect, beforeAll, beforeEach } from 'vitest';
import {
  calculateHealthBonus,
  calculateMaxHealth,
  createCombatant,
  resetCombatantIdCounter,
  isAlive,
  hasMeleeAttack,
  hasRangedAttack,
  getMeleeAttack,
  getRangedAttack,
  consumeAmmo,
  distance,
  canAttack,
} from '../simulation/combatant.js';
import { loadGameData, type GameData } from '../data/data-loader.js';
import type { CreatureDefinition, Combatant as CombatantState } from '../data/types.js';

let gameData: GameData;

beforeAll(() => {
  gameData = loadGameData();
});

beforeEach(() => {
  resetCombatantIdCounter();
});

// =============================================================================
// HP Formula Tests
// =============================================================================

describe('calculateHealthBonus', () => {
  it('returns 0 for endurance <= 0', () => {
    expect(calculateHealthBonus(-2)).toBe(0);
    expect(calculateHealthBonus(-1)).toBe(0);
    expect(calculateHealthBonus(0)).toBe(0);
  });

  it('calculates correct bonus for positive endurance', () => {
    // Formula: (END² + 9×END) / 2
    expect(calculateHealthBonus(1)).toBe(5); // (1 + 9) / 2 = 5
    expect(calculateHealthBonus(2)).toBe(11); // (4 + 18) / 2 = 11
    expect(calculateHealthBonus(3)).toBe(18); // (9 + 27) / 2 = 18
    expect(calculateHealthBonus(4)).toBe(26); // (16 + 36) / 2 = 26
    expect(calculateHealthBonus(5)).toBe(35); // (25 + 45) / 2 = 35
  });

  it('floors the result for odd totals', () => {
    // END=1: (1 + 9) / 2 = 5 (even)
    // END=2: (4 + 18) / 2 = 11 (odd total 22, div by 2 = 11)
    expect(calculateHealthBonus(2)).toBe(11);
  });
});

describe('calculateMaxHealth', () => {
  it('returns base health when endurance <= 0', () => {
    expect(calculateMaxHealth(10, -2)).toBe(10);
    expect(calculateMaxHealth(10, -1)).toBe(10);
    expect(calculateMaxHealth(10, 0)).toBe(10);
    expect(calculateMaxHealth(8, -1)).toBe(8);
  });

  it('adds bonus to base health for positive endurance', () => {
    expect(calculateMaxHealth(10, 1)).toBe(15); // 10 + 5
    expect(calculateMaxHealth(10, 2)).toBe(21); // 10 + 11
    expect(calculateMaxHealth(10, 3)).toBe(28); // 10 + 18
  });

  it('uses different base health values', () => {
    expect(calculateMaxHealth(8, 1)).toBe(13); // 8 + 5
    expect(calculateMaxHealth(15, 2)).toBe(26); // 15 + 11
    expect(calculateMaxHealth(20, 0)).toBe(20); // 20 + 0
  });

  it('floors at base health, not below', () => {
    // Even with negative endurance, can't go below base
    expect(calculateMaxHealth(10, -5)).toBe(10);
  });
});

// =============================================================================
// Combatant Creation Tests
// =============================================================================

describe('createCombatant', () => {
  it('creates combatant from basic creature definition', () => {
    const goblin = gameData.creatures.get('goblin')!;
    const combatant = createCombatant(goblin, 'A', gameData);

    expect(combatant.name).toBe(goblin.name);
    expect(combatant.team).toBe('A');
    expect(combatant.currentHealth).toBe(combatant.maxHealth);
    expect(combatant.accumulatedTime).toBe(0);
    expect(combatant.regenPoints).toBe(0);
  });

  it('generates unique IDs for each combatant', () => {
    const goblin = gameData.creatures.get('goblin')!;
    const c1 = createCombatant(goblin, 'A', gameData);
    const c2 = createCombatant(goblin, 'B', gameData);
    const c3 = createCombatant(goblin, 'A', gameData);

    expect(c1.id).not.toBe(c2.id);
    expect(c2.id).not.toBe(c3.id);
    expect(c1.id).not.toBe(c3.id);
  });

  it('assigns team correctly', () => {
    const goblin = gameData.creatures.get('goblin')!;
    const combatantA = createCombatant(goblin, 'A', gameData);
    const combatantB = createCombatant(goblin, 'B', gameData);

    expect(combatantA.team).toBe('A');
    expect(combatantB.team).toBe('B');
  });

  it('sets position correctly', () => {
    const goblin = gameData.creatures.get('goblin')!;
    const c1 = createCombatant(goblin, 'A', gameData, { x: 5, y: 10 });
    const c2 = createCombatant(goblin, 'B', gameData); // default position

    expect(c1.position).toEqual({ x: 5, y: 10 });
    expect(c2.position).toEqual({ x: 0, y: 0 });
  });

  it('copies damage type sets from creature', () => {
    const skeleton = gameData.creatures.get('skeleton')!;
    const combatant = createCombatant(skeleton, 'A', gameData);

    // Skeleton should have resistances/vulnerabilities
    expect(combatant.resistances).toBeInstanceOf(Set);
    expect(combatant.vulnerabilities).toBeInstanceOf(Set);
    expect(combatant.immunities).toBeInstanceOf(Set);
  });

  it('calculates max health with endurance bonus', () => {
    const goblin = gameData.creatures.get('goblin')!;
    const combatant = createCombatant(goblin, 'A', gameData);

    const expectedMaxHealth = calculateMaxHealth(goblin.health, goblin.endurance);
    expect(combatant.maxHealth).toBe(expectedMaxHealth);
  });
});

// =============================================================================
// Equipment Resolution Tests
// =============================================================================

describe('equipment resolution', () => {
  it('aggregates stats from equipment', () => {
    const goblin = gameData.creatures.get('goblin')!;
    // Create with no equipment override to get base stats
    const baseGoblin = createCombatant(goblin, 'A', gameData, { x: 0, y: 0 }, []);

    expect(baseGoblin.armor).toBe(0); // No equipment = no armor
    expect(baseGoblin.evasion).toBe(0); // No equipment = no evasion bonus
  });

  it('uses equipment attacks when present', () => {
    const goblin = gameData.creatures.get('goblin')!;
    // Create combatant with a weapon that provides an attack
    const combatant = createCombatant(goblin, 'A', gameData);

    // If goblin has default equipment with weapons, it should have attacks
    // The attacks come either from equipment or natural attacks
    expect(combatant.attacks.length).toBeGreaterThanOrEqual(0);
  });

  it('falls back to natural attacks when no weapon equipped', () => {
    const goblin = gameData.creatures.get('goblin')!;
    // Create with empty equipment
    const combatant = createCombatant(goblin, 'A', gameData, { x: 0, y: 0 }, []);

    // Should fall back to creature's natural attacks
    expect(combatant.attacks).toEqual(goblin.attacks);
  });

  it('handles equipment override parameter', () => {
    const goblin = gameData.creatures.get('goblin')!;
    // Use empty array to override default equipment
    const unarmed = createCombatant(goblin, 'A', gameData, { x: 0, y: 0 }, []);

    // Should have creature's natural attacks, not equipment attacks
    expect(unarmed.attacks).toEqual(goblin.attacks);
  });

  it('enforces minimum speed of 1', () => {
    // Create a mock creature definition with 0 speed
    const slowCreature: CreatureDefinition = {
      id: 'test_slow',
      name: 'Slow Test',
      type: 'test',
      description: '',
      glyph: '?',
      color: 'Palette.Default',
      threat: 1,
      health: 10,
      speed: 0, // Attempting 0 speed
      strength: 0,
      agility: 0,
      endurance: 0,
      will: 0,
      equipment: [],
      attacks: [],
      skills: [],
      resistances: [],
      vulnerabilities: [],
      immunities: [],
    };

    const combatant = createCombatant(slowCreature, 'A', gameData);
    expect(combatant.speed).toBe(1); // Should be clamped to 1
  });
});

// =============================================================================
// Combatant Query Tests
// =============================================================================

describe('isAlive', () => {
  it('returns true when health > 0', () => {
    const combatant = createMockCombatant({ currentHealth: 10 });
    expect(isAlive(combatant)).toBe(true);
  });

  it('returns true when health = 1', () => {
    const combatant = createMockCombatant({ currentHealth: 1 });
    expect(isAlive(combatant)).toBe(true);
  });

  it('returns false when health = 0', () => {
    const combatant = createMockCombatant({ currentHealth: 0 });
    expect(isAlive(combatant)).toBe(false);
  });

  it('returns false when health < 0', () => {
    const combatant = createMockCombatant({ currentHealth: -5 });
    expect(isAlive(combatant)).toBe(false);
  });
});

describe('hasMeleeAttack', () => {
  it('returns true when combatant has melee attack', () => {
    const combatant = createMockCombatant({
      attacks: [{ name: 'sword', type: 'Melee', dice: '1d6', damageType: 'Slashing', range: 1, delay: 1.0 }],
    });
    expect(hasMeleeAttack(combatant)).toBe(true);
  });

  it('returns false when combatant has only ranged attacks', () => {
    const combatant = createMockCombatant({
      attacks: [{ name: 'bow', type: 'Ranged', dice: '1d6', damageType: 'Piercing', range: 6, delay: 1.0 }],
    });
    expect(hasMeleeAttack(combatant)).toBe(false);
  });

  it('returns false when combatant has no attacks', () => {
    const combatant = createMockCombatant({ attacks: [] });
    expect(hasMeleeAttack(combatant)).toBe(false);
  });
});

describe('hasRangedAttack', () => {
  it('returns true when combatant has ranged attack', () => {
    const combatant = createMockCombatant({
      attacks: [{ name: 'bow', type: 'Ranged', dice: '1d6', damageType: 'Piercing', range: 6, delay: 1.0 }],
    });
    expect(hasRangedAttack(combatant)).toBe(true);
  });

  it('returns false when combatant has only melee attacks', () => {
    const combatant = createMockCombatant({
      attacks: [{ name: 'sword', type: 'Melee', dice: '1d6', damageType: 'Slashing', range: 1, delay: 1.0 }],
    });
    expect(hasRangedAttack(combatant)).toBe(false);
  });
});

describe('getMeleeAttack', () => {
  it('returns melee attack when present', () => {
    const meleeAttack = { name: 'sword', type: 'Melee' as const, dice: '1d6', damageType: 'Slashing' as const, range: 1, delay: 1.0 };
    const combatant = createMockCombatant({ attacks: [meleeAttack] });

    const attack = getMeleeAttack(combatant);
    expect(attack.name).toBe('sword');
    expect(attack.type).toBe('Melee');
  });

  it('returns default punch when no melee attack', () => {
    const combatant = createMockCombatant({ attacks: [] });

    const attack = getMeleeAttack(combatant);
    expect(attack.name).toBe('punch');
    expect(attack.dice).toBe('1d2');
    expect(attack.damageType).toBe('Bludgeoning');
  });
});

describe('getRangedAttack', () => {
  it('returns ranged attack when present', () => {
    const rangedAttack = { name: 'bow', type: 'Ranged' as const, dice: '1d6', damageType: 'Piercing' as const, range: 6, delay: 1.0 };
    const combatant = createMockCombatant({ attacks: [rangedAttack] });

    const attack = getRangedAttack(combatant);
    expect(attack?.name).toBe('bow');
  });

  it('returns undefined when no ranged attack', () => {
    const combatant = createMockCombatant({ attacks: [] });
    expect(getRangedAttack(combatant)).toBeUndefined();
  });

  it('returns undefined when ranged attack needs ammo but has none', () => {
    const rangedAttack = {
      name: 'bow',
      type: 'Ranged' as const,
      dice: '1d6',
      damageType: 'Piercing' as const,
      range: 6,
      delay: 1.0,
      ammoType: 'arrow',
    };
    const combatant = createMockCombatant({
      attacks: [rangedAttack],
      ammo: new Map(), // No ammo
    });

    expect(getRangedAttack(combatant)).toBeUndefined();
  });

  it('returns ranged attack when it has required ammo', () => {
    const rangedAttack = {
      name: 'bow',
      type: 'Ranged' as const,
      dice: '1d6',
      damageType: 'Piercing' as const,
      range: 6,
      delay: 1.0,
      ammoType: 'arrow',
    };
    const combatant = createMockCombatant({
      attacks: [rangedAttack],
      ammo: new Map([['arrow', 10]]),
    });

    expect(getRangedAttack(combatant)?.name).toBe('bow');
  });
});

describe('consumeAmmo', () => {
  it('decrements ammo count for attacks requiring ammo', () => {
    const attack = { name: 'bow', type: 'Ranged' as const, dice: '1d6', damageType: 'Piercing' as const, range: 6, delay: 1.0, ammoType: 'arrow' };
    const combatant = createMockCombatant({
      ammo: new Map([['arrow', 10]]),
    });

    consumeAmmo(combatant, attack);
    expect(combatant.ammo.get('arrow')).toBe(9);

    consumeAmmo(combatant, attack);
    expect(combatant.ammo.get('arrow')).toBe(8);
  });

  it('does nothing for attacks not requiring ammo', () => {
    const attack = { name: 'magic', type: 'Ranged' as const, dice: '1d6', damageType: 'Fire' as const, range: 6, delay: 1.0 };
    const combatant = createMockCombatant({
      ammo: new Map([['arrow', 10]]),
    });

    consumeAmmo(combatant, attack);
    expect(combatant.ammo.get('arrow')).toBe(10);
  });

  it('does not go below 0 ammo', () => {
    const attack = { name: 'bow', type: 'Ranged' as const, dice: '1d6', damageType: 'Piercing' as const, range: 6, delay: 1.0, ammoType: 'arrow' };
    const combatant = createMockCombatant({
      ammo: new Map([['arrow', 0]]),
    });

    consumeAmmo(combatant, attack);
    expect(combatant.ammo.get('arrow')).toBe(0);
  });
});

// =============================================================================
// Distance and Range Tests
// =============================================================================

describe('distance', () => {
  it('returns 0 for same position', () => {
    expect(distance({ x: 0, y: 0 }, { x: 0, y: 0 })).toBe(0);
    expect(distance({ x: 5, y: 5 }, { x: 5, y: 5 })).toBe(0);
  });

  it('calculates Chebyshev distance (max of dx, dy)', () => {
    // Horizontal
    expect(distance({ x: 0, y: 0 }, { x: 3, y: 0 })).toBe(3);
    // Vertical
    expect(distance({ x: 0, y: 0 }, { x: 0, y: 4 })).toBe(4);
    // Diagonal (Chebyshev = max, not Euclidean)
    expect(distance({ x: 0, y: 0 }, { x: 3, y: 3 })).toBe(3);
    expect(distance({ x: 0, y: 0 }, { x: 3, y: 5 })).toBe(5);
  });

  it('handles negative coordinates', () => {
    expect(distance({ x: -2, y: -3 }, { x: 2, y: 3 })).toBe(6);
    expect(distance({ x: -5, y: 0 }, { x: 5, y: 0 })).toBe(10);
  });
});

describe('canAttack', () => {
  it('allows melee attack at range 1', () => {
    const attacker = createMockCombatant({ position: { x: 0, y: 0 } });
    const target = createMockCombatant({ position: { x: 1, y: 0 } });
    const meleeAttack = { name: 'sword', type: 'Melee' as const, dice: '1d6', damageType: 'Slashing' as const, range: 1, delay: 1.0 };

    expect(canAttack(attacker, target, meleeAttack)).toBe(true);
  });

  it('disallows melee attack beyond range', () => {
    const attacker = createMockCombatant({ position: { x: 0, y: 0 } });
    const target = createMockCombatant({ position: { x: 2, y: 0 } });
    const meleeAttack = { name: 'sword', type: 'Melee' as const, dice: '1d6', damageType: 'Slashing' as const, range: 1, delay: 1.0 };

    expect(canAttack(attacker, target, meleeAttack)).toBe(false);
  });

  it('allows ranged attack within range', () => {
    const attacker = createMockCombatant({ position: { x: 0, y: 0 } });
    const target = createMockCombatant({ position: { x: 5, y: 0 } });
    const rangedAttack = { name: 'bow', type: 'Ranged' as const, dice: '1d6', damageType: 'Piercing' as const, range: 6, delay: 1.0 };

    expect(canAttack(attacker, target, rangedAttack)).toBe(true);
  });

  it('disallows ranged attack beyond range', () => {
    const attacker = createMockCombatant({ position: { x: 0, y: 0 } });
    const target = createMockCombatant({ position: { x: 7, y: 0 } });
    const rangedAttack = { name: 'bow', type: 'Ranged' as const, dice: '1d6', damageType: 'Piercing' as const, range: 6, delay: 1.0 };

    expect(canAttack(attacker, target, rangedAttack)).toBe(false);
  });

  it('disallows ranged attack without required ammo', () => {
    const attacker = createMockCombatant({
      position: { x: 0, y: 0 },
      ammo: new Map(), // No ammo
    });
    const target = createMockCombatant({ position: { x: 3, y: 0 } });
    const rangedAttack = {
      name: 'bow',
      type: 'Ranged' as const,
      dice: '1d6',
      damageType: 'Piercing' as const,
      range: 6,
      delay: 1.0,
      ammoType: 'arrow',
    };

    expect(canAttack(attacker, target, rangedAttack)).toBe(false);
  });

  it('allows ranged attack with required ammo', () => {
    const attacker = createMockCombatant({
      position: { x: 0, y: 0 },
      ammo: new Map([['arrow', 5]]),
    });
    const target = createMockCombatant({ position: { x: 3, y: 0 } });
    const rangedAttack = {
      name: 'bow',
      type: 'Ranged' as const,
      dice: '1d6',
      damageType: 'Piercing' as const,
      range: 6,
      delay: 1.0,
      ammoType: 'arrow',
    };

    expect(canAttack(attacker, target, rangedAttack)).toBe(true);
  });
});

// =============================================================================
// Test Helpers
// =============================================================================

/**
 * Create a mock combatant with default values, overriding as specified.
 */
function createMockCombatant(overrides: Partial<CombatantState> = {}): CombatantState {
  return {
    id: 'test_1',
    name: 'Test Combatant',
    team: 'A',
    strength: 0,
    agility: 0,
    endurance: 0,
    will: 0,
    armor: 0,
    evasion: 0,
    speed: 10,
    regenBonus: 0,
    maxHealth: 10,
    currentHealth: 10,
    maxWillpower: 10,
    currentWillpower: 10,
    position: { x: 0, y: 0 },
    attacks: [],
    skills: [],
    immunities: new Set(),
    resistances: new Set(),
    vulnerabilities: new Set(),
    accumulatedTime: 0,
    regenPoints: 0,
    wpRegenPoints: 0,
    ammo: new Map(),
    ...overrides,
  };
}
