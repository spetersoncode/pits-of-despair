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

    // Fog-of-war state
    private bool[,] _visibleTiles;   // Currently visible tiles (recalculated each turn)
    private bool[,] _exploredTiles;  // Tiles that have been seen at least once (cumulative memory)

    private int _mapWidth;
    private int _mapHeight;

    /// <summary>
    /// Initializes the vision system with required references.
    /// Called by GameLevel after scene setup.
    /// </summary>
    public void Initialize(MapSystem mapSystem, Player player)
    {
        _mapSystem = mapSystem;
        _player = player;
        _playerVision = player.GetNode<VisionComponent>("VisionComponent");

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

        // Subscribe to player turn completion
        _player.TurnCompleted += OnPlayerTurnCompleted;

        // Calculate initial vision
        CalculateVision();
    }

    /// <summary>
    /// Called when the player completes a turn (usually after moving).
    /// Recalculates field-of-view from player's new position.
    /// </summary>
    private void OnPlayerTurnCompleted()
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

        // Notify renderer to redraw
        EmitSignal(SignalName.VisionChanged);
    }

    /// <summary>
    /// Checks if a tile is currently visible to the player.
    /// </summary>
    public bool IsVisible(GridPosition position)
    {
        if (!_mapSystem.IsInBounds(position))
        {
            return false;
        }

        return _visibleTiles[position.X, position.Y];
    }

    /// <summary>
    /// Checks if a tile has been explored (seen at least once).
    /// Explored tiles are shown dimly even when not currently visible.
    /// </summary>
    public bool IsExplored(GridPosition position)
    {
        if (!_mapSystem.IsInBounds(position))
        {
            return false;
        }

        return _exploredTiles[position.X, position.Y];
    }

    public override void _ExitTree()
    {
        // Cleanup signal connections
        if (_player != null)
        {
            _player.TurnCompleted -= OnPlayerTurnCompleted;
        }
    }
}
