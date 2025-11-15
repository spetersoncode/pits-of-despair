using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation;
using PitsOfDespair.Generation.Generators;

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

    private TileType[,] _grid;

    public override void _Ready()
    {
        // Create default config if none is set
        if (BSPConfig == null)
        {
            BSPConfig = new BSPConfig();
        }

        GenerateMap();
    }

    /// <summary>
    /// Generates a new map using the BSP dungeon generator.
    /// </summary>
    public void GenerateMap()
    {
        // Create BSP generator instance
        var generator = DungeonGeneratorFactory.CreateBSP(BSPConfig);

        // Generate the map
        _grid = generator.Generate(MapWidth, MapHeight);

        EmitSignal(SignalName.MapChanged);
    }

    /// <summary>
    /// Initializes the map with floors and a border of walls.
    /// DEPRECATED: Use GenerateMap() instead.
    /// </summary>
    [System.Obsolete("Use GenerateMap() instead")]
    public void InitializeMap()
    {
        _grid = new TileType[MapWidth, MapHeight];

        // Fill with floors
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                _grid[x, y] = TileType.Floor;
            }
        }

        // Create wall border
        for (int x = 0; x < MapWidth; x++)
        {
            _grid[x, 0] = TileType.Wall;                    // Top wall
            _grid[x, MapHeight - 1] = TileType.Wall;        // Bottom wall
        }

        for (int y = 0; y < MapHeight; y++)
        {
            _grid[0, y] = TileType.Wall;                    // Left wall
            _grid[MapWidth - 1, y] = TileType.Wall;         // Right wall
        }

        EmitSignal(SignalName.MapChanged);
    }

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
            TileType.Floor => new Color(0.3f, 0.3f, 0.3f), // Dark gray
            TileType.Wall => new Color(0.6f, 0.6f, 0.6f),  // Light gray
            _ => Colors.White
        };
    }
}
