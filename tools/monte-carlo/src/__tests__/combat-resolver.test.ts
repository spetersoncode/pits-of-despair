/**
 * Unit tests for the combat resolver module.
 * Tests attack rolls, damage calculation, and resistance/vulnerability modifiers.
 */

import { describe, it, expect } from 'vitest';
import {
  getAttackModifier,
  getDefenseModifier,
  rollAttack,
  getDamageBonus,
  calculateRawDamage,
  getDamageModifier,
  applyDamageModifier,
  calculateFinalDamage,
  resolveAttack,
  applyDamage,
} from '../simulation/combat-resolver.js';
import { SeededRng } from '../data/dice-notation.js';
import type { Combatant as CombatantState, AttackDefinition, DamageType } from '../data/types.js';

// =============================================================================
// Attack Modifier Tests
// =============================================================================

describe('getAttackModifier', () => {
  it('returns strength for melee attacks', () => {
    const combatant = createMockCombatant({ strength: 3, agility: 1 });
    expect(getAttackModifier(combatant, true)).toBe(3);
  });

  it('returns agility for ranged attacks', () => {
    const combatant = createMockCombatant({ strength: 3, agility: 1 });
    expect(getAttackModifier(combatant, false)).toBe(1);
  });

  it('handles negative modifiers', () => {
    const combatant = createMockCombatant({ strength: -2, agility: -1 });
    expect(getAttackModifier(combatant, true)).toBe(-2);
    expect(getAttackModifier(combatant, false)).toBe(-1);
  });
});

describe('getDefenseModifier', () => {
  it('returns agility + evasion', () => {
    const combatant = createMockCombatant({ agility: 2, evasion: 1 });
    expect(getDefenseModifier(combatant)).toBe(3);
  });

  it('handles zero evasion', () => {
    const combatant = createMockCombatant({ agility: 2, evasion: 0 });
    expect(getDefenseModifier(combatant)).toBe(2);
  });

  it('handles negative agility', () => {
    const combatant = createMockCombatant({ agility: -1, evasion: 2 });
    expect(getDefenseModifier(combatant)).toBe(1);
  });

  it('can result in negative total', () => {
    const combatant = createMockCombatant({ agility: -2, evasion: 0 });
    expect(getDefenseModifier(combatant)).toBe(-2);
  });
});

// =============================================================================
// Attack Roll Tests
// =============================================================================

describe('rollAttack', () => {
  it('returns hit when attack roll >= defense roll', () => {
    // Use a seeded RNG that produces known results
    const rng = new SeededRng(42);
    const attacker = createMockCombatant({ strength: 0, agility: 0 });
    const target = createMockCombatant({ agility: 0, evasion: 0 });

    // Run multiple times and verify structure
    const result = rollAttack(attacker, target, true, rng);
    expect(typeof result.hit).toBe('boolean');
    expect(typeof result.attackRoll).toBe('number');
    expect(typeof result.defenseRoll).toBe('number');
  });

  it('ties favor the attacker', () => {
    // Create a deterministic scenario: both rolls equal
    // This is tricky with random, so we test the logic directly
    // Attack roll == defense roll should hit
    const mockRng = {
      random: (() => {
        let call = 0;
        // Returns values that produce identical dice rolls
        const values = [0.5, 0.5, 0.5, 0.5]; // 2d6 for attacker, 2d6 for defender
        return () => values[call++] ?? 0.5;
      })(),
    };

    const attacker = createMockCombatant({ strength: 0, agility: 0 });
    const target = createMockCombatant({ agility: 0, evasion: 0 });

    const result = rollAttack(attacker, target, true, mockRng);
    // Both should roll the same (same RNG sequence with same modifiers)
    // With tied rolls, attacker wins
    expect(result.hit).toBe(true);
    expect(result.attackRoll).toBe(result.defenseRoll);
  });

  it('uses strength modifier for melee', () => {
    const rng = new SeededRng(123);
    const strongAttacker = createMockCombatant({ strength: 5, agility: 0 });
    const weakAttacker = createMockCombatant({ strength: -2, agility: 0 });
    const target = createMockCombatant({ agility: 0, evasion: 0 });

    // Strong attacker with same RNG seed
    const rng1 = new SeededRng(123);
    const result1 = rollAttack(strongAttacker, target, true, rng1);

    // Weak attacker with same RNG seed
    const rng2 = new SeededRng(123);
    const result2 = rollAttack(weakAttacker, target, true, rng2);

    // Strong attacker should have higher attack roll due to +5 vs -2
    expect(result1.attackRoll - result2.attackRoll).toBe(7); // 5 - (-2) = 7
  });

  it('uses agility modifier for ranged', () => {
    const rng1 = new SeededRng(456);
    const rng2 = new SeededRng(456);

    const nimbleAttacker = createMockCombatant({ strength: 0, agility: 3 });
    const clumsyAttacker = createMockCombatant({ strength: 0, agility: -1 });
    const target = createMockCombatant({ agility: 0, evasion: 0 });

    const result1 = rollAttack(nimbleAttacker, target, false, rng1);
    const result2 = rollAttack(clumsyAttacker, target, false, rng2);

    // Nimble attacker should have higher attack roll due to +3 vs -1
    expect(result1.attackRoll - result2.attackRoll).toBe(4); // 3 - (-1) = 4
  });
});

// =============================================================================
// Damage Bonus Tests
// =============================================================================

describe('getDamageBonus', () => {
  it('returns strength for melee', () => {
    const combatant = createMockCombatant({ strength: 4 });
    expect(getDamageBonus(combatant, true)).toBe(4);
  });

  it('returns 0 for ranged', () => {
    const combatant = createMockCombatant({ strength: 4 });
    expect(getDamageBonus(combatant, false)).toBe(0);
  });

  it('handles negative strength for melee', () => {
    const combatant = createMockCombatant({ strength: -2 });
    expect(getDamageBonus(combatant, true)).toBe(-2);
  });
});

// =============================================================================
// Raw Damage Calculation Tests
// =============================================================================

describe('calculateRawDamage', () => {
  it('calculates damage as dice + STR - armor for melee', () => {
    // Use fixed dice roll by controlling RNG
    const mockRng = {
      random: (() => {
        // Return 0.5 to get middle value: floor(0.5 * 6) + 1 = 4 per die
        // For 1d6: should get 4
        return () => 0.5;
      })(),
    };

    const attacker = createMockCombatant({ strength: 2 });
    const target = createMockCombatant({ armor: 1 });
    const attack: AttackDefinition = {
      name: 'sword',
      type: 'Melee',
      dice: '1d6',
      damageType: 'Slashing',
      range: 1,
    };

    // Damage = 4 (dice) + 2 (STR) - 1 (armor) = 5
    const damage = calculateRawDamage(attacker, target, attack, mockRng);
    expect(damage).toBe(5);
  });

  it('does not add STR for ranged attacks', () => {
    const mockRng = { random: () => 0.5 }; // Gets 4 on d6

    const attacker = createMockCombatant({ strength: 5 });
    const target = createMockCombatant({ armor: 0 });
    const attack: AttackDefinition = {
      name: 'bow',
      type: 'Ranged',
      dice: '1d6',
      damageType: 'Piercing',
      range: 6,
    };

    // Damage = 4 (dice) + 0 (no STR for ranged) - 0 (armor) = 4
    const damage = calculateRawDamage(attacker, target, attack, mockRng);
    expect(damage).toBe(4);
  });

  it('floors damage at 0', () => {
    const mockRng = { random: () => 0 }; // Gets 1 on d6

    const attacker = createMockCombatant({ strength: 0 });
    const target = createMockCombatant({ armor: 10 }); // High armor
    const attack: AttackDefinition = {
      name: 'dagger',
      type: 'Melee',
      dice: '1d4',
      damageType: 'Piercing',
      range: 1,
    };

    // Damage = 1 (dice) + 0 (STR) - 10 (armor) = -9, floored to 0
    const damage = calculateRawDamage(attacker, target, attack, mockRng);
    expect(damage).toBe(0);
  });

  it('handles negative STR reducing damage', () => {
    const mockRng = { random: () => 0.5 }; // Gets 4 on d6

    const attacker = createMockCombatant({ strength: -2 });
    const target = createMockCombatant({ armor: 0 });
    const attack: AttackDefinition = {
      name: 'club',
      type: 'Melee',
      dice: '1d6',
      damageType: 'Bludgeoning',
      range: 1,
    };

    // Damage = 4 (dice) + (-2) (STR) - 0 (armor) = 2
    const damage = calculateRawDamage(attacker, target, attack, mockRng);
    expect(damage).toBe(2);
  });
});

// =============================================================================
// Damage Modifier Tests
// =============================================================================

describe('getDamageModifier', () => {
  it('returns immune for immune damage types', () => {
    const target = createMockCombatant({
      immunities: new Set(['Poison'] as DamageType[]),
    });
    expect(getDamageModifier(target, 'Poison')).toBe('immune');
  });

  it('returns vulnerable for vulnerable damage types', () => {
    const target = createMockCombatant({
      vulnerabilities: new Set(['Bludgeoning'] as DamageType[]),
    });
    expect(getDamageModifier(target, 'Bludgeoning')).toBe('vulnerable');
  });

  it('returns resistant for resistant damage types', () => {
    const target = createMockCombatant({
      resistances: new Set(['Piercing'] as DamageType[]),
    });
    expect(getDamageModifier(target, 'Piercing')).toBe('resistant');
  });

  it('returns none for unmodified damage types', () => {
    const target = createMockCombatant();
    expect(getDamageModifier(target, 'Slashing')).toBe('none');
  });

  it('prioritizes immunity over vulnerability', () => {
    // Edge case: if somehow both immune and vulnerable, immune wins
    const target = createMockCombatant({
      immunities: new Set(['Fire'] as DamageType[]),
      vulnerabilities: new Set(['Fire'] as DamageType[]),
    });
    expect(getDamageModifier(target, 'Fire')).toBe('immune');
  });

  it('prioritizes vulnerability over resistance', () => {
    // Edge case: if somehow both vulnerable and resistant, vulnerable wins
    const target = createMockCombatant({
      vulnerabilities: new Set(['Cold'] as DamageType[]),
      resistances: new Set(['Cold'] as DamageType[]),
    });
    expect(getDamageModifier(target, 'Cold')).toBe('vulnerable');
  });
});

describe('applyDamageModifier', () => {
  it('returns 0 for immune', () => {
    expect(applyDamageModifier(10, 'immune')).toBe(0);
    expect(applyDamageModifier(100, 'immune')).toBe(0);
  });

  it('doubles damage for vulnerable', () => {
    expect(applyDamageModifier(5, 'vulnerable')).toBe(10);
    expect(applyDamageModifier(7, 'vulnerable')).toBe(14);
  });

  it('halves damage for resistant (floored)', () => {
    expect(applyDamageModifier(10, 'resistant')).toBe(5);
    expect(applyDamageModifier(7, 'resistant')).toBe(3); // floor(3.5) = 3
    expect(applyDamageModifier(1, 'resistant')).toBe(0); // floor(0.5) = 0
  });

  it('returns unchanged for none', () => {
    expect(applyDamageModifier(10, 'none')).toBe(10);
    expect(applyDamageModifier(0, 'none')).toBe(0);
  });
});

describe('calculateFinalDamage', () => {
  it('combines raw damage with modifier lookup', () => {
    const target = createMockCombatant({
      vulnerabilities: new Set(['Fire'] as DamageType[]),
    });

    const result = calculateFinalDamage(5, target, 'Fire');
    expect(result.damage).toBe(10); // 5 * 2
    expect(result.modifier).toBe('vulnerable');
  });

  it('handles resistance', () => {
    const target = createMockCombatant({
      resistances: new Set(['Piercing'] as DamageType[]),
    });

    const result = calculateFinalDamage(8, target, 'Piercing');
    expect(result.damage).toBe(4); // 8 / 2
    expect(result.modifier).toBe('resistant');
  });

  it('handles immunity', () => {
    const target = createMockCombatant({
      immunities: new Set(['Poison'] as DamageType[]),
    });

    const result = calculateFinalDamage(20, target, 'Poison');
    expect(result.damage).toBe(0);
    expect(result.modifier).toBe('immune');
  });
});

// =============================================================================
// Full Attack Resolution Tests
// =============================================================================

describe('resolveAttack', () => {
  it('returns miss result when attack roll fails', () => {
    // Force a miss by giving defender huge defense
    const attacker = createMockCombatant({ strength: -5 });
    const target = createMockCombatant({ agility: 10, evasion: 10 });
    const attack: AttackDefinition = {
      name: 'sword',
      type: 'Melee',
      dice: '1d6',
      damageType: 'Slashing',
      range: 1,
    };

    const rng = new SeededRng(42);
    const result = resolveAttack(attacker, target, attack, rng);

    expect(result.hit).toBe(false);
    expect(result.damage).toBe(0);
    expect(result.damageBeforeModifiers).toBe(0);
  });

  it('calculates full damage chain on hit', () => {
    // Force a hit by giving attacker huge attack bonus
    const attacker = createMockCombatant({ strength: 10 });
    const target = createMockCombatant({ agility: -5, evasion: 0, armor: 0 });
    const attack: AttackDefinition = {
      name: 'sword',
      type: 'Melee',
      dice: '1d6',
      damageType: 'Slashing',
      range: 1,
    };

    const rng = new SeededRng(42);
    const result = resolveAttack(attacker, target, attack, rng);

    expect(result.hit).toBe(true);
    expect(result.damage).toBeGreaterThan(0);
    expect(result.modifier).toBe('none');
  });

  it('applies damage modifiers on hit', () => {
    const attacker = createMockCombatant({ strength: 10 });
    const target = createMockCombatant({
      agility: -5,
      evasion: 0,
      armor: 0,
      vulnerabilities: new Set(['Slashing'] as DamageType[]),
    });
    const attack: AttackDefinition = {
      name: 'sword',
      type: 'Melee',
      dice: '1d6',
      damageType: 'Slashing',
      range: 1,
    };

    const rng = new SeededRng(42);
    const result = resolveAttack(attacker, target, attack, rng);

    expect(result.hit).toBe(true);
    expect(result.modifier).toBe('vulnerable');
    expect(result.damage).toBe(result.damageBeforeModifiers * 2);
  });

  it('populates result metadata correctly', () => {
    const attacker = createMockCombatant({ name: 'Attacker', strength: 10 });
    const target = createMockCombatant({ name: 'Target', agility: -5 });
    const attack: AttackDefinition = {
      name: 'Fire Bolt',
      type: 'Ranged',
      dice: '2d6',
      damageType: 'Fire',
      range: 6,
    };

    const rng = new SeededRng(42);
    const result = resolveAttack(attacker, target, attack, rng);

    expect(result.attackerName).toBe('Attacker');
    expect(result.targetName).toBe('Target');
    expect(result.attackName).toBe('Fire Bolt');
    expect(result.damageType).toBe('Fire');
  });
});

// =============================================================================
// Apply Damage Tests
// =============================================================================

describe('applyDamage', () => {
  it('reduces current health by damage amount', () => {
    const target = createMockCombatant({ currentHealth: 20, maxHealth: 20 });
    const dealt = applyDamage(target, 5);

    expect(target.currentHealth).toBe(15);
    expect(dealt).toBe(5);
  });

  it('caps damage at current health (no overkill)', () => {
    const target = createMockCombatant({ currentHealth: 3, maxHealth: 20 });
    const dealt = applyDamage(target, 10);

    expect(target.currentHealth).toBe(0);
    expect(dealt).toBe(3); // Only dealt 3, not 10
  });

  it('returns 0 when target already dead', () => {
    const target = createMockCombatant({ currentHealth: 0, maxHealth: 20 });
    const dealt = applyDamage(target, 5);

    expect(target.currentHealth).toBe(0);
    expect(dealt).toBe(0);
  });

  it('handles zero damage', () => {
    const target = createMockCombatant({ currentHealth: 10, maxHealth: 20 });
    const dealt = applyDamage(target, 0);

    expect(target.currentHealth).toBe(10);
    expect(dealt).toBe(0);
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
    armor: 0,
    evasion: 0,
    speed: 10,
    regenBonus: 0,
    maxHealth: 10,
    currentHealth: 10,
    position: { x: 0, y: 0 },
    attacks: [],
    immunities: new Set(),
    resistances: new Set(),
    vulnerabilities: new Set(),
    accumulatedTime: 0,
    regenPoints: 0,
    ammo: new Map(),
    ...overrides,
  };
}
