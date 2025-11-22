# Item System

The **Item System** manages all consumables, equipment, and usable objects in the game. Items exist in two states: ground entities (pickable) and inventory entries (owned by creatures).

## Design Philosophy

**Data-Driven**: Item definitions live in YAML files. Type metadata provides category defaults (glyph, behavior). Effects chain from item data without hard-coded item logic.

**Entity-Agnostic Actions**: All item actions (use, drop, pickup) work identically for players and creatures. UI feedback is player-specific; core logic is universal.

**Type Inheritance**: Item types (potion, scroll, weapon, armor, ammo, ring, wand, staff) provide default properties. Individual items override as needed.

## Core Concepts

### Item Types

**Consumables**: Single-use items destroyed on activation. Potions, scrolls, ammo. Stack in inventory by data ID.

**Charged Items**: Multiple uses with recharge potential. Wands, staves. Track current charges per instance.

**Equipment**: Persistent items occupying equipment slots. Weapons, armor, rings. Provide stat modifiers while equipped.

### Item Lifecycle

**Ground State**: Item exists as world entity with `ItemData` property. Visible on map, can be walked over and picked up.

**Inventory State**: Item stored in `InventoryComponent` as `ItemInstance`. Assigned a key binding (a-z, A-Z). Can be used, equipped, or dropped.

**Transitions**: `PickupAction` moves ground→inventory. `DropItemAction` moves inventory→ground. Both are entity-agnostic.

### Effect System Integration

Items define effects in YAML. On activation, effects apply to target (self for healing, enemy for offensive items). Effect success determines consumption. See **[effects.md](effects.md)**.

### Targeting

**Self-Target Items**: Healing potions, teleport scrolls. No target selection needed. Use `UseItemAction`.

**Targeted Items**: Items requiring entity selection (confusion scrolls). Use `UseTargetedItemAction` with target position.

`RequiresTargeting()` method determines which path. Currently, items with confusion status effect require targeting.

## System Integration

**AI Integration**: AI can use items via `ItemUsageComponent`. Defensive items (healing) used when HP low. Offensive items (confusion) used against visible enemies. `SeekItemGoal` enables opportunistic pickup. `ItemEvaluator` prioritizes item value.

**Combat Integration**: Weapons provide attacks via `AttackData`. Armor provides damage reduction via stat modifiers. Ammo consumed per ranged attack.

**Turn System**: Item usage consumes a turn. Effect processing is free (no additional turn cost for status ticks).

**Inventory Management**: `InventoryComponent` handles storage, stacking, key bindings. `EquipComponent` manages equipment slots and stat modifiers.

## Data Format

Items defined in `Data/Items/*.yaml`. Key properties:

**Identity**: `name`, `type`, `description`, `glyph`, `color`

**Behavior**: `isConsumable`, `isEquippable`, `charges` (dice notation), `rechargeTurns`

**Equipment**: `equipSlot`, `attack` (for weapons), `armorValue`, stat bonuses

**Effects**: List of effect definitions with `type`, `amount`, `duration`, etc.

Type defaults apply when properties omitted. See type definitions for category-specific defaults.

## Adding New Items

**Step 1 - Create YAML Definition**: Add file to `Data/Items/`. Specify type, name, effects. Let type defaults handle common properties.

**Step 2 - Define Effects**: Use existing effect types (heal, apply_status, teleport, blink) or implement new ones. Effects determine item behavior on activation.

**Step 3 - Configure Targeting**: For offensive items requiring target selection, ensure effects use appropriate status types that trigger `RequiresTargeting()`.

**Step 4 - Test Integration**: Verify item appears correctly, activates properly, and AI can use it (if applicable).

## Adding New Item Types

**Step 1 - Add Type Metadata**: Register in `ItemTypeInfo` dictionary with default glyph, color, equippability, slot, and naming pattern.

**Step 2 - Update Logic**: If type requires special handling (new equipment slot, unique activation), update relevant components.

**Step 3 - Document**: Update this documentation with new type behavior.

---

*For effect implementation details, see [effects.md](effects.md). For AI item usage patterns, see [ai.md](ai.md). For action system integration, see [actions.md](actions.md).*
