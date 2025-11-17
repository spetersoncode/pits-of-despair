using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Renders the game map and entities using text glyphs with a monospace font.
/// </summary>
public partial class TextRenderer : Control
{
	[Export] public int TileSize { get; set; } = 18;
	[Export] public int FontSize { get; set; } = 18;

	private MapSystem _mapSystem;
	private Player _player;
	private EntityManager? _entityManager;
	private PlayerVisionSystem _visionSystem;
	private TargetingSystem _targetingSystem;
	private ProjectileSystem _projectileSystem;
	private Font _font;
	private readonly System.Collections.Generic.List<BaseEntity> _entities = new();
	private readonly System.Collections.Generic.HashSet<GridPosition> _discoveredItemPositions = new();
	private bool _wasTargetingActive = false;

	public override void _Ready()
	{
		// Load Fira Mono Medium font
		_font = GD.Load<Font>("res://Resources/Fonts/FiraMono-Medium.ttf");

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

	/// <summary>
	/// Sets the targeting system for rendering targeting overlay.
	/// </summary>
	public void SetTargetingSystem(TargetingSystem targetingSystem)
	{
		_targetingSystem = targetingSystem;
	}

	/// <summary>
	/// Sets the projectile system for rendering active projectiles.
	/// </summary>
	public void SetProjectileSystem(ProjectileSystem projectileSystem)
	{
		_projectileSystem = projectileSystem;
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

	public override void _Process(double delta)
	{
		// Continuously redraw during targeting mode or when projectiles are active
		bool needsRedraw = false;

		bool isTargetingActive = _targetingSystem != null && _targetingSystem.IsActive;
		if (isTargetingActive)
		{
			needsRedraw = true;
		}

		// If targeting just stopped, queue one final redraw to clear the overlay
		if (_wasTargetingActive && !isTargetingActive)
		{
			needsRedraw = true;
		}
		_wasTargetingActive = isTargetingActive;

		if (_projectileSystem != null && _projectileSystem.ActiveProjectiles.Count > 0)
		{
			needsRedraw = true;
		}

		if (needsRedraw)
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_mapSystem == null || _player == null)
		{
			return;
		}

		// Draw black background
		Vector2 viewportSize = GetViewportRect().Size;
		DrawRect(new Rect2(Vector2.Zero, viewportSize), Palette.Empty, true);

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
					color = Palette.FogOfWar;
				}

				// World position of this tile
				Vector2 tileWorldPos = new Vector2(x * TileSize, y * TileSize);
				Vector2 drawPos = offset + tileWorldPos;

				DrawChar(_font, drawPos, glyph.ToString(), FontSize, color);
			}
		}

		// Draw items first (between tiles and creatures)
		// Items remain visible once discovered (memorable)
		foreach (var entity in _entities)
		{
			// Only process items
			if (entity.GetNodeOrNull<ItemComponent>("ItemComponent") == null)
				continue;

			// Don't draw items at player position (player glyph takes precedence)
			if (entity.GridPosition.Equals(_player.GridPosition))
				continue;

			bool isVisible = _visionSystem?.IsVisible(entity.GridPosition) ?? true;
			bool isExplored = _visionSystem?.IsExplored(entity.GridPosition) ?? true;

			// Track discovered items
			if (isVisible || isExplored)
			{
				_discoveredItemPositions.Add(entity.GridPosition);
			}

			// Draw items if they're on an explored tile (memorable visibility)
			if (!isExplored && !isVisible)
			{
				continue;
			}

			Vector2 itemWorldPos = new Vector2(
				entity.GridPosition.X * TileSize,
				entity.GridPosition.Y * TileSize
			);
			Vector2 itemDrawPos = offset + itemWorldPos;

			// Dim items that are not currently visible
			Color itemColor = entity.GlyphColor;
			if (isExplored && !isVisible)
			{
				itemColor = new Color(itemColor.R * 0.5f, itemColor.G * 0.5f, itemColor.B * 0.5f);
			}

			DrawString(_font, itemDrawPos, entity.Glyph, HorizontalAlignment.Left, -1, FontSize, itemColor);
		}

		// Draw creatures (between items and player)
		// Only draw creatures on currently visible tiles
		foreach (var entity in _entities)
		{
			// Skip items (already drawn)
			if (entity.GetNodeOrNull<ItemComponent>("ItemComponent") != null)
				continue;

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

			DrawString(_font, entityDrawPos, entity.Glyph, HorizontalAlignment.Left, -1, FontSize, entity.GlyphColor);
		}

		// Draw the player last (should be at viewport center, on top of everything)
		DrawString(_font, viewportCenter, _player.Glyph, HorizontalAlignment.Left, -1, FontSize, _player.GlyphColor);

		// Draw projectiles (on top of player)
		if (_projectileSystem != null)
		{
			foreach (var projectile in _projectileSystem.ActiveProjectiles)
			{
				Vector2 projectileWorldPos = new Vector2(
					projectile.GridPosition.X * TileSize,
					projectile.GridPosition.Y * TileSize
				);
				Vector2 projectileDrawPos = offset + projectileWorldPos;

				DrawString(_font, projectileDrawPos, projectile.Glyph, HorizontalAlignment.Left, -1, FontSize, projectile.GlyphColor);
			}
		}

		// Draw targeting overlay (on top of everything else)
		if (_targetingSystem != null && _targetingSystem.IsActive)
		{
			// Draw valid tiles in range with subtle highlight
			if (_targetingSystem.ValidTiles != null)
			{
				foreach (var tile in _targetingSystem.ValidTiles)
				{
					Vector2 tileWorldPos = new Vector2(tile.X * TileSize, tile.Y * TileSize);
					Vector2 tileDrawPos = offset + tileWorldPos;

					// Draw subtle background highlight for tiles in range
					Color highlightColor = new Color(0.2f, 0.2f, 0.4f, 0.3f);
					DrawRect(new Rect2(tileDrawPos, new Vector2(TileSize, TileSize)), highlightColor, true);
				}
			}

			// Draw trace line from player to cursor
			GridPosition cursorPos = _targetingSystem.CursorPosition;
			DrawTraceLine(_player.GridPosition, cursorPos, offset);

			// Draw cursor at target position
			// Adjust upward to align with glyph visual center (baseline positioning)
			Vector2 cursorWorldPos = new Vector2(
				cursorPos.X * TileSize - 2.0f,
				cursorPos.Y * TileSize - TileSize + 4.0f
			);
			Vector2 cursorDrawPos = offset + cursorWorldPos;

			// Check if there's a creature at cursor position
			var targetEntity = _entityManager?.GetEntityAtPosition(cursorPos);
			bool hasCreature = targetEntity != null &&
				targetEntity.GetNodeOrNull<Components.HealthComponent>("HealthComponent") != null;

			if (hasCreature)
			{
				// Draw pulsing green highlight for valid target (creature)
				float pulse = (float)(Mathf.Sin(Time.GetTicksMsec() / 200.0) * 0.15 + 0.25);
				Color highlightColor = new Color(0.3f, 1.0f, 0.3f, pulse); // Green
				DrawRect(new Rect2(cursorDrawPos, new Vector2(TileSize, TileSize)), highlightColor, true);
			}
			else
			{
				// Draw pulsing red tint for empty tile (no target)
				float pulse = (float)(Mathf.Sin(Time.GetTicksMsec() / 200.0) * 0.1 + 0.15);
				Color highlightColor = new Color(1.0f, 0.3f, 0.3f, pulse); // Red
				DrawRect(new Rect2(cursorDrawPos, new Vector2(TileSize, TileSize)), highlightColor, true);
			}
		}
	}

	/// <summary>
	/// Draws a trace line from origin to target using simple line segments.
	/// </summary>
	private void DrawTraceLine(GridPosition origin, GridPosition target, Vector2 offset)
	{
		// For the player (origin), use viewport center since player is always drawn there
		// DrawString uses baseline positioning, so we need to adjust upward from viewportCenter
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 viewportCenter = viewportSize / 2;

		// Origin: center of player glyph
		// Adjust upward from baseline positioning
		Vector2 originDrawPos = viewportCenter + new Vector2(TileSize / 2.0f - 2.0f, -TileSize / 4.0f + 2.0f);

		// Target: center of target glyph/entity
		// All entities use DrawString with baseline positioning, fine-tuned adjustment
		Vector2 targetWorldPos = new Vector2(
			target.X * TileSize + TileSize / 2.0f - 2.0f,
			target.Y * TileSize - TileSize / 2.0f + 2.0f
		);
		Vector2 targetDrawPos = offset + targetWorldPos;

		// Draw line from origin to target
		Color lineColor = new Color(0.8f, 0.8f, 0.3f, 0.5f);
		DrawLine(originDrawPos, targetDrawPos, lineColor, 2.0f);
	}
}
