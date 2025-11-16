using Godot;

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

    public bool IsPlayerTurn => _isPlayerTurn;

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
    /// Transitions to creature turns.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (!_isPlayerTurn)
        {
            GD.PushWarning("EndPlayerTurn called but it's not the player's turn");
            return;
        }

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
}
