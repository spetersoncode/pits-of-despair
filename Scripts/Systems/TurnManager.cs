using Godot;
using PitsOfDespair.Systems.VisualEffects;

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
    private bool _waitingForEffects = false;
    private VisualEffectSystem _visualEffectSystem;
    private MessageSystem _messageSystem;

    public bool IsPlayerTurn => _isPlayerTurn;

    /// <summary>
    /// Sets the visual effect system reference for turn coordination.
    /// </summary>
    public void SetVisualEffectSystem(VisualEffectSystem visualEffectSystem)
    {
        // Disconnect from old system if exists
        if (_visualEffectSystem != null)
        {
            _visualEffectSystem.Disconnect(VisualEffectSystem.SignalName.AllEffectsCompleted, Callable.From(OnAllEffectsCompleted));
        }

        _visualEffectSystem = visualEffectSystem;

        // Connect to new system
        if (_visualEffectSystem != null)
        {
            _visualEffectSystem.Connect(VisualEffectSystem.SignalName.AllEffectsCompleted, Callable.From(OnAllEffectsCompleted));
        }
    }

    /// <summary>
    /// Sets the message system reference for message sequencing.
    /// </summary>
    public void SetMessageSystem(MessageSystem messageSystem)
    {
        _messageSystem = messageSystem;
    }

    /// <summary>
    /// Starts the first player turn. Call this after initialization.
    /// </summary>
    public void StartFirstPlayerTurn()
    {
        _isPlayerTurn = true;
        _messageSystem?.BeginSequence();
        EmitSignal(SignalName.PlayerTurnStarted);
    }

    /// <summary>
    /// Called when the player completes their action.
    /// Transitions to creature turns, potentially waiting for effects to complete.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (!_isPlayerTurn)
        {
            GD.PushWarning("EndPlayerTurn called but it's not the player's turn");
            return;
        }

        // Check if we need to wait for visual effect animations (including projectiles)
        if (_visualEffectSystem != null && _visualEffectSystem.HasActiveEffects)
        {
            _waitingForEffects = true;
            // Don't transition yet - wait for AllEffectsCompleted signal
        }
        else
        {
            // No effects, transition immediately
            TransitionToCreatureTurns();
        }
    }

    /// <summary>
    /// Called when all visual effects have completed their animations.
    /// Completes the turn transition if we were waiting.
    /// </summary>
    private void OnAllEffectsCompleted()
    {
        if (_waitingForEffects)
        {
            _waitingForEffects = false;
            TransitionToCreatureTurns();
        }
    }

    /// <summary>
    /// Transitions from player turn to creature turns.
    /// </summary>
    private void TransitionToCreatureTurns()
    {
        // Flush player turn messages before transitioning
        _messageSystem?.EndSequence();

        _isPlayerTurn = false;
        EmitSignal(SignalName.PlayerTurnEnded);

        // Begin sequencing for creature turns
        _messageSystem?.BeginSequence();
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

        // Flush creature turn messages
        _messageSystem?.EndSequence();

        _isPlayerTurn = true;
        EmitSignal(SignalName.CreatureTurnsEnded);

        // Begin sequencing for next player turn
        _messageSystem?.BeginSequence();
        EmitSignal(SignalName.PlayerTurnStarted);
    }

    public override void _ExitTree()
    {
        // Disconnect from visual effect system
        if (_visualEffectSystem != null)
        {
            _visualEffectSystem.Disconnect(VisualEffectSystem.SignalName.AllEffectsCompleted, Callable.From(OnAllEffectsCompleted));
        }
    }
}
