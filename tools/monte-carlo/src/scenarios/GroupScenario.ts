/**
 * GroupScenario - Team vs team combat simulation.
 */

import type {
  GroupConfig,
  TeamMember,
  GameData,
  SimulationResult,
  AggregateResult,
  Combatant as CombatantState,
} from '../data/types.js';
import type { RandomGenerator } from '../data/DiceNotation.js';
import { getCreature } from '../data/DataLoader.js';
import { createCombatant, resetCombatantIdCounter } from '../simulation/Combatant.js';
import { runCombat, DEFAULT_CONFIG } from '../simulation/CombatEngine.js';
import { aggregateResults, type Scenario } from './Scenario.js';

/**
 * Create combatants for a team.
 */
function createTeam(
  members: TeamMember[],
  team: 'A' | 'B',
  gameData: GameData,
  startX: number
): CombatantState[] {
  const combatants: CombatantState[] = [];
  let yOffset = 0;

  for (const member of members) {
    const creature = getCreature(member.creatureId, gameData);

    for (let i = 0; i < member.count; i++) {
      const combatant = createCombatant(
        creature,
        team,
        gameData,
        { x: startX, y: yOffset },
        member.equipmentOverrides
      );
      combatants.push(combatant);
      yOffset++;
    }
  }

  return combatants;
}

/**
 * Format team description for scenario name.
 */
function formatTeam(members: TeamMember[]): string {
  return members
    .map((m) => (m.count > 1 ? `${m.count}x ${m.creatureId}` : m.creatureId))
    .join(', ');
}

/**
 * Run a group battle scenario.
 */
export function runGroupBattle(
  config: GroupConfig,
  gameData: GameData,
  rng: RandomGenerator
): AggregateResult {
  const results: SimulationResult[] = [];

  for (let i = 0; i < config.iterations; i++) {
    resetCombatantIdCounter();

    const teamA = createTeam(config.teamA, 'A', gameData, 0);
    const teamB = createTeam(config.teamB, 'B', gameData, 5);

    const result = runCombat([...teamA, ...teamB], DEFAULT_CONFIG, rng);
    results.push(result);
  }

  const scenarioName = `[${formatTeam(config.teamA)}] vs [${formatTeam(config.teamB)}]`;
  return aggregateResults(results, scenarioName);
}

/**
 * GroupScenario class implementing Scenario interface.
 */
export class GroupScenario implements Scenario {
  public readonly name: string;
  private readonly config: Omit<GroupConfig, 'iterations'>;

  constructor(teamA: TeamMember[], teamB: TeamMember[]) {
    this.name = `[${formatTeam(teamA)}] vs [${formatTeam(teamB)}]`;
    this.config = { teamA, teamB };
  }

  run(
    iterations: number,
    gameData: GameData,
    rng: RandomGenerator
  ): AggregateResult {
    return runGroupBattle({ ...this.config, iterations }, gameData, rng);
  }
}

/**
 * Parse a team string like "goblin:3,skeleton:2" into TeamMembers.
 */
export function parseTeamString(teamStr: string): TeamMember[] {
  return teamStr.split(',').map((part) => {
    const [creatureId, countStr] = part.trim().split(':');
    const count = countStr ? parseInt(countStr, 10) : 1;
    return { creatureId: creatureId.trim(), count };
  });
}
