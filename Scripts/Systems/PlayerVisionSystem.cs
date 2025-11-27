using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages player field-of-view and fog-of-war state.
/// Tracks visible tiles (current vision) and explored tiles (memory).
/// Recalculates vision each turn using recursive shadowcasting.
/// </summary>
public partial class PlayerVisionSystem : Node
{
    [Signal]
    public delegate void VisionChangedEventHandler();

    private MapSystem _mapSystem;
    private Player _player;
    private VisionComponent _playerVision;
    private EntityManager _entityManager;
    private SkillComponent _playerSkillComponent;

    // Fog-of-war state
    private bool[,] _visibleTiles;   // Currently visible tiles (recalculated each turn)
    private bool[,] _exploredTiles;  // Tiles that have been seen at least once (cumulative memory)

    private int _mapWidth;
    private int _mapHeight;

    // Debug state
    private bool _revealModeEnabled = false;

    // Keen Hearing detection state
    private readonly HashSet<BaseEntity> _heardCreatures = new();
    private const float HearingChance = 0.5f;  // 50% chance to hear each creature

    /// <summary>
    /// Initializes the vision system with required references.
    /// Called by GameLevel after scene setup.
    /// </summary>
    public void Initialize(MapSystem mapSystem, Player player, EntityManager entityManager)
    {
        _mapSystem = mapSystem;
        _player = player;
        _entityManager = entityManager;
        _playerVision = player.GetNode<VisionComponent>("VisionComponent");
        _playerSkillComponent = player.GetNodeOrNull<SkillComponent>("SkillComponent");

        if (_playerVision == null)
        {
            GD.PushError("Player does not have a VisionComponent!");
            return;
        }

        // Initialize fog-of-war arrays
        _mapWidth = mapSystem.MapWidth;
        _mapHeight = mapSystem.MapHeight;
        _visibleTiles = new bool[_mapWidth, _mapHeight];
        _exploredTiles = new bool[_mapWidth, _mapHeight];

        // Connect to player turn completion
        _player.Connect(Player.SignalName.TurnCompleted, Callable.From<int>(OnPlayerTurnCompleted));

        // Calculate initial vision
        CalculateVision();
    }

    /// <summary>
    /// Called when the player completes a turn (usually after moving).
    /// Recalculates field-of-view from player's new position.
    /// </summary>
    private void OnPlayerTurnCompleted(int delayCost)
    {
        CalculateVision();
    }

    /// <summary>
    /// Calculates visible tiles from player's current position using shadowcasting.
    /// Updates both visible and explored tile state.
    /// </summary>
    private void CalculateVision()
    {
        // Clear current visible tiles
        for (int x = 0; x < _mapWidth; x++)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                _visibleTiles[x, y] = false;
            }
        }

        // Calculate new visible tiles using shadowcasting
        GridPosition playerPos = _player.GridPosition;
        int visionRange = _playerVision.VisionRange;

        HashSet<GridPosition> visiblePositions = FOVCalculator.CalculateVisibleTiles(
            playerPos,
            visionRange,
            _mapSystem
        );

        // Update visible and explored state
        foreach (GridPosition pos in visiblePositions)
        {
            if (_mapSystem.IsInBounds(pos))
            {
                _visibleTiles[pos.X, pos.Y] = true;
                _exploredTiles[pos.X, pos.Y] = true;  // Once seen, always remembered
            }
        }

        // Calculate heard creatures (Keen Hearing skill)
        CalculateHeardCreatures(playerPos, visiblePositions);

        // Notify renderer to redraw
        EmitSignal(SignalName.VisionChanged);
    }

    /// <summary>
    /// Calculates creatures detected via Keen Hearing skill.
    /// Uses two-step detection: (1) direct distance in hearing zone, (2) walking distance is reasonable.
    /// This allows hearing through thin walls but not thick dungeon sections.
    /// Uses Dijkstra (single flood-fill) instead of A* per creature for efficiency.
    /// </summary>
    private void CalculateHeardCreatures(GridPosition playerPos, HashSet<GridPosition> visiblePositions)
    {
        _heardCreatures.Clear();

        // Check if player has Keen Hearing skill
        if (_playerSkillComponent == null || !_playerSkillComponent.HasSkill("keen_hearing"))
        {
            return;
        }

        if (_entityManager == null)
        {
            return;
        }

        int hearingRange = _playerVision.VisionRange;
        int hearingRangeSq = hearingRange * hearingRange;

        // Build distance map once from player position (Dijkstra flood-fill)
        // Pass null for entityManager/player to skip occupancy checks - we want
        // distances to tiles regardless of what's standing on them
        var distanceMap = DijkstraMapBuilder.BuildDistanceMap(
            new List<GridPosition> { playerPos },
            _mapSystem
        );

        // Check all creatures
        foreach (var entity in _entityManager.GetAllEntities())
        {
            // Only creatures can be heard
            if (entity.Layer != EntityLayer.Creature)
            {
                continue;
            }

            // Skip if already visible (no need to "hear" what you can see)
            if (visiblePositions.Contains(entity.GridPosition))
            {
                continue;
            }

            // Step 1: Direct distance check (ignoring walls) - must be within hearing range
            int distSq = DistanceHelper.EuclideanDistance(playerPos, entity.GridPosition);
            if (distSq > hearingRangeSq)
            {
                continue;
            }

            float directDist = Mathf.Sqrt(distSq);

            // Step 2: Walking distance check via pre-computed Dijkstra map
            float walkingDist = distanceMap[entity.GridPosition.X, entity.GridPosition.Y];

            // Skip if unreachable or walking distance too long (> 1.5x direct distance)
            if (walkingDist == float.MaxValue || walkingDist > directDist * 1.5f)
            {
                continue;
            }

            // 50% chance to hear each creature
            if (GD.Randf() < HearingChance)
            {
                _heardCreatures.Add(entity);
            }
        }
    }

    /// <summary>
    /// Checks if a tile is currently visible to the player.
    /// In reveal mode, all tiles are visible.
    /// </summary>
    public bool IsVisible(GridPosition position)
    {
        if (!_mapSystem.IsInBounds(position))
        {
            return false;
        }

        // Reveal mode: see everything
        if (_revealModeEnabled)
        {
            return true;
        }

        return _visibleTiles[position.X, position.Y];
    }

    /// <summary>
    /// Checks if an entity is detected via Keen Hearing (heard but not visible).
    /// </summary>
    public bool IsHeard(BaseEntity entity) => _heardCreatures.Contains(entity);

    /// <summary>
    /// Checks if a tile has been explored (seen at least once).
    /// Explored tiles are shown dimly even when not currently visible.
    /// In reveal mode, all tiles are explored.
    /// </summary>
    public bool IsExplored(GridPosition position)
    {
        if (!_mapSystem.IsInBounds(position))
        {
            return false;
        }

        // Reveal mode: everything is explored
        if (_revealModeEnabled)
        {
            return true;
        }

        return _exploredTiles[position.X, position.Y];
    }

    /// <summary>
    /// Forces vision recalculation from current player position.
    /// Used by debug commands and other systems that bypass normal turn flow.
    /// </summary>
    public void ForceRecalculateVision()
    {
        CalculateVision();
    }

    /// <summary>
    /// Toggles reveal mode debug feature.
    /// When enabled, reveals the entire map.
    /// </summary>
    public void ToggleRevealMode()
    {
        _revealModeEnabled = !_revealModeEnabled;

        if (_revealModeEnabled)
        {
            // Reveal entire map
            RevealEntireMap();
        }
        else
        {
            // Return to normal FOV
            CalculateVision();
        }
    }

    /// <summary>
    /// Reveals the entire map by marking all tiles as visible and explored.
    /// Used by reveal mode debug feature.
    /// </summary>
    private void RevealEntireMap()
    {
        for (int x = 0; x < _mapWidth; x++)
        {
            for (int y = 0; y < _mapHeight; y++)
            {
                _visibleTiles[x, y] = true;
                _exploredTiles[x, y] = true;
            }
        }

        // Notify renderer to redraw
        EmitSignal(SignalName.VisionChanged);
    }

    public override void _ExitTree()
    {
        // Cleanup signal connections
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.TurnCompleted, Callable.From<int>(OnPlayerTurnCompleted));
        }
    }
}
