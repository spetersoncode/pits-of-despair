using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Systems.Vision;

/// <summary>
/// Manages non-player entity vision and detection.
/// Checks which entities can see the player each turn.
/// </summary>
public partial class NonPlayerVisionSystem : Node
{
    private MapSystem _mapSystem;
    private Player _player;
    private EntityManager _entityManager;
    private CombatSystem _combatSystem;

    /// <summary>
    /// Initializes the non-player vision system with required references.
    /// Called by GameLevel after scene setup.
    /// </summary>
    public void Initialize(MapSystem mapSystem, Player player, EntityManager entityManager, CombatSystem combatSystem)
    {
        _mapSystem = mapSystem;
        _player = player;
        _entityManager = entityManager;
        _combatSystem = combatSystem;

        // Connect to player turn completion
        _player.Connect(Player.SignalName.TurnCompleted, Callable.From<int>(OnPlayerTurnCompleted));

        // Check initial vision
        CheckNonPlayerVision();
    }

    /// <summary>
    /// Called when the player completes a turn.
    /// Checks if any non-player entities can see the player.
    /// </summary>
    private void OnPlayerTurnCompleted(int delayCost)
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
            HashSet<GridPosition> visiblePositions = FOVCalculator.CalculateVisibleTiles(
                entity.GridPosition,
                visionComponent.VisionRange,
                _mapSystem
            );

            // Check if player is visible
            if (visiblePositions.Contains(playerPos))
            {
                // Trigger JoinPlayerOnSight behavior
                var joinComponent = entity.GetNodeOrNull<AI.Components.JoinPlayerOnSightComponent>("JoinPlayerOnSightComponent");
                if (joinComponent != null)
                {
                    joinComponent.OnPlayerSeen(_combatSystem);
                    
                    // If it joined, we should also set it as a companion to the player
                    if (entity.Faction == Faction.Player)
                    {
                        // We need to set the protection target to the player
                        var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
                        if (aiComponent != null)
                        {
                            aiComponent.ProtectionTarget = _player;
                        }
                    }
                }

                // TODO: Trigger AI behavior when player is detected
            }
        }
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
