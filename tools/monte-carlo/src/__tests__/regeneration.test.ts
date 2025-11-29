/**
 * Unit tests for the regeneration module.
 * Tests DCSS-style regeneration rate calculations and healing.
 */

import { describe, it, expect } from 'vitest';
import {
  REGEN_THRESHOLD,
  BASE_REGEN_RATE,
  calculateRegenRate,
  processRegeneration,
  turnsToHealOne,
  turnsToFullHeal,
} from '../simulation/regeneration.js';
import type { Combatant as CombatantState } from '../data/types.js';

// =============================================================================
// Constants Tests
// =============================================================================

describe('constants', () => {
  it('has expected values', () => {
    expect(REGEN_THRESHOLD).toBe(100);
    expect(BASE_REGEN_RATE).toBe(20);
  });
});

// =============================================================================
// Calculate Regen Rate Tests
// =============================================================================

describe('calculateRegenRate', () => {
  it('returns base rate + maxHP/6 + bonus', () => {
    // Formula: 20 + floor(maxHP / 6) + regenBonus
    const combatant = createMockCombatant({ maxHealth: 12, regenBonus: 0 });
    // 20 + floor(12/6) + 0 = 20 + 2 + 0 = 22
    expect(calculateRegenRate(combatant)).toBe(22);
  });

  it('floors maxHP division', () => {
    // maxHP = 10: floor(10/6) = 1
    const combatant = createMockCombatant({ maxHealth: 10, regenBonus: 0 });
    expect(calculateRegenRate(combatant)).toBe(21); // 20 + 1 + 0
  });

  it('includes regen bonus from equipment', () => {
    // Ring of Regeneration adds +80
    const combatant = createMockCombatant({ maxHealth: 12, regenBonus: 80 });
    // 20 + 2 + 80 = 102
    expect(calculateRegenRate(combatant)).toBe(102);
  });

  it('handles low maxHP', () => {
    // maxHP = 4: floor(4/6) = 0
    const combatant = createMockCombatant({ maxHealth: 4, regenBonus: 0 });
    expect(calculateRegenRate(combatant)).toBe(20); // 20 + 0 + 0
  });

  it('handles high maxHP', () => {
    // maxHP = 100: floor(100/6) = 16
    const combatant = createMockCombatant({ maxHealth: 100, regenBonus: 0 });
    expect(calculateRegenRate(combatant)).toBe(36); // 20 + 16 + 0
  });

  it('handles negative regen bonus (debuff)', () => {
    const combatant = createMockCombatant({ maxHealth: 12, regenBonus: -10 });
    // 20 + 2 + (-10) = 12
    expect(calculateRegenRate(combatant)).toBe(12);
  });
});

// =============================================================================
// Process Regeneration Tests
// =============================================================================

describe('processRegeneration', () => {
  it('accumulates regen points without healing', () => {
    const combatant = createMockCombatant({
      maxHealth: 12,
      currentHealth: 5,
      regenPoints: 0,
      regenBonus: 0,
    });

    const healed = processRegeneration(combatant);

    // Regen rate = 22 (20 + 2 + 0)
    // Points accumulated: 22, threshold: 100, so no heal yet
    expect(healed).toBe(0);
    expect(combatant.regenPoints).toBe(22);
    expect(combatant.currentHealth).toBe(5);
  });

  it('heals 1 HP when threshold reached', () => {
    const combatant = createMockCombatant({
      maxHealth: 12,
      currentHealth: 5,
      regenPoints: 80, // Already accumulated 80
      regenBonus: 0,
    });

    const healed = processRegeneration(combatant);

    // Rate = 22, points = 80 + 22 = 102 >= 100
    // Heals 1, points = 102 - 100 = 2
    expect(healed).toBe(1);
    expect(combatant.regenPoints).toBe(2);
    expect(combatant.currentHealth).toBe(6);
  });

  it('can heal multiple HP in one call with high regen', () => {
    const combatant = createMockCombatant({
      maxHealth: 20,
      currentHealth: 10,
      regenPoints: 180, // Already accumulated
      regenBonus: 80, // Ring of Regen
    });

    const healed = processRegeneration(combatant);

    // Rate = 20 + 3 + 80 = 103
    // Points = 180 + 103 = 283
    // Heals 2 (283 -> 183 -> 83), remaining 83 points
    expect(healed).toBe(2);
    expect(combatant.regenPoints).toBe(83);
    expect(combatant.currentHealth).toBe(12);
  });

  it('does not heal dead combatants', () => {
    const combatant = createMockCombatant({
      maxHealth: 10,
      currentHealth: 0,
      regenPoints: 200,
      regenBonus: 0,
    });

    const healed = processRegeneration(combatant);

    expect(healed).toBe(0);
    // Points reset when dead/full
    expect(combatant.regenPoints).toBe(0);
    expect(combatant.currentHealth).toBe(0);
  });

  it('does not heal above max health', () => {
    const combatant = createMockCombatant({
      maxHealth: 10,
      currentHealth: 10, // Already full
      regenPoints: 200,
      regenBonus: 0,
    });

    const healed = processRegeneration(combatant);

    expect(healed).toBe(0);
    expect(combatant.regenPoints).toBe(0); // Reset at full health
    expect(combatant.currentHealth).toBe(10);
  });

  it('stops healing at max health mid-process', () => {
    const combatant = createMockCombatant({
      maxHealth: 10,
      currentHealth: 9, // Only 1 HP missing
      regenPoints: 250, // Enough for 2+ heals
      regenBonus: 80,
    });

    const healed = processRegeneration(combatant);

    // Should only heal 1 HP to reach max
    expect(healed).toBe(1);
    expect(combatant.currentHealth).toBe(10);
    // Remaining points capped at what's left after one heal
  });

  it('resets points when at full health', () => {
    const combatant = createMockCombatant({
      maxHealth: 10,
      currentHealth: 10,
      regenPoints: 50, // Had some accumulated
      regenBonus: 0,
    });

    processRegeneration(combatant);

    // Points should be reset, not stockpiled
    expect(combatant.regenPoints).toBe(0);
  });

  it('handles exactly threshold points', () => {
    const combatant = createMockCombatant({
      maxHealth: 60, // floor(60/6) = 10, rate = 30
      currentHealth: 5,
      regenPoints: 70, // 70 + 30 = 100 exactly
      regenBonus: 0,
    });

    const healed = processRegeneration(combatant);

    expect(healed).toBe(1);
    expect(combatant.regenPoints).toBe(0);
    expect(combatant.currentHealth).toBe(6);
  });
});

// =============================================================================
// Turns to Heal Tests
// =============================================================================

describe('turnsToHealOne', () => {
  it('calculates turns needed to heal 1 HP', () => {
    // Rate = 22 (20 + 2 + 0), threshold = 100
    // ceil(100 / 22) = ceil(4.54) = 5 turns
    const combatant = createMockCombatant({ maxHealth: 12, regenBonus: 0 });
    expect(turnsToHealOne(combatant)).toBe(5);
  });

  it('returns 1 for very high regen rate', () => {
    // Rate = 102 (20 + 2 + 80), threshold = 100
    // ceil(100 / 102) = 1 turn
    const combatant = createMockCombatant({ maxHealth: 12, regenBonus: 80 });
    expect(turnsToHealOne(combatant)).toBe(1);
  });

  it('handles rate equal to threshold', () => {
    // Need rate = 100, so: 20 + floor(x/6) + bonus = 100
    // If maxHealth = 480: floor(480/6) = 80, rate = 20 + 80 + 0 = 100
    const combatant = createMockCombatant({ maxHealth: 480, regenBonus: 0 });
    expect(turnsToHealOne(combatant)).toBe(1);
  });

  it('returns Infinity for zero or negative rate', () => {
    // This shouldn't happen in practice, but handle edge case
    const combatant = createMockCombatant({
      maxHealth: 1, // floor(1/6) = 0
      regenBonus: -20, // Nullifies base rate
    });
    // Rate = 20 + 0 + (-20) = 0
    expect(turnsToHealOne(combatant)).toBe(Infinity);
  });
});

describe('turnsToFullHeal', () => {
  it('calculates turns needed to fully heal', () => {
    const combatant = createMockCombatant({
      maxHealth: 12,
      currentHealth: 7, // 5 HP missing
      regenBonus: 0,
    });

    // turnsToHealOne = 5 (as calculated above)
    // Missing = 5, so 5 * 5 = 25 turns
    expect(turnsToFullHeal(combatant)).toBe(25);
  });

  it('returns 0 when already at full health', () => {
    const combatant = createMockCombatant({
      maxHealth: 10,
      currentHealth: 10,
    });
    expect(turnsToFullHeal(combatant)).toBe(0);
  });

  it('handles single HP missing', () => {
    const combatant = createMockCombatant({
      maxHealth: 12,
      currentHealth: 11, // 1 HP missing
      regenBonus: 0,
    });
    expect(turnsToFullHeal(combatant)).toBe(5); // 1 * turnsToHealOne
  });

  it('handles large health pools', () => {
    const combatant = createMockCombatant({
      maxHealth: 100,
      currentHealth: 50, // 50 HP missing
      regenBonus: 0,
    });

    // Rate = 20 + 16 + 0 = 36
    // turnsToHealOne = ceil(100/36) = 3
    // turnsToFullHeal = 50 * 3 = 150
    expect(turnsToFullHeal(combatant)).toBe(150);
  });
});

// =============================================================================
// Integration Tests
// =============================================================================

describe('regeneration integration', () => {
  it('simulates regeneration over multiple turns', () => {
    const combatant = createMockCombatant({
      maxHealth: 20,
      currentHealth: 15, // 5 HP missing
      regenPoints: 0,
      regenBonus: 0,
    });

    // Rate = 20 + 3 + 0 = 23
    // Should take about 5 turns per HP: ceil(100/23) = 5
    // Total: ~25 turns to fully heal

    let totalHealed = 0;
    let turns = 0;
    const maxTurns = 50; // Safety limit

    while (combatant.currentHealth < combatant.maxHealth && turns < maxTurns) {
      totalHealed += processRegeneration(combatant);
      turns++;
    }

    expect(totalHealed).toBe(5);
    expect(combatant.currentHealth).toBe(20);
    // Should be close to predicted 25 turns
    expect(turns).toBeGreaterThanOrEqual(20);
    expect(turns).toBeLessThanOrEqual(30);
  });

  it('ring of regeneration dramatically improves healing', () => {
    const withoutRing = createMockCombatant({
      maxHealth: 20,
      currentHealth: 10,
      regenPoints: 0,
      regenBonus: 0,
    });

    const withRing = createMockCombatant({
      maxHealth: 20,
      currentHealth: 10,
      regenPoints: 0,
      regenBonus: 80, // Ring of Regeneration
    });

    // Heal both to full and compare turns
    let turnsWithout = 0;
    let turnsWith = 0;

    while (withoutRing.currentHealth < withoutRing.maxHealth) {
      processRegeneration(withoutRing);
      turnsWithout++;
    }

    while (withRing.currentHealth < withRing.maxHealth) {
      processRegeneration(withRing);
      turnsWith++;
    }

    // Ring should heal much faster
    expect(turnsWith).toBeLessThan(turnsWithout / 3);
  });

  it('high max HP increases regen rate', () => {
    const lowHP = createMockCombatant({
      maxHealth: 10,
      currentHealth: 5,
    });

    const highHP = createMockCombatant({
      maxHealth: 100,
      currentHealth: 95, // Same 5 HP missing
    });

    const turnsLow = turnsToFullHeal(lowHP);
    const turnsHigh = turnsToFullHeal(highHP);

    // High HP combatant has higher regen rate (36 vs 21)
    // So should heal faster per HP
    expect(turnsToHealOne(highHP)).toBeLessThan(turnsToHealOne(lowHP));
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
    fleeTurnsRemaining: 0,
    fleeTargetDistance: 4,
    ...overrides,
  };
}
