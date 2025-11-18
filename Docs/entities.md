# Entity System Architecture

## Overview

Entities are the fundamental game objects in **Pits of Despair**—the player, monsters, items, projectiles, and gold. The entity system embraces **data-driven design** where entity definitions live in YAML files and a factory assembles them with appropriate components based on configuration.

This document focuses on entity architecture and creation. For component design patterns, see **[components.md](components.md)**.

## Entity as Minimal Container

The base entity is intentionally minimal—a lightweight container providing only universal properties:
- **Grid position** - Tile-based world location
- **Visual representation** - Glyph (Unicode character) and color for ASCII-style rendering
- **Walkability** - Whether other entities can pass through this tile
- **Item data** - Optional reference for collectible items

**Design rationale:** Entities provide structure and identity. Behavior comes from components.

## No Creature Hierarchies

Traditional roguelikes might have:
```
Entity → Creature → Monster → Goblin
                            → Rat
                  → Player
```

This project has:
```
BaseEntity (minimal container)
  ├── Player (player-specific signals and methods)
  ├── Gold (collectible currency)
  ├── Projectile (animated ranged attacks)
  └── [Dynamic creatures built by factory from YAML]
```

**Rationale:** Creatures are data, not code. A goblin and rat differ in stats and components, not class hierarchy. This eliminates rigid type systems and enables infinite creature variants through data file iteration—no new classes required.

## Data-Driven Entity Creation

### Creature Data Structure

Creature definitions in YAML files specify:
- **Visual properties** - Glyph, color, or creature type
- **Base attributes** - Stats (Strength, Agility, Endurance, Will), HP, vision range
- **Capability flags** - HasMovement, HasAI
- **Combat** - Attack patterns (name, type, dice notation, range)
- **AI configuration** - List of available goal types
- **Starting loadout** - Equipment items to equip on creation

The factory reads these definitions and assembles appropriate components based on flags and data presence.

### Factory Assembly Pattern

**Conditional component creation:**
- Stats component → Always for creatures
- Movement component → If `HasMovement` flag set
- Vision component → If `VisionRange > 0`
- Health component → If `MaxHP > 0`
- Attack component → If attacks defined OR can equip weapons
- Inventory + Equipment → If equipment list present
- AI component → If `HasAI` flag set

This enables YAML files to define entity capabilities declaratively—the factory handles component wiring, signal connections, and initialization automatically.

### Type System for Variants

Creatures can specify optional types (e.g., "goblinoid", "vermin") that provide default visual properties:
- **Base type** defines defaults (goblin glyph, color)
- **Variants** specify only differences (stats, equipment, attacks)
- Designers create new creatures without artist intervention

**Example:** A "goblin shaman" variant might use the "goblinoid" type for visuals but override stats and attacks, inheriting the default goblin appearance automatically.

## Action System

Actions represent discrete turn-consuming activities (move, attack, pickup, equip, use item, wait). The entity system integrates with actions through a unified entry point: `entity.ExecuteAction(action)`.

### Two-Phase Processing

**Validation phase:** `action.CanExecute()` checks if action is legal (range, resources, terrain, state)
**Execution phase:** `action.Execute()` performs the action, often emitting signals for systems to process

**Why separation matters:**
- AI evaluates action viability without side effects
- Player UI shows valid actions before commitment
- Action preconditions are centralized and testable

### Entity Integration

Entities invoke actions but don't implement action logic:
- Player executes actions from input (keyboard commands)
- AI entities execute actions selected by goal evaluation
- Systems execute actions for NPCs (automatic gold pickup, item use)

This decouples entity identity from entity behavior—a goblin and player use identical action code.

## AI System

AI entities have an AI component that stores a list of available goals (Idle, Wander, MeleeAttack, FleeForHelp, SearchLastKnownPosition, ReturnToSpawn). Each turn, the AI system evaluates all goals and selects the highest-scoring one based on current context.

### Data-Driven Behavior

Creature YAML files specify available goals:
```yaml
Goals:
  - Wander
  - MeleeAttack
  - Idle
```

This enables per-creature behavior customization:
- Simple rat: Wander, MeleeAttack, Idle
- Cowardly goblin: Wander, MeleeAttack, FleeForHelp, Idle
- Guard patrol: ReturnToSpawn, MeleeAttack, SearchLastKnownPosition, Idle

No code changes required—behaviors emerge from goal combinations.

### Multi-Turn State

The AI component tracks persistent state across turns:
- Last known player position
- Turns since player last seen
- Current pathfinding route
- Search/flee duration counters

Goals read and update this shared state, enabling complex behaviors that span multiple turns (chase player → lose sight → search last seen location → give up and return to spawn).

## Entity Lifecycle Management

The entity manager provides centralized lifecycle coordination:

### Registration and Tracking

When entities are created:
1. Factory creates entity with appropriate components
2. Manager adds entity to scene tree
3. Manager registers in tracking list
4. Manager caches position for spatial queries
5. Manager emits `EntityAdded` signal for systems to react

### Automatic Cleanup

Entities don't remove themselves. Instead:
1. Health component emits `Died` signal
2. Manager receives signal (subscribed during registration)
3. Manager removes from scene, tracking list, and position cache
4. Manager emits `EntityRemoved` signal

**Benefit:** Death handling is centralized—entities don't need cleanup logic.

### Position Caching

The manager maintains a dictionary mapping grid positions to entities for O(1) spatial queries:
- "What entity is at this position?"
- "Can I move to this tile?"
- "What creature am I bumping?"

Cache updates automatically—entities emit `PositionChanged` signals, manager updates cache. No manual synchronization required.

## Scene Composition

Entities can have components added two ways:

**Scene-based (Godot editor):**
- Player entity defined in `.tscn` file with child component nodes
- Components visible in scene tree
- Properties configured via inspector
- Use for persistent, always-present components

**Code-based (factory/initialization):**
- Dynamic creatures assembled by factory from YAML
- Conditional component creation based on data flags
- Programmatic configuration from data values
- Use for data-driven, conditional components

The player uses scene composition. Dynamic creatures use code composition. This hybrid approach balances visual editing convenience with data-driven flexibility.

## Item Entities

Items are minimal entities—no components, just data:
- Marked as walkable (entities can move through item tiles)
- Carry `ItemInstance` reference with charges, type, and properties
- Auto-collected when player moves onto their tile
- Can be dropped from inventory, creating new entity at target position

**Design choice:** Items don't need components because they're passive—they don't act, they're acted upon. This keeps item entities lightweight.

## Design Philosophy Summary

**Entities are identities, not implementations.** A goblin isn't defined by its class hierarchy—it's defined by its data file. The factory assembles the goblin from components based on YAML configuration.

**No special cases.** Player, goblins, rats, and items all follow the same entity architecture. Player has unique methods for player-specific signals, but uses standard components for combat, inventory, stats, and health.

**Data drives behavior.** Want a new creature? Write YAML. Want a cowardly variant? Add FleeForHelp goal to YAML. Want an elite with better gear? Specify equipment in YAML.

**Centralized coordination.** Factory creates, manager tracks, systems process. Entities don't coordinate themselves—they emit signals and hold state.

---

*For component design patterns and communication details, see **[components.md](components.md)**.*
