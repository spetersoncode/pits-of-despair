using Godot;
using System.Collections.Generic;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// AI component for non-player entities.
/// Manages goal-based AI, pathfinding data, and behavior parameters.
/// </summary>
public partial class AIComponent : Node
{
    [Export] public int SearchTurns { get; set; } = 12;
    [Export] public int SearchRadius { get; set; } = 3;
    [Export] public int FleeTurns { get; set; } = 20;

    // Goal-based AI (new stack-based system)
    public GoalStack GoalStack { get; private set; } = new GoalStack();

    // Goal-based AI (legacy - kept for transition)
    public List<Goal> AvailableGoals { get; set; } = new List<Goal>();
    public Goal CurrentGoal { get; set; }

    // Positional data
    public GridPosition SpawnPosition { get; set; }
    public GridPosition? LastKnownPlayerPosition { get; set; }
    public Queue<GridPosition> CurrentPath { get; set; } = new Queue<GridPosition>();

    // State tracking
    public int TurnsSincePlayerSeen { get; set; }
    public int SearchTurnsRemaining { get; set; }
    public int FleeturnsRemaining { get; set; }
    public int TurnsSinceLastYell { get; set; }

    // Protection/follow target (for friendly AI)
    /// <summary>
    /// The entity this AI should follow and protect.
    /// Used by FollowTargetGoal and DefendTargetGoal.
    /// Typically set to the player for friendly creatures.
    /// </summary>
    public BaseEntity? ProtectionTarget { get; set; }

    /// <summary>
    /// Desired distance to maintain from the protection target.
    /// Used by FollowTargetGoal to determine when to move closer.
    /// </summary>
    [Export] public int FollowDistance { get; set; } = 3;

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
        FleeturnsRemaining = 0;
        TurnsSinceLastYell = 0;
        CurrentGoal = null;

        // Initialize goal stack with BoredGoal at the bottom
        InitializeGoalStack();
    }

    /// <summary>
    /// Initializes the goal stack with BoredGoal at the bottom.
    /// Called automatically by Initialize(), but can be called manually if needed.
    /// </summary>
    public void InitializeGoalStack()
    {
        GoalStack.Clear();
        GoalStack.Push(new BoredGoal());
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
