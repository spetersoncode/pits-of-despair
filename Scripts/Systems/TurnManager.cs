using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Systems.VisualEffects;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages the energy-based turn flow.
/// Time advances when the player acts, creatures act when they have enough accumulated time.
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
    private int _pendingPlayerDelay = 0;
    private VisualEffectSystem _visualEffectSystem;
    private MessageSystem _messageSystem;
    private TimeSystem _timeSystem;
    private AISystem _aiSystem;

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
    /// Sets the time system reference for energy-based scheduling.
    /// </summary>
    public void SetTimeSystem(TimeSystem timeSystem)
    {
        _timeSystem = timeSystem;
    }

    /// <summary>
    /// Sets the AI system reference for processing creature turns.
    /// </summary>
    public void SetAISystem(AISystem aiSystem)
    {
        _aiSystem = aiSystem;
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
    /// Advances time and processes creature turns.
    /// </summary>
    /// <param name="playerDelay">The delay cost of the player's action.</param>
    public void EndPlayerTurn(int playerDelay)
    {
        if (!_isPlayerTurn)
        {
            GD.PushWarning("EndPlayerTurn called but it's not the player's turn");
            return;
        }

        // Store the player delay for use after effects complete
        _pendingPlayerDelay = playerDelay;

        // Check if we need to wait for visual effect animations (including projectiles)
        if (_visualEffectSystem != null && _visualEffectSystem.HasActiveEffects)
        {
            _waitingForEffects = true;
            // Don't transition yet - wait for AllEffectsCompleted signal
        }
        else
        {
            // No effects, transition immediately
            ProcessCreatureTurns();
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
            ProcessCreatureTurns();
        }
    }

    /// <summary>
    /// Advances time and processes all creatures that have enough energy to act.
    /// Creatures act in speed order (fastest first) and can act multiple times if they have enough energy.
    /// </summary>
    private void ProcessCreatureTurns()
    {
        // Flush player turn messages before transitioning
        _messageSystem?.EndSequence();

        _isPlayerTurn = false;
        EmitSignal(SignalName.PlayerTurnEnded);

        // Advance time for all creatures by the player's action delay
        _timeSystem?.AdvanceTime(_pendingPlayerDelay);

        // Begin sequencing for creature turns
        _messageSystem?.BeginSequence();
        EmitSignal(SignalName.CreatureTurnsStarted);

        // Process all creatures that have enough energy to act
        // Loop continues until no creatures have enough energy for an action
        const int maxIterations = 1000; // Safety limit to prevent infinite loops
        int iterations = 0;

        while (iterations < maxIterations)
        {
            iterations++;

            // Get the next creature ready to act (fastest first)
            var readyCreature = _timeSystem?.GetNextReadyCreature(ActionDelay.Standard);

            if (readyCreature == null)
            {
                // No more creatures ready to act
                break;
            }

            // Process this creature's turn
            int creatureDelay = _aiSystem?.ProcessSingleCreatureTurn(readyCreature) ?? ActionDelay.Standard;

            // Deduct the action's delay cost from the creature's accumulated time
            _timeSystem?.DeductCreatureTime(readyCreature, creatureDelay);
        }

        if (iterations >= maxIterations)
        {
            GD.PushWarning("TurnManager: Hit max iterations processing creature turns");
        }

        // All creatures processed
        EndCreatureTurns();
    }

    /// <summary>
    /// Ends the creature turn phase and returns to player turn.
    /// </summary>
    private void EndCreatureTurns()
    {
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
