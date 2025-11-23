using Godot;
using PitsOfDespair.Systems.Projectiles;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages the turn-based game flow.
/// Coordinates player turn -> creature turns -> player turn cycle.
/// </summary>
public partial class TurnManager : Node
{
    [Signal]
    public delegate void PlayerTurnStartedEventHandler();

    [Signal]
    public delegate void PlayerTurnEndedEventHandler();

    [Signal]
    public delegate void CreatureTurnsStartedEventHandler();

    [Signal]
    public delegate void CreatureTurnsEndedEventHandler();

    private bool _isPlayerTurn = true;
    private bool _waitingForProjectiles = false;
    private ProjectileSystem _projectileSystem;

    public bool IsPlayerTurn => _isPlayerTurn;

    /// <summary>
    /// Sets the projectile system reference for turn coordination.
    /// </summary>
    public void SetProjectileSystem(ProjectileSystem projectileSystem)
    {
        // Disconnect from old system if exists
        if (_projectileSystem != null)
        {
            _projectileSystem.Disconnect(ProjectileSystem.SignalName.AllProjectilesCompleted, Callable.From(OnAllProjectilesCompleted));
        }

        _projectileSystem = projectileSystem;

        // Connect to new system
        if (_projectileSystem != null)
        {
            _projectileSystem.Connect(ProjectileSystem.SignalName.AllProjectilesCompleted, Callable.From(OnAllProjectilesCompleted));
        }
    }

    /// <summary>
    /// Starts the first player turn. Call this after initialization.
    /// </summary>
    public void StartFirstPlayerTurn()
    {
        _isPlayerTurn = true;
        EmitSignal(SignalName.PlayerTurnStarted);
    }

    /// <summary>
    /// Called when the player completes their action.
    /// Transitions to creature turns, potentially waiting for projectiles to complete.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (!_isPlayerTurn)
        {
            GD.PushWarning("EndPlayerTurn called but it's not the player's turn");
            return;
        }

        // Check if we need to wait for projectile animations
        if (_projectileSystem != null && _projectileSystem.HasActiveProjectiles)
        {
            _waitingForProjectiles = true;
            // Don't transition yet - wait for AllProjectilesCompleted signal
        }
        else
        {
            // No projectiles, transition immediately
            TransitionToCreatureTurns();
        }
    }

    /// <summary>
    /// Called when all projectiles have completed their animations.
    /// Completes the turn transition if we were waiting.
    /// </summary>
    private void OnAllProjectilesCompleted()
    {
        if (_waitingForProjectiles)
        {
            _waitingForProjectiles = false;
            TransitionToCreatureTurns();
        }
    }

    /// <summary>
    /// Transitions from player turn to creature turns.
    /// </summary>
    private void TransitionToCreatureTurns()
    {
        _isPlayerTurn = false;
        EmitSignal(SignalName.PlayerTurnEnded);
        EmitSignal(SignalName.CreatureTurnsStarted);
    }

    /// <summary>
    /// Called when all creatures have completed their turns.
    /// Transitions back to player turn.
    /// </summary>
    public void EndCreatureTurns()
    {
        if (_isPlayerTurn)
        {
            GD.PushWarning("EndCreatureTurns called but it's already the player's turn");
            return;
        }

        _isPlayerTurn = true;
        EmitSignal(SignalName.CreatureTurnsEnded);
        EmitSignal(SignalName.PlayerTurnStarted);
    }

    public override void _ExitTree()
    {
        // Disconnect from projectile system
        if (_projectileSystem != null)
        {
            _projectileSystem.Disconnect(ProjectileSystem.SignalName.AllProjectilesCompleted, Callable.From(OnAllProjectilesCompleted));
        }
    }
}
