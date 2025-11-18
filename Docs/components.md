# Component Architecture

## Philosophy

**Pits of Despair** uses a component-based entity architecture built on Godot's node system. Entities are lightweight containers that gain behaviors and properties by composing child node components, rather than through inheritance hierarchies.

This approach aligns with Godot's design philosophy: **composition over inheritance**.

## Design Rationale

### Why Components?

**Flexibility Through Composition**
- Entities gain capabilities by attaching components as child nodes
- Behaviors can be mixed and matched freely without rigid class hierarchies
- New entity types emerge from combining existing components in different ways

**Independence and Reusability**
- Each component is self-contained with minimal dependencies
- Components work on any entity, from player to goblin to item
- Adding vision to a new creature type requires only adding a VisionComponent child node

**Signal-Based Decoupling**
- Components communicate via Godot signals, not direct references
- Systems subscribe to component signals to coordinate behavior
- Components remain testable and swappable without breaking dependencies

**Designer Empowerment**
- Components expose `[Export]` properties configurable in the Godot editor
- Non-programmers can create entity variants by adjusting component parameters
- Scene composition in the editor naturally maps to component architecture

## Component Categories

Components fall into distinct conceptual categories based on their role:

### State Components
**Purpose**: Track entity state and emit change notifications

Components like health, stats, and status effects manage numerical values and persistent state. They calculate derived values (e.g., MaxHP from Endurance), emit signals when state changes, and respond to modification requests.

**Key Characteristic**: Other components and systems listen to their change signals to react to state updates.

### Behavior Components
**Purpose**: Request actions without executing them

Components like movement and attack emit signals requesting behaviors (move, attack) but don't execute the requests themselves. This separates intent from execution, allowing systems to validate and coordinate actions.

**Key Characteristic**: They emit "request" signals that systems process and fulfill.

### Capability Components
**Purpose**: Mark entities as possessing an ability

Components like vision and inventory indicate that an entity has a particular capability. They store capability-specific data (vision range, inventory slots) and provide interfaces for systems to interact with that capability.

**Key Characteristic**: Their mere presence indicates the entity can perform certain actions.

### Data Components
**Purpose**: Store and manage structured data

Components like AI and equipment hold complex data structures (goal lists, equipped items) and provide accessors for that data. They encapsulate data organization and expose methods to query or modify it safely.

**Key Characteristic**: They manage data lifetime and enforce invariants on that data.

## Communication Patterns

### Component → System: Signal Emission

Components emit signals when they need external coordination:
- Movement requests when an entity wants to move
- Attack requests when initiating combat
- Health change notifications when HP changes
- Stat change notifications when modifiers update

Systems subscribe to these signals and orchestrate responses, often involving multiple components or entities.

### System → Component: Method Calls

Systems call component methods to:
- Query state (GetAttackModifier, IsAlive)
- Trigger effects (TakeDamage, Heal)
- Update data (SetGoals, AddStatus)

Components provide clean interfaces for these interactions, hiding implementation details.

### Component ↔ Component: Sibling Reference

Components on the same entity can reference each other when calculating derived values:
- HealthComponent references StatsComponent to calculate MaxHP from Endurance
- AttackComponent might reference EquipComponent to determine current weapon attacks

**Guideline**: Sibling references are acceptable for reading state within the same entity. Avoid components modifying sibling state directly—prefer signals for coordination.

## Multi-Source Modifiers

Many components support modifiers from multiple sources (equipment, buffs, temporary effects). This pattern allows stacking bonuses and penalties from different origins.

**Key Design**: Each modifier is tracked by a unique source identifier (string key). This enables:
- Adding/removing specific modifiers without affecting others
- Preventing duplicate modifiers from the same source
- Calculating total values as the sum of all active modifiers

**Example Applications**:
- Stats: equipment bonuses, potion buffs, curse debuffs
- Armor: worn armor, magical shields, defensive stances
- Evasion penalties: heavy armor, slow status effects

When a source is removed (unequip item, buff expires), its modifier cleanly disappears without disrupting other modifiers.

## Component Composition Patterns

Entities gain capabilities by combining components. A combat-capable entity typically composes Health, Stats, and Attack components. An autonomous creature adds Movement, Vision, and AI components. Complex entities layer additional components for inventory, equipment, and status effects.

For specific entity types and their component compositions, see **[entities.md](entities.md)**.

## Benefits Realized

**Rapid Prototyping**
Creating new entity types is fast—compose existing components in new configurations and tune parameters. No code required for variants.

**Maintainability**
Changes to a component (like adding armor reduction to damage calculation) automatically apply to all entities with that component.

**Testing**
Components can be tested in isolation. Mock entities with only the components needed for the test scenario.

**Performance**
Godot's node system is optimized for this pattern. Components are lightweight. Signal emissions are efficient.

**Extensibility**
New behaviors extend the system by adding new components, not by modifying existing code. New components integrate via signals without breaking existing functionality.

## Design Constraints

**Parent Reference Pattern**
Components typically cache a reference to their parent entity during `_Ready()`. This provides quick access to the entity and enables traversal to sibling components.

**Initialization Order**
Components must handle initialization gracefully. Sibling components may not be ready when a component's `_Ready()` executes. Use null-safe accessors when referencing siblings during initialization.

**Signal Lifecycle**
Components that subscribe to signals must disconnect them in `_ExitTree()` to prevent memory leaks and errors when nodes are freed.

**Export Properties**
Components expose tunable parameters via `[Export]` attributes. This makes entities data-driven and designer-friendly, reducing hardcoded values.

## Trade-offs

**Indirection**
Signal-based communication adds indirection compared to direct method calls. The benefit of decoupling outweighs the slight complexity increase.

**Discovery**
Understanding entity behavior requires looking at multiple component files rather than a single class. Good naming and documentation mitigate this.

**Overhead**
Each component is a node with lifecycle overhead. For entities with many components, this is acceptable given Godot's optimization. Avoid creating components for trivial properties—use simple properties on the entity instead.

---

*This component architecture provides the foundation for flexible, maintainable entity design in Pits of Despair. By embracing composition and signal-based communication, the system supports rapid iteration and robust gameplay systems.*
