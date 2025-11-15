using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Renders the game map and entities using text glyphs with a monospace font.
/// </summary>
public partial class TextRenderer : Control
{
    [Export] public int TileSize { get; set; } = 16;
    [Export] public int FontSize { get; set; } = 14;

    private MapSystem _mapSystem;
    private Player _player;
    private EntityManager? _entityManager;
    private PlayerVisionSystem _visionSystem;
    private Font _font;
    private readonly System.Collections.Generic.List<BaseEntity> _entities = new();

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
            _player.PositionChanged -= OnPlayerMoved;
        }

        _player = player;

        if (_player != null)
        {
            _player.PositionChanged += OnPlayerMoved;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Sets the entity manager to track and render entities.
    /// </summary>
    public void SetEntityManager(EntityManager entityManager)
    {
        if (_entityManager != null)
        {
            _entityManager.EntityAdded -= OnEntityAdded;
            _entityManager.EntityRemoved -= OnEntityRemoved;
        }

        _entityManager = entityManager;

        if (_entityManager != null)
        {
            _entityManager.EntityAdded += OnEntityAdded;
            _entityManager.EntityRemoved += OnEntityRemoved;

            // Add existing entities
            _entities.Clear();
            _entities.AddRange(_entityManager.GetAllEntities());

            // Subscribe to position changes for all entities
            foreach (var entity in _entities)
            {
                entity.PositionChanged += OnEntityMoved;
            }

            QueueRedraw();
        }
    }

    /// <summary>
    /// Sets the player vision system for fog-of-war rendering.
    /// </summary>
    public void SetPlayerVisionSystem(PlayerVisionSystem visionSystem)
    {
        if (_visionSystem != null)
        {
            _visionSystem.VisionChanged -= OnVisionChanged;
        }

        _visionSystem = visionSystem;

        if (_visionSystem != null)
        {
            _visionSystem.VisionChanged += OnVisionChanged;
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

    private void OnEntityAdded(BaseEntity entity)
    {
        _entities.Add(entity);
        entity.PositionChanged += OnEntityMoved;
        QueueRedraw();
    }

    private void OnEntityRemoved(BaseEntity entity)
    {
        entity.PositionChanged -= OnEntityMoved;
        _entities.Remove(entity);
        QueueRedraw();
    }

    private void OnEntityMoved(int x, int y)
    {
        QueueRedraw();
    }

    private void OnVisionChanged()
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

                // Check visibility for fog-of-war
                bool isVisible = _visionSystem?.IsVisible(pos) ?? true;
                bool isExplored = _visionSystem?.IsExplored(pos) ?? true;

                // Hidden tiles (not explored) are not drawn
                if (!isExplored && !isVisible)
                {
                    continue;
                }

                TileType tile = _mapSystem.GetTileAt(pos);
                char glyph = _mapSystem.GetGlyphForTile(tile);
                Color color = _mapSystem.GetColorForTile(tile);

                // Dim explored-but-not-visible tiles (fog-of-war)
                if (isExplored && !isVisible)
                {
                    color = new Color(0.25f, 0.25f, 0.25f);  // Dark grey
                }

                // World position of this tile
                Vector2 tileWorldPos = new Vector2(x * TileSize, y * TileSize);
                Vector2 drawPos = offset + tileWorldPos;

                DrawChar(_font, drawPos, glyph.ToString(), FontSize, color);
            }
        }

        // Draw entities (goblins, monsters, etc.) - between tiles and player
        // Only draw entities on visible tiles
        foreach (var entity in _entities)
        {
            // Check if entity is on a visible tile
            bool entityVisible = _visionSystem?.IsVisible(entity.GridPosition) ?? true;
            if (!entityVisible)
            {
                continue;
            }

            Vector2 entityWorldPos = new Vector2(
                entity.GridPosition.X * TileSize,
                entity.GridPosition.Y * TileSize
            );
            Vector2 entityDrawPos = offset + entityWorldPos;

            DrawChar(_font, entityDrawPos, entity.Glyph.ToString(), FontSize, entity.GlyphColor);
        }

        // Draw the player last (should be at viewport center, on top of everything)
        DrawChar(_font, viewportCenter, _player.Glyph.ToString(), FontSize, _player.GlyphColor);
    }
}
