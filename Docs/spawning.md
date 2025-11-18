# Spawning System

The spawning system populates dungeon floors with creatures, items, and gold through a budget-based, data-driven architecture. Inspired by DCSS and NetHack spawning mechanics, the system uses weighted random selection, spatial placement strategies, and monster band formation to create varied, balanced encounters.

## Core Architecture

**Budget-Based Spawning**: Floor-wide spawn budgets replace per-room allocation. Spawn tables define creature, item, and gold budgets using dice notation. Each spawn consumes budget points equal to entity count. Spawning continues until budget exhausted or placement space runs out. This approach works with any map topology—rooms, caves, or open areas.

**Weighted Pool Selection**: Spawn tables organize entries into pools (common, uncommon, rare) with weights determining selection frequency. Two-tier weighted selection—first choose pool, then choose entry from pool. Powerful creatures and rare items placed in low-weight pools for controlled scarcity. Designers tune encounter difficulty through weight adjustments without code changes.

**Strategy-Based Execution**: Spawn entries specify type (single, multiple, band, unique) determining which spawn strategy handles execution. Strategies encapsulate spawning logic—single entities, monster packs with leaders, or unique bosses. Placement strategies handle spatial positioning—random scatter, center placement, surrounding formations. Strategy pattern enables clean separation between "what spawns" and "how it spawns."

**Space Allocation**: Spawner calculates required space for each entry before attempting placement. Single creatures need minimal space. Bands require contiguous areas for formation placement. Occupied positions tracked throughout spawning to prevent overlap. Minimum spacing enforced between spawn groups to avoid clustering. Failed placements retry with different entries—no wasted budget on impossible spawns.

## Data Structure

**Spawn Tables**: Top-level configuration for entire floor defining creature pools, item pools, and spawn budgets. Each table references pools by ID and weight. Budget specified as dice notation allowing natural variation between runs. Tables map to floor depth—early floors use weaker tables, deeper floors introduce stronger creatures and rarer items.

**Spawn Pools**: Weighted collections of spawn entries sharing rarity tier (common, uncommon, rare). Pool weight determines selection frequency relative to other pools. Entries within pool have individual weights for fine-grained probability control. Same creature can appear in multiple pools with different weights creating depth-based progression—rats common on floor 1, uncommon on floor 5.

**Spawn Entries**: Individual spawn definitions specifying creature or item ID, spawn type, count range, placement strategy, and spatial requirements. Entries configure minimum space needed (NxN area), minimum isolation distance from other spawns, and placement preference. Count specified as dice notation enabling variable spawn sizes—1d3+1 produces 2-4 creatures per entry.

**Band Definitions**: Monster pack configurations with leader and follower groups. Leader specifies creature ID and placement (typically center). Followers define creature types, count ranges, placement strategies, and distance ranges from leader. Multiple follower groups enable mixed compositions—orc band with warrior leader, archer followers, and wolf companions. Bands can be inline in spawn entry or external references shared across tables.

## Spawning Flow

**Initialization**: SpawnManager receives dependencies (EntityFactory, EntityManager, MapSystem) and floor depth. Loads appropriate spawn table for depth. Initializes spawn and placement strategy registries. Queries map for all walkable tiles—spawn candidates regardless of room structure.

**Budget Allocation**: SpawnDensityController rolls budgets from spawn table dice notation. Separate budgets for creatures, items, and gold. Budget values vary each run creating different encounter densities. No budget reserved or pre-allocated—spawning opportunistically consumes budget until exhausted.

**Creature Population**: Main spawning loop selects random pool weighted by pool weights. Selects random entry from pool weighted by entry weights. Calculates space requirements based on entry type and count. Searches walkable tiles for suitable location meeting space and spacing constraints. Retrieves spawn strategy for entry type. Strategy executes spawn using placement strategy and available tiles. Budget decrements by entity count. Loop continues until budget depleted or max attempts reached.

**Item and Gold Population**: Similar flow to creatures using item pools and single placement strategy. Items avoid creature-occupied positions. Gold budget divided into random-sized piles (5-15 per pile). Each pile spawned at available position. Gold spawning simplest—no strategies or complex placement.

**Failure Handling**: Entry selection continues until valid spawn succeeds—skips invalid entries, missing creature definitions, impossible placements. Attempt counter prevents infinite loops when space exhausted. Partial budget consumption acceptable—some floors more dense, others sparse. Warning logged if significant budget remains unspent indicating space shortage or configuration issues.

## Spawn Strategies

**Single Strategy**: Spawns individual entities or multiples of same type at random positions. Determines count from entry dice notation. Uses random placement to scatter entities across available tiles. Validates creature or item ID exists in data before spawning. Marks spawned positions as occupied preventing overlap. Handles both creature and item spawning—unified code path reduces duplication.

**Band Strategy**: Spawns monster packs with leader-follower structure. Loads band definition from entry—inline configuration or external reference by ID. Spawns leader first using leader's placement strategy (typically center of available area). For each follower group, rolls count and spawns around leader using follower placement (typically surrounding). Distance ranges control follower proximity—tight formations vs scattered packs. Returns total entity count including leader and all followers. Fails if leader placement impossible—no orphaned followers.

**Unique Strategy**: Spawns boss or named creatures with special placement. Uses center or specific placement strategies rather than random. Future-proofed for duplicate prevention—track spawned uniques across floors. Currently allows re-spawning on different floors. Clear tracking when descending depths. Placement failures logged with warnings since unique spawns critical for progression.

## Placement Strategies

**Random Placement**: Filters occupied positions from available tiles. Randomly selects requested count from unoccupied tiles. No clustering or pattern—pure spatial randomization. Default for most single and multiple spawns. Simple, fast, works with any tile configuration.

**Center Placement**: Finds geometric center of available tile set. Selects center and nearby tiles for multi-entity spawns. Used for leader positioning in bands and unique creatures. Creates focal point for encounters. Falls back to random if center occupied.

**Surrounding Placement**: Places entities in ring around anchor position (leader). Accepts minimum and maximum distance parameters. Searches tiles within distance range filtering occupied positions. Distributes followers evenly when possible. Used by band followers maintaining formation coherence. Gracefully handles partial placement if some ring positions occupied.

**Formation Placement**: Arranges entities in patterns (line, wedge, box). Currently supports line formations. Useful for tactical encounters with positioning significance. Future extensibility for complex formation types. Requires contiguous available space—fails gracefully if space inadequate.

## Density Control

**Budget Calculation**: SpawnDensityController wraps spawn table budget retrieval. Rolls dice notation for creatures, items, and gold. Provides debug information for spawn configuration verification. Simple wrapper around spawn table—future extensibility for difficulty modifiers or player-driven density adjustments.

**Spatial Constraints**: Required space calculation prevents oversized spawns in small areas. Bands need 3x3 minimum for formation. Multiple spawns calculate space from max count (ceil of square root). Spacing requirements keep spawn groups separated (typically 3 tile minimum). Area clearing verification ensures contiguous walkable space exists.

**Attempt Limits**: Maximum attempts prevent infinite loops when budget exceeds available space. Set to budget × 10 allowing reasonable retry iterations. Attempts reset on successful spawn—only consecutive failures count. Early exit if no valid tiles remain. Partial population better than complete failure—some encounters better than none.

## System Integration

**Map System**: Provides all walkable tiles for spawn candidate identification. Area clearing validation for contiguous space requirements. Formation detection for band placement. Topology-agnostic—works with rooms, caves, cellular automata, any walkable tile set.

**Entity Manager**: Registers spawned entities maintaining spatial index. Position occupation queries prevent entity overlap. Handles entity lifecycle—spawned entities integrate immediately with turn system and AI. No special spawn-only entity states—entities fully functional on creation.

**Entity Factory**: Creates creature and item instances from data IDs. Handles unknown IDs with warnings—graceful degradation on missing data. Factory pattern centralizes entity creation—spawner doesn't know entity implementation details. Spawner provides grid positions, factory handles scene instantiation and component setup.

**Data Loader**: Loads spawn tables by ID mapped to floor depth. Retrieves band definitions for external band references. Future extensibility for biome-specific tables or special event spawns. Centralized data access point—spawner doesn't touch file system directly.

## Design Patterns

**Strategy Pattern**: ISpawnStrategy defines execution interface. Concrete strategies (Single, Band, Unique) implement different spawning behaviors. SpawnManager selects strategy based on entry type. IPlacementStrategy similarly encapsulates positioning logic. Runtime selection enables data-driven configuration without code branches.

**Factory Pattern**: EntityFactory creates entities from string IDs. SpawnManager doesn't instantiate entities directly. Strategy creates clear separation between spawn logic and entity creation. Extension point for new entity types—factory handles instantiation details.

**Two-Tier Selection**: Pool selection followed by entry selection provides controlled randomization. Outer tier controls rarity distribution (common vs rare). Inner tier controls specific spawn within rarity. Designers tune probability at both levels independently. Prevents rare pool entries from being too common or common pool entries too uniform.

**Budget Management**: Global budget replaces room-by-room allocation. Flexible distribution—dense areas with multiple spawns, sparse areas untouched. Allows natural variation without artificial uniformity. Works with irregular map layouts where room counts vary significantly.

**Composition Over Inheritance**: Spawn strategies composed with placement strategies. Bands use leader placement strategy + follower placement strategy. No inheritance hierarchy for spawn types. New spawn behaviors created by combining existing strategies in novel ways.

## Extensibility

### Adding New Spawn Strategies

Implement ISpawnStrategy interface with Name property and Execute method. Accept SpawnEntryData, available tiles, and occupied position set. Calculate entity count and positions using placement strategies. Call EntityFactory to create entities, register with EntityManager, add positions to occupied set, and return SpawnResult. Handle failures gracefully with partial or empty results.

**Registration and Configuration**: Register strategy in SpawnManager InitializeStrategies dictionary. Update GetSpawnStrategy enum mapping. Extend SpawnEntryType if adding new type. Add type value to YAML schema only if existing types (single, multiple, band, unique) insufficient.

**Design Considerations**: Work with any map topology. Respect occupied positions. Compose with placement strategies rather than custom positioning. Return meaningful counts for budget consumption. Log warnings for configuration issues.

**Example Extensions**: Scattered placement for items across large areas. Guard formations around map features. Patrol routes along predefined paths. Ambush spawns near chokepoints.

### Adding New Placement Strategies

Placement strategies control spatial positioning requiring implementation and registration only—no data changes.

**Strategy Implementation**: Implement IPlacementStrategy interface with Name property and SelectPositions method. Accept available tiles, count, occupied positions, and optional anchor position. Filter occupied positions from available tiles. Apply spatial algorithm (distance constraints, patterns, geometric calculations). Return list of selected positions—may be fewer than requested count if space limited. Handle empty input gracefully returning empty list.

**Strategy Registration**: Register in SpawnManager InitializeStrategies. Add to _placementStrategies dictionary. Placement strategies referenced by name in YAML—designers specify "random", "center", "surrounding", "formation" directly. New strategies immediately available in spawn and band definitions.

**Parameterization**: Simple strategies (random, center) need no parameters—stateless single instance shared. Complex strategies (surrounding with distance, formation with pattern type) require parameters. Create parameterized instances in spawn/band strategy when needed. SurroundingPlacement example shows distance parameter pattern—construct with follower-specific distances rather than default.

**Design Considerations**: Algorithm should handle irregular tile sets—not assume rectangular areas. Scale gracefully with count—selecting 1 vs 20 entities should both work. Anchor position enables relative placement for bands and formations. Respect occupied positions but don't assume perfect distribution possible. Deterministic when possible for consistent results with same input.

**Example Extensions**: Perimeter placement around room edges. Corner placement in rectangular rooms. Diagonal or cardinal line formations. Spiral patterns outward from anchor. Distance-graduated placement (concentric rings). Cluster placement grouping entities tightly.

### Adding New Spawn Tables

Spawn tables enable floor progression and biome variation requiring only data definition—no code changes.

**Table Definition**: Create YAML file defining creature pools, item pools, and budgets. Specify pool IDs, weights, and entries. Define creature/item IDs, types, weights, counts, and placement preferences. Use dice notation for budgets and counts enabling natural variation. Reference existing band definitions or define bands inline.

**Floor Mapping**: Update SpawnManager _spawnTableIdsByFloor array mapping depths to table IDs. Array index represents floor depth—entry represents table ID. Falls back to last table for deeper floors enabling indefinite progression. Alternative: extend DataLoader with depth-based table selection logic.

**Pool Organization**: Organize entries by rarity tier balancing encounter difficulty. Common pools (weight 60-70) contain standard enemies and items. Uncommon pools (weight 20-30) introduce moderate challenges. Rare pools (weight 5-10) provide dangerous encounters and valuable loot. Out-of-depth spawns in rare pools create risk/reward moments.

**Design Considerations**: Budget ranges should match expected floor size—larger floors need higher budgets. Weight tuning controls encounter variety versus consistency—higher variance creates surprises, lower variance more predictable. Cross-floor progression gradually shifts common pools toward stronger baselines. Test multiple runs verifying budget consumption and spatial distribution.

**Example Applications**: Biome-specific tables for dungeon zones (mines, crypts, sewers). Event tables for special floors (treasure rooms, boss floors). Difficulty variants for player-selected challenge modes. Seasonal or thematic variations for special events.

---

*The spawning system provides DCSS-inspired encounter generation with flexible data-driven configuration and strategy-based execution working with any map topology.*
