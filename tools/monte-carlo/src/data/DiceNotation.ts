/**
 * Dice notation parser and roller.
 * Supports standard notation: NdS+M (e.g., "2d6+3", "1d8", "3d4-1")
 */

/**
 * Parsed dice expression.
 */
export interface ParsedDice {
  count: number; // Number of dice
  sides: number; // Sides per die
  modifier: number; // Flat modifier (+/-)
}

/**
 * Result of a dice roll.
 */
export interface RollResult {
  total: number;
  rolls: number[];
  modifier: number;
}

// Regex pattern for dice notation: NdS or NdS+M or NdS-M
const DICE_PATTERN = /^(\d+)d(\d+)([+-]\d+)?$/i;

/**
 * Random number generator interface for seeding.
 */
export interface RandomGenerator {
  /** Returns a random float in [0, 1) */
  random(): number;
}

/**
 * Default RNG using Math.random.
 */
export const defaultRng: RandomGenerator = {
  random: () => Math.random(),
};

/**
 * Seeded random number generator using a simple LCG algorithm.
 * Provides deterministic sequences for testing.
 */
export class SeededRng implements RandomGenerator {
  private state: number;

  constructor(seed: number) {
    // Ensure seed is a positive integer
    this.state = Math.abs(Math.floor(seed)) || 1;
  }

  random(): number {
    // Linear Congruential Generator (LCG)
    // Using parameters from Numerical Recipes
    this.state = (this.state * 1664525 + 1013904223) >>> 0;
    return this.state / 0x100000000;
  }
}

/**
 * Parse a dice notation string into its components.
 * @param notation Dice notation string (e.g., "2d6+3")
 * @returns Parsed dice components
 * @throws Error if notation is invalid
 */
export function parseDice(notation: string): ParsedDice {
  const trimmed = notation.trim();
  const match = trimmed.match(DICE_PATTERN);

  if (!match) {
    throw new Error(`Invalid dice notation: "${notation}"`);
  }

  const count = parseInt(match[1], 10);
  const sides = parseInt(match[2], 10);
  const modifier = match[3] ? parseInt(match[3], 10) : 0;

  if (count < 1) {
    throw new Error(`Dice count must be at least 1: "${notation}"`);
  }
  if (sides < 1) {
    throw new Error(`Dice sides must be at least 1: "${notation}"`);
  }

  return { count, sides, modifier };
}

/**
 * Roll dice based on parsed components.
 * @param dice Parsed dice components
 * @param rng Random number generator (defaults to Math.random)
 * @returns Roll result with total and individual rolls
 */
export function rollParsedDice(
  dice: ParsedDice,
  rng: RandomGenerator = defaultRng
): RollResult {
  const rolls: number[] = [];

  for (let i = 0; i < dice.count; i++) {
    // Generate 1 to sides (inclusive)
    const roll = Math.floor(rng.random() * dice.sides) + 1;
    rolls.push(roll);
  }

  const total = rolls.reduce((sum, r) => sum + r, 0) + dice.modifier;

  return {
    total,
    rolls,
    modifier: dice.modifier,
  };
}

/**
 * Roll dice from a notation string.
 * @param notation Dice notation string (e.g., "2d6+3")
 * @param rng Random number generator (defaults to Math.random)
 * @returns Roll result with total and individual rolls
 */
export function roll(
  notation: string,
  rng: RandomGenerator = defaultRng
): RollResult {
  const parsed = parseDice(notation);
  return rollParsedDice(parsed, rng);
}

/**
 * Roll dice and return just the total.
 * @param notation Dice notation string (e.g., "2d6+3")
 * @param rng Random number generator (defaults to Math.random)
 * @returns Total of the roll
 */
export function rollTotal(
  notation: string,
  rng: RandomGenerator = defaultRng
): number {
  return roll(notation, rng).total;
}

/**
 * Roll NdS with a modifier, matching the game's DiceRoller.Roll(count, sides, modifier).
 * @param count Number of dice
 * @param sides Sides per die
 * @param modifier Flat modifier to add
 * @param rng Random number generator
 * @returns Total of the roll
 */
export function rollDice(
  count: number,
  sides: number,
  modifier: number = 0,
  rng: RandomGenerator = defaultRng
): number {
  let total = modifier;
  for (let i = 0; i < count; i++) {
    total += Math.floor(rng.random() * sides) + 1;
  }
  return total;
}

/**
 * Calculate the minimum possible result for a dice notation.
 */
export function minRoll(notation: string): number {
  const dice = parseDice(notation);
  return dice.count + dice.modifier; // All 1s
}

/**
 * Calculate the maximum possible result for a dice notation.
 */
export function maxRoll(notation: string): number {
  const dice = parseDice(notation);
  return dice.count * dice.sides + dice.modifier; // All max
}

/**
 * Calculate the average (expected) result for a dice notation.
 */
export function avgRoll(notation: string): number {
  const dice = parseDice(notation);
  // Average of a single die is (1 + sides) / 2
  const avgPerDie = (1 + dice.sides) / 2;
  return dice.count * avgPerDie + dice.modifier;
}

/**
 * Weighted random rounding, matching the game's SpeedComponent.WeightedRound().
 * A value of 6.4 has 60% chance of 6, 40% chance of 7.
 * @param value The value to round
 * @param rng Random number generator
 * @returns Rounded integer
 */
export function weightedRound(
  value: number,
  rng: RandomGenerator = defaultRng
): number {
  const floor = Math.floor(value);
  const fraction = value - floor;

  if (fraction === 0) {
    return floor;
  }

  // Random roll: if roll < fraction, round up
  return rng.random() < fraction ? floor + 1 : floor;
}
