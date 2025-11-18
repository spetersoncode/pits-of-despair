# Entity System Architecture

## Overview

Entities are the fundamental game objects in **Pits of Despair**—the player, monsters, items, projectiles, and gold. The entity system embraces **data-driven design** where entity definitions live in YAML files and a factory assembles them with appropriate components based on configuration.

This document focuses on entity architecture and creation. For component design patterns, see **[components.md](components.md)**.

## Entity as Minimal Container

Base entity provides only universal properties: grid position, visual representation (glyph and color), walkability, and optional item data. Entities provide structure and identity; behavior comes from components.

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

Creatures are data, not code. Goblins and rats differ in stats/components, not class hierarchy. Enables infinite creature variants through data files without new classes.

## Data-Driven Entity Creation

### Creature Data Structure

YAML definitions specify: visual properties (glyph, color, type), base attributes (stats, HP, vision), capability flags (HasMovement, HasAI), combat (attack patterns), AI configuration (goal types), and starting loadout.

Factory assembles components based on flags and data presence.

### Factory Assembly Pattern

Conditional creation: Stats (always), Movement (`HasMovement` flag), Vision (`VisionRange > 0`), Health (`MaxHP > 0`), Attack (attacks defined or equips weapons), Inventory/Equipment (equipment present), AI (`HasAI` flag).

YAML defines capabilities declaratively; factory handles wiring, signals, and initialization.

### Type System for Variants

Creatures can specify types ("goblinoid", "vermin") providing default visuals. Base type defines defaults (glyph, color), variants specify only differences (stats, equipment, attacks). Enables designer-created creatures without artist intervention.

## Action System

Actions represent discrete turn-consuming activities (move, attack, pickup, equip, use item, wait). The entity system integrates with actions through a unified entry point: `entity.ExecuteAction(action)`.

### Two-Phase Processing

**Validation**: `CanExecute()` checks legality (range, resources, terrain, state).

**Execution**: `Execute()` performs action, emits signals.

Separation enables AI evaluation without side effects, UI previewing, and centralized testable preconditions.

### Entity Integration

Entities invoke actions without implementing logic. Player executes from input, AI from goal evaluation, systems for NPCs. Decouples identity from behavior—goblins and player use identical action code.

## AI System

AI entities have an AI component that stores a list of available goals (Idle, Wander, MeleeAttack, FleeForHelp, SearchLastKnownPosition, ReturnToSpawn). Each turn, the AI system evaluates all goals and selects the highest-scoring one based on current context.

### Data-Driven Behavior

YAML files specify available goals. Examples: simple rat (Wander, MeleeAttack, Idle), cowardly goblin (adds FleeForHelp), guard patrol (adds ReturnToSpawn, SearchLastKnownPosition). Behaviors emerge from goal combinations without code changes.

### Multi-Turn State

AI component tracks: last known player position, turns since seen, pathfinding route, search/flee counters. Goals read/update shared state, enabling multi-turn behaviors (chase → lose sight → search → return to spawn).

## Entity Lifecycle Management

The entity manager provides centralized lifecycle coordination:

### Registration and Tracking

Entity creation flow: factory creates with components → manager adds to scene tree → registers in tracking list → caches position for spatial queries → emits `EntityAdded` signal.

### Automatic Cleanup

Entities don't self-remove. Health emits `Died` → manager receives signal → removes from scene/tracking/cache → emits `EntityRemoved`. Centralizes death handling.

### Position Caching

Manager maintains position-to-entity dictionary for O(1) spatial queries. Cache updates automatically via `PositionChanged` signals. No manual synchronization.

## Scene Composition

**Scene-based**: Player defined in `.tscn` with child component nodes, properties in inspector. For persistent components.

**Code-based**: Dynamic creatures assembled by factory from YAML with conditional components. For data-driven entities.

Hybrid approach balances visual editing convenience with data-driven flexibility.

## Item Entities

Items are minimal entities without components: walkable, carry `ItemInstance` reference (charges, type, properties), auto-collected on player movement, droppable to create new entities. Passive items don't need components—keeps them lightweight.

---

*For component design patterns, see **[components.md](components.md)**.*
