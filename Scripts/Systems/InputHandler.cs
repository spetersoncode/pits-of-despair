using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using PitsOfDespair.UI;

namespace PitsOfDespair.Systems;

/// <summary>
/// Handles turn-based player input for movement and inventory.
/// Converts input to actions and executes them through the action system.
/// </summary>
public partial class InputHandler : Node
{
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

    private Player _player;
    private TurnManager _turnManager;
    private ActionContext _actionContext;
    private GameHUD _gameHUD;
    private PlayerVisionSystem _visionSystem;
    private TargetingSystem _targetingSystem;
    private char? _pendingItemKey = null;
    private bool _isReachAttack = false;

    /// <summary>
    /// Sets the player to control.
    /// </summary>
    public void SetPlayer(Player player)
    {
        // Disconnect from old player if exists
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.TurnCompleted, Callable.From(OnPlayerTurnCompleted));
        }

        _player = player;

        // Connect to new player's turn completion
        if (_player != null)
        {
            _player.Connect(Player.SignalName.TurnCompleted, Callable.From(OnPlayerTurnCompleted));
        }
    }

    /// <summary>
    /// Sets the turn manager to coordinate turn flow.
    /// </summary>
    public void SetTurnManager(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }

    /// <summary>
    /// Sets the action context for action execution.
    /// </summary>
    public void SetActionContext(ActionContext actionContext)
    {
        _actionContext = actionContext;
    }

    /// <summary>
    /// Sets the GameHUD reference for menu state checking.
    /// </summary>
    public void SetGameHUD(GameHUD gameHUD)
    {
        _gameHUD = gameHUD;

        // Connect to item targeting signal
        if (_gameHUD != null)
        {
            _gameHUD.Connect(GameHUD.SignalName.StartItemTargeting, Callable.From<char>(OnStartItemTargeting));
            _gameHUD.Connect(GameHUD.SignalName.StartReachAttackTargeting, Callable.From<char>(OnStartReachAttackTargeting));
        }
    }

    /// <summary>
    /// Sets the PlayerVisionSystem reference for god mode toggling.
    /// </summary>
    public void SetPlayerVisionSystem(PlayerVisionSystem visionSystem)
    {
        _visionSystem = visionSystem;
    }

    /// <summary>
    /// Sets the TargetingSystem reference for ranged attacks.
    /// </summary>
    public void SetTargetingSystem(TargetingSystem targetingSystem)
    {
        _targetingSystem = targetingSystem;

        // Connect to targeting signals
        if (_targetingSystem != null)
        {
            _targetingSystem.Connect(TargetingSystem.SignalName.TargetConfirmed, Callable.From<Vector2I>(OnTargetConfirmed));
            _targetingSystem.Connect(TargetingSystem.SignalName.TargetCanceled, Callable.From(OnTargetCanceled));
        }
    }

    public override void _ExitTree()
    {
        // Clean up signal connections
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.TurnCompleted, Callable.From(OnPlayerTurnCompleted));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_player == null || _turnManager == null || _actionContext == null)
        {
            return;
        }

        // Only process key presses, not key releases or repeats
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Handle targeting mode input separately
            if (_targetingSystem != null && _targetingSystem.IsActive)
            {
                HandleTargetingInput(keyEvent);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Help modal works anytime (doesn't require player turn)
            // Process before menu check so it can toggle even when help is open
            if (keyEvent.Keycode == Key.Slash && keyEvent.ShiftPressed) // ? key
            {
                EmitSignal(SignalName.HelpRequested);
                GetViewport().SetInputAsHandled();
                return;
            }

            // If a menu is open, don't process menu toggle keys (A/D/I)
            // Let the panels handle the input for item selection instead
            if (_gameHUD != null && _gameHUD.IsAnyMenuOpen())
            {
                // Don't process menu toggle keys while a menu is open
                // The menu panels will handle a-z key presses for item selection
                return;
            }

            // Inventory toggle works anytime (doesn't require player turn)
            if (keyEvent.Keycode == Key.I)
            {
                EmitSignal(SignalName.InventoryToggleRequested);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Activate item menu (doesn't require player turn)
            if (keyEvent.Keycode == Key.A)
            {
                EmitSignal(SignalName.ActivateItemRequested);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Drop item menu (doesn't require player turn)
            if (keyEvent.Keycode == Key.D)
            {
                EmitSignal(SignalName.DropItemRequested);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Equip menu (doesn't require player turn)
            if (keyEvent.Keycode == Key.E)
            {
                EmitSignal(SignalName.EquipMenuRequested);
                GetViewport().SetInputAsHandled();
                return;
            }

            // God mode toggle (Ctrl+G) - debug feature
            if (keyEvent.Keycode == Key.G && keyEvent.CtrlPressed)
            {
                _visionSystem?.ToggleGodMode();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Only process turn-based actions during player turn
            if (!_turnManager.IsPlayerTurn)
            {
                return;
            }

            // Check for fire/targeting mode (F key)
            if (keyEvent.Keycode == Key.F)
            {
                if (HasRangedWeaponEquipped() && _targetingSystem != null && _gameHUD != null)
                {
                    int range = GetRangedWeaponRange();
                    if (range > 0)
                    {
                        // Use Chebyshev (grid-based) distance for tactical targeting
                        _targetingSystem.StartTargeting(_player.GridPosition, range, requiresCreature: true, useGridDistance: true);
                        _gameHUD.EnterTargetingMode();
                    }
                }
                GetViewport().SetInputAsHandled();
                return;
            }

            // Check for pickup action
            if (keyEvent.Keycode == Key.G)
            {
                var pickupAction = new PickupAction();
                _player.ExecuteAction(pickupAction, _actionContext);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Check for wait action
            if (IsWaitKey(keyEvent.Keycode))
            {
                var waitAction = new WaitAction();
                _player.ExecuteAction(waitAction, _actionContext);
                GetViewport().SetInputAsHandled();
                return;
            }

            // Check for movement
            Vector2I direction = GetDirectionFromKey(keyEvent.Keycode);

            if (direction != Vector2I.Zero)
            {
                var moveAction = new MoveAction(direction);
                _player.ExecuteAction(moveAction, _actionContext);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    /// <summary>
    /// Called when the player completes their turn (after successful action).
    /// </summary>
    private void OnPlayerTurnCompleted()
    {
        _turnManager?.EndPlayerTurn();
    }

    /// <summary>
    /// Converts a keycode to a movement direction.
    /// </summary>
    private Vector2I GetDirectionFromKey(Key keycode)
    {
        return keycode switch
        {
            // Arrow keys (cardinal directions)
            Key.Up => Vector2I.Up,
            Key.Down => Vector2I.Down,
            Key.Left => Vector2I.Left,
            Key.Right => Vector2I.Right,

            // Numpad (8 directions)
            Key.Kp8 => Vector2I.Up,         // North
            Key.Kp2 => Vector2I.Down,       // South
            Key.Kp4 => Vector2I.Left,       // West
            Key.Kp6 => Vector2I.Right,      // East
            Key.Kp7 => new Vector2I(-1, -1), // Northwest
            Key.Kp9 => new Vector2I(1, -1),  // Northeast
            Key.Kp1 => new Vector2I(-1, 1),  // Southwest
            Key.Kp3 => new Vector2I(1, 1),   // Southeast

            _ => Vector2I.Zero
        };
    }

    /// <summary>
    /// Checks if the given keycode is a wait action key.
    /// </summary>
    private bool IsWaitKey(Key keycode)
    {
        return keycode switch
        {
            Key.Space => true,   // Spacebar
            Key.Kp5 => true,     // Numpad 5
            _ => false
        };
    }

    /// <summary>
    /// Handles input while in targeting mode.
    /// </summary>
    private void HandleTargetingInput(InputEventKey keyEvent)
    {
        if (_targetingSystem == null)
            return;

        // ESC cancels targeting
        if (keyEvent.Keycode == Key.Escape)
        {
            _targetingSystem.CancelTarget();
            return;
        }

        // Enter, Space, or F confirms targeting
        if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter || keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.F)
        {
            _targetingSystem.ConfirmTarget();
            return;
        }

        // Tab cycles to next target
        if (keyEvent.Keycode == Key.Tab && !keyEvent.ShiftPressed)
        {
            _targetingSystem.CycleNextTarget();
            return;
        }

        // Shift+Tab cycles to previous target
        if (keyEvent.Keycode == Key.Tab && keyEvent.ShiftPressed)
        {
            _targetingSystem.CyclePreviousTarget();
            return;
        }

        // Numpad +/- for target cycling
        if (keyEvent.Keycode == Key.KpAdd)
        {
            _targetingSystem.CycleNextTarget();
            return;
        }

        if (keyEvent.Keycode == Key.KpSubtract)
        {
            _targetingSystem.CyclePreviousTarget();
            return;
        }

        // Numpad directions move cursor
        Vector2I direction = GetDirectionFromKey(keyEvent.Keycode);
        if (direction != Vector2I.Zero)
        {
            _targetingSystem.MoveCursor(direction);
        }
    }

    /// <summary>
    /// Called when user requests to target an item.
    /// Starts targeting mode for the specified item.
    /// </summary>
    private void OnStartItemTargeting(char itemKey)
    {
        if (_player == null || _targetingSystem == null)
            return;

        // Store the pending item key
        _pendingItemKey = itemKey;
        _isReachAttack = false;

        // Get the item to determine range
        var slot = _player.GetInventorySlot(itemKey);
        if (slot != null)
        {
            int range = slot.Item.Template.GetTargetingRange();
            // Use Chebyshev (grid-based) distance for consistent tactical targeting
            _targetingSystem.StartTargeting(_player.GridPosition, range, requiresCreature: true, useGridDistance: true);
        }
    }

    /// <summary>
    /// Called when user requests to use a reach attack with an equipped weapon.
    /// Starts targeting mode for the reach attack.
    /// </summary>
    private void OnStartReachAttackTargeting(char itemKey)
    {
        if (_player == null || _targetingSystem == null)
            return;

        // Store the pending item key and mark as reach attack
        _pendingItemKey = itemKey;
        _isReachAttack = true;

        // Get the weapon to determine range
        var slot = _player.GetInventorySlot(itemKey);
        if (slot != null && slot.Item.Template.Attack != null)
        {
            int range = slot.Item.Template.Attack.Range;
            // Use Chebyshev (grid-based) distance for reach attacks
            _targetingSystem.StartTargeting(_player.GridPosition, range, requiresCreature: true, useGridDistance: true);
        }
    }

    /// <summary>
    /// Called when targeting is confirmed. Executes ranged attack or targeted item action.
    /// </summary>
    private void OnTargetConfirmed(Vector2I targetPosition)
    {
        if (_player == null || _actionContext == null)
        {
            return;
        }

        // Exit targeting mode in UI
        if (_gameHUD != null)
        {
            _gameHUD.ExitTargetingMode();
        }

        // Check if we're targeting for an item, reach attack, or ranged weapon
        if (_pendingItemKey.HasValue)
        {
            if (_isReachAttack)
            {
                // Execute reach attack action
                // Get the equipped weapon's attack index
                var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
                if (attackComponent != null)
                {
                    var reachAttackAction = new ReachAttackAction(GridPosition.FromVector2I(targetPosition), attackIndex: 0);
                    _player.ExecuteAction(reachAttackAction, _actionContext);
                }

                // Clear pending state
                _pendingItemKey = null;
                _isReachAttack = false;
            }
            else
            {
                // Execute targeted item action
                var itemAction = new ActivateTargetedItemAction(_pendingItemKey.Value, GridPosition.FromVector2I(targetPosition));
                _player.ExecuteAction(itemAction, _actionContext);

                // Clear pending item
                _pendingItemKey = null;
            }
        }
        else
        {
            // Find the ranged attack index
            var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
            if (attackComponent == null)
            {
                return;
            }

            int rangedAttackIndex = -1;
            for (int i = 0; i < attackComponent.Attacks.Count; i++)
            {
                var attack = attackComponent.Attacks[i];
                if (attack != null && attack.Type == AttackType.Ranged)
                {
                    rangedAttackIndex = i;
                    break;
                }
            }

            if (rangedAttackIndex == -1)
            {
                return;
            }

            // Execute ranged attack action
            var rangedAttackAction = new RangedAttackAction(GridPosition.FromVector2I(targetPosition), rangedAttackIndex);
            _player.ExecuteAction(rangedAttackAction, _actionContext);
        }
    }

    /// <summary>
    /// Called when targeting is canceled. Returns to normal gameplay.
    /// </summary>
    private void OnTargetCanceled()
    {
        // Exit targeting mode in UI
        if (_gameHUD != null)
        {
            _gameHUD.ExitTargetingMode();
        }

        // Clear pending item and reach attack state
        _pendingItemKey = null;
        _isReachAttack = false;

        // No turn consumed
    }

    /// <summary>
    /// Checks if the player has a ranged weapon equipped.
    /// </summary>
    private bool HasRangedWeaponEquipped()
    {
        if (_player == null)
            return false;

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null)
            return false;

        // Check if ranged weapon slot is equipped
        var rangedWeaponKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
        return rangedWeaponKey != null;
    }

    /// <summary>
    /// Gets the range of the equipped ranged weapon.
    /// </summary>
    private int GetRangedWeaponRange()
    {
        if (_player == null)
            return 0;

        var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return 0;

        // Find first ranged attack
        for (int i = 0; i < attackComponent.Attacks.Count; i++)
        {
            var attack = attackComponent.Attacks[i];
            if (attack != null && attack.Type == AttackType.Ranged)
            {
                return attack.Range;
            }
        }

        return 0;
    }
}
