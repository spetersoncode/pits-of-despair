using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Generation;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Generators;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages the game map, including tile data and queries.
/// </summary>
public partial class MapSystem : Node
{
    [Export] public int MapWidth { get; set; } = 100;
    [Export] public int MapHeight { get; set; } = 100;

    /// <summary>
    /// Configuration for BSP dungeon generation.
    /// Create a BSPConfig resource to customize generation parameters.
    /// </summary>
    [Export] public BSPConfig BSPConfig { get; set; }

    [Signal]
    public delegate void MapChangedEventHandler();

    [Signal]
    public delegate void GenerationCompleteEventHandler();

    private TileType[,] _grid;
    private DungeonMetadata _metadata;
    private DataLoader _dataLoader;
    private PipelineConfig _currentPipelineConfig;
    private FloorConfig _currentFloorConfig;

    /// <summary>
    /// Metadata from the last generation (regions, passages, chokepoints, etc.)
    /// </summary>
    public DungeonMetadata Metadata => _metadata;

    /// <summary>
    /// The pipeline config used for the current map generation.
    /// Used by spawning system to access layout-dependent spawn settings.
    /// </summary>
    public PipelineConfig CurrentPipelineConfig => _currentPipelineConfig;

    /// <summary>
    /// The floor config used for the current map generation.
    /// Used by spawning system to access difficulty/content settings.
    /// </summary>
    public FloorConfig CurrentFloorConfig => _currentFloorConfig;

    public override void _Ready()
    {
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");

        // Create default config if none is set
        if (BSPConfig == null)
        {
            BSPConfig = new BSPConfig();
        }

        // Note: Map generation is triggered by GameLevel.InitializeMap() with the correct floor depth
        // Do NOT auto-generate here - that would always use floor 1
    }

    /// <summary>
    /// Generates a new map using the BSP dungeon generator directly.
    /// Prefer GenerateFromPipeline() or GenerateForFloor() for data-driven generation.
    /// </summary>
    public void GenerateMap()
    {
        // Create BSP generator instance
        var generator = DungeonGeneratorFactory.CreateBSP(BSPConfig) as BSPDungeonGenerator;

        // Generate the map
        _grid = generator.Generate(MapWidth, MapHeight);
        _metadata = null; // Legacy generation doesn't produce metadata
        _currentPipelineConfig = null;
        _currentFloorConfig = null;

        EmitSignal(SignalName.MapChanged);
    }

    /// <summary>
    /// Generate dungeon from a pipeline configuration by ID.
    /// </summary>
    /// <param name="pipelineId">ID of the pipeline config (filename without extension).</param>
    public void GenerateFromPipeline(string pipelineId)
    {
        var pipelineConfig = _dataLoader.Pipelines.Get(pipelineId);
        if (pipelineConfig == null)
        {
            GD.PushError($"MapSystem: Pipeline config '{pipelineId}' not found, falling back to BSP generation");
            GenerateMap();
            return;
        }

        GenerateFromPipeline(pipelineConfig, null);
    }

    /// <summary>
    /// Generate dungeon from a PipelineConfig object.
    /// </summary>
    /// <param name="pipelineConfig">Pipeline configuration.</param>
    /// <param name="floorConfig">Optional floor configuration for spawning context.</param>
    public void GenerateFromPipeline(PipelineConfig pipelineConfig, FloorConfig floorConfig = null)
    {
        GD.Print($"[MapSystem] Generating from pipeline: {pipelineConfig.Name}");

        _currentPipelineConfig = pipelineConfig;
        _currentFloorConfig = floorConfig;

        var pipeline = GenerationPipeline.FromConfig(pipelineConfig);
        var result = pipeline.Execute(pipelineConfig);

        _grid = result.Grid;
        _metadata = result.Metadata;
        MapWidth = result.Width;
        MapHeight = result.Height;

        GD.Print($"[MapSystem] Generation complete: {result.PassesExecuted.Count} passes, {result.GetWalkablePercent():F1}% walkable");

        EmitSignal(SignalName.MapChanged);
        EmitSignal(SignalName.GenerationComplete);
    }

    /// <summary>
    /// Generate dungeon for a specific floor depth.
    /// Automatically selects the appropriate floor and pipeline configs.
    /// </summary>
    /// <param name="floorDepth">The floor depth (1-based).</param>
    public void GenerateForFloor(int floorDepth)
    {
        var floorConfig = _dataLoader.FloorConfigs.GetForDepth(floorDepth);
        if (floorConfig == null)
        {
            GD.PushWarning($"MapSystem: No floor config for depth {floorDepth}, falling back to BSP generation");
            GenerateMap();
            return;
        }

        // Select pipeline from floor config
        var rng = new System.Random();
        var pipelineId = floorConfig.SelectPipeline(rng);

        var pipelineConfig = _dataLoader.Pipelines.Get(pipelineId);
        if (pipelineConfig == null)
        {
            GD.PushWarning($"MapSystem: Pipeline '{pipelineId}' not found for floor {floorDepth}, falling back to BSP generation");
            GenerateMap();
            return;
        }

        GenerateFromPipeline(pipelineConfig, floorConfig);
    }

    // Metadata query methods

    /// <summary>
    /// Get the region containing the given position (if metadata available).
    /// </summary>
    public Region GetRegionAt(GridPosition pos) => _metadata?.GetRegionAt(pos);

    /// <summary>
    /// Get all detected chokepoints (if metadata available).
    /// </summary>
    public System.Collections.Generic.List<Chokepoint> GetChokepoints() => _metadata?.Chokepoints;

    /// <summary>
    /// Get the distance field from entrance (if metadata available).
    /// </summary>
    public DistanceField GetDistanceFromEntrance() => _metadata?.EntranceDistance;

    /// <summary>
    /// Get tile classification at position (if metadata available).
    /// </summary>
    public TileClassification GetTileClassification(GridPosition pos) =>
        _metadata?.GetClassification(pos) ?? TileClassification.Wall;

    /// <summary>
    /// Gets the tile type at the specified grid position.
    /// </summary>
    public TileType GetTileAt(GridPosition pos)
    {
        if (!IsInBounds(pos))
        {
            return TileType.Wall; // Out of bounds treated as wall
        }

        return _grid[pos.X, pos.Y];
    }

    /// <summary>
    /// Checks if a grid position is walkable.
    /// </summary>
    public bool IsWalkable(GridPosition pos)
    {
        if (!IsInBounds(pos))
        {
            return false;
        }

        return _grid[pos.X, pos.Y] == TileType.Floor;
    }

    /// <summary>
    /// Sets the tile type at the specified position.
    /// </summary>
    public void SetTile(GridPosition pos, TileType tileType)
    {
        if (!IsInBounds(pos))
        {
            return;
        }

        _grid[pos.X, pos.Y] = tileType;
        EmitSignal(SignalName.MapChanged);
    }

    /// <summary>
    /// Checks if a grid position is within map bounds.
    /// </summary>
    public bool IsInBounds(GridPosition pos)
    {
        return pos.X >= 0 && pos.X < MapWidth &&
               pos.Y >= 0 && pos.Y < MapHeight;
    }

    /// <summary>
    /// Checks if a grid position is on the boundary edge of the map.
    /// Boundary positions should not be dug through to prevent breaking dungeon containment.
    /// </summary>
    public bool IsOnBoundary(GridPosition pos)
    {
        return pos.X <= 0 || pos.X >= MapWidth - 1 ||
               pos.Y <= 0 || pos.Y >= MapHeight - 1;
    }

    /// <summary>
    /// Checks if a wall at this position is safe to dig/tunnel through.
    /// Returns false for boundary walls (map edges) to prevent breaking dungeon containment.
    /// </summary>
    public bool IsSafeToDig(GridPosition pos)
    {
        // Must be in bounds
        if (!IsInBounds(pos))
            return false;

        // Cannot dig boundary walls
        if (IsOnBoundary(pos))
            return false;

        // Must be a wall to dig
        return GetTileAt(pos) == TileType.Wall;
    }

    /// <summary>
    /// Finds a valid spawn position (walkable tile) on the map.
    /// Searches from the center outward in a spiral pattern.
    /// </summary>
    public GridPosition GetValidSpawnPosition()
    {
        // Try center first
        var center = new GridPosition(MapWidth / 2, MapHeight / 2);
        if (IsWalkable(center))
        {
            return center;
        }

        // Spiral outward from center to find a walkable tile
        for (int radius = 1; radius < Mathf.Max(MapWidth, MapHeight) / 2; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Only check tiles on the edge of the current radius
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    var pos = new GridPosition(center.X + x, center.Y + y);
                    if (IsWalkable(pos))
                    {
                        return pos;
                    }
                }
            }
        }

        // Fallback: scan entire map (should never happen with valid dungeons)
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                var pos = new GridPosition(x, y);
                if (IsWalkable(pos))
                {
                    return pos;
                }
            }
        }

        // Last resort: return center even if not walkable
        return center;
    }

    /// <summary>
    /// Gets the character glyph for a tile type.
    /// </summary>
    public char GetGlyphForTile(TileType tileType)
    {
        return tileType switch
        {
            TileType.Floor => '.',
            TileType.Wall => '#',
            _ => '?'
        };
    }

    /// <summary>
    /// Gets the color for a tile type.
    /// </summary>
    public Color GetColorForTile(TileType tileType)
    {
        return tileType switch
        {
            TileType.Floor => Palette.Basalt,
            TileType.Wall => Palette.Default,
            _ => Palette.Default
        };
    }

    /// <summary>
    /// Gets all walkable tiles on the map without room segmentation.
    /// </summary>
    /// <returns>List of all walkable tile positions.</returns>
    public System.Collections.Generic.List<GridPosition> GetAllWalkableTiles()
    {
        var walkableTiles = new System.Collections.Generic.List<GridPosition>();

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                var pos = new GridPosition(x, y);
                if (IsWalkable(pos))
                {
                    walkableTiles.Add(pos);
                }
            }
        }

        return walkableTiles;
    }

    /// <summary>
    /// Checks if an NxN area centered at position is completely walkable.
    /// </summary>
    /// <param name="center">Center position of the area</param>
    /// <param name="size">Size of the square area (e.g., 3 for 3x3)</param>
    /// <returns>True if all tiles in the area are walkable</returns>
    public bool IsAreaClear(GridPosition center, int size)
    {
        int halfSize = size / 2;

        for (int x = center.X - halfSize; x <= center.X + halfSize; x++)
        {
            for (int y = center.Y - halfSize; y <= center.Y + halfSize; y++)
            {
                var pos = new GridPosition(x, y);
                if (!IsWalkable(pos))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Finds contiguous walkable tiles around an anchor position using flood fill.
    /// </summary>
    /// <param name="anchor">Starting position</param>
    /// <param name="maxTiles">Maximum number of tiles to find</param>
    /// <returns>List of connected walkable positions</returns>
    public System.Collections.Generic.List<GridPosition> FindContiguousArea(GridPosition anchor, int maxTiles)
    {
        if (!IsWalkable(anchor))
        {
            return new System.Collections.Generic.List<GridPosition>();
        }

        var result = new System.Collections.Generic.List<GridPosition>();
        var visited = new System.Collections.Generic.HashSet<GridPosition>();
        var queue = new System.Collections.Generic.Queue<GridPosition>();

        queue.Enqueue(anchor);
        visited.Add(anchor);

        while (queue.Count > 0 && result.Count < maxTiles)
        {
            var current = queue.Dequeue();
            result.Add(current);

            // Check 4-directional neighbors
            var neighbors = new[]
            {
                new GridPosition(current.X + 1, current.Y),
                new GridPosition(current.X - 1, current.Y),
                new GridPosition(current.X, current.Y + 1),
                new GridPosition(current.X, current.Y - 1)
            };

            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && IsWalkable(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Finds suitable spawn locations with minimum required space.
    /// </summary>
    /// <param name="requiredSize">Minimum NxN area size needed</param>
    /// <param name="count">Maximum number of locations to find</param>
    /// <param name="minSpacing">Minimum distance between returned locations (0 for no spacing)</param>
    /// <returns>List of suitable spawn positions</returns>
    public System.Collections.Generic.List<GridPosition> FindSuitableSpawnLocations(int requiredSize, int count, int minSpacing = 0)
    {
        var locations = new System.Collections.Generic.List<GridPosition>();
        var walkableTiles = GetAllWalkableTiles();

        // Shuffle to get random distribution
        var random = new System.Random();
        for (int i = walkableTiles.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (walkableTiles[i], walkableTiles[j]) = (walkableTiles[j], walkableTiles[i]);
        }

        foreach (var pos in walkableTiles)
        {
            if (locations.Count >= count)
            {
                break;
            }

            // Check if area is clear
            if (!IsAreaClear(pos, requiredSize))
            {
                continue;
            }

            // Check spacing from other locations
            if (minSpacing > 0)
            {
                bool tooClose = false;
                foreach (var existing in locations)
                {
                    int distance = Mathf.Abs(existing.X - pos.X) + Mathf.Abs(existing.Y - pos.Y);
                    if (distance < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }
            }

            locations.Add(pos);
        }

        return locations;
    }
}
