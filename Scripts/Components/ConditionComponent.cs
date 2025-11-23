using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Actions;
using PitsOfDespair.Conditions;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Components;

/// <summary>
/// Manages active conditions on an entity.
/// Tracks buffs, debuffs, and other temporary conditions with duration.
/// </summary>
public partial class ConditionComponent : Node
{
    #region Signals

    /// <summary>
    /// Emitted when a condition is added to this entity.
    /// </summary>
    [Signal]
    public delegate void ConditionAddedEventHandler(string conditionName);

    /// <summary>
    /// Emitted when a condition is removed from this entity.
    /// </summary>
    [Signal]
    public delegate void ConditionRemovedEventHandler(string conditionName);

    /// <summary>
    /// Emitted when a condition message needs to be displayed.
    /// </summary>
    [Signal]
    public delegate void ConditionMessageEventHandler(string message, string color);

    #endregion

    #region Properties

    /// <summary>
    /// Whether this component is attached to a player-controlled entity.
    /// Player conditions process on PlayerTurnStarted, creature conditions on CreatureTurnsStarted.
    /// </summary>
    [Export] public bool IsPlayerControlled { get; set; } = false;

    /// <summary>
    /// List of currently active conditions.
    /// </summary>
    private readonly List<Condition> _activeConditions = new();

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

        // Find TurnManager by traversing up to GameLevel and searching for TurnManager
        Node current = this;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                _turnManager = current.GetNodeOrNull<TurnManager>("TurnManager");
                break;
            }
            current = current.GetParent();
        }

        if (_turnManager == null)
        {
            GD.PrintErr("ConditionComponent: TurnManager not found. Unable to connect to turn signals.");
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

    #region Condition Management

    /// <summary>
    /// Adds a condition to this entity.
    /// If a condition with the same TypeId already exists, refreshes its duration instead of stacking.
    /// Emits a signal with the condition message and color.
    /// </summary>
    public void AddCondition(Condition condition)
    {
        if (condition == null)
        {
            GD.PrintErr("ConditionComponent: Attempted to add null condition");
            return;
        }

        // Check if this condition type is already active
        var existingCondition = _activeConditions.FirstOrDefault(c => c.TypeId == condition.TypeId);
        if (existingCondition != null)
        {
            // Refresh duration instead of stacking
            var resolvedDuration = condition.ResolveDuration();
            existingCondition.RefreshDuration(resolvedDuration);
            var refreshMessage = $"{condition.Name} duration refreshed to {resolvedDuration} turns.";
            EmitSignal(SignalName.ConditionMessage, refreshMessage, Core.Palette.ToHex(Core.Palette.StatusNeutral));
            return;
        }

        // Add new condition
        _activeConditions.Add(condition);
        condition.RemainingTurns = condition.ResolveDuration();

        // Apply condition effects and get message
        var target = GetParent() as Entities.BaseEntity;
        if (target != null)
        {
            var conditionMsg = condition.OnApplied(target);
            EmitSignal(SignalName.ConditionAdded, condition.Name);
            if (!string.IsNullOrEmpty(conditionMsg.Message))
            {
                EmitSignal(SignalName.ConditionMessage, conditionMsg.Message, conditionMsg.Color);
            }
        }
    }

    /// <summary>
    /// Removes a specific condition from this entity.
    /// Emits a signal with the removal message and color.
    /// </summary>
    public void RemoveCondition(Condition condition)
    {
        if (condition == null || !_activeConditions.Contains(condition))
        {
            return;
        }

        var target = GetParent() as Entities.BaseEntity;
        Conditions.ConditionMessage conditionMsg = Conditions.ConditionMessage.Empty;

        if (target != null)
        {
            conditionMsg = condition.OnRemoved(target);
        }

        _activeConditions.Remove(condition);
        EmitSignal(SignalName.ConditionRemoved, condition.Name);

        if (!string.IsNullOrEmpty(conditionMsg.Message))
        {
            EmitSignal(SignalName.ConditionMessage, conditionMsg.Message, conditionMsg.Color);
        }
    }

    /// <summary>
    /// Removes all conditions of a specific type.
    /// Emits signals for each removed condition.
    /// </summary>
    public void RemoveConditionByType(string typeId)
    {
        var conditionsToRemove = _activeConditions.Where(c => c.TypeId == typeId).ToList();
        foreach (var condition in conditionsToRemove)
        {
            RemoveCondition(condition);
        }
    }

    /// <summary>
    /// Gets all currently active conditions.
    /// </summary>
    public IReadOnlyList<Condition> GetActiveConditions()
    {
        return _activeConditions.AsReadOnly();
    }

    /// <summary>
    /// Checks if a condition of the given type is currently active.
    /// </summary>
    public bool HasCondition(string typeId)
    {
        return _activeConditions.Any(c => c.TypeId == typeId);
    }

    #endregion

    #region Turn Processing

    /// <summary>
    /// Called at the start of each turn for this entity.
    /// Processes all active conditions and removes expired ones.
    /// </summary>
    private void OnTurnStarted()
    {
        var target = GetParent() as Entities.BaseEntity;
        if (target == null)
        {
            GD.PrintErr("ConditionComponent.OnTurnStarted: target is null!");
            return;
        }

        // Process each condition and collect expired ones
        var expiredConditions = new List<Condition>();

        foreach (var condition in _activeConditions)
        {
            // Let condition do per-turn processing (e.g., poison damage)
            var turnMessage = condition.OnTurnProcessed(target);
            if (!string.IsNullOrEmpty(turnMessage.Message))
            {
                EmitSignal(SignalName.ConditionMessage, turnMessage.Message, turnMessage.Color);
            }

            // Decrement remaining turns
            condition.RemainingTurns--;

            // Mark as expired if duration reached
            if (condition.RemainingTurns <= 0)
            {
                expiredConditions.Add(condition);
            }
        }

        // Remove expired conditions (RemoveCondition now emits signals internally)
        foreach (var condition in expiredConditions)
        {
            RemoveCondition(condition);
        }
    }

    #endregion
}
