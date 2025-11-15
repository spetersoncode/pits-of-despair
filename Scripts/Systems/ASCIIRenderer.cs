using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Renders the game map and entities using ASCII characters with a monospace font.
/// </summary>
public partial class ASCIIRenderer : Control
{
    [Export] public int TileSize { get; set; } = 16;
    [Export] public int FontSize { get; set; } = 14;

    private MapSystem _mapSystem;
    private Player _player;
    private Font _font;

    public override void _Ready()
    {
        // Use the default monospace font
        _font = ThemeDB.FallbackFont;

        // Set size to match viewport
        Size = GetViewportRect().Size;
        Position = Vector2.Zero;
    }

    /// <summary>
    /// Sets the map system to render.
    /// </summary>
    public void SetMapSystem(MapSystem mapSystem)
    {
        if (_mapSystem != null)
        {
            _mapSystem.MapChanged -= OnMapChanged;
        }

        _mapSystem = mapSystem;

        if (_mapSystem != null)
        {
            _mapSystem.MapChanged += OnMapChanged;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Sets the player to render.
    /// </summary>
    public void SetPlayer(Player player)
    {
        if (_player != null)
        {
            _player.Moved -= OnPlayerMoved;
        }

        _player = player;

        if (_player != null)
        {
            _player.Moved += OnPlayerMoved;
            QueueRedraw();
        }
    }

    private void OnMapChanged()
    {
        QueueRedraw();
    }

    private void OnPlayerMoved(int x, int y)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_mapSystem == null || _player == null)
        {
            return;
        }

        // Draw black background
        Vector2 viewportSize = GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero, viewportSize), Colors.Black, true);

        // Calculate offset to keep player centered on screen
        Vector2 viewportCenter = viewportSize / 2;

        // Player's world position in pixels
        Vector2 playerWorldPos = new Vector2(
            _player.CurrentPosition.X * TileSize,
            _player.CurrentPosition.Y * TileSize
        );

        // Offset needed to center player on screen
        Vector2 offset = viewportCenter - playerWorldPos;

        // Draw the map
        for (int x = 0; x < _mapSystem.MapWidth; x++)
        {
            for (int y = 0; y < _mapSystem.MapHeight; y++)
            {
                GridPosition pos = new GridPosition(x, y);
                TileType tile = _mapSystem.GetTileAt(pos);

                char glyph = _mapSystem.GetGlyphForTile(tile);
                Color color = _mapSystem.GetColorForTile(tile);

                // World position of this tile
                Vector2 tileWorldPos = new Vector2(x * TileSize, y * TileSize);
                Vector2 drawPos = offset + tileWorldPos;

                DrawChar(_font, drawPos, glyph.ToString(), FontSize, color);
            }
        }

        // Draw the player (should be at viewport center)
        DrawChar(_font, viewportCenter, _player.Glyph, FontSize, _player.GlyphColor);
    }
}
