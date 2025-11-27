using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.VisualEffects;

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
	private VisualEffectSystem _visualEffectSystem;
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
	/// Sets the visual effect system for rendering explosions and other effects.
	/// </summary>
	public void SetVisualEffectSystem(VisualEffectSystem visualEffectSystem)
	{
		_visualEffectSystem = visualEffectSystem;
	}

	/// <summary>
	/// Gets the current render offset used to center the view on the player.
	/// Used by visual effect system to position shader-based effects.
	/// </summary>
	public Vector2 GetRenderOffset()
	{
		if (_player == null)
		{
			return Vector2.Zero;
		}

		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 viewportCenter = viewportSize / 2;

		// Player's world position in pixels
		Vector2 playerWorldPos = new Vector2(
			_player.CurrentPosition.X * TileSize,
			_player.CurrentPosition.Y * TileSize
		);

		// Offset needed to center player on screen
		return viewportCenter - playerWorldPos;
	}

	/// <summary>
	/// Converts a grid position to screen coordinates for overlay rendering.
	/// Accounts for baseline text positioning offset.
	/// </summary>
	/// <param name="gridPos">The grid position to convert.</param>
	/// <returns>Screen position for the top-left corner of the tile overlay.</returns>
	public Vector2 GridToOverlayPosition(GridPosition gridPos)
	{
		return new Vector2(
			gridPos.X * TileSize - 2.0f,
			gridPos.Y * TileSize - TileSize + 4.0f);
	}

	/// <summary>
	/// Converts a grid position to screen coordinates for the tile center.
	/// Used for effects, projectiles, trace lines, and other centered visuals.
	/// </summary>
	/// <param name="gridPos">The grid position to convert.</param>
	/// <returns>Screen position for the center of the tile.</returns>
	public Vector2 GridToTileCenter(GridPosition gridPos)
	{
		return new Vector2(
			gridPos.X * TileSize + TileSize / 2.0f - 2.0f,
			gridPos.Y * TileSize - TileSize / 2.0f + 2.0f);
	}

	/// <summary>
	/// Converts a fractional tile position to screen coordinates for the center.
	/// Used for smooth projectile movement between tiles.
	/// </summary>
	/// <param name="tilePos">The tile position (can be fractional).</param>
	/// <returns>Screen position for the center.</returns>
	public Vector2 TilePosToCenter(Vector2 tilePos)
	{
		return new Vector2(
			tilePos.X * TileSize + TileSize / 2.0f - 2.0f,
			tilePos.Y * TileSize - TileSize / 2.0f + 2.0f);
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

		// Redraw while visual effects (including projectiles) are animating
		if (_visualEffectSystem != null && _visualEffectSystem.HasActiveEffects)
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

		// Build set of positions occupied by entities (to suppress floor dot rendering)
		var occupiedPositions = new System.Collections.Generic.HashSet<GridPosition>();
		foreach (var entity in _entities)
		{
			occupiedPositions.Add(entity.GridPosition);
		}
		// Player position also suppresses floor dot
		occupiedPositions.Add(_player.GridPosition);

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

				// Skip floor dot rendering if an entity occupies this tile AND it's visible
				// Entities render their own glyphs which should take precedence
				// Only hide dots in visible tiles to avoid revealing creature positions in fog-of-war
				if (tile == TileType.Floor && occupiedPositions.Contains(pos) && isVisible)
				{
					continue;
				}

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

		// Draw entities in layer order (lowest to highest priority)
		// Only the top-layer entity at each position should be visible
		// Player is always on top and drawn separately at viewport center

		// Build a map of which entity to draw at each position (highest layer wins)
		var entityToDraw = new System.Collections.Generic.Dictionary<GridPosition, BaseEntity>();
		foreach (var entity in _entities)
		{
			// Skip entities at player position - player always wins
			if (entity.GridPosition.Equals(_player.GridPosition))
				continue;

			if (!entityToDraw.TryGetValue(entity.GridPosition, out var existing) || entity.Layer > existing.Layer)
			{
				entityToDraw[entity.GridPosition] = entity;
			}
		}

		// Draw the winning entity at each position
		foreach (var kvp in entityToDraw)
		{
			var entity = kvp.Value;
			bool isVisible = _visionSystem?.IsVisible(entity.GridPosition) ?? true;
			bool isExplored = _visionSystem?.IsExplored(entity.GridPosition) ?? true;

			// Track discovered items/features/decorations for memorable visibility
			if ((isVisible || isExplored) && entity.Layer <= EntityLayer.Item)
			{
				_discoveredItemPositions.Add(entity.GridPosition);
			}

			// Creatures only visible when in FOV, items/features/decorations are memorable
			if (entity.Layer == EntityLayer.Creature)
			{
				if (!isVisible)
					continue;
			}
			else
			{
				// Items, features, decorations: draw if explored (memorable)
				if (!isExplored && !isVisible)
					continue;
			}

			Vector2 entityWorldPos = new Vector2(
				entity.GridPosition.X * TileSize,
				entity.GridPosition.Y * TileSize
			);
			Vector2 entityDrawPos = offset + entityWorldPos;

			// Dim non-creature entities that are not currently visible (fog of war)
			Color entityColor = entity.GlyphColor;
			if (entity.Layer != EntityLayer.Creature && isExplored && !isVisible)
			{
				entityColor = new Color(entityColor.R * 0.5f, entityColor.G * 0.5f, entityColor.B * 0.5f);
			}

			DrawString(_font, entityDrawPos, entity.Glyph, HorizontalAlignment.Left, -1, FontSize, entityColor);
		}

		// Draw the player last (should be at viewport center, on top of everything)
		DrawString(_font, viewportCenter, _player.Glyph, HorizontalAlignment.Left, -1, FontSize, _player.GlyphColor);

		// Note: Projectiles and visual effects render via shader nodes (ColorRect children)
		// They don't need explicit drawing here - they render themselves

		// Draw visual effects (fallback for non-shader effects)
		if (_visualEffectSystem != null)
		{
			foreach (var effect in _visualEffectSystem.ActiveEffects)
			{
				DrawVisualEffect(effect, offset);
			}
		}

		// Draw cursor targeting overlay (examine and action modes)
		if (_cursorSystem != null && _cursorSystem.IsActive)
		{
			GridPosition cursorPos = _cursorSystem.CursorPosition;
			Vector2 cursorDrawPos = offset + GridToOverlayPosition(cursorPos);

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
						Vector2 tileDrawPos = offset + GridToOverlayPosition(tile);
						Color baseColor = Palette.TargetingRangeOverlay;
						Color highlightColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.3f);
						DrawRect(new Rect2(tileDrawPos, new Vector2(TileSize, TileSize)), highlightColor, true);
					}
				}

				// Draw AOE blast radius preview (red flashing overlay)
				if (_cursorSystem.AffectedPositions != null && _cursorSystem.AffectedPositions.Count > 0)
				{
					// Slow pulsing red overlay for blast radius
					float flashPulse = (float)(Mathf.Sin(Time.GetTicksMsec() / 300.0) * 0.15 + 0.25);
					Color aoeColor = new Color(Palette.Danger.R, Palette.Danger.G, Palette.Danger.B, flashPulse);

					foreach (var affectedPos in _cursorSystem.AffectedPositions)
					{
						Vector2 aoeDrawPos = offset + GridToOverlayPosition(affectedPos);
						DrawRect(new Rect2(aoeDrawPos, new Vector2(TileSize, TileSize)), aoeColor, true);
					}
				}

				// Draw trace line from origin to cursor (skip for Line/Cone modes where template is the focus)
				var targetingType = _cursorSystem.TargetingDefinition?.Type;
				bool skipTraceLine = targetingType == Targeting.TargetingType.Line ||
				                     targetingType == Targeting.TargetingType.Cone;
				if (!skipTraceLine)
				{
					DrawTraceLine(_cursorSystem.OriginPosition, cursorPos, offset);
				}
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

			// Draw pulsing fill inside border when cursor is on a valid target
			// Only for Creature targeting - AOE types have their own red preview
			bool showValidHighlight = _cursorSystem.IsOnValidTarget &&
				_cursorSystem.TargetingDefinition?.Type == Targeting.TargetingType.Creature;
			if (showValidHighlight)
			{
				float fillPulse = (float)(Mathf.Sin(Time.GetTicksMsec() / 200.0) * 0.2 + 0.25);
				Color fillColor = Palette.TargetingValid; // Green for valid targets
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
		Vector2 targetDrawPos = offset + GridToTileCenter(target);

		// Draw line from origin to target
		Color baseColor = Palette.TargetingLine;
		Color lineColor = new Color(baseColor.R, baseColor.G, baseColor.B, 0.5f);
		DrawLine(originDrawPos, targetDrawPos, lineColor, 2.0f);
	}

	/// <summary>
	/// Draws a visual effect (explosion, heal glow, etc.).
	/// Effects with shader nodes render themselves via ColorRect children.
	/// </summary>
	private void DrawVisualEffect(VisualEffectData effect, Vector2 offset)
	{
		// All effects now use shader-based rendering via ColorRect children
		// This method exists for potential future non-shader effects
		// Currently, all effects should have a ShaderNode and render themselves
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
