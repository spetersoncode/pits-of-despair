using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Projectiles;

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
	private CursorTargetingSystem _cursorSystem;
	private ProjectileSystem _projectileSystem;
	private Font _font;
	private readonly System.Collections.Generic.List<BaseEntity> _entities = new();
	private readonly System.Collections.Generic.HashSet<GridPosition> _discoveredItemPositions = new();
	private bool _wasCursorActive = false;
	private readonly System.Collections.Generic.HashSet<BaseEntity> _trackedEntities = new();

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
			_mapSystem.Disconnect(MapSystem.SignalName.MapChanged, Callable.From(OnMapChanged));
		}

		_mapSystem = mapSystem;

		if (_mapSystem != null)
		{
			_mapSystem.Connect(MapSystem.SignalName.MapChanged, Callable.From(OnMapChanged));
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
			_player.Disconnect(Player.SignalName.PositionChanged, Callable.From<int, int>(OnPlayerMoved));
		}

		_player = player;

		if (_player != null)
		{
			_player.Connect(Player.SignalName.PositionChanged, Callable.From<int, int>(OnPlayerMoved));
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
			_entityManager.Disconnect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));
			_entityManager.Disconnect(EntityManager.SignalName.EntityRemoved, Callable.From<BaseEntity>(OnEntityRemoved));
		}

		_entityManager = entityManager;

		if (_entityManager != null)
		{
			_entityManager.Connect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));
			_entityManager.Connect(EntityManager.SignalName.EntityRemoved, Callable.From<BaseEntity>(OnEntityRemoved));

			// Add existing entities
			_entities.Clear();
			_entities.AddRange(_entityManager.GetAllEntities());

			// Subscribe to position changes for all entities
			foreach (var entity in _entities)
			{
				entity.Connect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnEntityMoved));
				_trackedEntities.Add(entity);
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
			_visionSystem.Disconnect(PlayerVisionSystem.SignalName.VisionChanged, Callable.From(OnVisionChanged));
		}

		_visionSystem = visionSystem;

		if (_visionSystem != null)
		{
			_visionSystem.Connect(PlayerVisionSystem.SignalName.VisionChanged, Callable.From(OnVisionChanged));
			QueueRedraw();
		}
	}

	/// <summary>
	/// Sets the cursor targeting system for rendering cursor overlay (examine and action modes).
	/// </summary>
	public void SetCursorTargetingSystem(CursorTargetingSystem cursorSystem)
	{
		_cursorSystem = cursorSystem;
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
		entity.Connect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnEntityMoved));
		_trackedEntities.Add(entity);
		QueueRedraw();
	}

	private void OnEntityRemoved(BaseEntity entity)
	{
		entity.Disconnect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnEntityMoved));
		_entities.Remove(entity);
		_trackedEntities.Remove(entity);
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
		// Continuously redraw during cursor targeting mode or when projectiles are active
		bool needsRedraw = false;

		bool isCursorActive = _cursorSystem != null && _cursorSystem.IsActive;
		if (isCursorActive)
		{
			needsRedraw = true;
		}

		// If cursor just stopped, queue one final redraw to clear the overlay
		if (_wasCursorActive && !isCursorActive)
		{
			needsRedraw = true;
		}
		_wasCursorActive = isCursorActive;

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
			if (entity.ItemData == null)
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
			if (entity.ItemData != null)
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

		// Draw projectiles as animated lines (on top of player)
		if (_projectileSystem != null)
		{
			foreach (var projectile in _projectileSystem.ActiveProjectiles)
			{
				DrawProjectileLine(projectile, offset);
			}
		}

		// Draw cursor targeting overlay (examine and action modes)
		if (_cursorSystem != null && _cursorSystem.IsActive)
		{
			GridPosition cursorPos = _cursorSystem.CursorPosition;
			Vector2 cursorWorldPos = new Vector2(
				cursorPos.X * TileSize - 2.0f,
				cursorPos.Y * TileSize - TileSize + 4.0f
			);
			Vector2 cursorDrawPos = offset + cursorWorldPos;

			// Check if there's an entity/creature at cursor position
			var cursorEntity = _entityManager?.GetEntityAtPosition(cursorPos);
			bool hasEntity = cursorEntity != null;
			bool hasCreature = hasEntity &&
				cursorEntity.GetNodeOrNull<Components.HealthComponent>("HealthComponent") != null;

			bool isExamineMode = _cursorSystem.CurrentMode == CursorTargetingSystem.TargetingMode.Examine;

			// Draw range overlay and trace line for action modes only
			if (!isExamineMode)
			{
				// Draw valid tiles in range with subtle highlight
				if (_cursorSystem.ValidTiles != null)
				{
					foreach (var tile in _cursorSystem.ValidTiles)
					{
						Vector2 tileWorldPos = new Vector2(tile.X * TileSize, tile.Y * TileSize - TileSize);
						Vector2 tileDrawPos = offset + tileWorldPos;

						Color baseColor = Palette.TargetingRangeOverlay;
						Color highlightColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.3f);
						DrawRect(new Rect2(tileDrawPos, new Vector2(TileSize, TileSize)), highlightColor, true);
					}
				}

				// Draw trace line from origin to cursor
				DrawTraceLine(_cursorSystem.OriginPosition, cursorPos, offset);
			}

			// Draw box border cursor for ALL modes
			// Always show white border for visibility (solid, no pulse)
			Color borderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f); // White, fully opaque

			// Draw thin border (1 pixel)
			float borderWidth = 1.0f;
			DrawRect(new Rect2(cursorDrawPos.X, cursorDrawPos.Y, TileSize, borderWidth), borderColor, true); // Top
			DrawRect(new Rect2(cursorDrawPos.X, cursorDrawPos.Y + TileSize - borderWidth, TileSize, borderWidth), borderColor, true); // Bottom
			DrawRect(new Rect2(cursorDrawPos.X, cursorDrawPos.Y, borderWidth, TileSize), borderColor, true); // Left
			DrawRect(new Rect2(cursorDrawPos.X + TileSize - borderWidth, cursorDrawPos.Y, borderWidth, TileSize), borderColor, true); // Right

			// Draw pulsing fill inside border only when there's an entity
			if (hasEntity)
			{
				float fillPulse = (float)(Mathf.Sin(Time.GetTicksMsec() / 200.0) * 0.2 + 0.25);
				Color fillColor = Palette.TargetingValid; // Green for entities
				Color fillHighlight = new Color(fillColor.R, fillColor.G, fillColor.B, fillPulse);
				DrawRect(new Rect2(cursorDrawPos, new Vector2(TileSize, TileSize)), fillHighlight, true);
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
		Color baseColor = Palette.TargetingLine;
		Color lineColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.5f);
		DrawLine(originDrawPos, targetDrawPos, lineColor, 2.0f);
	}

	/// <summary>
	/// Draws an animated projectile with shape and trail support.
	/// </summary>
	private void DrawProjectileLine(ProjectileData projectile, Vector2 offset)
	{
		Vector2 currentPos = projectile.GetCurrentPosition();
		Vector2 currentDrawPos = GetProjectileDrawPosition(currentPos, offset);
		Vector2 direction = projectile.GetDirection();

		// Draw trail first (behind the head)
		if (projectile.TrailPositions.Count > 0)
		{
			DrawProjectileTrail(projectile, offset);
		}

		// Draw projectile head
		DrawProjectileShape(projectile.Definition, currentDrawPos, direction);
	}

	/// <summary>
	/// Converts a tile position to screen draw position for projectiles.
	/// </summary>
	private Vector2 GetProjectileDrawPosition(Vector2 tilePos, Vector2 offset)
	{
		return offset + new Vector2(
			tilePos.X * TileSize + TileSize / 2.0f - 2.0f,
			tilePos.Y * TileSize - TileSize / 2.0f + 2.0f
		);
	}

	/// <summary>
	/// Draws the projectile trail (fading segments behind the head).
	/// </summary>
	private void DrawProjectileTrail(ProjectileData projectile, Vector2 offset)
	{
		var definition = projectile.Definition!;
		var trailPositions = projectile.TrailPositions;

		for (int i = 0; i < trailPositions.Count; i++)
		{
			Vector2 trailDrawPos = GetProjectileDrawPosition(trailPositions[i], offset);
			Color trailColor = definition.GetTrailColorAtSegment(i, trailPositions.Count);

			// Draw smaller version of shape for trail
			float trailScale = 1.0f - ((float)i / trailPositions.Count) * 0.5f;
			float trailSize = definition.Size * trailScale;

			switch (definition.Shape)
			{
				case ProjectileShape.Circle:
					DrawCircle(trailDrawPos, trailSize * 0.7f, trailColor);
					break;
				case ProjectileShape.Diamond:
					DrawDiamond(trailDrawPos, trailSize * 0.7f, trailColor);
					break;
				case ProjectileShape.Triangle:
					DrawTriangle(trailDrawPos, trailSize * 0.7f, projectile.GetDirection(), trailColor);
					break;
				case ProjectileShape.Line:
					// Line trails are just dots
					DrawCircle(trailDrawPos, 2.0f, trailColor);
					break;
			}
		}
	}

	/// <summary>
	/// Draws the projectile head based on its shape definition.
	/// </summary>
	private void DrawProjectileShape(ProjectileDefinition definition, Vector2 position, Vector2 direction)
	{
		Color headColor = new Color(definition.HeadColor.R, definition.HeadColor.G, definition.HeadColor.B, 0.9f);

		switch (definition.Shape)
		{
			case ProjectileShape.Circle:
				DrawCircle(position, definition.Size, headColor);
				break;

			case ProjectileShape.Diamond:
				DrawDiamond(position, definition.Size, headColor);
				break;

			case ProjectileShape.Triangle:
				DrawTriangle(position, definition.Size, direction, headColor);
				break;

			case ProjectileShape.Line:
				DrawProjectileLineShape(position, direction, definition.Size, definition.LineWidth, headColor);
				break;
		}
	}

	/// <summary>
	/// Draws a diamond shape at the given position.
	/// </summary>
	private void DrawDiamond(Vector2 center, float size, Color color)
	{
		Vector2[] points = new Vector2[]
		{
			center + new Vector2(0, -size),      // Top
			center + new Vector2(size, 0),       // Right
			center + new Vector2(0, size),       // Bottom
			center + new Vector2(-size, 0)       // Left
		};
		DrawPolygon(points, new Color[] { color });
	}

	/// <summary>
	/// Draws a triangle pointing in the given direction.
	/// </summary>
	private void DrawTriangle(Vector2 center, float size, Vector2 direction, Color color)
	{
		// Calculate perpendicular vector for triangle base
		Vector2 perp = new Vector2(-direction.Y, direction.X);

		// Triangle points: tip in direction of travel, base behind
		Vector2 tip = center + direction * size;
		Vector2 baseLeft = center - direction * size * 0.5f + perp * size * 0.6f;
		Vector2 baseRight = center - direction * size * 0.5f - perp * size * 0.6f;

		Vector2[] points = new Vector2[] { tip, baseLeft, baseRight };
		DrawPolygon(points, new Color[] { color });
	}

	/// <summary>
	/// Draws a line segment projectile shape.
	/// </summary>
	private void DrawProjectileLineShape(Vector2 center, Vector2 direction, float length, float width, Color color)
	{
		Vector2 halfSegment = direction * (length * TileSize / 12.0f);
		Vector2 lineStart = center - halfSegment;
		Vector2 lineEnd = center + halfSegment;
		DrawLine(lineStart, lineEnd, color, width);
	}

	public override void _ExitTree()
	{
		// Disconnect from map system
		if (_mapSystem != null)
		{
			_mapSystem.Disconnect(MapSystem.SignalName.MapChanged, Callable.From(OnMapChanged));
		}

		// Disconnect from player
		if (_player != null)
		{
			_player.Disconnect(Player.SignalName.PositionChanged, Callable.From<int, int>(OnPlayerMoved));
		}

		// Disconnect from entity manager
		if (_entityManager != null)
		{
			_entityManager.Disconnect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));
			_entityManager.Disconnect(EntityManager.SignalName.EntityRemoved, Callable.From<BaseEntity>(OnEntityRemoved));
		}

		// Disconnect from all tracked entities
		foreach (var entity in _trackedEntities)
		{
			if (entity != null && GodotObject.IsInstanceValid(entity))
			{
				entity.Disconnect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnEntityMoved));
			}
		}
		_trackedEntities.Clear();

		// Disconnect from vision system
		if (_visionSystem != null)
		{
			_visionSystem.Disconnect(PlayerVisionSystem.SignalName.VisionChanged, Callable.From(OnVisionChanged));
		}
	}
}
