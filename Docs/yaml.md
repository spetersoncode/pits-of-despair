# YAML Data System

Game data (creatures, items, spawn configs, encounter templates, faction themes) is defined in YAML files under `Data/`, loaded at runtime via DataLoader singleton. Type-based smart defaults minimize verbosity while maintaining clarity.

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

**ID Generation**: DataLoader creates IDs from filenames with intentionally different patterns by data category:
- **Creatures**: Filename-only (`rat.yaml` → `"rat"`, `goblin_scout.yaml` → `"goblin_scout"`)
- **Items**: Type-prefixed (`club.yaml` → `"weapon_club"`, `healing_8.yaml` → `"potion_healing_8"`)
- **Pipeline Configs**: Filename-only (`bsp_standard.yaml` → `"bsp_standard"`)
- **Floor Configs**: Filename-only (`floor_1.yaml` → `"floor_1"`)
- **Encounter Templates**: Filename-only (`lair.yaml` → `"lair"`)
- **Faction Themes**: Filename-only (`goblinoid.yaml` → `"goblinoid"`)

**ID Rationale**: Items use type prefixes because item type drives gameplay mechanics (inventory categorization, equipment slots, usage behavior). Creature type exists only for visual/behavior defaults, not gameplay categorization, so creature IDs remain simple. This split allows `/give weapon_club` to be unambiguous while keeping creature references concise.

**Flexible Loading**: YamlDotNet configured with `IgnoreUnmatchedProperties()` allows schema evolution—new fields can be added without breaking existing files.

## Data Categories

### Creatures

**Required**: name, type, maxHP

**Optional**: glyph (type default), color (type default), visionRange (default: 16), hasMovement (default: true), hasAI (default: true), strength (default: 0), agility (default: 0), endurance (default: 0), will (default: 0), goals (AI), attacks, equipment, immunities, resistances, vulnerabilities

**Type Defaults**: Each creature type ("rodents", "goblinoid", "undead", etc.) defines default glyph and color applied if not specified.

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

### Pipeline Configs

Pipeline configs in `Data/Pipelines/*.yaml` define generation passes and layout-dependent spawn settings.

**Required**: name, passes (generation pass list)

**Optional**: description, dimensions (width/height), metadata settings, spawnSettings block

**Spawn Settings** (layout-dependent):
- `itemDensity`: Items as % of walkable tiles (e.g., 0.006)
- `encounterChance`: Probability per region (0.0-1.0)
- `maxEncounterRatio`: Max encounters as fraction of regions
- `minEncounterSpacing`: Minimum tiles between encounter centers
- `maxEncountersPerRegion`: Cap per region
- `playerExclusionRadius`: Safe zone around player start
- `encounterWeights`: Weighted list of encounter templates suited to layout

### Floor Configs

Floor configs in `Data/Floors/*.yaml` define difficulty/content for specific floors.

**Required**: name, floor, pipeline

**Optional**: minThreat/maxThreat (creature filtering), themeWeights (faction distribution), creatureOutOfDepthChance, outOfDepthFloors, uniqueCreatures, creatureSelection (scoring parameters)

**Pipeline Selection**: Single pipeline ID or weighted list for random selection:
```yaml
pipeline: bsp_standard  # Single pipeline
# OR
pipelines:  # Weighted random selection
  - id: bsp_standard
    weight: 60
  - id: caves
    weight: 40
```

**Weights**: Theme weights control faction distribution. Higher weight = more likely. Format: `[{id: "goblinoid", weight: 40}, {id: "undead", weight: 30}]`

### Encounter Templates

**Structure**: Template type, budget range, slots defining creature composition, AI configuration.

**Required**: name, type (Lair, Patrol, Ambush, GuardPost, TreasureGuard, Infestation, Pack)

**Optional**: minBudget/maxBudget (encounter cost range), minRegionSize (spatial requirement), slots (creature slot definitions), placement preferences, aiConfig (initial state, territory, wake conditions)

**Slots**: Define creature roles within encounter. Each slot specifies: role (leader, follower, guard), preferredArchetypes (archetype filter), minCount/maxCount (dice notation), threatMultiplier, placement strategy.

**AI Config**: Controls spawned creature behavior—initialState (sleeping for ambush), territoryBound, wakeDistance, leaderBehavior.

### Faction Themes

**Structure**: Creature grouping with visual identity.

**Required**: name, creatures (ID list)

**Optional**: color (territory display), description

## Field Naming Standards

**YAML Fields**: camelCase (`creatureId`, `maxHP`, `armorValue`, `duration`)

**C# Properties**: PascalCase (auto-mapped)

**File Names**: snake_case (`elder_rat.yaml`, `short_sword.yaml`)

**No Aliases Needed**: YamlDotNet's CamelCaseNamingConvention handles mapping automatically. Only use `[YamlMember(Alias = "...")]` when C# naming diverges from standard patterns.

## Dice Notation

**Design Principle**: Support dice notation throughout YAML anywhere a numeric value is needed. Dice notation is inherently optional—fixed numbers work as valid input alongside dice expressions.

String format for random values: `"XdY+Z"` where X = number of dice, Y = die size, Z = modifier.

**Examples**: `"1d6"` (1-6), `"2d4+1"` (3-9), `"1d2"` (1-2), `"10"` (fixed value)

**Unified Fields**: Numeric fields that support dice notation use string type and accept both formats:
- `duration: "3"` - fixed 3 turns
- `duration: "2d4"` - random 2-8 turns
- `charges: "2d3"` - random 2-6 charges
- `dice: "1d6"` - damage dice

**Implementation**: `DiceRoller.Roll(string)` handles both formats. Fixed numbers like `"10"` return 10; dice notation like `"2d6"` rolls and returns the result.

**Always Quote**: Dice strings must be quoted in YAML to prevent parsing as mathematical expressions.

## Comments in Data Files

YAML comments (`#`) document complex structures, explain design decisions, and clarify optional fields. See `Data/Pipelines/bsp_standard.yaml` and `Data/Floors/floor_1.yaml` for commented examples.

## Adding New Types

**Creature Types**: Add entry to CreatureData.TypeInfo dictionary with DefaultGlyph and DefaultColor. Register before loading data.

**Item Types**: Add entry to ItemData.TypeInfo dictionary with Prefix (optional), PluralType, DefaultGlyph, DefaultColor, IsEquippable, IsConsumable, EquipSlot (if equippable). Update `ApplyDefaults()` if new type requires special handling.

**Type Discovery**: No registration required for individual creatures/items—only for new type categories. Files are auto-discovered via recursive directory scanning.

## See Also

- [spawning.md](spawning.md) - Spawning system architecture and YAML formats
- [glyphs.md](glyphs.md) - Glyph and color assignment patterns
