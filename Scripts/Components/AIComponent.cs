using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// AI state for non-player entities.
/// Manages state machine, pathfinding data, and behavior parameters.
/// </summary>
public partial class AIComponent : Node
{
    public enum AIState
    {
        Idle,          // Waiting at spawn position
        Chasing,       // Pursuing visible player
        Investigating, // Searching last known player position
        Returning      // Returning to spawn position
    }

    [Export] public int SearchTurns { get; set; } = 5;
    [Export] public int SearchRadius { get; set; } = 3;

    public AIState CurrentState { get; set; } = AIState.Idle;
    public GridPosition SpawnPosition { get; set; }
    public GridPosition? LastKnownPlayerPosition { get; set; }
    public Queue<GridPosition> CurrentPath { get; set; } = new Queue<GridPosition>();
    public int InvestigationTurnsRemaining { get; set; }

    /// <summary>
    /// Initializes the AI component with spawn position.
    /// </summary>
    public void Initialize(GridPosition spawnPosition)
    {
        SpawnPosition = spawnPosition;
        CurrentState = AIState.Idle;
        LastKnownPlayerPosition = null;
        CurrentPath.Clear();
        InvestigationTurnsRemaining = 0;
    }

    /// <summary>
    /// Clears the current path.
    /// </summary>
    public void ClearPath()
    {
        CurrentPath.Clear();
    }

    /// <summary>
    /// Gets the next position in the path, or null if path is empty.
    /// </summary>
    public GridPosition? GetNextPosition()
    {
        if (CurrentPath.Count > 0)
        {
            return CurrentPath.Dequeue();
        }
        return null;
    }

    /// <summary>
    /// Gets the parent entity.
    /// </summary>
    public BaseEntity GetEntity()
    {
        return GetParent<BaseEntity>();
    }
}
