/**
 * Unit tests for the turn scheduler module.
 * Tests speed/delay calculations and turn ordering.
 */

import { describe, it, expect } from 'vitest';
import {
  AVERAGE_SPEED,
  MIN_SPEED,
  MIN_DELAY,
  STANDARD_ACTION_DELAY,
  calculateDelay,
  advanceTime,
  getNextReady,
  deductTime,
  turnOrder,
} from '../simulation/turn-scheduler.js';
import { SeededRng } from '../data/dice-notation.js';
import type { Combatant as CombatantState } from '../data/types.js';

// =============================================================================
// Constants Tests
// =============================================================================

describe('constants', () => {
  it('has expected values', () => {
    expect(AVERAGE_SPEED).toBe(10);
    expect(MIN_SPEED).toBe(1);
    expect(MIN_DELAY).toBe(6);
    expect(STANDARD_ACTION_DELAY).toBe(10);
  });
});

// =============================================================================
// Calculate Delay Tests
// =============================================================================

describe('calculateDelay', () => {
  it('returns base delay at average speed (10)', () => {
    // At speed 10: delay = 10 * (10/10) = 10
    const rng = { random: () => 0.5 }; // Deterministic for weighted rounding
    const delay = calculateDelay(10, 10, rng);
    expect(delay).toBe(10);
  });

  it('calculates slower delay for speed < 10', () => {
    // At speed 8: delay = 10 * (10/8) = 12.5
    // Weighted rounding: if random < fraction (0.5), round UP
    // With random = 0.4 < 0.5, rounds up to 13
    const rngUp = { random: () => 0.4 };
    expect(calculateDelay(8, 10, rngUp)).toBe(13);

    // With random = 0.6 >= 0.5, rounds down to 12
    const rngDown = { random: () => 0.6 };
    expect(calculateDelay(8, 10, rngDown)).toBe(12);
  });

  it('calculates faster delay for speed > 10', () => {
    // At speed 12: delay = 10 * (10/12) = 8.33
    // With random < 0.33, should round up to 9; otherwise 8
    const rng = { random: () => 0.5 }; // > 0.33, rounds down
    const delay = calculateDelay(12, 10, rng);
    expect(delay).toBe(8);
  });

  it('enforces minimum delay of 6', () => {
    // Very high speed: delay = 10 * (10/20) = 5, but min is 6
    const rng = { random: () => 0.5 };
    const delay = calculateDelay(20, 10, rng);
    expect(delay).toBe(6);
  });

  it('clamps speed to minimum of 1', () => {
    // Speed 0 should be treated as 1
    // delay = 10 * (10/1) = 100
    const rng = { random: () => 0.5 };
    const delay = calculateDelay(0, 10, rng);
    expect(delay).toBe(100);

    // Negative speed also clamped to 1
    const delayNeg = calculateDelay(-5, 10, rng);
    expect(delayNeg).toBe(100);
  });

  it('scales with different base costs', () => {
    // Free action (cost 0) - should still have minimum delay
    const rng = { random: () => 0.5 };
    const freeDelay = calculateDelay(10, 0, rng);
    expect(freeDelay).toBe(6); // MIN_DELAY

    // Expensive action (cost 20)
    // delay = 20 * (10/10) = 20
    const expensiveDelay = calculateDelay(10, 20, rng);
    expect(expensiveDelay).toBe(20);
  });

  it('uses weighted rounding correctly', () => {
    // Speed 15: delay = 10 * (10/15) = 6.67
    // Fraction = 0.67, so 67% chance of rounding up

    // RNG < 0.67 -> rounds up to 7
    const rngUp = { random: () => 0.5 };
    expect(calculateDelay(15, 10, rngUp)).toBe(7);

    // RNG >= 0.67 -> rounds down to 6
    const rngDown = { random: () => 0.8 };
    expect(calculateDelay(15, 10, rngDown)).toBe(6);
  });
});

// =============================================================================
// Advance Time Tests
// =============================================================================

describe('advanceTime', () => {
  it('adds time to all living combatants', () => {
    const combatants = [
      createMockCombatant({ id: 'a', currentHealth: 10, accumulatedTime: 0 }),
      createMockCombatant({ id: 'b', currentHealth: 10, accumulatedTime: 5 }),
    ];

    advanceTime(combatants, 10);

    expect(combatants[0].accumulatedTime).toBe(10);
    expect(combatants[1].accumulatedTime).toBe(15);
  });

  it('does not add time to dead combatants', () => {
    const combatants = [
      createMockCombatant({ id: 'alive', currentHealth: 10, accumulatedTime: 0 }),
      createMockCombatant({ id: 'dead', currentHealth: 0, accumulatedTime: 5 }),
    ];

    advanceTime(combatants, 10);

    expect(combatants[0].accumulatedTime).toBe(10);
    expect(combatants[1].accumulatedTime).toBe(5); // Unchanged
  });

  it('handles empty array', () => {
    const combatants: CombatantState[] = [];
    expect(() => advanceTime(combatants, 10)).not.toThrow();
  });

  it('handles zero time advance', () => {
    const combatants = [createMockCombatant({ accumulatedTime: 5 })];
    advanceTime(combatants, 0);
    expect(combatants[0].accumulatedTime).toBe(5);
  });
});

// =============================================================================
// Get Next Ready Tests
// =============================================================================

describe('getNextReady', () => {
  it('returns null when no combatants are ready', () => {
    const combatants = [
      createMockCombatant({ speed: 10, accumulatedTime: 5 }), // Needs 10
      createMockCombatant({ speed: 10, accumulatedTime: 8 }), // Needs 10
    ];
    const rng = { random: () => 0.5 };

    const ready = getNextReady(combatants, 10, rng);
    expect(ready).toBeNull();
  });

  it('returns combatant when accumulated time >= delay', () => {
    const combatants = [
      createMockCombatant({ id: 'ready', speed: 10, accumulatedTime: 10 }),
      createMockCombatant({ id: 'not_ready', speed: 10, accumulatedTime: 5 }),
    ];
    const rng = { random: () => 0.5 };

    const ready = getNextReady(combatants, 10, rng);
    expect(ready?.id).toBe('ready');
  });

  it('prefers faster combatant when multiple are ready', () => {
    const combatants = [
      createMockCombatant({ id: 'slow', speed: 8, accumulatedTime: 15 }),
      createMockCombatant({ id: 'fast', speed: 12, accumulatedTime: 15 }),
      createMockCombatant({ id: 'medium', speed: 10, accumulatedTime: 15 }),
    ];
    const rng = { random: () => 0.5 };

    const ready = getNextReady(combatants, 10, rng);
    expect(ready?.id).toBe('fast');
  });

  it('ignores dead combatants', () => {
    const combatants = [
      createMockCombatant({ id: 'dead_fast', speed: 20, currentHealth: 0, accumulatedTime: 100 }),
      createMockCombatant({ id: 'alive_slow', speed: 5, currentHealth: 10, accumulatedTime: 100 }),
    ];
    const rng = { random: () => 0.5 };

    const ready = getNextReady(combatants, 10, rng);
    expect(ready?.id).toBe('alive_slow');
  });

  it('returns null when all combatants are dead', () => {
    const combatants = [
      createMockCombatant({ currentHealth: 0, accumulatedTime: 100 }),
      createMockCombatant({ currentHealth: 0, accumulatedTime: 100 }),
    ];
    const rng = { random: () => 0.5 };

    const ready = getNextReady(combatants, 10, rng);
    expect(ready).toBeNull();
  });
});

// =============================================================================
// Deduct Time Tests
// =============================================================================

describe('deductTime', () => {
  it('reduces accumulated time by calculated delay', () => {
    const combatant = createMockCombatant({ speed: 10, accumulatedTime: 25 });
    const rng = { random: () => 0.5 };

    deductTime(combatant, 10, rng);
    // At speed 10, delay = 10
    expect(combatant.accumulatedTime).toBe(15);
  });

  it('can result in negative accumulated time', () => {
    const combatant = createMockCombatant({ speed: 10, accumulatedTime: 5 });
    const rng = { random: () => 0.5 };

    deductTime(combatant, 10, rng);
    // 5 - 10 = -5
    expect(combatant.accumulatedTime).toBe(-5);
  });

  it('deducts different amounts based on speed', () => {
    // Slow combatant: higher delay
    const slow = createMockCombatant({ speed: 5, accumulatedTime: 50 });
    const rng1 = { random: () => 0.5 };
    deductTime(slow, 10, rng1);
    // Speed 5: delay = 10 * (10/5) = 20
    expect(slow.accumulatedTime).toBe(30);

    // Fast combatant: lower delay
    const fast = createMockCombatant({ speed: 20, accumulatedTime: 50 });
    const rng2 = { random: () => 0.5 };
    deductTime(fast, 10, rng2);
    // Speed 20: delay = 10 * (10/20) = 5, but MIN_DELAY = 6
    expect(fast.accumulatedTime).toBe(44);
  });
});

// =============================================================================
// Turn Order Generator Tests
// =============================================================================

describe('turnOrder', () => {
  it('yields combatants in speed order initially', () => {
    const combatants = [
      createMockCombatant({ id: 'slow', team: 'A', speed: 5, currentHealth: 10 }),
      createMockCombatant({ id: 'fast', team: 'B', speed: 15, currentHealth: 10 }),
      createMockCombatant({ id: 'medium', team: 'A', speed: 10, currentHealth: 10 }),
    ];
    const rng = new SeededRng(42);

    const order = turnOrder(combatants, rng);
    const first = order.next().value;

    // Fast should go first due to higher speed
    expect(first?.id).toBe('fast');
  });

  it('terminates when only one team remains', () => {
    const combatants = [
      createMockCombatant({ id: 'a1', team: 'A', speed: 10, currentHealth: 1 }),
      createMockCombatant({ id: 'b1', team: 'B', speed: 10, currentHealth: 1 }),
    ];
    const rng = new SeededRng(42);

    const order = turnOrder(combatants, rng);

    // Get first turn
    const turn1 = order.next();
    expect(turn1.done).toBe(false);

    // Kill one combatant
    combatants[1].currentHealth = 0;

    // Next iteration should detect only one team alive
    const turn2 = order.next();

    // The generator should terminate after detecting single team
    // (This may take one more iteration depending on implementation)
    let iterations = 0;
    let done = turn2.done;
    while (!done && iterations < 10) {
      const result = order.next();
      done = result.done ?? false;
      iterations++;
    }
    expect(done).toBe(true);
  });

  it('fast combatants get more turns', () => {
    const combatants = [
      createMockCombatant({ id: 'fast', team: 'A', speed: 20, currentHealth: 100 }),
      createMockCombatant({ id: 'slow', team: 'B', speed: 5, currentHealth: 100 }),
    ];
    const rng = new SeededRng(42);

    const order = turnOrder(combatants, rng);

    // Count turns for each over many iterations
    const turnCounts = { fast: 0, slow: 0 };
    for (let i = 0; i < 20; i++) {
      const result = order.next();
      if (result.done) break;
      turnCounts[result.value!.id as 'fast' | 'slow']++;
    }

    // Fast should have significantly more turns than slow
    // At speed 20 vs 5, fast should have ~4x the turns
    expect(turnCounts.fast).toBeGreaterThan(turnCounts.slow * 2);
  });
});

// =============================================================================
// Integration Tests
// =============================================================================

describe('turn system integration', () => {
  it('cycles through combatants fairly at equal speed', () => {
    const combatants = [
      createMockCombatant({ id: 'a', team: 'A', speed: 10, currentHealth: 10 }),
      createMockCombatant({ id: 'b', team: 'B', speed: 10, currentHealth: 10 }),
    ];
    const rng = new SeededRng(42);

    const order = turnOrder(combatants, rng);

    // At equal speed, should roughly alternate
    const turns: string[] = [];
    for (let i = 0; i < 10; i++) {
      const result = order.next();
      if (result.done) break;
      turns.push(result.value!.id);
    }

    // Both should have roughly equal turns
    const aTurns = turns.filter((t) => t === 'a').length;
    const bTurns = turns.filter((t) => t === 'b').length;
    expect(Math.abs(aTurns - bTurns)).toBeLessThanOrEqual(2);
  });

  it('handles speed changes mid-combat (via direct mutation)', () => {
    const combatants = [
      createMockCombatant({ id: 'a', team: 'A', speed: 10, currentHealth: 10 }),
      createMockCombatant({ id: 'b', team: 'B', speed: 10, currentHealth: 10 }),
    ];
    const rng = new SeededRng(42);

    const order = turnOrder(combatants, rng);

    // Take a few turns
    order.next();
    order.next();

    // Dramatically increase one combatant's speed
    combatants[0].speed = 30;

    // Continue and see if faster combatant dominates
    const turns: string[] = [];
    for (let i = 0; i < 10; i++) {
      const result = order.next();
      if (result.done) break;
      turns.push(result.value!.id);
    }

    // After speed boost, 'a' should have more turns
    const aTurns = turns.filter((t) => t === 'a').length;
    expect(aTurns).toBeGreaterThan(turns.length / 2);
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
