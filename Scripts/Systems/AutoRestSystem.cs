using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages automatic resting for the player.
/// Repeatedly executes WaitAction until HP or WP is restored, or interrupted.
/// Stops on: enemy visible, HP/WP filled (that wasn't full at start), any keypress.
/// </summary>
public partial class AutoRestSystem : Node
{
    /// <summary>
    /// Emitted when auto-rest starts.
    /// </summary>
    [Signal]
    public delegate void AutoRestStartedEventHandler();

    /// <summary>
    /// Emitted when auto-rest completes (HP/WP restored).
    /// </summary>
    [Signal]
    public delegate void AutoRestCompleteEventHandler();

    private Player _player;
    private EntityManager _entityManager;
    private PlayerVisionSystem _visionSystem;
    private TurnManager _turnManager;
    private ActionContext _actionContext;

    private bool _isActive = false;

    // Track which bars need filling at start
    private bool _needsHealthRestore = false;
    private bool _needsWillpowerRestore = false;

    /// <summary>
    /// Whether auto-rest is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Initializes the auto-rest system with required dependencies.
    /// </summary>
    public void Initialize(
        Player player,
        EntityManager entityManager,
        PlayerVisionSystem visionSystem,
        TurnManager turnManager,
        ActionContext actionContext)
    {
        _player = player;
        _entityManager = entityManager;
        _visionSystem = visionSystem;
        _turnManager = turnManager;
        _actionContext = actionContext;

        // Connect to turn manager to take actions each turn
        _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnPlayerTurnStarted));
    }

    /// <summary>
    /// Starts auto-rest mode.
    /// </summary>
    public void Start()
    {
        if (_isActive)
            return;

        // Check for immediate interrupts before starting - don't start if enemies visible
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            return;
        }

        // Check if there's anything to restore
        var health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
        var willpower = _player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        bool healthFull = health == null || health.CurrentHealth >= health.MaxHealth;
        bool willpowerFull = willpower == null || willpower.CurrentWillpower >= willpower.MaxWillpower;

        if (healthFull && willpowerFull)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, "You are already fully rested.", Palette.ToHex(Palette.Default));
            return;
        }

        // Record which bars need filling (not full at start)
        _needsHealthRestore = !healthFull;
        _needsWillpowerRestore = !willpowerFull;

        _isActive = true;
        EmitSignal(SignalName.AutoRestStarted);

        // If it's already the player's turn, take the first step
        if (_turnManager.IsPlayerTurn)
        {
            TakeRestStep();
        }
    }

    /// <summary>
    /// Stops auto-rest mode.
    /// </summary>
    public void Stop()
    {
        if (!_isActive)
            return;

        _isActive = false;
        _needsHealthRestore = false;
        _needsWillpowerRestore = false;
    }

    /// <summary>
    /// Called when the player's turn starts. Takes the next auto-rest step if active.
    /// </summary>
    private void OnPlayerTurnStarted()
    {
        if (!_isActive)
            return;

        // Check for interrupts each turn
        var visibleEnemy = GetVisibleEnemy();
        if (visibleEnemy != null)
        {
            _actionContext.CombatSystem.EmitActionMessage(_player, $"You spotted a {visibleEnemy.DisplayName}.", Palette.ToHex(Palette.Caution));
            Stop();
            return;
        }

        // Check if any bar that needed filling is now full
        if (CheckRestComplete())
        {
            EmitSignal(SignalName.AutoRestComplete);
            Stop();
            return;
        }

        TakeRestStep();
    }

    /// <summary>
    /// Takes a rest step by executing a WaitAction.
    /// </summary>
    private void TakeRestStep()
    {
        var waitAction = new WaitAction();
        _player.ExecuteAction(waitAction, _actionContext);
    }

    /// <summary>
    /// Checks if rest is complete (any bar that needed filling is now full).
    /// </summary>
    private bool CheckRestComplete()
    {
        var health = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
        var willpower = _player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        // If we needed health and it's now full, we're done
        if (_needsHealthRestore && health != null && health.CurrentHealth >= health.MaxHealth)
        {
            return true;
        }

        // If we needed willpower and it's now full, we're done
        if (_needsWillpowerRestore && willpower != null && willpower.CurrentWillpower >= willpower.MaxWillpower)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if any hostile creatures are visible to the player.
    /// Returns the first visible hostile entity found, or null if none.
    /// Only considers actual creatures (non-walkable entities), not items.
    /// </summary>
    private BaseEntity? GetVisibleEnemy()
    {
        foreach (var entity in _entityManager.GetAllEntities())
        {
            // Skip walkable entities (items, stairs, etc.) - not threats
            if (entity.IsWalkable)
                continue;

            // Skip non-hostile entities
            if (!Faction.Player.IsHostileTo(entity.Faction))
                continue;

            // Skip dead entities
            if (entity.IsDead)
                continue;

            // Check if visible
            if (_visionSystem.IsVisible(entity.GridPosition))
            {
                return entity;
            }
        }
        return null;
    }

    /// <summary>
    /// Cancels auto-rest if any key is pressed.
    /// Called by InputHandler when auto-rest is active.
    /// </summary>
    public void OnAnyKeyPressed()
    {
        if (_isActive)
        {
            Stop();
        }
    }

    public override void _ExitTree()
    {
        if (_turnManager != null)
        {
            _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnPlayerTurnStarted));
        }
    }
}
