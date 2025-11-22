using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems.Input.Processors;
using PitsOfDespair.Systems.Input.Services;
using PitsOfDespair.UI;

namespace PitsOfDespair.Systems.Input;

/// <summary>
/// Orchestrates input handling by delegating to specialized processors.
/// Manages game state and routes input to the appropriate processor based on context.
/// </summary>
public partial class InputHandler : Node
{
    // Signals for UI coordination
    [Signal]
    public delegate void InventoryToggleRequestedEventHandler();

    [Signal]
    public delegate void HelpRequestedEventHandler();

    [Signal]
    public delegate void ActivateItemRequestedEventHandler();

    [Signal]
    public delegate void DropItemRequestedEventHandler();

    [Signal]
    public delegate void EquipMenuRequestedEventHandler();

    [Signal]
    public delegate void DebugModeToggledEventHandler();

    [Signal]
    public delegate void DebugConsoleRequestedEventHandler();

    // Core dependencies
    private Player _player;
    private TurnManager _turnManager;
    private ActionContext _actionContext;
    private GameHUD _gameHUD;
    private CursorTargetingSystem _cursorSystem;
    private AutoExploreSystem _autoExploreSystem;

    // Input processors
    private readonly KeybindingService _keybindingService = KeybindingService.Instance;
    private readonly GameplayInputProcessor _gameplayProcessor;
    private readonly CursorInputProcessor _cursorProcessor;

    public InputHandler()
    {
        _gameplayProcessor = new GameplayInputProcessor(_keybindingService);
        _cursorProcessor = new CursorInputProcessor(_keybindingService);
    }

    public void SetPlayer(Player player)
    {
        // Disconnect from old player
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.TurnCompleted, Callable.From(OnPlayerTurnCompleted));
        }

        _player = player;
        _gameplayProcessor.SetPlayer(player);

        // Connect to new player
        if (_player != null)
        {
            _player.Connect(Player.SignalName.TurnCompleted, Callable.From(OnPlayerTurnCompleted));
        }
    }

    public void SetTurnManager(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }

    public void SetActionContext(ActionContext actionContext)
    {
        _actionContext = actionContext;
        _gameplayProcessor.SetActionContext(actionContext);
    }

    public void SetGameHUD(GameHUD gameHUD)
    {
        _gameHUD = gameHUD;
        _gameplayProcessor.SetGameHUD(gameHUD);

        // Connect to item targeting signals
        if (_gameHUD != null)
        {
            _gameHUD.Connect(GameHUD.SignalName.StartItemTargeting, Callable.From<char>(OnStartItemTargeting));
            _gameHUD.Connect(GameHUD.SignalName.StartReachAttackTargeting, Callable.From<char>(OnStartReachAttackTargeting));
        }
    }

    public void SetCursorTargetingSystem(CursorTargetingSystem cursorSystem)
    {
        _cursorSystem = cursorSystem;
        _gameplayProcessor.SetCursorTargetingSystem(cursorSystem);
        _cursorProcessor.SetCursorTargetingSystem(cursorSystem);

        // Connect to cursor signals
        if (_cursorSystem != null)
        {
            _cursorSystem.Connect(CursorTargetingSystem.SignalName.TargetConfirmed, Callable.From<Vector2I>(OnTargetConfirmed));
            _cursorSystem.Connect(CursorTargetingSystem.SignalName.CursorCanceled, Callable.From<int>(OnCursorCanceled));
            _cursorSystem.Connect(CursorTargetingSystem.SignalName.EntityExamined, Callable.From<BaseEntity>(OnEntityExamined));
        }
    }

    public void SetAutoExploreSystem(AutoExploreSystem autoExploreSystem)
    {
        _autoExploreSystem = autoExploreSystem;
    }

    public override void _ExitTree()
    {
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.TurnCompleted, Callable.From(OnPlayerTurnCompleted));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_player == null || _turnManager == null || _actionContext == null)
            return;

        // Only process key presses, not releases or repeats
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            return;

        // If entity detail modal is open, let it handle all input (only ESC closes it)
        if (_gameHUD != null && _gameHUD.IsEntityDetailModalOpen())
            return;

        // If autoexplore is active, any keypress cancels it (except the one that starts a new autoexplore)
        if (_autoExploreSystem != null && _autoExploreSystem.IsActive)
        {
            // Check if this is the autoexplore key - if so, let it toggle off naturally
            if (_keybindingService.TryGetAction(keyEvent, out var action) && action == InputAction.AutoExplore)
            {
                _autoExploreSystem.Stop();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Any other key cancels autoexplore
            _autoExploreSystem.OnAnyKeyPressed();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Route input to appropriate processor based on game state
        if (ProcessCursorInput(keyEvent)) return;
        if (ProcessSystemInput(keyEvent)) return;
        if (ProcessMenuInput(keyEvent)) return;
        if (ProcessGameplayInput(keyEvent)) return;
    }

    /// <summary>
    /// Processes cursor targeting input (highest priority when active).
    /// </summary>
    private bool ProcessCursorInput(InputEventKey keyEvent)
    {
        if (_cursorSystem != null && _cursorSystem.IsActive)
        {
            if (_cursorProcessor.ProcessInput(keyEvent))
            {
                GetViewport().SetInputAsHandled();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Processes system-level input (debug, help) that works anytime.
    /// </summary>
    private bool ProcessSystemInput(InputEventKey keyEvent)
    {
        if (!_keybindingService.TryGetAction(keyEvent, out var action))
            return false;

        switch (action)
        {
            case InputAction.ToggleDebug:
                EmitSignal(SignalName.DebugModeToggled);
                GetViewport().SetInputAsHandled();
                return true;

            case InputAction.ToggleDebugConsole:
                EmitSignal(SignalName.DebugConsoleRequested);
                GetViewport().SetInputAsHandled();
                return true;

            case InputAction.ToggleHelp:
                EmitSignal(SignalName.HelpRequested);
                GetViewport().SetInputAsHandled();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Processes menu toggle input (works anytime, except when menus are open).
    /// </summary>
    private bool ProcessMenuInput(InputEventKey keyEvent)
    {
        // Don't process menu toggles if a menu is already open
        // (let menus handle letter key selection)
        if (_gameHUD != null && _gameHUD.IsAnyMenuOpen())
            return false;

        if (!_keybindingService.TryGetAction(keyEvent, out var action))
            return false;

        switch (action)
        {
            case InputAction.ToggleInventory:
                EmitSignal(SignalName.InventoryToggleRequested);
                GetViewport().SetInputAsHandled();
                return true;

            case InputAction.ToggleActivate:
                EmitSignal(SignalName.ActivateItemRequested);
                GetViewport().SetInputAsHandled();
                return true;

            case InputAction.ToggleDrop:
                EmitSignal(SignalName.DropItemRequested);
                GetViewport().SetInputAsHandled();
                return true;

            case InputAction.ToggleEquip:
                EmitSignal(SignalName.EquipMenuRequested);
                GetViewport().SetInputAsHandled();
                return true;

            case InputAction.ToggleExamine:
                if (_cursorSystem != null && _player != null && _gameHUD != null)
                {
                    // Close any open menus before entering examine mode
                    if (_gameHUD.IsAnyMenuOpen())
                    {
                        _gameHUD.CloseAllMenus();
                    }
                    _cursorSystem.StartExamine(_player.GridPosition);
                }
                GetViewport().SetInputAsHandled();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Processes gameplay input (movement, combat, items) during player turn.
    /// </summary>
    private bool ProcessGameplayInput(InputEventKey keyEvent)
    {
        // Don't process gameplay input if any menu/modal is open
        // (let them handle input, including text entry)
        if (_gameHUD != null && _gameHUD.IsAnyMenuOpen())
            return false;

        // Only process gameplay actions during player turn
        if (!_turnManager.IsPlayerTurn)
            return false;

        // Check for autoexplore action
        if (_keybindingService.TryGetAction(keyEvent, out var action) && action == InputAction.AutoExplore)
        {
            if (_autoExploreSystem != null)
            {
                _autoExploreSystem.Start();
                GetViewport().SetInputAsHandled();
                return true;
            }
        }

        if (_gameplayProcessor.ProcessInput(keyEvent))
        {
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    // Signal handlers

    private void OnPlayerTurnCompleted()
    {
        _turnManager?.EndPlayerTurn();
    }

    private void OnStartItemTargeting(char itemKey)
    {
        _gameplayProcessor.StartItemTargeting(itemKey);
    }

    private void OnStartReachAttackTargeting(char itemKey)
    {
        _gameplayProcessor.StartReachAttackTargeting(itemKey);
    }

    private void OnTargetConfirmed(Vector2I targetPosition)
    {
        // Exit targeting mode in UI
        if (_gameHUD != null)
        {
            _gameHUD.ExitTargetingMode();
        }

        // Execute action
        _gameplayProcessor.OnTargetConfirmed(targetPosition);
    }

    private void OnCursorCanceled(int mode)
    {
        var targetingMode = (CursorTargetingSystem.TargetingMode)mode;

        // Exit targeting mode in UI if it was an action mode
        if (targetingMode != CursorTargetingSystem.TargetingMode.Examine && _gameHUD != null)
        {
            _gameHUD.ExitTargetingMode();
            _gameplayProcessor.OnTargetCanceled();
        }
    }

    private void OnEntityExamined(BaseEntity entity)
    {
        if (_gameHUD != null)
        {
            _gameHUD.ShowEntityDetail(entity);
        }
    }
}
