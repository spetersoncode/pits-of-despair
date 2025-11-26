/**
 * Unit tests for the dice notation parser and roller.
 */

import { describe, it, expect } from 'vitest';
import {
  parseDice,
  roll,
  rollTotal,
  rollDice,
  minRoll,
  maxRoll,
  avgRoll,
  weightedRound,
  SeededRng,
} from '../data/dice-notation.js';

describe('parseDice', () => {
  it('parses simple dice notation', () => {
    expect(parseDice('1d6')).toEqual({ count: 1, sides: 6, modifier: 0 });
    expect(parseDice('2d8')).toEqual({ count: 2, sides: 8, modifier: 0 });
    expect(parseDice('3d4')).toEqual({ count: 3, sides: 4, modifier: 0 });
  });

  it('parses dice with positive modifier', () => {
    expect(parseDice('1d6+2')).toEqual({ count: 1, sides: 6, modifier: 2 });
    expect(parseDice('2d4+5')).toEqual({ count: 2, sides: 4, modifier: 5 });
  });

  it('parses dice with negative modifier', () => {
    expect(parseDice('1d6-1')).toEqual({ count: 1, sides: 6, modifier: -1 });
    expect(parseDice('2d8-3')).toEqual({ count: 2, sides: 8, modifier: -3 });
  });

  it('handles whitespace', () => {
    expect(parseDice('  1d6  ')).toEqual({ count: 1, sides: 6, modifier: 0 });
  });

  it('is case insensitive', () => {
    expect(parseDice('1D6')).toEqual({ count: 1, sides: 6, modifier: 0 });
    expect(parseDice('2D8+3')).toEqual({ count: 2, sides: 8, modifier: 3 });
  });

  it('throws on invalid notation', () => {
    expect(() => parseDice('invalid')).toThrow('Invalid dice notation');
    expect(() => parseDice('d6')).toThrow('Invalid dice notation');
    expect(() => parseDice('1d')).toThrow('Invalid dice notation');
    expect(() => parseDice('')).toThrow('Invalid dice notation');
  });

  it('throws on zero dice count', () => {
    expect(() => parseDice('0d6')).toThrow('Dice count must be at least 1');
  });

  it('throws on zero sides', () => {
    expect(() => parseDice('1d0')).toThrow('Dice sides must be at least 1');
  });
});

describe('roll with SeededRng', () => {
  it('produces deterministic results', () => {
    const rng1 = new SeededRng(12345);
    const rng2 = new SeededRng(12345);

    // Same seed should produce same sequence
    for (let i = 0; i < 10; i++) {
      expect(roll('2d6', rng1).total).toBe(roll('2d6', rng2).total);
    }
  });

  it('produces different results with different seeds', () => {
    const rng1 = new SeededRng(12345);
    const rng2 = new SeededRng(54321);

    // Different seeds should (very likely) produce different sequences
    const results1 = Array.from({ length: 10 }, () => rollTotal('2d6', rng1));
    const results2 = Array.from({ length: 10 }, () => rollTotal('2d6', rng2));

    // At least some results should differ
    expect(results1.some((r, i) => r !== results2[i])).toBe(true);
  });

  it('returns correct roll structure', () => {
    const rng = new SeededRng(42);
    const result = roll('2d6+3', rng);

    expect(result.rolls).toHaveLength(2);
    expect(result.rolls.every((r) => r >= 1 && r <= 6)).toBe(true);
    expect(result.modifier).toBe(3);
    expect(result.total).toBe(result.rolls[0] + result.rolls[1] + 3);
  });
});

describe('rollDice', () => {
  it('produces values in expected range', () => {
    const rng = new SeededRng(99);

    for (let i = 0; i < 100; i++) {
      const result = rollDice(2, 6, 0, rng);
      expect(result).toBeGreaterThanOrEqual(2); // Minimum: 1+1
      expect(result).toBeLessThanOrEqual(12); // Maximum: 6+6
    }
  });

  it('applies modifier correctly', () => {
    const rng = new SeededRng(123);

    for (let i = 0; i < 100; i++) {
      const result = rollDice(1, 6, 5, rng);
      expect(result).toBeGreaterThanOrEqual(6); // Min: 1+5
      expect(result).toBeLessThanOrEqual(11); // Max: 6+5
    }
  });

  it('handles negative modifier', () => {
    const rng = new SeededRng(456);

    for (let i = 0; i < 100; i++) {
      const result = rollDice(1, 6, -2, rng);
      expect(result).toBeGreaterThanOrEqual(-1); // Min: 1-2
      expect(result).toBeLessThanOrEqual(4); // Max: 6-2
    }
  });
});

describe('minRoll', () => {
  it('calculates minimum roll', () => {
    expect(minRoll('1d6')).toBe(1);
    expect(minRoll('2d6')).toBe(2);
    expect(minRoll('3d4')).toBe(3);
    expect(minRoll('1d6+2')).toBe(3);
    expect(minRoll('2d6-1')).toBe(1);
  });
});

describe('maxRoll', () => {
  it('calculates maximum roll', () => {
    expect(maxRoll('1d6')).toBe(6);
    expect(maxRoll('2d6')).toBe(12);
    expect(maxRoll('3d4')).toBe(12);
    expect(maxRoll('1d6+2')).toBe(8);
    expect(maxRoll('2d6-1')).toBe(11);
  });
});

describe('avgRoll', () => {
  it('calculates average roll', () => {
    expect(avgRoll('1d6')).toBe(3.5);
    expect(avgRoll('2d6')).toBe(7);
    expect(avgRoll('1d6+2')).toBe(5.5);
    expect(avgRoll('2d6-1')).toBe(6);
    expect(avgRoll('3d4')).toBe(7.5);
  });
});

describe('weightedRound', () => {
  it('rounds integers unchanged', () => {
    const rng = new SeededRng(1);
    expect(weightedRound(5, rng)).toBe(5);
    expect(weightedRound(10, rng)).toBe(10);
    expect(weightedRound(0, rng)).toBe(0);
  });

  it('produces distribution matching fraction', () => {
    // For 6.4, expect ~60% of rounds to be 6, ~40% to be 7
    const rng = new SeededRng(42);
    const iterations = 10000;
    let count6 = 0;
    let count7 = 0;

    for (let i = 0; i < iterations; i++) {
      const result = weightedRound(6.4, rng);
      if (result === 6) count6++;
      else if (result === 7) count7++;
      else throw new Error(`Unexpected result: ${result}`);
    }

    // Allow 5% tolerance
    const ratio6 = count6 / iterations;
    const ratio7 = count7 / iterations;

    expect(ratio6).toBeGreaterThan(0.55);
    expect(ratio6).toBeLessThan(0.65);
    expect(ratio7).toBeGreaterThan(0.35);
    expect(ratio7).toBeLessThan(0.45);
  });

  it('always rounds up when fraction is very high', () => {
    const rng = new SeededRng(999);
    let roundedUp = 0;

    for (let i = 0; i < 100; i++) {
      if (weightedRound(5.99, rng) === 6) roundedUp++;
    }

    // Should round up ~99% of the time
    expect(roundedUp).toBeGreaterThan(90);
  });

  it('always rounds down when fraction is very low', () => {
    const rng = new SeededRng(888);
    let roundedDown = 0;

    for (let i = 0; i < 100; i++) {
      if (weightedRound(5.01, rng) === 5) roundedDown++;
    }

    // Should round down ~99% of the time
    expect(roundedDown).toBeGreaterThan(90);
  });
});

describe('roll distribution', () => {
  it('approximates expected average over many rolls', () => {
    const rng = new SeededRng(777);
    const iterations = 10000;
    let total = 0;

    for (let i = 0; i < iterations; i++) {
      total += rollTotal('2d6', rng);
    }

    const average = total / iterations;
    // Expected average for 2d6 is 7, allow 2% tolerance
    expect(average).toBeGreaterThan(6.86);
    expect(average).toBeLessThan(7.14);
  });
});
