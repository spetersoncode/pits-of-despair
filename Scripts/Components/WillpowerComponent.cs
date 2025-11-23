using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Components;

/// <summary>
/// Component managing entity Willpower resource for skills.
/// Willpower is derived from the WIL stat and regenerates passively over time.
/// </summary>
public partial class WillpowerComponent : Node
{
    #region Signals

    /// <summary>
    /// Emitted when willpower changes (current, max).
    /// </summary>
    [Signal]
    public delegate void WillpowerChangedEventHandler(int current, int max);

    #endregion

    #region Properties

    /// <summary>
    /// Maximum willpower based on WIL stat.
    /// Formula: 10 + (WIL × 5)
    /// </summary>
    public int MaxWillpower { get; private set; }

    /// <summary>
    /// Current willpower available for skills.
    /// </summary>
    public int CurrentWillpower { get; private set; }

    /// <summary>
    /// Base regeneration rate per turn.
    /// Formula: 10 + MaxWillpower / 5
    /// At 100 points accumulated, restore 1 WP.
    /// </summary>
    public int BaseRegenRate => 10 + MaxWillpower / 5;

    #endregion

    #region Private Fields

    private BaseEntity? _entity;
    private StatsComponent? _stats;
    private TurnManager? _turnManager;
    private bool _turnSignalConnected = false;

    /// <summary>
    /// Accumulated regeneration points. At 100 points, restore 1 WP.
    /// Uses same pattern as HealthComponent for consistency.
    /// </summary>
    private int _regenPoints = 0;

    /// <summary>
    /// Multi-source max WP modifiers from skills, items, buffs, etc.
    /// Key is source identifier (e.g., "skill_arcane_focus"), value is WP bonus.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, int> _maxWPModifiers = new();

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        _stats = _entity?.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate initial MaxWillpower
        RecalculateMaxWillpower();
        CurrentWillpower = MaxWillpower;

        // Connect to stat changes to update MaxWP when WIL changes
        if (_stats != null)
        {
            _stats.Connect(StatsComponent.SignalName.StatsChanged, Callable.From(OnStatsChanged));
        }

        // Connect to turn signals for passive regeneration
        ConnectToTurnManager();
    }

    public override void _ExitTree()
    {
        // Clean up stat signal connection
        if (_stats != null && IsInstanceValid(_stats))
        {
            _stats.Disconnect(StatsComponent.SignalName.StatsChanged, Callable.From(OnStatsChanged));
        }

        // Clean up turn signal connections
        if (_turnManager != null && _turnSignalConnected && IsInstanceValid(_turnManager))
        {
            bool isPlayer = _entity is Player;
            if (isPlayer)
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

    #region Turn Manager Connection

    /// <summary>
    /// Connects to TurnManager for passive regeneration.
    /// Player entities connect to PlayerTurnStarted, creatures to CreatureTurnsStarted.
    /// </summary>
    private void ConnectToTurnManager()
    {
        if (_turnSignalConnected)
            return;

        // Find TurnManager by traversing up to GameLevel
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
            // TurnManager not found - entity may be outside game context
            return;
        }

        // Connect to appropriate turn signal based on entity type
        bool isPlayer = _entity is Player;
        if (isPlayer)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }
        else
        {
            _turnManager.Connect(TurnManager.SignalName.CreatureTurnsStarted, Callable.From(OnTurnStarted));
        }

        _turnSignalConnected = true;
    }

    #endregion

    #region Regeneration

    /// <summary>
    /// Called at the start of each turn. Processes passive regeneration.
    /// </summary>
    private void OnTurnStarted()
    {
        ProcessRegeneration();
    }

    /// <summary>
    /// Processes passive regeneration using accumulating points.
    /// Each turn adds BaseRegenRate to accumulated points.
    /// At 100 points, restores 1 WP (excess carries over).
    /// </summary>
    private void ProcessRegeneration()
    {
        // Don't regenerate if at full willpower
        if (CurrentWillpower >= MaxWillpower)
        {
            // Reset regen points when full to avoid stockpiling
            _regenPoints = 0;
            return;
        }

        // Accumulate regeneration points
        _regenPoints += BaseRegenRate;

        // Restore 1 WP for every 100 points accumulated
        while (_regenPoints >= 100 && CurrentWillpower < MaxWillpower)
        {
            RestoreWillpower(1);
            _regenPoints -= 100;
        }
    }

    #endregion

    #region Stat Updates

    /// <summary>
    /// Called when stats change. Recalculates MaxWillpower based on new WIL value.
    /// </summary>
    private void OnStatsChanged()
    {
        RecalculateMaxWillpower();
    }

    /// <summary>
    /// Recalculates MaxWillpower based on WIL stat and external modifiers.
    /// Formula: 10 + (WIL × 5) + modifiers
    /// </summary>
    private void RecalculateMaxWillpower()
    {
        int wil = _stats?.TotalWill ?? 0;
        int modifierBonus = GetTotalMaxWPModifiers();
        int newMax = 10 + (wil * 5) + modifierBonus;

        // Ensure minimum of 10
        if (newMax < 10)
            newMax = 10;

        int oldMax = MaxWillpower;
        MaxWillpower = newMax;

        // If max increased, add the difference to current (like HP behavior)
        if (newMax > oldMax && oldMax > 0)
        {
            CurrentWillpower += (newMax - oldMax);
        }

        // Clamp current to new max
        if (CurrentWillpower > MaxWillpower)
        {
            CurrentWillpower = MaxWillpower;
        }

        EmitSignal(SignalName.WillpowerChanged, CurrentWillpower, MaxWillpower);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to spend willpower for a skill.
    /// </summary>
    /// <param name="amount">Amount of WP to spend</param>
    /// <returns>True if successful, false if insufficient WP</returns>
    public bool SpendWillpower(int amount)
    {
        if (amount <= 0)
            return true;

        if (CurrentWillpower < amount)
            return false;

        CurrentWillpower -= amount;
        EmitSignal(SignalName.WillpowerChanged, CurrentWillpower, MaxWillpower);
        return true;
    }

    /// <summary>
    /// Restores willpower, clamped to maximum.
    /// </summary>
    /// <param name="amount">Amount of WP to restore</param>
    public void RestoreWillpower(int amount)
    {
        if (amount <= 0 || CurrentWillpower >= MaxWillpower)
            return;

        CurrentWillpower = Mathf.Min(MaxWillpower, CurrentWillpower + amount);
        EmitSignal(SignalName.WillpowerChanged, CurrentWillpower, MaxWillpower);
    }

    /// <summary>
    /// Fully restores willpower to maximum.
    /// Called on floor transitions.
    /// </summary>
    public void FullRestore()
    {
        if (CurrentWillpower == MaxWillpower)
            return;

        CurrentWillpower = MaxWillpower;
        _regenPoints = 0;
        EmitSignal(SignalName.WillpowerChanged, CurrentWillpower, MaxWillpower);
    }

    /// <summary>
    /// Sets current willpower to a specific value (for loading/debug).
    /// </summary>
    /// <param name="amount">New current WP value</param>
    public void SetWillpower(int amount)
    {
        CurrentWillpower = Mathf.Clamp(amount, 0, MaxWillpower);
        EmitSignal(SignalName.WillpowerChanged, CurrentWillpower, MaxWillpower);
    }

    /// <summary>
    /// Gets the parent entity.
    /// </summary>
    public BaseEntity? GetEntity()
    {
        return _entity;
    }

    #endregion

    #region Max WP Modifiers

    /// <summary>
    /// Adds a max WP modifier from a named source.
    /// Used by passive skills, items, buffs, etc.
    /// </summary>
    /// <param name="source">Source identifier (e.g., "skill_arcane_focus")</param>
    /// <param name="value">WP bonus value (can be positive or negative)</param>
    public void AddMaxWPModifier(string source, int value)
    {
        _maxWPModifiers[source] = value;
        RecalculateMaxWillpower();
    }

    /// <summary>
    /// Removes a max WP modifier by source name.
    /// </summary>
    /// <param name="source">Source identifier to remove</param>
    public void RemoveMaxWPModifier(string source)
    {
        if (_maxWPModifiers.Remove(source))
        {
            RecalculateMaxWillpower();
        }
    }

    /// <summary>
    /// Gets the total max WP bonus from all modifier sources.
    /// </summary>
    private int GetTotalMaxWPModifiers()
    {
        int total = 0;
        foreach (var value in _maxWPModifiers.Values)
        {
            total += value;
        }
        return total;
    }

    #endregion
}
