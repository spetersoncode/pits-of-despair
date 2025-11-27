using Godot;
using System.Collections.Generic;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Entity;
using PitsOfDespair.Systems.Vision;

namespace PitsOfDespair.Systems.AutoPathing;

/// <summary>
/// Manages automatic exploration for the player.
/// Finds nearest unexplored tiles or autopickup items and paths to them.
/// Stops on interrupts: enemies visible, items collected, no valid targets.
/// </summary>
public partial class AutoExploreSystem : AutoPathingSystem
{
    /// <summary>
    /// Emitted when autoexplore starts (for UI feedback).
    /// </summary>
    [Signal]
    public delegate void AutoExploreStartedEventHandler();

    /// <summary>
    /// Emitted when there's nothing left to explore.
    /// </summary>
    [Signal]
    public delegate void ExplorationCompleteEventHandler();

    public override void Initialize(
        Player player,
        MapSystem mapSystem,
        EntityManager entityManager,
        PlayerVisionSystem visionSystem,
        TurnManager turnManager,
        ActionContext actionContext)
    {
        base.Initialize(player, mapSystem, entityManager, visionSystem, turnManager, actionContext);

        // Connect to player item pickup to detect autopickup events
        _player.Connect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
    }

    /// <summary>
    /// Called when the player picks up an item.
    /// Stops autoexplore after autopickup so player can reassess.
    /// </summary>
    private void OnItemPickedUp(string itemName, bool success, string message)
    {
        if (_isActive && success)
        {
            Stop();
        }
    }

    /// <summary>
    /// Uses Dijkstra's algorithm to find the closest reachable target.
    /// Targets are: unexplored walkable tiles OR autopickup items.
    /// </summary>
    protected override GridPosition? FindTarget()
    {
        var start = _player.GridPosition;
        var visited = new HashSet<GridPosition>();
        var queue = new PriorityQueue<GridPosition, int>();

        queue.Enqueue(start, 0);
        var distances = new Dictionary<GridPosition, int> { [start] = 0 };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            int currentDist = distances[current];

            // Check if this is a valid target (skip the starting position)
            if (current != start && IsAutoExploreTarget(current))
            {
                return current;
            }

            // Expand to neighbors
            foreach (var dir in Directions)
            {
                var neighbor = new GridPosition(current.X + dir.X, current.Y + dir.Y);

                if (visited.Contains(neighbor))
                    continue;

                if (!_mapSystem.IsInBounds(neighbor))
                    continue;

                // Only expand through explored, walkable tiles (safe pathing)
                // But allow targeting unexplored tiles adjacent to explored ones
                if (!_mapSystem.IsWalkable(neighbor))
                    continue;

                // Don't path through hostile creatures
                var entityAtPos = _entityManager.GetEntityAtPosition(neighbor);
                if (entityAtPos != null && !entityAtPos.IsWalkable && Faction.Player.IsHostileTo(entityAtPos.Faction))
                    continue;

                int newDist = currentDist + 1;

                if (!distances.TryGetValue(neighbor, out int existingDist) || newDist < existingDist)
                {
                    distances[neighbor] = newDist;
                    queue.Enqueue(neighbor, newDist);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a position is a valid autoexplore target.
    /// Valid targets: unexplored walkable tiles, autopickup items, or gold.
    /// </summary>
    private bool IsAutoExploreTarget(GridPosition position)
    {
        // Check for autopickup items and gold
        var entities = _entityManager.GetEntitiesAtPosition(position);
        foreach (var entity in entities)
        {
            // Gold piles
            if (entity is Entities.Gold)
            {
                return true;
            }

            // Autopickup items (potions, scrolls, ammo)
            if (entity.ItemData != null && entity.ItemData.AutoPickup)
            {
                return true;
            }
        }

        // Check if tile is unexplored and walkable
        if (!_visionSystem.IsExplored(position) && _mapSystem.IsWalkable(position))
        {
            return true;
        }

        return false;
    }

    protected override void OnStarted()
    {
        EmitSignal(SignalName.AutoExploreStarted);
    }

    protected override void OnNoTargetFound()
    {
        EmitSignal(SignalName.ExplorationComplete);
    }

    protected override void OnTargetReached()
    {
        EmitSignal(SignalName.ExplorationComplete);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
        }
    }
}
