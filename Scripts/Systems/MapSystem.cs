using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages the game map, including tile data and queries.
/// </summary>
public partial class MapSystem : Node
{
    [Export] public int MapWidth { get; set; } = 100;
    [Export] public int MapHeight { get; set; } = 100;

    [Signal]
    public delegate void MapChangedEventHandler();

    private TileType[,] _grid;

    public override void _Ready()
    {
        InitializeMap();
    }

    /// <summary>
    /// Initializes the map with floors and a border of walls.
    /// </summary>
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
