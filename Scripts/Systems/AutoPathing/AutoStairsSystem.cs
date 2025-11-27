using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems.AutoPathing;

/// <summary>
/// Manages automatic pathing to known stairs for the player.
/// Finds explored stairs and paths to them.
/// Stops on interrupts: enemies visible, no valid path, reached stairs.
/// </summary>
public partial class AutoStairsSystem : AutoPathingSystem
{
    /// <summary>
    /// Emitted when the player reaches the stairs.
    /// </summary>
    [Signal]
    public delegate void ReachedStairsEventHandler();

    /// <summary>
    /// Finds the nearest stairs that the player has explored.
    /// </summary>
    protected override GridPosition? FindTarget()
    {
        GridPosition? nearestStairs = null;
        int nearestDistance = int.MaxValue;

        foreach (var entity in _entityManager.GetAllEntities())
        {
            if (entity is not Stairs stairs)
                continue;

            // Only consider stairs the player has seen
            if (!_visionSystem.IsExplored(stairs.GridPosition))
                continue;

            int distance = DistanceHelper.ChebyshevDistance(_player.GridPosition, stairs.GridPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestStairs = stairs.GridPosition;
            }
        }

        return nearestStairs;
    }

    protected override void OnNoTargetFound()
    {
        _actionContext.CombatSystem.EmitActionMessage(_player, "You don't know where the stairs are.", Palette.ToHex(Palette.Default));
    }

    protected override void OnTargetReached()
    {
        EmitSignal(SignalName.ReachedStairs);
    }
}
