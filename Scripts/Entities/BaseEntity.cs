using Godot;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PitsOfDespair.Actions;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Entities;

/// <summary>
/// Base class for all entities in the game (player, monsters, NPCs, etc.).
/// Entities are composed of child node components for behavior.
/// </summary>
public partial class BaseEntity : Node2D
{
    /// <summary>
    /// Emitted when the entity's grid position changes.
    /// Parameters: x (int), y (int)
    /// </summary>
    [Signal]
    public delegate void PositionChangedEventHandler(int x, int y);

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

    /// <summary>
    /// Current position on the game grid.
    /// </summary>
    public GridPosition GridPosition { get; set; }

    /// <summary>
    /// Display name of this entity (e.g., "Rat", "Goblin", "Player").
    /// </summary>
    public string DisplayName { get; set; } = "Unknown";

    /// <summary>
    /// Atmospheric description of this entity.
    /// Used for examine command and entity details.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    private string _glyph = "?";

    /// <summary>
    /// Character or symbol representing this entity (supports Unicode).
    /// Must be a single character (grapheme cluster).
    /// </summary>
    public string Glyph
    {
        get => _glyph;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                GD.PushWarning($"BaseEntity: Attempted to set empty glyph for '{DisplayName}', using '?'");
                _glyph = "?";
                return;
            }

            // Use StringInfo to count grapheme clusters (user-perceived characters)
            var textElementEnumerator = System.Globalization.StringInfo.GetTextElementEnumerator(value);
            int count = 0;
            while (textElementEnumerator.MoveNext())
                count++;

            if (count != 1)
            {
                GD.PushWarning($"BaseEntity: Glyph for '{DisplayName}' must be a single character, got '{value}' ({count} characters), using first character");
                textElementEnumerator.Reset();
                textElementEnumerator.MoveNext();
                _glyph = textElementEnumerator.GetTextElement();
            }
            else
            {
                _glyph = value;
            }
        }
    }

    /// <summary>
    /// Color to render the glyph.
    /// </summary>
    public Color GlyphColor { get; set; } = Palette.Default;

    /// <summary>
    /// Whether other entities can move through this entity.
    /// True for items, false for creatures.
    /// </summary>
    public bool IsWalkable { get; set; } = false;

    /// <summary>
    /// Entity we last swapped positions with. Prevents ping-pong swapping.
    /// Cleared when we move without swapping.
    /// </summary>
    public BaseEntity LastSwappedWith { get; set; } = null;

    /// <summary>
    /// Item data if this entity is a collectible item.
    /// Null for non-item entities (creatures, player, etc.).
    /// </summary>
    public ItemInstance? ItemData { get; set; } = null;

    /// <summary>
    /// Faction allegiance of this entity.
    /// Determines combat targeting and AI behavior.
    /// Player is always Friendly faction.
    /// </summary>
    public Faction Faction { get; set; } = Faction.Hostile;

    /// <summary>
    /// The creature data file ID used to create this entity (e.g., "cat", "goblin").
    /// Used for recreating companions across floor transitions.
    /// Empty string for non-creature entities (player, items).
    /// </summary>
    public string CreatureId { get; set; } = string.Empty;

    #region Condition System

    /// <summary>
    /// Whether this entity is player-controlled.
    /// Player conditions process on PlayerTurnEnded, creature conditions on CreatureTurnsEnded.
    /// </summary>
    public bool IsPlayerControlled { get; set; } = false;

    /// <summary>
    /// List of currently active conditions.
    /// </summary>
    private readonly List<Condition> _activeConditions = new();

    /// <summary>
    /// Cached reference to turn manager for condition processing.
    /// </summary>
    private TurnManager? _turnManager;

    /// <summary>
    /// Tracks whether turn signals are connected.
    /// </summary>
    private bool _conditionSignalsConnected = false;

    #endregion

    /// <summary>
    /// Whether this entity is dead.
    /// Checks HealthComponent if present, otherwise returns false.
    /// </summary>
    public bool IsDead
    {
        get
        {
            var health = GetNodeOrNull<HealthComponent>("HealthComponent");
            return health != null && !health.IsAlive();
        }
    }

    /// <summary>
    /// Updates the entity's grid position and emits PositionChanged signal.
    /// </summary>
    /// <param name="newPosition">The new grid position.</param>
    public void SetGridPosition(GridPosition newPosition)
    {
        GridPosition = newPosition;
        EmitSignal(SignalName.PositionChanged, newPosition.X, newPosition.Y);
    }

    /// <summary>
    /// Execute an action using the action system.
    /// Can be overridden by subclasses to add additional behavior (e.g., Player emits TurnCompleted).
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">The action context containing game systems and state.</param>
    /// <returns>The result of the action execution.</returns>
    public virtual ActionResult ExecuteAction(Action action, ActionContext context)
    {
        return action.Execute(this, context);
    }

    /// <summary>
    /// Fires an AI event to all child components that implement IAIEventHandler.
    /// Components can respond by adding actions to the event args or setting Handled = true.
    /// </summary>
    /// <param name="eventName">The event name from AIEvents constants.</param>
    /// <param name="args">The event arguments containing ActionList and context.</param>
    public void FireEvent(string eventName, GetActionsEventArgs args)
    {
        foreach (var child in GetChildren())
        {
            if (child is IAIEventHandler handler)
            {
                handler.HandleAIEvent(eventName, args);
                if (args.Handled)
                {
                    break;
                }
            }
        }
    }

    #region Lifecycle

    public override void _Ready()
    {
        // Use deferred initialization to ensure the scene tree is fully set up
        CallDeferred(nameof(InitializeConditionSystem));
    }

    /// <summary>
    /// Deferred initialization for condition system.
    /// Finds TurnManager and connects to appropriate turn signal.
    /// </summary>
    private void InitializeConditionSystem()
    {
        if (_conditionSignalsConnected)
        {
            return;
        }

        // Find TurnManager by traversing up to GameLevel
        Node? current = this;
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
            // Not an error - items and other non-combat entities don't need turn processing
            return;
        }

        // Connect to appropriate turn signal based on entity type
        // Process conditions at END of turn so duration 1 effects last through the turn they're applied
        if (IsPlayerControlled)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnEnded, Callable.From(OnConditionTurnEnded));
        }
        else
        {
            _turnManager.Connect(TurnManager.SignalName.CreatureTurnsEnded, Callable.From(OnConditionTurnEnded));
        }

        _conditionSignalsConnected = true;
    }

    public override void _ExitTree()
    {
        // Clean up signal connections
        if (_turnManager != null && _conditionSignalsConnected)
        {
            if (IsPlayerControlled)
            {
                _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnEnded, Callable.From(OnConditionTurnEnded));
            }
            else
            {
                _turnManager.Disconnect(TurnManager.SignalName.CreatureTurnsEnded, Callable.From(OnConditionTurnEnded));
            }
            _conditionSignalsConnected = false;
        }
    }

    #endregion

    #region Condition Management

    /// <summary>
    /// Adds a condition to this entity.
    /// If a condition with the same TypeId already exists, refreshes its duration instead of stacking.
    /// </summary>
    public void AddCondition(Condition condition)
    {
        if (condition == null)
        {
            GD.PrintErr("BaseEntity: Attempted to add null condition");
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
            EmitSignal(SignalName.ConditionMessage, refreshMessage, Palette.ToHex(Palette.StatusNeutral));
            return;
        }

        // Add new condition
        _activeConditions.Add(condition);
        condition.RemainingTurns = condition.ResolveDuration();

        // Apply condition effects and get message
        var conditionMsg = condition.OnApplied(this);
        EmitSignal(SignalName.ConditionAdded, condition.Name);
        if (!string.IsNullOrEmpty(conditionMsg.Message))
        {
            EmitSignal(SignalName.ConditionMessage, conditionMsg.Message, conditionMsg.Color);
        }
    }

    /// <summary>
    /// Adds a condition that has already had OnApplied called.
    /// Used when the caller needs to capture the OnApplied message for custom routing.
    /// </summary>
    public void AddConditionWithoutMessage(Condition condition)
    {
        if (condition == null)
        {
            GD.PrintErr("BaseEntity: Attempted to add null condition");
            return;
        }

        // Check if this condition type is already active
        var existingCondition = _activeConditions.FirstOrDefault(c => c.TypeId == condition.TypeId);
        if (existingCondition != null)
        {
            // Refresh duration instead of stacking
            var resolvedDuration = condition.ResolveDuration();
            existingCondition.RefreshDuration(resolvedDuration);
            // Note: OnApplied was already called, so we don't emit refresh message here
            return;
        }

        // Add new condition (OnApplied already called by caller)
        _activeConditions.Add(condition);
        condition.RemainingTurns = condition.ResolveDuration();
        EmitSignal(SignalName.ConditionAdded, condition.Name);
    }

    /// <summary>
    /// Removes a specific condition from this entity.
    /// </summary>
    public void RemoveCondition(Condition condition)
    {
        if (condition == null || !_activeConditions.Contains(condition))
        {
            return;
        }

        var conditionMsg = condition.OnRemoved(this);

        _activeConditions.Remove(condition);
        EmitSignal(SignalName.ConditionRemoved, condition.Name);

        if (!string.IsNullOrEmpty(conditionMsg.Message))
        {
            EmitSignal(SignalName.ConditionMessage, conditionMsg.Message, conditionMsg.Color);
        }
    }

    /// <summary>
    /// Removes all conditions of a specific type.
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

    /// <summary>
    /// Removes all conditions whose SourceId starts with the given prefix.
    /// Used for bulk removal (e.g., removing all equipment conditions on unequip).
    /// </summary>
    public void RemoveConditionsBySource(string sourcePrefix)
    {
        var conditionsToRemove = _activeConditions
            .Where(c => c.SourceId != null && c.SourceId.StartsWith(sourcePrefix))
            .ToList();

        foreach (var condition in conditionsToRemove)
        {
            RemoveCondition(condition);
        }
    }

    /// <summary>
    /// Checks if any condition from the given source is currently active.
    /// </summary>
    public bool HasConditionFromSource(string sourcePrefix)
    {
        return _activeConditions.Any(c => c.SourceId != null && c.SourceId.StartsWith(sourcePrefix));
    }

    /// <summary>
    /// Gets all conditions from a specific source.
    /// </summary>
    public IEnumerable<Condition> GetConditionsBySource(string sourcePrefix)
    {
        return _activeConditions.Where(c => c.SourceId != null && c.SourceId.StartsWith(sourcePrefix));
    }

    #endregion

    #region Turn Processing

    /// <summary>
    /// Called at the end of each turn for this entity.
    /// Processes all active conditions and removes expired ones.
    /// Processing at turn end ensures duration 1 effects last through the turn they're applied.
    /// </summary>
    private void OnConditionTurnEnded()
    {
        // Process each condition and collect expired ones
        var expiredConditions = new List<Condition>();

        foreach (var condition in _activeConditions)
        {
            // Let condition do per-turn processing (e.g., poison damage, regen)
            var turnMessage = condition.OnTurnProcessed(this);
            if (!string.IsNullOrEmpty(turnMessage.Message))
            {
                EmitSignal(SignalName.ConditionMessage, turnMessage.Message, turnMessage.Color);
            }

            // Only decrement and expire Temporary conditions
            if (condition.DurationMode == ConditionDuration.Temporary)
            {
                condition.RemainingTurns--;

                if (condition.RemainingTurns <= 0)
                {
                    expiredConditions.Add(condition);
                }
            }
            // Permanent, WhileEquipped, and WhileActive conditions never auto-expire
        }

        // Remove expired conditions
        foreach (var condition in expiredConditions)
        {
            RemoveCondition(condition);
        }
    }

    #endregion
}
