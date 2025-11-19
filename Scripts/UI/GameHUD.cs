using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.UI;

/// <summary>
/// Represents the current menu state of the HUD.
/// </summary>
public enum MenuState
{
    None,
    Inventory,
    Activate,
    Drop,
    Equip
}

/// <summary>
/// Main HUD coordinator that manages and wires up UI panels.
/// </summary>
public partial class GameHUD : Control
{
    [Signal]
    public delegate void StartItemTargetingEventHandler(char itemKey);

    [Signal]
    public delegate void StartReachAttackTargetingEventHandler(char itemKey);

    private SidePanel _sidePanel;
    private MessageLog _messageLog;
    private InventoryModal _inventoryPanel;
    private ActivateItemModal _activateItemPanel;
    private DropItemModal _dropItemPanel;
    private EquipModal _equipPanel;
    private HelpModal _helpModal;
    private Player _player;
    private ActionContext _actionContext;
    private MenuState _currentMenuState = MenuState.None;

    public override void _Ready()
    {
        _sidePanel = GetNode<SidePanel>("HBoxContainer/SidePanel");
        _messageLog = GetNode<MessageLog>("HBoxContainer/VBoxContainer/MessageLog");
        _inventoryPanel = GetNode<InventoryModal>("InventoryModal");
        _activateItemPanel = GetNode<ActivateItemModal>("ActivateItemModal");
        _dropItemPanel = GetNode<DropItemModal>("DropItemModal");
        _equipPanel = GetNode<EquipModal>("EquipModal");
        _helpModal = GetNode<HelpModal>("HelpModal");
    }

    /// <summary>
    /// Initializes the HUD by connecting to game systems.
    /// Should be called after the HUD is added to the scene tree.
    /// </summary>
    /// <param name="player">The player entity.</param>
    /// <param name="combatSystem">The combat system for attack events.</param>
    /// <param name="entityManager">The entity manager for death notifications.</param>
    /// <param name="floorDepth">The current floor depth.</param>
    /// <param name="actionContext">The action context for executing actions.</param>
    /// <param name="goldManager">The gold manager for tracking player gold.</param>
    /// <param name="visionSystem">The player vision system for checking visible entities.</param>
    public void Initialize(Player player, CombatSystem combatSystem, EntityManager entityManager, int floorDepth, ActionContext actionContext, GoldManager goldManager, PlayerVisionSystem visionSystem = null)
    {
        _player = player;
        _actionContext = actionContext;

        // Wire up side panel
        _sidePanel.ConnectToPlayer(player);
        _sidePanel.ConnectToGoldManager(goldManager);
        _sidePanel.SetFloorDepth(floorDepth);
        _sidePanel.SetEntityManager(entityManager);
        if (visionSystem != null)
        {
            _sidePanel.SetVisionSystem(visionSystem);
        }

        // Wire up message log
        _messageLog.ConnectToCombatSystem(combatSystem);
        _messageLog.SetPlayer(player);

        // Wire up inventory panel
        _inventoryPanel.ConnectToPlayer(player);
        _inventoryPanel.Connect(InventoryModal.SignalName.Cancelled, Callable.From(OnInventoryCancelled));

        // Wire up activate, drop, and equip panels
        _activateItemPanel.ConnectToPlayer(player);
        _dropItemPanel.ConnectToPlayer(player);
        _equipPanel.ConnectToPlayer(player);

        // Connect panel signals
        _activateItemPanel.Connect(ActivateItemModal.SignalName.ItemSelected, Callable.From<char>(OnActivateItemSelected));
        _activateItemPanel.Connect(ActivateItemModal.SignalName.Cancelled, Callable.From(OnActivateItemCancelled));
        _dropItemPanel.Connect(DropItemModal.SignalName.ItemSelected, Callable.From<char>(OnDropItemSelected));
        _dropItemPanel.Connect(DropItemModal.SignalName.Cancelled, Callable.From(OnDropItemCancelled));
        _equipPanel.Connect(EquipModal.SignalName.ItemSelected, Callable.From<char>(OnEquipItemSelected));
        _equipPanel.Connect(EquipModal.SignalName.Cancelled, Callable.From(OnEquipItemCancelled));
        _helpModal.Connect(HelpModal.SignalName.Cancelled, Callable.From(OnHelpCancelled));

        // Subscribe to entity additions to connect their HealthComponents to message log
        entityManager.Connect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));

        // Connect existing entities that were added before HUD initialization
        foreach (var entity in entityManager.GetAllEntities())
        {
            var healthComponent = entity.GetNodeOrNull<Components.HealthComponent>("HealthComponent");
            if (healthComponent != null)
            {
                _messageLog.ConnectToHealthComponent(healthComponent, entity.DisplayName);
            }
        }

        // Also connect the player's HealthComponent (player is not added via EntityManager)
        var playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
        _messageLog.ConnectToHealthComponent(playerHealth, player.DisplayName);

        // Subscribe to wait action
        player.Connect(Player.SignalName.Waited, Callable.From(() => _messageLog.AddMessage("You wait.", Palette.ToHex(Palette.Success))));

        // Subscribe to item events
        player.Connect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
        player.Connect(Player.SignalName.ItemUsed, Callable.From<string, bool, string>(OnItemUsed));
        player.Connect(Player.SignalName.ItemDropped, Callable.From<string>(OnItemDropped));

        // Subscribe to gold collection
        player.Connect(Player.SignalName.GoldCollected, Callable.From<int, int>(OnGoldCollected));

        // Subscribe to standing on entities (for "You see here" messages)
        player.Connect(Player.SignalName.StandingOnEntity, Callable.From<string, string, Color>(OnStandingOnEntity));

        // Subscribe to status effect messages
        var statusComponent = player.GetNodeOrNull<Components.StatusComponent>("StatusComponent");
        if (statusComponent != null)
        {
            statusComponent.Connect(Components.StatusComponent.SignalName.StatusMessage, Callable.From<string>((message) => _messageLog.AddMessage(message, Palette.ToHex(Palette.HealthFull))));
        }

        // Add welcome message
        _messageLog.AddMessage("Welcome to the Pits of Despair. Don't even think about trying to escape. (? for help)");
    }

    /// <summary>
    /// Checks if any menu is currently open (including help modal).
    /// </summary>
    public bool IsAnyMenuOpen()
    {
        return _currentMenuState != MenuState.None || _helpModal.Visible;
    }

    /// <summary>
    /// Gets the current menu state.
    /// </summary>
    public MenuState GetCurrentMenuState()
    {
        return _currentMenuState;
    }

    /// <summary>
    /// Toggles the inventory panel visibility.
    /// Called by InputHandler when 'I' is pressed.
    /// </summary>
    public void ToggleInventory()
    {
        if (_currentMenuState == MenuState.Inventory)
        {
            // Close inventory
            _inventoryPanel.ToggleInventory();
            _currentMenuState = MenuState.None;
        }
        else if (_currentMenuState == MenuState.None)
        {
            // Open inventory
            _inventoryPanel.ToggleInventory();
            _currentMenuState = MenuState.Inventory;
        }
        else
        {
            // Another menu is open, close it first
            CloseAllMenus();
            _inventoryPanel.ToggleInventory();
            _currentMenuState = MenuState.Inventory;
        }
    }

    /// <summary>
    /// Shows the activate item menu.
    /// Called by InputHandler when 'A' is pressed.
    /// </summary>
    public void ShowActivateMenu()
    {
        // Close any open menus first
        if (_currentMenuState != MenuState.None && _currentMenuState != MenuState.Activate)
        {
            CloseAllMenus();
        }

        _activateItemPanel.ShowMenu();
        _currentMenuState = MenuState.Activate;
    }

    /// <summary>
    /// Shows the drop item menu.
    /// Called by InputHandler when 'D' is pressed.
    /// </summary>
    public void ShowDropMenu()
    {
        // Close any open menus first
        if (_currentMenuState != MenuState.None && _currentMenuState != MenuState.Drop)
        {
            CloseAllMenus();
        }

        _dropItemPanel.ShowMenu();
        _currentMenuState = MenuState.Drop;
    }

    /// <summary>
    /// Shows the equip item menu.
    /// Called by InputHandler when 'E' is pressed.
    /// </summary>
    public void ShowEquipMenu()
    {
        // Close any open menus first
        if (_currentMenuState != MenuState.None && _currentMenuState != MenuState.Equip)
        {
            CloseAllMenus();
        }

        _equipPanel.ShowMenu();
        _currentMenuState = MenuState.Equip;
    }

    /// <summary>
    /// Shows the help modal, or hides it if already visible.
    /// Called by InputHandler when '?' is pressed.
    /// </summary>
    public void ShowHelp()
    {
        // Toggle if already visible
        if (_helpModal.Visible)
        {
            _helpModal.HideHelp();
            return;
        }

        // Don't open help if another menu is already open
        if (_currentMenuState != MenuState.None)
        {
            return;
        }

        _helpModal.ShowHelp();
    }

    /// <summary>
    /// Enters targeting mode.
    /// Called by InputHandler when 'F' is pressed with ranged weapon equipped.
    /// </summary>
    public void EnterTargetingMode()
    {
        // Close any open menus first
        if (_currentMenuState != MenuState.None)
        {
            CloseAllMenus();
        }
        // Targeting mode is handled by TargetingSystem, not as a menu state
    }

    /// <summary>
    /// Exits targeting mode.
    /// Called when targeting is confirmed or canceled.
    /// </summary>
    public void ExitTargetingMode()
    {
        // Targeting mode is handled by TargetingSystem
        // No menu state to reset
    }

    private void OnInventoryCancelled()
    {
        _inventoryPanel.ToggleInventory();
        _currentMenuState = MenuState.None;
    }

    private void OnActivateItemSelected(char key)
    {
        _activateItemPanel.HideMenu();
        _currentMenuState = MenuState.None;

        // Get the item to check what type of activation it needs
        var slot = _player.GetInventorySlot(key);
        if (slot == null)
        {
            return;
        }

        var equipComponent = _player.GetNodeOrNull<Scripts.Components.EquipComponent>("EquipComponent");
        bool isEquipped = equipComponent != null && equipComponent.IsEquipped(key);

        // Check if this is a reach weapon (equipped melee weapon with range > 1)
        if (isEquipped &&
            slot.Item.Template.Attack != null &&
            slot.Item.Template.Attack.Type == AttackType.Melee &&
            slot.Item.Template.Attack.Range > 1)
        {
            // Reach weapon - signal InputHandler to start reach attack targeting
            EmitSignal(SignalName.StartReachAttackTargeting, key);
            EnterTargetingMode();
        }
        else if (slot.Item.Template.RequiresTargeting())
        {
            // Item requires targeting - signal InputHandler to start targeting
            EmitSignal(SignalName.StartItemTargeting, key);
            EnterTargetingMode();
        }
        else
        {
            // Regular activation (no targeting needed)
            var action = new ActivateItemAction(key);
            _player.ExecuteAction(action, _actionContext);
        }
    }

    private void OnActivateItemCancelled()
    {
        _activateItemPanel.HideMenu();
        _currentMenuState = MenuState.None;
    }

    private void OnDropItemSelected(char key)
    {
        _dropItemPanel.HideMenu();
        _currentMenuState = MenuState.None;

        // Execute DropItemAction
        var action = new DropItemAction(key);
        _player.ExecuteAction(action, _actionContext);
    }

    private void OnDropItemCancelled()
    {
        _dropItemPanel.HideMenu();
        _currentMenuState = MenuState.None;
    }

    private void OnEquipItemSelected(char key)
    {
        _equipPanel.HideMenu();
        _currentMenuState = MenuState.None;

        // Execute EquipAction
        var action = new EquipAction(key);
        _player.ExecuteAction(action, _actionContext);
    }

    private void OnEquipItemCancelled()
    {
        _equipPanel.HideMenu();
        _currentMenuState = MenuState.None;
    }

    private void OnHelpCancelled()
    {
        // Help modal doesn't change menu state, just hide it
        // No turn is consumed, no menu state to reset
    }

    /// <summary>
    /// Closes all open menus and resets menu state.
    /// </summary>
    private void CloseAllMenus()
    {
        _inventoryPanel.Hide();
        _activateItemPanel.HideMenu();
        _dropItemPanel.HideMenu();
        _equipPanel.HideMenu();
        _currentMenuState = MenuState.None;
    }

    private void OnItemPickedUp(string itemName, bool success, string message)
    {
        if (success)
        {
            _messageLog.AddMessage(message, Palette.ToHex(Palette.Success));
        }
        else
        {
            _messageLog.AddMessage(message, Palette.ToHex(Palette.Disabled));
        }
    }

    private void OnItemUsed(string itemName, bool success, string message)
    {
        if (success)
        {
            // Include item name in the message
            string fullMessage = $"You use {itemName}. {message}";
            _messageLog.AddMessage(fullMessage, Palette.ToHex(Palette.Success));
        }
        else
        {
            _messageLog.AddMessage(message, Palette.ToHex(Palette.Disabled));
        }
    }

    private void OnItemDropped(string itemName)
    {
        _messageLog.AddMessage($"You drop {itemName}.", Palette.ToHex(Palette.Default));
    }

    private void OnGoldCollected(int amount, int totalGold)
    {
        string message = amount == 1
            ? $"You collect 1 gold. (Total: {totalGold})"
            : $"You collect {amount} gold. (Total: {totalGold})";
        _messageLog.AddMessage(message, Palette.ToHex(Palette.Gold));
    }

    private void OnStandingOnEntity(string entityName, string entityGlyph, Color entityColor)
    {
        // Convert Color to hex string for BBCode
        string colorHex = $"#{(int)(entityColor.R * 255):X2}{(int)(entityColor.G * 255):X2}{(int)(entityColor.B * 255):X2}";
        // Show both glyph and item name in color
        _messageLog.AddMessage($"You stand over the [color={colorHex}]{entityGlyph} {entityName}[/color].", Palette.ToHex(Palette.Default));
    }

    private void OnEntityAdded(BaseEntity entity)
    {
        // Connect the entity's HealthComponent to the message log for death messages
        var healthComponent = entity.GetNodeOrNull<Components.HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            _messageLog.ConnectToHealthComponent(healthComponent, entity.DisplayName);
        }
    }
}
