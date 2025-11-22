using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Components;

/// <summary>
/// AI component for non-player entities.
/// Manages goal-stack AI and behavior parameters.
/// </summary>
public partial class AIComponent : Node
{
    // Goal-based AI (stack-based system)
    public GoalStack GoalStack { get; private set; } = new GoalStack();

    // Positional data
    public GridPosition SpawnPosition { get; set; }

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
    /// Gets the parent entity.
    /// </summary>
    public BaseEntity GetEntity()
    {
        return GetParent<BaseEntity>();
    }
}
