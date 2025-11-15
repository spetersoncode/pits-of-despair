using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages non-player entity vision and detection.
/// Checks which entities can see the player each turn.
/// </summary>
public partial class NonPlayerVisionSystem : Node
{
    private MapSystem _mapSystem;
    private Player _player;
    private EntityManager _entityManager;

    /// <summary>
    /// Initializes the non-player vision system with required references.
    /// Called by GameLevel after scene setup.
    /// </summary>
    public void Initialize(MapSystem mapSystem, Player player, EntityManager entityManager)
    {
        _mapSystem = mapSystem;
        _player = player;
        _entityManager = entityManager;

        // Subscribe to player turn completion
        _player.TurnCompleted += OnPlayerTurnCompleted;

        // Check initial vision
        CheckNonPlayerVision();
    }

    /// <summary>
    /// Called when the player completes a turn.
    /// Checks if any non-player entities can see the player.
    /// </summary>
    private void OnPlayerTurnCompleted()
    {
        CheckNonPlayerVision();
    }

    /// <summary>
    /// Checks which non-player entities can see the player and logs detection.
    /// </summary>
    private void CheckNonPlayerVision()
    {
        GridPosition playerPos = _player.GridPosition;

        foreach (BaseEntity entity in _entityManager.GetAllEntities())
        {
            var visionComponent = entity.GetNodeOrNull<VisionComponent>("VisionComponent");
            if (visionComponent == null)
                continue;

            // Calculate visible tiles for this creature
            HashSet<GridPosition> visiblePositions = ShadowcastingHelper.CalculateVisibleTiles(
                entity.GridPosition,
                visionComponent.VisionRange,
                _mapSystem
            );

            // Check if player is visible
            if (visiblePositions.Contains(playerPos))
            {
                GD.Print($"{entity.DisplayName} at ({entity.GridPosition.X},{entity.GridPosition.Y}) can see player at ({playerPos.X},{playerPos.Y})");
            }
        }
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
