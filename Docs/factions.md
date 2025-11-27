# Faction System

The faction system determines allegiance and combat targeting between entities. Factions control who can attack whom and enable friendly creatures for summoning, rescued prisoners, and allied NPCs.

## Factions

**Hostile**: Default faction for enemies. Hostile to the player and Friendly entities. Hostile entities don't attack each other.

**Friendly**: Allied with the player. Cannot attack or be attacked by other Friendly entities (including the player). Hostile to Hostile entities.

**Neutral**: Non-combatant entities. Won't attack anyone and won't be targeted by faction-based AI.

## Architecture

### Faction Property

`BaseEntity.Faction` property stores the entity's allegiance. Defaults to `Neutral` for non-combatants (items, decorations, features). Creatures get their faction from YAML data (defaulting to `Hostile`). Player is always `Player` faction.

### Faction Extension Methods

`FactionExtensions` (in BaseEntity.cs) provides relationship helpers:

- `IsHostileTo(Faction)`: Returns true if factions are enemies (Hostile↔Friendly)
- `IsFriendlyTo(Faction)`: Returns true if factions are allies (same faction)

### Combat Integration

`CombatSystem.OnAttackRequested()` checks faction compatibility before processing attacks. Attacks between entities with `IsFriendlyTo()` returning true are silently blocked (no friendly fire).

### AI Integration

`AIContext` provides faction-aware helpers for goal evaluation:

- `GetVisibleEnemies()`: Returns entities hostile to this entity within vision and LOS
- `GetEnemiesNearProtectionTarget(maxDistance)`: Returns hostile entities near the protection target
- `GetClosestEnemy(enemies)`: Utility to find nearest enemy from a list

## Friendly AI Behavior

Friendly creatures use `BoredGoal` like all other creatures. Their behavior emerges from:

1. **Combat check**: BoredGoal attacks visible enemies first (same as hostiles)
2. **FollowLeaderComponent**: Responds to `OnIAmBored`, pushes `FollowEntityGoal` when too far from protection target

**FollowEntityGoal**: Unified follow goal used by both bodyguards and pack followers. Takes target entity and follow distance as parameters. Aborts when enemies visible.

The `AIComponent.ProtectionTarget` determines who to follow. `FollowLeaderComponent.FollowDistance` determines how close to stay.

## Configuration

### YAML Creature Data

```yaml
name: allied_warrior
faction: Friendly
visionRange: 10
hasMovement: true
hasAI: true
ai:
  - type: FollowLeader
    followDistance: 3
```

## Creating Friendly Creatures

### From YAML (Pre-defined Friendly)

Set `faction: Friendly` in creature YAML. ProtectionTarget must be assigned at runtime.

### Runtime Conversion (Rescue/Summon)

Use `EntityFactory.SetupAsFriendlyCompanion(entity, protectionTarget)`:

```csharp
// Summon a creature and make it protect the player
var creature = entityFactory.CreateCreature("skeleton_warrior", position);
entityFactory.SetupAsFriendlyCompanion(creature, player);
entityManager.AddEntity(creature);
```

This sets `Faction = Friendly` and assigns `AIComponent.ProtectionTarget`.

### Converting Existing Creatures

For rescue scenarios (e.g., freeing a prisoner):

```csharp
// Convert hostile/neutral creature to friendly
prisoner.Faction = Faction.Friendly;
var ai = prisoner.GetNodeOrNull<AIComponent>("AIComponent");
if (ai != null)
{
    ai.ProtectionTarget = player;
}
```

## Use Cases

**Summoning**: Create creature via EntityFactory, call SetupAsFriendlyCompanion, add to EntityManager.

**Rescued Prisoners**: Find prisoner entity, set Faction to Friendly, assign ProtectionTarget to player.

**Temporary Allies**: Same as summoning, but may want to track duration and revert or remove later.

**Faction-Switching**: Directly modify `entity.Faction` property. Update ProtectionTarget as needed.

## See Also

- [ai.md](ai.md) - AI system and goal architecture
- [combat.md](combat.md) - Combat system integration
- [entities.md](entities.md) - Entity architecture
