/**
 * Unit tests for the inline creature parser.
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { parseInlineCreature, parseInlineCreatures } from '../data/inline-parser.js';
import { loadGameData, type GameData } from '../data/data-loader.js';

// Load game data once for all tests
let gameData: GameData;

beforeAll(() => {
  gameData = loadGameData();
});

describe('parseInlineCreature', () => {
  describe('base creature mode', () => {
    it('parses creature with base and name override', () => {
      const json = '{"base":"goblin","name":"test-goblin"}';
      const { creature, warnings } = parseInlineCreature(json, gameData);

      expect(creature.name).toBe('test-goblin');
      expect(creature.id).toBe('inline_test-goblin');
      expect(warnings).toHaveLength(0);
    });

    it('inherits stats from base creature', () => {
      const json = '{"base":"goblin","name":"inherited"}';
      const { creature } = parseInlineCreature(json, gameData);

      // Should inherit goblin's base stats
      const baseGoblin = gameData.creatures.get('goblin')!;
      expect(creature.health).toBe(baseGoblin.health);
      expect(creature.speed).toBe(baseGoblin.speed);
    });

    it('applies stat overrides on top of base', () => {
      const json = '{"base":"goblin","name":"strong","strength":5}';
      const { creature } = parseInlineCreature(json, gameData);

      expect(creature.strength).toBe(5);
      // Other stats should still be inherited
      const baseGoblin = gameData.creatures.get('goblin')!;
      expect(creature.agility).toBe(baseGoblin.agility);
    });

    it('overrides all stat types', () => {
      const json = JSON.stringify({
        base: 'goblin',
        name: 'all-stats',
        strength: 1,
        agility: 2,
        endurance: 3,
        will: 4,
        health: 15,
        speed: 12,
      });
      const { creature } = parseInlineCreature(json, gameData);

      expect(creature.strength).toBe(1);
      expect(creature.agility).toBe(2);
      expect(creature.endurance).toBe(3);
      expect(creature.will).toBe(4);
      expect(creature.health).toBe(15);
      expect(creature.speed).toBe(12);
    });

    it('replaces equipment when specified', () => {
      const json = '{"base":"goblin","name":"armed","equipment":["club","leather"]}';
      const { creature } = parseInlineCreature(json, gameData);

      expect(creature.equipment).toEqual(['club', 'leather']);
    });

    it('replaces damage modifiers when specified', () => {
      const json = JSON.stringify({
        base: 'goblin',
        name: 'resistant',
        resistances: ['Piercing', 'Slashing'],
        vulnerabilities: ['Fire'],
        immunities: ['Poison'],
      });
      const { creature } = parseInlineCreature(json, gameData);

      expect(creature.resistances).toEqual(['Piercing', 'Slashing']);
      expect(creature.vulnerabilities).toEqual(['Fire']);
      expect(creature.immunities).toEqual(['Poison']);
    });
  });

  describe('complete definition mode', () => {
    it('creates creature from scratch with required name', () => {
      const json = '{"name":"custom-creature","health":10}';
      const { creature, warnings } = parseInlineCreature(json, gameData);

      expect(creature.name).toBe('custom-creature');
      expect(creature.id).toBe('inline_custom-creature');
      expect(creature.type).toBe('inline');
      expect(warnings).toHaveLength(0);
    });

    it('uses default values for unspecified stats', () => {
      const json = '{"name":"minimal"}';
      const { creature } = parseInlineCreature(json, gameData);

      expect(creature.strength).toBe(0);
      expect(creature.agility).toBe(0);
      expect(creature.endurance).toBe(0);
      expect(creature.will).toBe(0);
      expect(creature.health).toBe(10);
      expect(creature.speed).toBe(10);
      expect(creature.equipment).toEqual([]);
      expect(creature.attacks).toEqual([]);
      expect(creature.resistances).toEqual([]);
      expect(creature.vulnerabilities).toEqual([]);
      expect(creature.immunities).toEqual([]);
    });

    it('accepts full creature definition', () => {
      const json = JSON.stringify({
        name: 'full-custom',
        strength: 3,
        agility: 2,
        endurance: 1,
        will: 0,
        health: 20,
        speed: 8,
        equipment: ['club'],
        resistances: ['Bludgeoning'],
      });
      const { creature } = parseInlineCreature(json, gameData);

      expect(creature.name).toBe('full-custom');
      expect(creature.strength).toBe(3);
      expect(creature.agility).toBe(2);
      expect(creature.endurance).toBe(1);
      expect(creature.will).toBe(0);
      expect(creature.health).toBe(20);
      expect(creature.speed).toBe(8);
      expect(creature.equipment).toEqual(['club']);
      expect(creature.resistances).toEqual(['Bludgeoning']);
    });
  });

  describe('error handling', () => {
    it('throws on invalid JSON', () => {
      expect(() => parseInlineCreature('{invalid}', gameData)).toThrow('Invalid JSON');
    });

    it('throws on non-object JSON', () => {
      expect(() => parseInlineCreature('"string"', gameData)).toThrow(
        'Inline creature must be a JSON object'
      );
      expect(() => parseInlineCreature('[1,2,3]', gameData)).toThrow(
        'Inline creature must be a JSON object'
      );
      expect(() => parseInlineCreature('null', gameData)).toThrow(
        'Inline creature must be a JSON object'
      );
    });

    it('throws when name missing without base', () => {
      expect(() => parseInlineCreature('{"strength":5}', gameData)).toThrow(
        "Inline creature requires 'name' when no 'base' is specified"
      );
    });

    it('throws on invalid base creature', () => {
      expect(() =>
        parseInlineCreature('{"base":"nonexistent","name":"test"}', gameData)
      ).toThrow(/Base creature 'nonexistent' not found/);
    });

    it('throws on invalid damage types in resistances', () => {
      const json = '{"name":"test","resistances":["InvalidType"]}';
      expect(() => parseInlineCreature(json, gameData)).toThrow(
        /Invalid damage type.*InvalidType/
      );
    });

    it('throws on invalid damage types in vulnerabilities', () => {
      const json = '{"name":"test","vulnerabilities":["BadType"]}';
      expect(() => parseInlineCreature(json, gameData)).toThrow(
        /Invalid damage type.*BadType/
      );
    });

    it('throws on invalid damage types in immunities', () => {
      const json = '{"name":"test","immunities":["WrongType"]}';
      expect(() => parseInlineCreature(json, gameData)).toThrow(
        /Invalid damage type.*WrongType/
      );
    });

    it('throws on zero or negative health', () => {
      expect(() => parseInlineCreature('{"name":"test","health":0}', gameData)).toThrow(
        /Invalid health value.*must be > 0/
      );
      expect(() => parseInlineCreature('{"name":"test","health":-5}', gameData)).toThrow(
        /Invalid health value.*must be > 0/
      );
    });

    it('throws on zero or negative speed', () => {
      expect(() => parseInlineCreature('{"name":"test","speed":0}', gameData)).toThrow(
        /Invalid speed value.*must be > 0/
      );
      expect(() => parseInlineCreature('{"name":"test","speed":-1}', gameData)).toThrow(
        /Invalid speed value.*must be > 0/
      );
    });
  });

  describe('typo detection warnings', () => {
    it('warns on unknown fields', () => {
      const json = '{"base":"goblin","name":"test","unknownField":123}';
      const { warnings } = parseInlineCreature(json, gameData);

      expect(warnings.length).toBeGreaterThan(0);
      expect(warnings[0]).toContain('unknownField');
    });

    it('suggests strength for str-like typos', () => {
      const json = '{"base":"goblin","name":"test","str":5}';
      const { warnings } = parseInlineCreature(json, gameData);

      expect(warnings.some((w) => w.includes('strength'))).toBe(true);
    });

    it('suggests agility for agi-like typos', () => {
      const json = '{"base":"goblin","name":"test","agi":5}';
      const { warnings } = parseInlineCreature(json, gameData);

      expect(warnings.some((w) => w.includes('agility'))).toBe(true);
    });

    it('suggests health for hp-like typos', () => {
      const json = '{"base":"goblin","name":"test","hp":20}';
      const { warnings } = parseInlineCreature(json, gameData);

      expect(warnings.some((w) => w.includes('health'))).toBe(true);
    });

    it('suggests resistances for resist-like typos', () => {
      const json = '{"base":"goblin","name":"test","resist":["Piercing"]}';
      const { warnings } = parseInlineCreature(json, gameData);

      expect(warnings.some((w) => w.includes('resistances'))).toBe(true);
    });
  });
});

describe('parseInlineCreatures', () => {
  it('parses array of inline creatures', () => {
    const json = JSON.stringify([
      { base: 'goblin', name: 'variant1' },
      { base: 'goblin', name: 'variant2', strength: 2 },
      { name: 'custom', health: 15 },
    ]);
    const results = parseInlineCreatures(json, gameData);

    expect(results).toHaveLength(3);
    expect(results[0].creature.name).toBe('variant1');
    expect(results[1].creature.name).toBe('variant2');
    expect(results[1].creature.strength).toBe(2);
    expect(results[2].creature.name).toBe('custom');
    expect(results[2].creature.health).toBe(15);
  });

  it('throws on non-array JSON', () => {
    expect(() => parseInlineCreatures('{"name":"test"}', gameData)).toThrow(
      'Expected a JSON array'
    );
  });

  it('throws with index on invalid creature in array', () => {
    const json = '[{"base":"goblin","name":"ok"},{"strength":5}]';
    expect(() => parseInlineCreatures(json, gameData)).toThrow(/index 1/);
  });

  it('collects warnings from all creatures', () => {
    const json = JSON.stringify([
      { base: 'goblin', name: 'v1', unknownA: 1 },
      { base: 'goblin', name: 'v2', unknownB: 2 },
    ]);
    const results = parseInlineCreatures(json, gameData);

    expect(results[0].warnings.length).toBeGreaterThan(0);
    expect(results[1].warnings.length).toBeGreaterThan(0);
  });
});

describe('inline attacks', () => {
  it('parses custom attacks', () => {
    const json = JSON.stringify({
      name: 'attacker',
      attacks: [
        {
          name: 'fire breath',
          type: 'Ranged',
          dice: '2d6',
          damageType: 'Fire',
          range: 5,
        },
      ],
    });
    const { creature } = parseInlineCreature(json, gameData);

    expect(creature.attacks).toHaveLength(1);
    expect(creature.attacks[0].name).toBe('fire breath');
    expect(creature.attacks[0].type).toBe('Ranged');
    expect(creature.attacks[0].dice).toBe('2d6');
    expect(creature.attacks[0].damageType).toBe('Fire');
    expect(creature.attacks[0].range).toBe(5);
  });

  it('defaults range based on attack type', () => {
    const json = JSON.stringify({
      name: 'attacker',
      attacks: [
        { name: 'melee', type: 'Melee', dice: '1d6', damageType: 'Slashing' },
        { name: 'ranged', type: 'Ranged', dice: '1d4', damageType: 'Piercing' },
      ],
    });
    const { creature } = parseInlineCreature(json, gameData);

    expect(creature.attacks[0].range).toBe(1); // Melee default
    expect(creature.attacks[1].range).toBe(6); // Ranged default
  });
});
