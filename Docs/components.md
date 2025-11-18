# Component Architecture

## Philosophy

**Pits of Despair** uses a component-based entity architecture built on Godot's node system. Entities are lightweight containers that gain behaviors and properties by composing child node components, rather than through inheritance hierarchies.

This approach aligns with Godot's design philosophy: **composition over inheritance**.

## Design Rationale

### Why Components?

**Flexibility**: Entities gain capabilities through child node components. Behaviors mix freely without class hierarchies. New entity types emerge from component combinations.

**Independence**: Self-contained components with minimal dependencies work on any entity. Adding vision requires only adding VisionComponent child node.

**Decoupling**: Components communicate via signals, not direct references. Systems subscribe to signals. Components remain testable and swappable.

**Designer Empowerment**: `[Export]` properties enable non-programmers to create variants through editor configuration.

## Component Categories

Components fall into distinct conceptual categories based on their role:

### State Components
Track entity state and emit change notifications. Calculate derived values (MaxHP from Endurance), emit signals on changes, respond to modifications. Others listen to change signals.

### Behavior Components
Request actions without executing them. Emit "request" signals (move, attack) that systems process and fulfill. Separates intent from execution.

### Capability Components
Mark entities as possessing abilities. Store capability data (vision range, inventory slots), provide interaction interfaces. Presence indicates capability.

### Data Components
Store and manage structured data (goal lists, equipped items). Encapsulate organization, expose query/modify methods, enforce invariants.

## Communication Patterns

### Component → System: Signal Emission

Components emit signals for external coordination: movement requests, attack requests, health changes, stat updates. Systems subscribe and orchestrate responses.

### System → Component: Method Calls

Systems call component methods to query state, trigger effects, or update data. Components provide clean interfaces hiding implementation.

### Component ↔ Component: Sibling Reference

Components on same entity can reference siblings for derived values (HealthComponent reads StatsComponent for MaxHP). Acceptable for reading state; avoid direct modification, prefer signals.

## Multi-Source Modifiers

Components support modifiers from multiple sources (equipment, buffs, effects). Each modifier tracked by unique source identifier enables adding/removing specific modifiers, preventing duplicates, and calculating totals.

Examples: stats (equipment/buffs/debuffs), armor (worn/magical/stances), evasion (armor/status). Source removal cleanly removes modifier.

## Component Composition Patterns

Entities combine components for capabilities: combat (Health, Stats, Attack), autonomy (Movement, Vision, AI), complexity (Inventory, Equipment, Status). See **[entities.md](entities.md)** for specific compositions.

## Benefits

**Rapid Prototyping**: Compose existing components in new configurations. No code for variants.

**Maintainability**: Component changes automatically apply to all entities using it.

**Testing**: Test components in isolation with mock entities.

**Performance**: Godot's node system optimized for this pattern. Lightweight components, efficient signals.

**Extensibility**: Add new behaviors via new components without modifying existing code. Signal integration preserves functionality.

## Design Constraints

**Parent Reference**: Components cache parent entity reference in `_Ready()` for quick access and sibling traversal.

**Initialization Order**: Handle gracefully—siblings may not be ready. Use null-safe accessors.

**Signal Lifecycle**: Disconnect signals in `_ExitTree()` to prevent leaks.

**Export Properties**: Expose tunable parameters via `[Export]` for data-driven, designer-friendly entities.

## Trade-offs

**Indirection**: Signal communication adds indirection vs direct calls. Decoupling benefits outweigh complexity.

**Discovery**: Understanding behavior requires viewing multiple files vs single class. Mitigated by naming and documentation.

**Overhead**: Each component has node lifecycle overhead. Acceptable given Godot optimization. Use entity properties for trivial data.

---

*For entity composition patterns using these components, see **[entities.md](entities.md)**.*
