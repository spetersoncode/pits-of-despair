using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Actions;
using PitsOfDespair.Status;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Components;

/// <summary>
/// Manages active status effects on an entity.
/// Tracks buffs, debuffs, and other temporary conditions with duration.
/// </summary>
public partial class StatusComponent : Node
{
    #region Signals

    /// <summary>
    /// Emitted when a status is added to this entity.
    /// </summary>
    [Signal]
    public delegate void StatusAddedEventHandler(string statusName);

    /// <summary>
    /// Emitted when a status is removed from this entity.
    /// </summary>
    [Signal]
    public delegate void StatusRemovedEventHandler(string statusName);

    /// <summary>
    /// Emitted when a status message needs to be displayed.
    /// </summary>
    [Signal]
    public delegate void StatusMessageEventHandler(string message);

    #endregion

    #region Properties

    /// <summary>
    /// Whether this component is attached to a player-controlled entity.
    /// Player statuses process on PlayerTurnStarted, creature statuses on CreatureTurnsStarted.
    /// </summary>
    [Export] public bool IsPlayerControlled { get; set; } = false;

    /// <summary>
    /// List of currently active statuses.
    /// </summary>
    private readonly List<Status.Status> _activeStatuses = new();

    private TurnManager? _turnManager;

    #endregion

    #region Initialization

    public override void _Ready()
    {
        // Find TurnManager by traversing up to GameLevel then searching for TurnManager
        // StatusComponent is on Player/Creature, which is child of GameLevel
        Node gameLevel = GetParent()?.GetParent();
        if (gameLevel == null)
        {
            GD.PrintErr("StatusComponent: Cannot find GameLevel (parent's parent is null)");
            return;
        }

        _turnManager = gameLevel.GetNodeOrNull<TurnManager>("TurnManager");
        if (_turnManager == null)
        {
            GD.PrintErr("StatusComponent: TurnManager not found in GameLevel");
            return;
        }

        // Subscribe to appropriate turn signal based on entity type
        if (IsPlayerControlled)
        {
            _turnManager.PlayerTurnStarted += OnTurnStarted;
        }
        else
        {
            _turnManager.CreatureTurnsStarted += OnTurnStarted;
        }

        GD.Print($"StatusComponent: Successfully connected to TurnManager (IsPlayerControlled={IsPlayerControlled})");
    }

    public override void _ExitTree()
    {
        // Clean up signal connections
        if (_turnManager != null)
        {
            if (IsPlayerControlled)
            {
                _turnManager.PlayerTurnStarted -= OnTurnStarted;
            }
            else
            {
                _turnManager.CreatureTurnsStarted -= OnTurnStarted;
            }
        }
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Adds a status to this entity.
    /// If a status with the same TypeId already exists, refreshes its duration instead of stacking.
    /// Returns a message describing what happened (empty string if no message).
    /// </summary>
    public string AddStatus(Status.Status status)
    {
        if (status == null)
        {
            GD.PrintErr("StatusComponent: Attempted to add null status");
            return string.Empty;
        }

        // Check if this status type is already active
        var existingStatus = _activeStatuses.FirstOrDefault(s => s.TypeId == status.TypeId);
        if (existingStatus != null)
        {
            // Refresh duration instead of stacking
            existingStatus.RefreshDuration(status.Duration);
            return $"{status.Name} duration refreshed to {status.Duration} turns.";
        }

        // Add new status
        _activeStatuses.Add(status);
        status.RemainingTurns = status.Duration;

        // Apply status effects and get message
        var target = GetParent() as Entities.BaseEntity;
        if (target != null)
        {
            string message = status.OnApplied(target);
            EmitSignal(SignalName.StatusAdded, status.Name);
            return message;
        }

        return string.Empty;
    }

    /// <summary>
    /// Removes a specific status from this entity.
    /// Returns a message describing what happened (empty string if no message).
    /// </summary>
    public string RemoveStatus(Status.Status status)
    {
        if (status == null || !_activeStatuses.Contains(status))
        {
            return string.Empty;
        }

        var target = GetParent() as Entities.BaseEntity;
        string message = string.Empty;

        if (target != null)
        {
            message = status.OnRemoved(target);
        }

        _activeStatuses.Remove(status);
        EmitSignal(SignalName.StatusRemoved, status.Name);

        return message;
    }

    /// <summary>
    /// Removes all statuses of a specific type.
    /// Returns messages for all removed statuses.
    /// </summary>
    public List<string> RemoveStatusByType(string typeId)
    {
        var messages = new List<string>();
        var statusesToRemove = _activeStatuses.Where(s => s.TypeId == typeId).ToList();
        foreach (var status in statusesToRemove)
        {
            string message = RemoveStatus(status);
            if (!string.IsNullOrEmpty(message))
            {
                messages.Add(message);
            }
        }
        return messages;
    }

    /// <summary>
    /// Gets all currently active statuses.
    /// </summary>
    public IReadOnlyList<Status.Status> GetActiveStatuses()
    {
        return _activeStatuses.AsReadOnly();
    }

    /// <summary>
    /// Checks if a status of the given type is currently active.
    /// </summary>
    public bool HasStatus(string typeId)
    {
        return _activeStatuses.Any(s => s.TypeId == typeId);
    }

    #endregion

    #region Turn Processing

    /// <summary>
    /// Called at the start of each turn for this entity.
    /// Processes all active statuses and removes expired ones.
    /// </summary>
    private void OnTurnStarted()
    {
        GD.Print($"StatusComponent.OnTurnStarted called! Active statuses: {_activeStatuses.Count}");

        var target = GetParent() as Entities.BaseEntity;
        if (target == null)
        {
            GD.PrintErr("StatusComponent.OnTurnStarted: target is null!");
            return;
        }

        // Process each status and collect expired ones
        var expiredStatuses = new List<Status.Status>();

        foreach (var status in _activeStatuses)
        {
            GD.Print($"  Processing status: {status.Name}, Remaining: {status.RemainingTurns} turns");

            // Let status do per-turn processing (e.g., poison damage)
            string turnMessage = status.OnTurnProcessed(target);
            if (!string.IsNullOrEmpty(turnMessage))
            {
                EmitSignal(SignalName.StatusMessage, turnMessage);
            }

            // Decrement remaining turns
            status.RemainingTurns--;
            GD.Print($"  After decrement: {status.RemainingTurns} turns remaining");

            // Mark as expired if duration reached
            if (status.RemainingTurns <= 0)
            {
                GD.Print($"  Status {status.Name} expired!");
                expiredStatuses.Add(status);
            }
        }

        // Remove expired statuses and emit their messages
        foreach (var status in expiredStatuses)
        {
            string removeMessage = RemoveStatus(status);
            GD.Print($"  Removed status: {status.Name}, Message: {removeMessage}");
            if (!string.IsNullOrEmpty(removeMessage))
            {
                EmitSignal(SignalName.StatusMessage, removeMessage);
            }
        }
    }

    #endregion
}
