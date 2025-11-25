# Dungeon Generation Metadata - Brainstorming & Exploration

**Status:** Exploratory brainstorming - foundation for future implementation
**Date:** 2025-11-24
**Context:** The spawning system redesign identified a need for dungeon metadata to inform spawn decisions. This document explores algorithm-agnostic approaches to spatial analysis that work with BSP rooms, cellular automata caves, hybrid generators, or any future technique.

---

## Core Problem

The current dungeon generator returns only a `TileType[,]` grid. Downstream systems (spawning, AI patrol routes, item placement) must work with raw tiles, losing valuable structural information that the generator inherently knows during creation.

**What We Lose**:
- Room boundaries (BSP knows rooms; caves don't have "rooms" but have open areas)
- Corridor paths (BSP creates corridors; caves have natural passages)
- Spatial relationships (which areas connect to which)
- Structural features (chokepoints, dead ends, large open spaces)
- Distance metrics (how far is this tile from the entrance/exit)

**The Challenge**: Different algorithms produce fundamentally different structures. BSP creates discrete rooms connected by corridors. Cellular automata creates organic caves with no clear room boundaries. Drunkard's walk creates winding tunnels. A metadata system must work with all of these.

---

## Design Principles

### 1. Algorithm-Agnostic Abstractions

**Principle**: Metadata concepts should describe spatial properties, not algorithm artifacts. "Room" is a BSP concept; "open area" is universal.

**Implications**:
- Use generic terms: "region" not "room", "passage" not "corridor"
- Define by properties: "area with >N contiguous tiles" not "BSP leaf node"
- Post-process analysis works on any tile grid
- Algorithm-specific metadata is optional enrichment, not requirement

### 2. Layered Information

**Principle**: Metadata should be queryable at multiple granularities—from individual tiles to entire floor structure.

**Layers**:
- **Tile-level**: What kind of space is this tile? (open, passage, chokepoint, edge)
- **Region-level**: What area does this tile belong to? Properties of that area?
- **Connection-level**: How do regions connect? What are the paths between them?
- **Floor-level**: Overall structure, distances, strategic positions

### 3. Lazy Computation

**Principle**: Compute metadata on-demand, not eagerly. Not all systems need all metadata.

**Implications**:
- Core generation remains fast (just tile grid)
- Spatial analysis runs only when queried
- Cache results for repeated queries
- Systems request only what they need

### 4. Composable Analysis

**Principle**: Complex spatial understanding built from simple, reusable analysis passes.

**Building Blocks**:
- Flood fill → contiguous regions
- Distance transform → distance from walls/features
- Skeletonization → passage identification
- Connectivity graph → region relationships

---

## Universal Spatial Concepts

These concepts apply regardless of generation algorithm:

### Regions (Open Areas)

**Definition**: Contiguous areas of walkable tiles meeting minimum size threshold.

**Properties**:
- Bounding box
- Tile count (area)
- Centroid position
- Perimeter tiles
- Shape metrics (compactness, elongation)

**Identification Algorithm**:
```
1. Flood fill from unassigned walkable tiles
2. If filled area >= minimum_region_size, mark as region
3. Small areas become "alcoves" or merge with adjacent regions
4. Repeat until all walkable tiles assigned
```

**BSP Mapping**: Rooms map directly to regions. Corridors may form separate narrow regions or merge with connected rooms.

**Cave Mapping**: Large open caverns become regions. Narrow passages between them are passages, not regions.

### Passages (Connections)

**Definition**: Narrow walkable areas connecting regions.

**Properties**:
- Connected region IDs
- Length (tile count)
- Width (narrowest point)
- Path tiles (ordered list)

**Identification Algorithm**:
```
1. Find tiles not assigned to regions (narrow areas)
2. Trace connected narrow tiles
3. Identify which regions each passage touches
4. Calculate passage metrics
```

**Alternative - Skeletonization**:
```
1. Compute medial axis of walkable space
2. Skeleton branches in narrow areas = passages
3. Skeleton nodes in open areas = region centers
```

### Chokepoints

**Definition**: Tiles where passage width is minimal—tactical bottlenecks.

**Properties**:
- Position
- Width (tiles across narrowest dimension)
- Regions connected
- Strategic value score

**Identification Algorithm**:
```
1. For each passage, find narrowest point(s)
2. For region boundaries, find tiles with fewest adjacent floor tiles
3. Score by: how much traffic must pass through here?
```

**Use Cases**:
- Guard placement (defend chokepoint)
- Ambush positions (attack from chokepoint)
- Player tactics (funnel enemies)

### Distance Fields

**Definition**: Per-tile distance to features of interest.

**Types**:
- Distance from nearest wall
- Distance from entrance/spawn point
- Distance from exit/stairs
- Distance from nearest region center

**Computation**: Breadth-first flood fill from feature tiles, recording distance at each step.

**Use Cases**:
- Spawn difficulty scaling (farther from entrance = harder)
- Room "depth" feeling (center of open area vs edge)
- Patrol route generation (follow distance contours)

### Tile Classifications

**Definition**: Categorical labels for individual tiles based on local structure.

**Categories**:
- **Open**: Part of large region, far from walls
- **Edge**: Region tile adjacent to wall
- **Corner**: Edge tile with walls on multiple sides
- **Passage**: Narrow connecting area
- **Chokepoint**: Minimal-width passage point
- **Dead End**: Passage with only one exit
- **Alcove**: Small enclosed space off main area

**Computation**: Local neighborhood analysis (3x3 or 5x5 kernel).

---

## Region Graph

### Concept

Represent floor structure as a graph where nodes are regions and edges are connections (passages or direct adjacency).

```
     [Region A]
        |
    (passage 1)
        |
     [Region B] --- (passage 2) --- [Region C]
        |
    (passage 3)
        |
     [Region D]
```

### Graph Properties

**Node (Region) Properties**:
- Area (tile count)
- Position (centroid)
- Shape metrics
- Content potential (what could spawn here)

**Edge (Connection) Properties**:
- Type (passage, direct adjacency, door)
- Length
- Width (narrowest point)
- Traversal cost

### Graph Queries

**Pathfinding**: Shortest path between regions considering passage widths.

**Reachability**: Which regions accessible from starting region?

**Centrality**: Which regions are most "central" to floor layout?

**Clustering**: Groups of tightly connected regions (wings, sections).

### Use Cases

**Spawn Distribution**: Ensure each region has appropriate content.

**Faction Territories**: Assign connected region clusters to factions.

**Objective Placement**: Put stairs in region with specific graph properties (far from start, single entrance).

**AI Patrol Routes**: Generate routes that traverse multiple regions.

---

## Algorithm-Specific Enrichment

While core metadata is algorithm-agnostic, generators can provide enriched information:

### BSP Enrichment

**Available Data**:
- Explicit room boundaries (leaf nodes)
- Explicit corridors (connection paths)
- Room hierarchy (BSP tree structure)
- Room neighbors (sibling relationships)

**Enrichment**:
- Tag regions as "room" vs "corridor"
- Provide room dimensions directly (no detection needed)
- Include BSP tree depth (useful for difficulty scaling)

### Cellular Automata Enrichment

**Available Data**:
- Iteration count at each tile (how "stable" is this area)
- Birth/death history (organic vs carved feel)

**Enrichment**:
- Tag regions by formation type (natural cavern vs carved passage)
- Identify "unstable" areas (recently changed, could be unstable floors?)

### Prefab/Vault Enrichment

**Available Data**:
- Prefab type and metadata
- Intended room function
- Pre-placed features

**Enrichment**:
- Tag regions with prefab purpose (treasure room, shrine, etc.)
- Include prefab-specific spawn hints
- Mark pre-placed feature locations

### Hybrid Generator Enrichment

**Available Data**:
- Which algorithm generated which area
- Transition zones between algorithms

**Enrichment**:
- Tag regions by source algorithm
- Mark transition areas (natural cave opening into carved dungeon)
- Different spawn rules for different area types

---

## Metadata Interface Design

### Generation Result

```
IDungeonGenerator.Generate() returns:
  - TileType[,] grid (required, current behavior)
  - DungeonMetadata metadata (optional, new)
```

### DungeonMetadata Structure

```
DungeonMetadata:
  - List<Region> regions
  - List<Passage> passages
  - RegionGraph connectivity
  - Dictionary<Vector2I, TileClassification> tileClassifications
  - Dictionary<string, DistanceField> distanceFields
  - AlgorithmMetadata algorithmSpecific (optional)
```

### Lazy Computation Pattern

```
// Metadata computed on first access, then cached
public class DungeonMetadata
{
    private TileType[,] _grid;
    private List<Region> _regions;

    public List<Region> Regions
    {
        get
        {
            if (_regions == null)
                _regions = RegionDetector.FindRegions(_grid);
            return _regions;
        }
    }
}
```

### Query Interface

```
// Tile-level queries
TileClassification GetTileClassification(Vector2I position);
int GetDistanceFromWall(Vector2I position);
int GetDistanceFromEntrance(Vector2I position);
Region GetRegionAt(Vector2I position);

// Region-level queries
List<Region> GetAllRegions();
List<Region> GetRegionsLargerThan(int minArea);
Region GetRegionContaining(Vector2I position);
List<Region> GetConnectedRegions(Region region);

// Passage-level queries
List<Passage> GetPassages();
List<Chokepoint> GetChokepoints();
Passage GetPassageBetween(Region a, Region b);

// Floor-level queries
RegionGraph GetConnectivityGraph();
List<Vector2I> GetStrategicPositions();
float GetFloorOpenness(); // Ratio of open area to passages
```

---

## Spatial Analysis Algorithms

### Region Detection (Flood Fill)

```
Input: TileType[,] grid, int minRegionSize
Output: List<Region>

1. Create visited[,] array, all false
2. Create regions list
3. For each walkable tile (x, y) not visited:
   a. Flood fill from (x, y), collecting all connected walkable tiles
   b. If count >= minRegionSize:
      - Create Region with collected tiles
      - Calculate bounding box, centroid, area
      - Add to regions list
   c. Mark all collected tiles as visited
4. Return regions
```

### Passage Detection (Narrow Area Finding)

```
Input: TileType[,] grid, List<Region> regions, int maxPassageWidth
Output: List<Passage>

1. Create regionMap[,] mapping tiles to region IDs (-1 if unassigned)
2. Find all walkable tiles not in any region (narrow areas)
3. For each narrow tile cluster:
   a. Trace connected narrow tiles
   b. Find which regions this cluster touches
   c. If touches 2+ regions:
      - Create Passage connecting those regions
      - Calculate length, width, path
4. Return passages
```

### Chokepoint Detection

```
Input: TileType[,] grid, List<Passage> passages
Output: List<Chokepoint>

1. For each passage:
   a. Walk along passage path
   b. At each tile, count perpendicular walkable tiles (width)
   c. Find local minima in width
   d. Create Chokepoint at minimum width positions
2. Also check region boundaries:
   a. For each region edge tile
   b. Count adjacent walkable tiles not in region
   c. If count == 1 or 2, potential chokepoint
3. Return chokepoints sorted by strategic value
```

### Distance Field Computation

```
Input: TileType[,] grid, List<Vector2I> sources
Output: int[,] distances

1. Create distances[,] array, all MAX_INT
2. Create queue
3. For each source position:
   a. distances[source] = 0
   b. Add source to queue
4. While queue not empty:
   a. Pop position from queue
   b. For each walkable neighbor:
      c. If distances[neighbor] > distances[position] + 1:
         - distances[neighbor] = distances[position] + 1
         - Add neighbor to queue
5. Return distances
```

### Connectivity Graph Construction

```
Input: List<Region> regions, List<Passage> passages
Output: RegionGraph

1. Create graph with region nodes
2. For each passage:
   a. Add edge between passage.regionA and passage.regionB
   b. Edge weight = passage length or 1/passage width
3. For each pair of adjacent regions (sharing border):
   a. If not already connected by passage:
      b. Add direct adjacency edge with weight 1
4. Return graph
```

---

## Integration with Spawning

### Region-Based Spawn Distribution

```
For each region:
  1. Calculate region "budget share" based on:
     - Area (larger regions get more spawns)
     - Distance from entrance (farther = more dangerous)
     - Graph centrality (central regions = more traffic)
  2. Select spawn entries appropriate to region properties:
     - Large open regions: bands, multiple creatures
     - Small alcoves: single creatures, items
     - Near entrance: weaker creatures
     - Near exit: stronger guardians
  3. Use region centroid/tiles for placement
```

### Passage-Based Placement

```
For passages and chokepoints:
  1. Identify strategic positions:
     - Chokepoints for guards
     - Long passages for patrols
     - Dead ends for ambushes
  2. Assign appropriate creatures:
     - Single tough creature at chokepoint
     - Mobile creature in passage (patrol route)
     - Ambush predator in dead end
```

### Distance-Based Difficulty

```
Using entrance distance field:
  1. Categorize tiles by distance:
     - Near (0-25%): Easy spawns
     - Mid (25-75%): Standard spawns
     - Far (75-100%): Difficult spawns
  2. Weight spawn selection by zone
  3. Ensure exit area has appropriate challenge
```

---

## Integration with AI

### Patrol Route Generation

```
Using region graph and passages:
  1. Select patrol type:
     - Loop: Cycle through connected regions
     - Corridor: Back and forth along passage
     - Guard: Stay near chokepoint
  2. Generate waypoints:
     - Region centroids for room visits
     - Passage midpoints for corridor patrols
     - Chokepoint positions for guards
  3. Assign route to creature at spawn
```

### Territory Assignment

```
Using region graph clustering:
  1. Identify connected region clusters
  2. Assign faction to each cluster
  3. Creatures spawned in cluster get faction
  4. Border regions between clusters = contested
```

### Strategic Position Awareness

```
AI can query metadata for tactical decisions:
  - Flee toward nearest chokepoint (defensive)
  - Avoid chokepoints when pursuing (flanking)
  - Rally at region centroid (grouping)
  - Retreat to dead end (cornered behavior)
```

---

## Algorithm Flexibility Examples

### BSP Dungeon

```
Generation: BSP creates rooms and corridors
Metadata: Rooms → Regions, Corridors → Passages (direct mapping)
Enrichment: Room types, BSP depth, explicit connections
Spawning: Room-based distribution, corridor patrols
```

### Cellular Automata Cave

```
Generation: CA creates organic cave system
Metadata: Large caverns → Regions (flood fill detection)
          Narrow tunnels → Passages (width analysis)
Enrichment: None (algorithm provides only tiles)
Spawning: Cavern-based distribution, tunnel ambushes
```

### Drunkard's Walk Tunnels

```
Generation: Random walk creates winding passages
Metadata: Wide areas → Regions, Narrow areas → Passages
Enrichment: Walk origin points (could be entrances)
Spawning: Spread along tunnel, clusters at wide points
```

### Hybrid: BSP + Cave Wings

```
Generation: BSP core with CA cave branches
Metadata: BSP rooms → tagged regions, Cave areas → organic regions
Enrichment: Source algorithm per region
Spawning: Different rules for dungeon vs cave areas
```

### Prefab Integration

```
Generation: Any algorithm + inserted prefabs
Metadata: Prefabs → specially tagged regions
Enrichment: Prefab type, intended function, pre-placed features
Spawning: Respect prefab spawn hints, don't overwrite pre-placed
```

---

## Implementation Priorities

### Phase 1: Core Detection
1. Region detection via flood fill
2. Basic tile classification (open, edge, narrow)
3. Simple distance field (from walls)
4. Metadata container and lazy computation

### Phase 2: Connectivity
1. Passage detection
2. Chokepoint identification
3. Region connectivity graph
4. Graph queries (path, reachability)

### Phase 3: Spawning Integration
1. Region-based spawn distribution
2. Distance-based difficulty zones
3. Strategic position placement
4. Update spawn strategies to use metadata

### Phase 4: AI Integration
1. Patrol route generation from metadata
2. Territory assignment using graph
3. AI tactical queries
4. Position-based behavior hints

### Phase 5: Algorithm Enrichment
1. BSP-specific metadata export
2. Enrichment interface for future algorithms
3. Hybrid generator support
4. Prefab metadata integration

---

## Open Questions

### Detection Thresholds

**What defines a "region" vs a "passage"?**
- Minimum region area: 16 tiles? 25 tiles? Configurable?
- Maximum passage width: 2 tiles? 3 tiles?
- These affect how cellular automata caves are interpreted

**Should thresholds be algorithm-specific?**
- BSP: Smaller thresholds (rooms are well-defined)
- CA: Larger thresholds (caves are more organic)
- Or: Let algorithm provide hints

### Performance Considerations

**When to compute metadata?**
- During generation (eager, slower generation)
- On first query (lazy, potential frame spike)
- Background thread (async, complexity)

**Caching strategy?**
- Full cache (memory cost)
- LRU cache (complexity)
- Recompute each query (CPU cost)

### Metadata Persistence

**Should metadata be saved with the level?**
- Pro: Faster reload, consistent results
- Con: Save file size, serialization complexity

**Should metadata be deterministic from seed?**
- Pro: Can regenerate instead of saving
- Con: Must preserve exact algorithm version

### Edge Cases

**Disconnected regions?**
- Should be impossible with correct generators
- But: Log warning, handle gracefully
- Spawning: Skip disconnected regions or connect artificially

**Tiny regions?**
- Below threshold: Don't create region
- Options: Merge with nearest, mark as alcove, ignore

**Very narrow floors (all passages)?**
- Entire floor might be "passage" by width rules
- Solution: Relative thresholds based on floor dimensions

---

## Related Documents

- [spawning-system-redesign.md](spawning-system-redesign.md) - Consumer of dungeon metadata
- [dungeon-generator.md](../dungeon-generator.md) - Current generation architecture
- [ai.md](../ai.md) - AI systems that could use spatial awareness

---

*"The dungeon is more than tiles—it's topology, flow, and strategic space. Generation creates the tiles; analysis reveals the structure."*
