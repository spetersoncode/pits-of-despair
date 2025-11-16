using Godot;
using System.Collections.Generic;
using PitsOfDespair.AI;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// AI component for non-player entities.
/// Manages goal-based AI, pathfinding data, and behavior parameters.
/// </summary>
public partial class AIComponent : Node
{
    [Export] public int SearchTurns { get; set; } = 5;
    [Export] public int SearchRadius { get; set; } = 3;

    // Goal-based AI
    public List<Goal> AvailableGoals { get; set; } = new List<Goal>();
    public Goal CurrentGoal { get; set; }

    // Positional data
    public GridPosition SpawnPosition { get; set; }
    public GridPosition? LastKnownPlayerPosition { get; set; }
    public Queue<GridPosition> CurrentPath { get; set; } = new Queue<GridPosition>();

    // State tracking
    public int TurnsSincePlayerSeen { get; set; }
    public int SearchTurnsRemaining { get; set; }

    /// <summary>
    /// Initializes the AI component with spawn position.
    /// </summary>
    public void Initialize(GridPosition spawnPosition)
    {
        SpawnPosition = spawnPosition;
        LastKnownPlayerPosition = null;
        CurrentPath.Clear();
        TurnsSincePlayerSeen = 0;
        SearchTurnsRemaining = 0;
        CurrentGoal = null;
    }

    /// <summary>
    /// Sets the available goals for this AI.
    /// Called by EntityFactory after instantiating goals from creature data.
    /// </summary>
    public void SetGoals(List<Goal> goals)
    {
        AvailableGoals = goals;
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
