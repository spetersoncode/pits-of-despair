# Message System

The MessageSystem handles combat message sequencing, formatting, and combination to ensure messages appear in correct narrative order.

## Problem Solved

Without sequencing, signals fire in execution order which creates confusing log output:

```
The skeleton is vulnerable to bludgeoning damage  <- modifier fires first
skeleton dies!                                     <- death fires second
your magic missile hits the skeleton for 12 damage <- damage message fires last
```

With MessageSystem, messages are buffered per-turn and output in narrative order:

```
Your magic missile hits the skeleton for 12 damage (vulnerable to bludgeoning), killing it!
```

## Architecture

```
[CombatSystem]      ──┐
[HealthComponent]   ──┼──► [MessageSystem] ──► [MessageLog]
[SkillExecutor]     ──┘    (Logic/Sequencing)   (Pure Display)
[TurnManager] ───────────┘
```

**MessageSystem** (`Scripts/Systems/MessageSystem/`):
- Signal connections to CombatSystem, HealthComponent
- Message formatting (weapon colors, damage messages)
- Sequencing and combining same-target messages
- Outputs formatted strings to MessageLog

**MessageLog** (`Scripts/UI/`):
- Pure UI component
- Receives pre-formatted `(message, color)` pairs
- Handles display, scrolling, message coalescing ("x3")
- No knowledge of game systems

## Turn-Based Sequencing

TurnManager controls message sequencing:

1. `BeginSequence()` - Called at turn start, enables buffering
2. Combat events are recorded via signal handlers
3. `EndSequence()` - Called at turn end, flushes combined messages

Messages are flushed at turn transitions:
- Player turn end → flush → creature turns begin
- Creature turns end → flush → player turn begins

## Message Priority

Messages are sorted by priority before output:

| Priority | Type | Example |
|----------|------|---------|
| 0 | Discovery | "you spotted a rat" |
| 10 | ActionDamage | "your magic missile hits for 12 damage" |
| 20 | DamageModifier | "vulnerable to bludgeoning" |
| 30 | StatusEffect | "rat is burning" |
| 40 | Death | "skeleton dies!" |
| 50 | Reward | "you gained 50 XP" |

## Message Combining

Same-target combat events are combined into single sentences:

**Separate events:**
- Damage dealt: 12
- Modifier: vulnerable to bludgeoning
- Target died: true

**Combined output:**
```
Your magic missile hits the skeleton for 12 damage (vulnerable to bludgeoning), killing it!
```

## Signal Handlers

MessageSystem connects to:
- `CombatSystem.AttackHit` → `RecordDamage()`
- `CombatSystem.AttackBlocked` → `RecordBlocked()`
- `CombatSystem.AttackMissed` → `RecordMiss()`
- `CombatSystem.SkillDamageDealt` → `RecordDamage()`
- `HealthComponent.DamageModifierApplied` → `RecordModifier()`
- `HealthComponent.Died` → `RecordDeath()`

## Immediate Fallback

When `IsSequencing == false`, messages are output immediately without buffering. This ensures messages still appear even outside the turn cycle.

## Setup

MessageSystem is wired in GameLevel:

```csharp
_messageSystem = new MessageSystem { Name = "MessageSystem" };
AddChild(_messageSystem);

_turnManager.SetMessageSystem(_messageSystem);

// In GameHUD.Initialize:
_messageSystem.SetMessageLog(_messageLog);
_messageSystem.SetPlayer(player);
_messageSystem.SetEntityManager(entityManager);
_messageSystem.ConnectToCombatSystem(combatSystem);
```

## Adding New Message Types

1. Add priority value to `MessagePriority` enum if needed
2. Add signal handler in MessageSystem
3. Either:
   - Update `CombatMessageData` and `FormatCombinedCombatMessage()` for combinable messages
   - Use `QueueMessage()` for standalone messages
