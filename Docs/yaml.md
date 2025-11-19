# YAML Data System

Game data (creatures, items, bands, spawn tables) is defined in YAML files under `Data/`, loaded at runtime via DataLoader singleton. Type-based smart defaults minimize verbosity while maintaining clarity.

## Design Principles

**Simplicity**: Content creators specify only what differs from type defaults. No boilerplate required.

**Smart Defaults**: Three-tier system applies defaults based on type field:
1. Explicit YAML values (highest priority)
2. Type-based defaults via `ApplyDefaults()` method
3. C# initializers and DataDefaults constants (fallback)

**Consistency**: All YAML uses camelCase field names, mapping to PascalCase C# properties via YamlDotNet's naming convention. Files use snake_case naming.

**YAML Compliance**: Standard YAML 1.2 with 2-space indentation, minimal quoting, proper data types.

## Core Concepts

**Type System**: Each data class defines TypeInfo dictionaries mapping type strings to default configurations. When data loads, `ApplyDefaults()` applies type-specific glyphs, colors, behavior flags, and other properties only if not explicitly set in YAML.

**Palette Integration**: Custom PaletteColorConverter allows semantic color names ("Steel", "Coral") or hex values in YAML, automatically converting to hex strings for runtime use.

**ID Generation**: DataLoader creates IDs from filenames. Standard types use filename only (`rat.yaml` → `rat`). Items prefix with type (`weapon_club.yaml` → `weapon_club`) for categorization.

**Flexible Loading**: YamlDotNet configured with `IgnoreUnmatchedProperties()` allows schema evolution—new fields can be added without breaking existing files.

## Data Categories

### Creatures

**Required**: name, type, maxHP

**Optional**: glyph (type default), color (type default), visionRange (default: 16), hasMovement (default: true), hasAI (default: true), strength (default: 0), agility (default: 0), endurance (default: 0), will (default: 0), goals (AI), attacks, equipment, immunities, resistances, vulnerabilities

**Type Defaults**: Each creature type ("vermin", "goblinoid", etc.) defines default glyph and color applied if not specified.

**Behavioral Defaults**: Most creatures are mobile with AI and 16-tile vision. Override when needed (e.g., `visionRange: 0` for blind creatures, `hasMovement: false` for stationary traps).

### Items

**Required**: name, type

**Optional**: glyph, color, equipSlot, attack (weapons), armorValue (armor), effects (potions/scrolls), charges

**Type-Based Behavior**:
- Weapons: Default glyph "/", color Silver, equipSlot MeleeWeapon
- Armor: Default glyph "[", equipSlot Armor
- Potions: Default glyph "!", name prefix "potion of ", isConsumable true
- Scrolls: Default glyph "♪", name prefix "scroll of ", isConsumable true

**Automatic Properties**: `isConsumable`, `isEquippable`, `equipSlot` auto-set based on type unless explicitly overridden.

### Bands

**Structure**: Leader creature with placement, followers with count dice and positioning.

**Required**: name, leader.creatureId, followers array with creatureId and count.dice per entry

**Optional**: placement (defaults: "center" for leader, "surrounding" for followers), distance.min/max (defaults: 1/2)

**Usage**: Can be defined as standalone files in `Data/Bands/` or inline within spawn tables. External files enable reuse across multiple spawn tables; inline definitions work for level-specific configurations.

### Spawn Tables

**Structure**: Budget dice for creatures/items/gold, weighted pools containing weighted entries.

**Budgets**: Dice notation determines spawn points per level generation (`"2d20+60"`).

**Pools**: Weighted categories (common/uncommon/rare) selected randomly based on weights. Higher weight = more likely selection.

**Entry Types**:
- `single`: One creature
- `multiple`: Small group using count dice
- `band`: Creature band (bandId reference or inline definition)
- `unique`: Spawns once per level maximum

**Defaults**: Entry weight defaults to 1 (equal probability within pool), count defaults to "1", type defaults to "single".

## Field Naming Standards

**YAML Fields**: camelCase (`creatureId`, `maxHP`, `armorValue`, `durationDice`)

**C# Properties**: PascalCase (auto-mapped)

**File Names**: snake_case (`elder_rat.yaml`, `short_sword.yaml`)

**No Aliases Needed**: YamlDotNet's CamelCaseNamingConvention handles mapping automatically. Only use `[YamlMember(Alias = "...")]` when C# naming diverges from standard patterns.

## Dice Notation

String format for random values: `"XdY+Z"` where X = number of dice, Y = die size, Z = modifier.

**Examples**: `"1d6"` (1-6), `"2d4+1"` (3-9), `"1d2"` (1-2)

**Always Quote**: Dice strings must be quoted in YAML to prevent parsing as mathematical expressions.

## Comments in Data Files

YAML comments (`#`) document complex structures, explain design decisions, and clarify optional fields. See `Data/SpawnTables/floor_1.yaml` for comprehensive commenting example showing both inline and end-of-line comment patterns.

## Adding New Types

**Creature Types**: Add entry to CreatureData.TypeInfo dictionary with DefaultGlyph and DefaultColor. Register before loading data.

**Item Types**: Add entry to ItemData.TypeInfo dictionary with Prefix (optional), PluralType, DefaultGlyph, DefaultColor, IsEquippable, IsConsumable, EquipSlot (if equippable). Update `ApplyDefaults()` if new type requires special handling.

**Type Discovery**: No registration required for individual creatures/items—only for new type categories. Files are auto-discovered via recursive directory scanning.

---

*For implementation details, see DataLoader.cs, CreatureData.cs, ItemData.cs, BandData.cs, SpawnTableData.cs. For example data structures, see files in `Data/` subdirectories.*
