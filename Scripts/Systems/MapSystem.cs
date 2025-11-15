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
    private System.Collections.Generic.List<RoomBounds> _rooms = new();

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
        var generator = DungeonGeneratorFactory.CreateBSP(BSPConfig) as BSPDungeonGenerator;

        // Generate the map
        _grid = generator.Generate(MapWidth, MapHeight);

        // Extract and store room data for entity spawning
        _rooms = generator.GetRooms();

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
            TileType.Wall => Colors.White,
            _ => Colors.White
        };
    }

    /// <summary>
    /// Get floor tile positions for each room in the dungeon.
    /// Used for per-room entity spawning.
    /// </summary>
    /// <returns>List of rooms, where each room is a list of floor tile positions.</returns>
    public System.Collections.Generic.List<System.Collections.Generic.List<GridPosition>> GetRoomFloorTiles()
    {
        var roomTiles = new System.Collections.Generic.List<System.Collections.Generic.List<GridPosition>>();

        foreach (var room in _rooms)
        {
            var floorTiles = new System.Collections.Generic.List<GridPosition>();

            // Collect all floor tiles within room bounds
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    if (IsWalkable(pos))
                    {
                        floorTiles.Add(pos);
                    }
                }
            }

            if (floorTiles.Count > 0)
            {
                roomTiles.Add(floorTiles);
            }
        }

        return roomTiles;
    }
}
