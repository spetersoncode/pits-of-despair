/**
 * VerboseLogger - Detailed combat logging for debugging/troubleshooting.
 * Provides full visibility into combat mechanics: AI decisions, dice rolls,
 * turn scheduling, and state changes.
 */

import type {
  Combatant as CombatantState,
  AttackDefinition,
  SkillDefinition,
  Position,
} from '../data/types.js';
import type { AttackResult } from '../simulation/combat-resolver.js';

// =============================================================================
// Types
// =============================================================================

export interface AIDecisionInfo {
  action: 'attack' | 'skill' | 'move' | 'wait';
  reasoning: string;
  target?: string;
  option?: string;
}

export interface AttackLogInfo {
  result: AttackResult;
  attackMod: number;
  defenseMod: number;
  weaponDice: string;
  damageRolled: number;
  damageBonus: number;
  armor: number;
}

export interface SkillLogInfo {
  skillName: string;
  targetName: string;
  wpCost: number;
  wpRemaining: number;
  damage: number;
  damageType: string;
  modifier: string;
}

// =============================================================================
// Verbose Logger Class
// =============================================================================

export class VerboseLogger {
  private _fightNumber: number = 0;
  private _currentTurn: number = 0;
  private _turnHeaderPrinted: boolean = false;

  /**
   * Start a new fight.
   */
  startFight(): void {
    this._fightNumber++;
    this._currentTurn = 0;
    console.log(`\n${'='.repeat(60)}`);
    console.log(`=== FIGHT ${this._fightNumber} ===`);
    console.log('='.repeat(60));
  }

  /**
   * Log combat initialization with all combatants.
   */
  logCombatStart(combatants: CombatantState[]): void {
    console.log('\n-- Combat Start --');

    const teamA = combatants.filter((c) => c.team === 'A');
    const teamB = combatants.filter((c) => c.team === 'B');

    console.log('\nTeam A:');
    for (const c of teamA) {
      this._logCombatantStats(c);
    }

    console.log('\nTeam B:');
    for (const c of teamB) {
      this._logCombatantStats(c);
    }
    console.log('');
  }

  private _logCombatantStats(c: CombatantState): void {
    const attacks = c.attacks.map((a) => `${a.name} (${a.dice} ${a.damageType})`).join(', ');
    const skills = c.skills.length > 0 ? c.skills.map((s) => s.name).join(', ') : 'none';

    console.log(`  ${c.name}`);
    console.log(`    HP: ${c.currentHealth}/${c.maxHealth}, WP: ${c.currentWillpower}/${c.maxWillpower}`);
    console.log(`    STR: ${c.strength}, AGI: ${c.agility}, END: ${c.endurance}, WIL: ${c.will}`);
    console.log(`    Speed: ${c.speed}, Armor: ${c.armor}, Evasion: ${c.evasion}`);
    console.log(`    Position: (${c.position.x}, ${c.position.y})`);
    console.log(`    Attacks: ${attacks || 'none'}`);
    if (c.skills.length > 0) {
      console.log(`    Skills: ${skills}`);
    }
    if (c.immunities.size > 0) {
      console.log(`    Immunities: ${[...c.immunities].join(', ')}`);
    }
    if (c.resistances.size > 0) {
      console.log(`    Resistances: ${[...c.resistances].join(', ')}`);
    }
    if (c.vulnerabilities.size > 0) {
      console.log(`    Vulnerabilities: ${[...c.vulnerabilities].join(', ')}`);
    }
  }

  /**
   * Log when a new turn begins.
   */
  logTurnStart(turn: number): void {
    if (turn !== this._currentTurn) {
      this._currentTurn = turn;
      this._turnHeaderPrinted = false;
    }
  }

  /**
   * Ensure turn header is printed (lazy - only when something happens).
   */
  private _ensureTurnHeader(): void {
    if (!this._turnHeaderPrinted) {
      console.log(`\n-- Turn ${this._currentTurn} --`);
      this._turnHeaderPrinted = true;
    }
  }

  /**
   * Log when a combatant becomes ready to act.
   */
  logActorReady(actor: CombatantState, accumulatedTime: number, delay: number): void {
    this._ensureTurnHeader();
    console.log(`\n[${actor.name}] Ready (accTime: ${accumulatedTime.toFixed(0)}, delay: ${delay.toFixed(0)})`);
    console.log(`  HP: ${actor.currentHealth}/${actor.maxHealth}, WP: ${actor.currentWillpower}/${actor.maxWillpower}`);
  }

  /**
   * Log AI decision with reasoning.
   */
  logAIDecision(info: AIDecisionInfo): void {
    let msg = `  AI: ${info.reasoning}`;
    console.log(msg);
  }

  /**
   * Log a full attack with dice breakdown.
   */
  logAttack(info: AttackLogInfo): void {
    const { result, attackMod, defenseMod, weaponDice, damageRolled, damageBonus, armor } = info;

    console.log(`  Attack: ${result.attackName} vs ${result.targetName}`);

    // Attack roll breakdown
    const attackModSign = attackMod >= 0 ? `+${attackMod}` : `${attackMod}`;
    const defenseModSign = defenseMod >= 0 ? `+${defenseMod}` : `${defenseMod}`;
    const hitStr = result.hit ? 'HIT' : 'MISS';
    console.log(`    Roll: 2d6${attackModSign} = ${result.attackRoll} vs 2d6${defenseModSign} = ${result.defenseRoll} → ${hitStr}`);

    if (result.hit) {
      // Damage breakdown
      const bonusStr = damageBonus !== 0 ? ` + ${damageBonus} (STR)` : '';
      const armorStr = armor !== 0 ? ` - ${armor} (armor)` : '';
      let damageCalc = `${weaponDice} rolled ${damageRolled}${bonusStr}${armorStr} = ${result.damageBeforeModifiers}`;

      if (result.modifier !== 'none') {
        damageCalc += ` → ${result.damage} (${result.modifier})`;
      }

      console.log(`    Damage: ${damageCalc} ${result.damageType}`);
    }
  }

  /**
   * Log damage applied to target.
   */
  logDamageApplied(targetName: string, damage: number, oldHp: number, newHp: number, maxHp: number, died: boolean): void {
    let msg = `    → ${targetName} takes ${damage} damage (${oldHp} → ${newHp}/${maxHp} HP)`;
    if (died) {
      msg += ' - DIES!';
    }
    console.log(msg);
  }

  /**
   * Log skill usage.
   */
  logSkill(info: SkillLogInfo): void {
    console.log(`  Skill: ${info.skillName} vs ${info.targetName} (${info.wpCost} WP, ${info.wpRemaining} remaining)`);

    if (info.damage > 0) {
      let damageStr = `${info.damage} ${info.damageType}`;
      if (info.modifier !== 'none') {
        damageStr += ` (${info.modifier})`;
      }
      console.log(`    Damage: ${damageStr}`);
    } else {
      console.log(`    (no damage)`);
    }
  }

  /**
   * Log movement.
   */
  logMovement(actorName: string, from: Position, to: Position): void {
    console.log(`  → Moves from (${from.x}, ${from.y}) to (${to.x}, ${to.y})`);
  }

  /**
   * Log wait action.
   */
  logWait(actorName: string): void {
    console.log(`  → Waits`);
  }

  /**
   * Log HP regeneration.
   */
  logRegeneration(actorName: string, healed: number, current: number, max: number): void {
    console.log(`  → Regenerates ${healed} HP (${current}/${max})`);
  }

  /**
   * Log WP regeneration.
   */
  logWillpowerRegeneration(actorName: string, restored: number, current: number, max: number): void {
    console.log(`  → Restores ${restored} WP (${current}/${max})`);
  }

  /**
   * Log action cost/delay.
   */
  logActionCost(actorName: string, actionCost: number, speed: number, weaponDelay?: number): void {
    let msg = `  Action cost: ${actionCost.toFixed(0)} (speed ${speed}`;
    if (weaponDelay !== undefined && weaponDelay !== 1.0) {
      msg += `, weapon delay ${weaponDelay}x`;
    }
    msg += ')';
    console.log(msg);
  }

  /**
   * Log combat end.
   */
  logCombatEnd(winner: 'A' | 'B' | 'draw', turns: number, combatants: CombatantState[]): void {
    console.log(`\n-- Combat End --`);

    if (winner === 'draw') {
      console.log(`Result: DRAW (max turns reached)`);
    } else {
      console.log(`Winner: Team ${winner}`);
    }
    console.log(`Turns: ${turns}`);

    const survivors = combatants.filter((c) => c.currentHealth > 0);
    if (survivors.length > 0) {
      console.log('Survivors:');
      for (const s of survivors) {
        console.log(`  ${s.name}: ${s.currentHealth}/${s.maxHealth} HP`);
      }
    }
  }

  /**
   * Print a separator between fights.
   */
  printFightSeparator(): void {
    console.log('\n' + '-'.repeat(60));
  }
}

/**
 * Create a new verbose logger instance.
 */
export function createVerboseLogger(): VerboseLogger {
  return new VerboseLogger();
}
