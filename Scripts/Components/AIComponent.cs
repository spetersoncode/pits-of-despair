using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.AI.Goals;
using PitsOfDespair.Conditions;
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
    /// <summary>
    /// Emitted when the creature wakes from sleep.
    /// Parameter: the entity that woke up
    /// </summary>
    [Signal]
    public delegate void WokeUpEventHandler(BaseEntity entity);

    // Goal-based AI (stack-based system)
    public GoalStack GoalStack { get; private set; } = new GoalStack();

    // Positional data
    public GridPosition SpawnPosition { get; set; }

    // Protection/follow target (for friendly AI)
    /// <summary>
    /// The entity this AI should follow and protect.
    /// Used by FollowLeaderComponent to determine follow behavior.
    /// Typically set to the player for friendly creatures.
    /// </summary>
    public BaseEntity? ProtectionTarget { get; set; }

    /// <summary>
    /// Desired distance to maintain from the protection target.
    /// Used by FollowLeaderComponent when FollowEntityGoal is not parameterized.
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
            var entity = GetEntity();
            if (entity != null)
            {
                EmitSignal(SignalName.WokeUp, entity);
            }
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
    /// Walks up the OriginalIntent chain to find the "real" intent behind atomic goals.
    /// </summary>
    public Intent GetIntent()
    {
        if (IsSleeping)
            return Intent.Sleeping;

        // Check current goals to determine intent
        var currentGoal = GoalStack.Peek();
        if (currentGoal == null)
            return Intent.Idle;

        // Walk up the OriginalIntent chain to find the intent-bearing goal
        // Atomic goals like MoveDirectionGoal don't represent intent themselves
        var goal = currentGoal;
        while (goal != null)
        {
            var intent = GetIntentFromGoal(goal);
            if (intent != null)
                return intent.Value;

            // Walk up to parent goal
            goal = goal.OriginalIntent;
        }

        return Intent.Idle;
    }

    /// <summary>
    /// Gets the intent for a specific goal, or null if this goal type doesn't define intent.
    /// </summary>
    private Intent? GetIntentFromGoal(Goal goal)
    {
        return goal switch
        {
            FleeGoal => Intent.Fleeing,
            KillTargetGoal => Intent.Attacking,
            PatrolGoal => Intent.Patrolling,
            PatrolRouteGoal => Intent.Patrolling,
            ConfusedWanderGoal => Intent.Confused,
            // WanderGoal with OriginalIntent delegates to parent (e.g., confusion)
            WanderGoal wg when wg.OriginalIntent != null => null,
            WanderGoal => Intent.Wandering,
            FollowEntityGoal => Intent.Following,
            SeekItemGoal => Intent.Scavenging,
            ApproachGoal approachGoal => DetermineApproachIntent(approachGoal),
            BoredGoal => DetermineBoredIntent(),
            // Atomic goals like MoveDirectionGoal don't define intent - return null to walk up chain
            _ => null
        };
    }

    private Intent DetermineApproachIntent(ApproachGoal approachGoal)
    {
        // Check original intent to determine what we're approaching for
        var originalIntent = approachGoal.OriginalIntent;
        return originalIntent switch
        {
            KillTargetGoal => Intent.Attacking,
            FollowEntityGoal => Intent.Following,
            PatrolGoal => Intent.Patrolling,
            PatrolRouteGoal => Intent.Patrolling,
            _ => Intent.Attacking // Default to attacking if approaching something
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
