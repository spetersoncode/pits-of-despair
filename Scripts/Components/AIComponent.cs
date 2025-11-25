using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

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

    // Sleeping state
    /// <summary>
    /// Whether this creature is sleeping and should skip turns.
    /// Sleeping creatures wake when: damaged, player proximity, or noise.
    /// </summary>
    public bool IsSleeping { get; set; } = false;

    /// <summary>
    /// Distance at which player proximity wakes this creature.
    /// </summary>
    [Export] public int WakeDistance { get; set; } = 5;

    // Territory behavior
    /// <summary>
    /// Region ID where this creature spawned. Used for territory behavior.
    /// -1 means no home region assigned.
    /// </summary>
    public int HomeRegionId { get; set; } = -1;

    /// <summary>
    /// Center of home territory for territorial behavior.
    /// </summary>
    public GridPosition? HomeCenter { get; set; }

    /// <summary>
    /// Radius of home territory for wander constraints.
    /// </summary>
    public int HomeTerritoryRadius { get; set; } = 10;

    /// <summary>
    /// Initializes the AI component with spawn position.
    /// </summary>
    public void Initialize(GridPosition spawnPosition)
    {
        SpawnPosition = spawnPosition;
        HomeCenter = spawnPosition;
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

    /// <summary>
    /// Wakes the creature from sleep.
    /// </summary>
    public void Wake()
    {
        if (IsSleeping)
        {
            IsSleeping = false;
            GD.Print($"{GetEntity()?.DisplayName ?? "Creature"} wakes up!");
        }
    }

    /// <summary>
    /// Puts the creature to sleep.
    /// </summary>
    public void Sleep()
    {
        IsSleeping = true;
    }

    /// <summary>
    /// Checks if player is within wake distance and wakes if so.
    /// </summary>
    public void CheckWakeFromPlayerProximity(GridPosition playerPosition)
    {
        if (!IsSleeping)
            return;

        var entity = GetEntity();
        if (entity == null)
            return;

        int distance = DistanceHelper.ChebyshevDistance(entity.GridPosition, playerPosition);
        if (distance <= WakeDistance)
        {
            Wake();
        }
    }

    /// <summary>
    /// Gets the current intent (player-facing AI state) derived from goal stack.
    /// </summary>
    public Intent GetIntent()
    {
        if (IsSleeping)
            return Intent.Sleeping;

        // Check current goals to determine intent
        var currentGoal = GoalStack.Peek();
        if (currentGoal == null)
            return Intent.Idle;

        // Check goal type to determine intent
        return currentGoal switch
        {
            FleeGoal => Intent.Fleeing,
            KillTargetGoal killGoal => IsTargetAdjacent(killGoal)
                ? Intent.Attacking
                : Intent.Hunting,
            PatrolGoal => Intent.Patrolling,
            PatrolRouteGoal => Intent.Patrolling,
            WanderGoal => Intent.Wandering,
            FollowTargetGoal => Intent.Following,
            DefendTargetGoal => Intent.Guarding,
            SeekItemGoal => Intent.Scavenging,
            ApproachGoal approachGoal => DetermineApproachIntent(approachGoal),
            BoredGoal => DetermineBoredIntent(),
            _ => Intent.Idle
        };
    }

    private bool IsTargetAdjacent(KillTargetGoal killGoal)
    {
        var entity = GetEntity();
        if (entity == null || killGoal.Target == null)
            return false;

        int distance = DistanceHelper.ChebyshevDistance(
            entity.GridPosition,
            killGoal.Target.GridPosition);
        return distance <= 1;
    }

    private Intent DetermineApproachIntent(ApproachGoal approachGoal)
    {
        // Check original intent to determine what we're approaching for
        var originalIntent = approachGoal.OriginalIntent;
        return originalIntent switch
        {
            KillTargetGoal => Intent.Hunting,
            FollowTargetGoal => Intent.Following,
            PatrolGoal => Intent.Patrolling,
            PatrolRouteGoal => Intent.Patrolling,
            _ => Intent.Hunting // Default to hunting if approaching something
        };
    }

    private Intent DetermineBoredIntent()
    {
        // If we have a protection target, we're guarding
        if (ProtectionTarget != null && GodotObject.IsInstanceValid(ProtectionTarget))
            return Intent.Guarding;

        // Otherwise idle
        return Intent.Idle;
    }
}
