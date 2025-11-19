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
    public delegate void StatusMessageEventHandler(string message, string color);

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

    private bool _isConnected = false;

    public override void _Ready()
    {
        // Prevent duplicate connections
        if (_isConnected)
        {
            return;
        }

        // Find TurnManager in GameLevel
        _turnManager = GetTree().Root.GetNodeOrNull<TurnManager>("GameLevel/TurnManager");

        if (_turnManager == null)
        {
            GD.PrintErr("StatusComponent: TurnManager not found at GameLevel/TurnManager");
            return;
        }

        // Connect to appropriate turn signal based on entity type
        if (IsPlayerControlled)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }
        else
        {
            _turnManager.Connect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnTurnStarted));
        }

        _isConnected = true;
    }

    public override void _ExitTree()
    {
        // Clean up signal connections
        if (_turnManager != null)
        {
            if (IsPlayerControlled)
            {
                _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
            }
            else
            {
                _turnManager.Disconnect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnTurnStarted));
            }
        }
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Adds a status to this entity.
    /// If a status with the same TypeId already exists, refreshes its duration instead of stacking.
    /// Emits a signal with the status message and color.
    /// </summary>
    public void AddStatus(Status.Status status)
    {
        if (status == null)
        {
            GD.PrintErr("StatusComponent: Attempted to add null status");
            return;
        }

        // Check if this status type is already active
        var existingStatus = _activeStatuses.FirstOrDefault(s => s.TypeId == status.TypeId);
        if (existingStatus != null)
        {
            // Refresh duration instead of stacking
            existingStatus.RefreshDuration(status.Duration);
            var refreshMessage = $"{status.Name} duration refreshed to {status.Duration} turns.";
            EmitSignal(SignalName.StatusMessage, refreshMessage, Core.Palette.ToHex(Core.Palette.StatusNeutral));
            return;
        }

        // Add new status
        _activeStatuses.Add(status);
        status.RemainingTurns = status.Duration;

        // Apply status effects and get message
        var target = GetParent() as Entities.BaseEntity;
        if (target != null)
        {
            var statusMsg = status.OnApplied(target);
            EmitSignal(SignalName.StatusAdded, status.Name);
            if (!string.IsNullOrEmpty(statusMsg.Message))
            {
                EmitSignal(SignalName.StatusMessage, statusMsg.Message, statusMsg.Color);
            }
        }
    }

    /// <summary>
    /// Removes a specific status from this entity.
    /// Emits a signal with the removal message and color.
    /// </summary>
    public void RemoveStatus(Status.Status status)
    {
        if (status == null || !_activeStatuses.Contains(status))
        {
            return;
        }

        var target = GetParent() as Entities.BaseEntity;
        Status.StatusMessage statusMsg = Status.StatusMessage.Empty;

        if (target != null)
        {
            statusMsg = status.OnRemoved(target);
        }

        _activeStatuses.Remove(status);
        EmitSignal(SignalName.StatusRemoved, status.Name);

        if (!string.IsNullOrEmpty(statusMsg.Message))
        {
            EmitSignal(SignalName.StatusMessage, statusMsg.Message, statusMsg.Color);
        }
    }

    /// <summary>
    /// Removes all statuses of a specific type.
    /// Emits signals for each removed status.
    /// </summary>
    public void RemoveStatusByType(string typeId)
    {
        var statusesToRemove = _activeStatuses.Where(s => s.TypeId == typeId).ToList();
        foreach (var status in statusesToRemove)
        {
            RemoveStatus(status);
        }
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
            // Let status do per-turn processing (e.g., poison damage)
            var turnMessage = status.OnTurnProcessed(target);
            if (!string.IsNullOrEmpty(turnMessage.Message))
            {
                EmitSignal(SignalName.StatusMessage, turnMessage.Message, turnMessage.Color);
            }

            // Decrement remaining turns
            status.RemainingTurns--;

            // Mark as expired if duration reached
            if (status.RemainingTurns <= 0)
            {
                expiredStatuses.Add(status);
            }
        }

        // Remove expired statuses (RemoveStatus now emits signals internally)
        foreach (var status in expiredStatuses)
        {
            RemoveStatus(status);
        }
    }

    #endregion
}
