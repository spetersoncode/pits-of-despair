# Dungeon Generator

Modular pipeline-based dungeon generation supporting multiple algorithms (BSP, Cellular Automata, Drunkard's Walk, Simple Room Placement). YAML-configurable passes enable complex generation workflows with post-processing for connectivity, validation, and metadata analysis.

## Pipeline Architecture

**Pass-Based Generation**: Generation pipeline executes ordered passes on shared context. Each pass implements `IGenerationPass` interface with priority-based execution. Three roles: Base (primary topology), Modifier (transforms existing), PostProcess (analysis/repair). Pipeline validates exactly one Base pass exists.

**YAML Configuration**: Floor configs define pipeline in `Data/Floors/*.yaml`. Each pass specifies algorithm, priority, and parameters. Configs support floor depth ranges for progression. See `Data/Floors/default.yaml` for reference.

**Generation Context**: Shared state passed through all passes containing grid, metadata, random instance, and inter-pass data. Passes read/write grid and contribute to metadata. Context provides utility methods for bounds checking and tile operations.

**Metadata System**: `DungeonMetadata` captures spatial analysis (regions, passages, chokepoints, distance fields). Algorithm-agnostic—works with any base generator. Enables intelligent spawning and AI behavior.

## Available Passes

### Base Generators

**BSP** (`bsp`): Binary Space Partitioning creates structured room-corridor layouts. Recursively divides space, creates rooms in leaves, connects via L-shaped corridors. Produces predictable, balanced dungeons.

**Cellular Automata** (`cellular_automata`): Cave generation via noise and iteration rules. Can be Base (generate caves) or Modifier (transform existing regions). Produces organic, irregular shapes.

**Drunkard's Walk** (`drunkard_walk`): Random walk tunnel carving. Multiple walkers create branching passages. Optional room creation along paths. Produces winding, organic tunnel networks.

**Simple Room Placement** (`simple_rooms`): Scatter rooms randomly, connect with MST. Less structured than BSP, more chaotic layouts. Supports extra connections for loops.

### Post-Processing Passes

**Connectivity** (`connectivity`): Validates all walkable areas connected. Repairs disconnected islands using MST corridor carving.

**Validation** (`validation`): Read-only constraint checking (walkable %, region count). Warns or throws on validation failure.

**Metadata** (`metadata`): Executes spatial analysis chain (wall distance, tile classification, region detection, passage/chokepoint detection).

**Prefabs** (`prefabs`): Inserts pre-designed rooms into suitable regions. Collects spawn hints for entity placement.

## Legacy Architecture

**Algorithm Abstraction**: `IDungeonGenerator` interface defines generation contract returning `TileType[,]` grid. Factory pattern centralizes instantiation enabling multiple algorithm implementations. MapSystem orchestrates generation lifecycle and exposes query API. Clean separation between generation (tile layout) and spawning (entity placement).

**BSP Implementation**: Recursive binary tree partitioning divides dungeon space into progressively smaller rectangles. Leaf nodes contain single rooms. Internal nodes represent partition boundaries. Post-order traversal connects sibling rooms with L-shaped corridors. Guarantees all rooms reachable through corridor network.

**Generation Pipeline**: Initialize grid with walls. Recursively split space into binary tree partitions. Carve random-sized rooms in leaf partitions. Connect sibling rooms with corridors. Finalize border walls. Emit completion signal. Entire process deterministic when using fixed seed.

**Tile Carving Approach**: Start with solid wall grid. Carve floor tiles for rooms and corridors. Preserves 1-tile border around entire map. Simple two-type system (floor, wall) sufficient for roguelike mechanics. Extensible with additional tile types (water, lava, doors) by extending `TileType` enum.

## Binary Space Partitioning

**Recursive Subdivision**: Root partition spans entire map minus border. Each partition attempts split along longer axis creating balanced tree. Recursion stops when partition below maximum size or depth limit reached. Split position randomly chosen within valid range ensuring children exceed minimum size. Maximum depth safety limit prevents infinite recursion.

**Split Orientation**: Forced split when only one direction splittable (partition too narrow or short). Aspect ratio bias prefers vertical split for wide partitions, horizontal for tall. Random selection when aspect ratios balanced. Prevents degenerate layouts with extremely narrow corridors or rooms.

**Partition Constraints**: Minimum partition size (default 8 tiles) prevents impossibly small rooms. Maximum partition size (default 14 tiles) controls split frequency—larger values create fewer, bigger rooms. Children must accommodate minimum room dimensions plus buffer. Split position ensures both children viable preventing empty partitions.

**Tree Structure**: Binary tree with internal nodes representing splits and leaf nodes containing rooms. Each node stores rectangular boundaries (X, Y, Width, Height). Left and right children created during split. Leaf detection drives room creation and corridor connection logic. Tree depth correlates with room count and layout complexity.

## Room Generation

**Leaf Placement**: Rooms created exclusively in leaf partitions ensuring no overlap. Post-order traversal visits all leaves. One room per leaf maintains clear spatial separation. Partition boundaries naturally enforce spacing between rooms.

**Room Sizing**: Random dimensions between configured minimum and maximum (default 6-12 tiles). Clamped to partition dimensions preventing overflow. Variable sizing creates layout variety. Typically fills 70-90% of partition space leaving buffer for walls.

**Room Positioning**: Random offset within partition boundaries. Calculated to keep entire room inside partition. Tends toward partition center minimizing gaps to edges. Offset variation prevents uniform alignment creating organic appearance.

**Floor Carving**: Iterates room rectangle setting all tiles to `TileType.Floor`. Boundary checks prevent out-of-bounds writes. Simple nested loop over width and height. No wall thickness considerations—partition boundaries naturally create walls between rooms.

## Corridor Connection

**Binary Tree Traversal**: Post-order traversal connects children before processing parent. Each internal node connects one random room from left subtree with one from right subtree. Single connection per node pair maintains minimal spanning tree structure. Guarantees all rooms connected without redundant corridors.

**Random Room Selection**: Recursive descent picks random room from subtree. Leaf nodes return their room directly. Internal nodes randomly select left or right child's room. Uniform distribution across subtree depth. Creates varied connection patterns between generation runs.

**L-Shaped Corridors**: Two-segment paths connecting room centers. Horizontal-then-vertical (50% chance) or vertical-then-horizontal (50% chance). Randomization prevents predictable patterns. Simpler than A* pathfinding while guaranteeing connection. Natural looking connections through right-angle turns.

**Corridor Carving**: Horizontal segments iterate X-axis carving floor tiles at target Y with configurable width. Vertical segments iterate Y-axis carving floor tiles at target X. Width parameter controls corridor thickness (default 1). Centered carving ensures corridors aligned with room centers. Boundary validation prevents edge overflow.

## Configuration System

**BSPConfig Resource**: Godot Resource class exposing generation parameters to designer. Saved in scene or external `.tres` files. Live-editable in editor during development. Parameters control dungeon character without code changes.

**Partition Parameters**: `MinPartitionSize` (default 8) and `MaxPartitionSize` (default 14) control split frequency. Smaller ranges create uniform layouts. Larger ranges increase variation. Must accommodate room size constraints to prevent invalid configurations.

**Room Parameters**: `MinRoomWidth`/`MinRoomHeight` (default 6) and `MaxRoomWidth`/`MaxRoomHeight` (default 12) control room dimensions. Wider ranges increase variety at cost of predictability. Must fit within partition constraints. Directly impact room density and dungeon feel.

**Corridor Parameters**: `CorridorWidth` (default 1) controls passage thickness. Wider corridors feel more open and reduce chokepoints. Narrower corridors emphasize tactical positioning. Future extensibility for variable-width or styled corridors.

**Seed Control**: `Seed` property enables reproducible generation. Value -1 uses current time for random layouts. Fixed positive values generate identical dungeons. Useful for testing, sharing, and debugging specific layouts.

## Map System Integration

**Generation Lifecycle**: MapSystem owns `TileType[,]` grid storage. Instantiates generator via factory. Calls `Generate(width, height)` receiving populated grid. Stores grid for query operations. Emits `MapChanged` signal on completion. Clean separation between generation and storage.

**Query Interface**: `GetTileAt()` retrieves tile type at position. `IsWalkable()` checks floor and boundary validity. `IsInBounds()` validates coordinates. `GetAllWalkableTiles()` returns floor positions for spawning. `FindContiguousArea()` flood-fills connected regions. `IsAreaClear()` validates NxN spaces. Comprehensive API supports all gameplay systems.

**Spawning Integration**: SpawnManager queries walkable tiles post-generation. Places entities on floor positions respecting occupied space. Uses contiguity validation for formation placement. Clear boundary between map topology (generation) and entity population (spawning). See spawning.md for entity placement details.

**Rendering Integration**: `GetGlyphForTile()` maps tile types to ASCII characters (`.` for floor, `#` for wall). `GetColorForTile()` maps to color palette. Rendering layer consumes map data without modifying. Clean read-only access pattern.

## Data Structures

**Grid Representation**: Two-dimensional `TileType[,]` array indexed as `[x, y]`. Fixed dimensions from MapWidth/MapHeight exports. X increases left-to-right, Y increases top-to-bottom. Direct indexing provides O(1) access. Entire grid fits in contiguous memory for cache efficiency.

**GridPosition Abstraction**: Immutable value type encapsulating (X, Y) coordinates. Provides world coordinate conversion, Vector2I interop, directional movement, and equality semantics. Used throughout systems as position parameter avoiding raw integer pairs. Type safety prevents coordinate confusion.

**BSPNode Structure**: Internal binary tree node storing partition bounds and optional room. Left and right children for binary split. `IsLeaf()` predicate for room creation logic. Simple struct with rectangular geometry. Tree discarded after generation—only final grid persists.

**Rectangle Value Type**: Immutable bounds representation with X, Y, Width, Height. Used for rooms and partitions. Simple data holder with no behavior. Could be unified with Godot Rect2I for consistency.

## Seed and Randomness

**Deterministic Generation**: `System.Random` instance seeded from `BSPConfig.GetActualSeed()`. All random decisions (split positions, orientations, room sizes, corridor directions) use same instance. Identical seed produces identical dungeons. Enables reproducible testing and potential seed sharing.

**Random Seed Selection**: Config value -1 generates seed from current millisecond timestamp. Provides different layout each play session. Designers can lock seed for specific test cases. No external random source pollution—generator self-contained.

**Random Decision Points**: Split orientation (aspect ratio vs random). Split position (within valid range). Room dimensions (within min/max). Room offset (within partition space). Corridor direction (H-then-V vs V-then-H). Random room selection from subtree. All decisions feed from single predictable source.

## Validation and Post-Processing

**Implicit Validation**: Boundary checks in room and corridor carving prevent out-of-bounds writes. Partition size constraints prevent invalid splits. Minimum split calculation ensures children viable. Leaf detection prevents rooms in internal nodes. Design prevents invalid states rather than fixing them.

**Connectivity Guarantee**: Binary tree traversal ensures each partition pair connected. Recursive connection propagates through tree creating single connected component. All leaf rooms reachable from any starting room. No isolated areas possible with correct BSP algorithm execution.

**Spawning Validation**: `GetAllWalkableTiles()` identifies valid spawn positions. `FindContiguousArea()` verifies formation placement areas connected. `IsAreaClear()` checks NxN spaces before entity placement. Occupied position tracking prevents entity overlap. Validation happens at spawning time, not generation time.

## Multi-Level Support

**Floor Depth Parameter**: GameLevel exports `FloorDepth` property passed to SpawnManager. Same BSP algorithm generates all floors with identical configuration. Depth variation achieved through spawn tables not generation parameters. Consistent topology across floors with varied populations.

**Progression Model**: Early floors (1-2) use basic spawn tables with weak creatures. Deeper floors progressively introduce stronger enemies through table selection. Generation algorithm unchanged—difficulty scaling entirely spawn-driven. See spawning.md for floor-specific population details.

**Future Extensions**: Biome-specific generators could vary algorithm by floor range (BSP for dungeons, cellular automata for caves). Floor depth could influence generation parameters (bigger rooms deeper). Special floors could use different algorithms (boss arenas, treasure vaults). Current architecture supports these extensions through factory pattern.

## Adding New Passes

### Pass Implementation

1. Create pass class implementing `IGenerationPass`:
   - `Name`: Display name for logging
   - `Priority`: Execution order (lower first)
   - `Role`: PassRole.Base, Modifier, or PostProcess
   - `CanExecute()`: Pre-execution validation
   - `Execute()`: Main generation/modification logic

2. Create config class (optional) parsing PassConfig dictionary to typed properties

3. Register in `GenerationPassFactory.EnsureInitialized()`:
   ```csharp
   Register("my_pass", cfg => new MyGenerationPass(cfg));
   ```

### Pass Guidelines

**Base Generators**: Initialize grid with walls, carve floor tiles, register regions in metadata. Ensure 1-tile border preservation. Handle random seed via `context.Random`.

**Modifiers**: Transform existing topology. Check `CanExecute` for required preconditions (e.g., existing regions). Update affected regions in metadata.

**PostProcess**: Read-only analysis or repair passes. Connectivity repair, validation, metadata computation. Should not require specific base generator.

### YAML Configuration

```yaml
pipeline:
  - pass: my_pass
    priority: 100
    config:
      myParameter: value
      anotherParam: 42
```

Config values accessed via `passConfig.GetConfigValue<T>("key", defaultValue)`.

## Legacy Extensibility

### Adding New Algorithms (Legacy)

For direct `IDungeonGenerator` implementations without pipeline:

**Algorithm Implementation**: Implement `IDungeonGenerator` interface with `Generate(width, height)` method. Initialize `TileType[,]` grid with walls. Apply algorithm-specific logic carving floor tiles. Ensure border preservation (1-tile wall perimeter). Return populated grid. Handle random seed if determinism needed.

**Factory Registration**: Add creation method to `DungeonGeneratorFactory` (e.g., `CreateCellularAutomata(config)`). Instantiate generator with configuration. Return as `IDungeonGenerator` interface.

## See Also

- [spawning.md](spawning.md) - Entity placement after generation
