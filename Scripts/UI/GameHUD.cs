using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
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
    private SidePanel _sidePanel;
    private MessageLog _messageLog;
    private InventoryPanel _inventoryPanel;
    private ActivateItemPanel _activateItemPanel;
    private DropItemPanel _dropItemPanel;
    private EquipPanel _equipPanel;
    private Player _player;
    private ActionContext _actionContext;
    private MenuState _currentMenuState = MenuState.None;

    public override void _Ready()
    {
        _sidePanel = GetNode<SidePanel>("VBoxContainer/HBoxContainer/SidePanel");
        _messageLog = GetNode<MessageLog>("VBoxContainer/MessageLog");
        _inventoryPanel = GetNode<InventoryPanel>("InventoryPanel");
        _activateItemPanel = GetNode<ActivateItemPanel>("ActivateItemPanel");
        _dropItemPanel = GetNode<DropItemPanel>("DropItemPanel");
        _equipPanel = GetNode<EquipPanel>("EquipPanel");
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
    /// <param name="visionSystem">The player vision system for checking visible entities.</param>
    public void Initialize(Player player, CombatSystem combatSystem, EntityManager entityManager, int floorDepth, ActionContext actionContext, PlayerVisionSystem visionSystem = null)
    {
        _player = player;
        _actionContext = actionContext;

        // Wire up side panel
        _sidePanel.ConnectToPlayer(player);
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
        _inventoryPanel.Cancelled += OnInventoryCancelled;

        // Wire up activate, drop, and equip panels
        _activateItemPanel.ConnectToPlayer(player);
        _dropItemPanel.ConnectToPlayer(player);
        _equipPanel.ConnectToPlayer(player);

        // Connect panel signals
        _activateItemPanel.ItemSelected += OnActivateItemSelected;
        _activateItemPanel.Cancelled += OnActivateItemCancelled;
        _dropItemPanel.ItemSelected += OnDropItemSelected;
        _dropItemPanel.Cancelled += OnDropItemCancelled;
        _equipPanel.ItemSelected += OnEquipItemSelected;
        _equipPanel.Cancelled += OnEquipItemCancelled;

        // Subscribe to entity additions to connect their HealthComponents to message log
        entityManager.EntityAdded += OnEntityAdded;

        // Also connect the player's HealthComponent (player is not added via EntityManager)
        var playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
        _messageLog.ConnectToHealthComponent(playerHealth, player.DisplayName);

        // Subscribe to wait action
        player.Waited += () => _messageLog.AddMessage("You wait.", Palette.ToHex(Palette.Success));

        // Subscribe to item events
        player.ItemPickedUp += OnItemPickedUp;
        player.ItemUsed += OnItemUsed;
        player.ItemDropped += OnItemDropped;

        // Subscribe to gold collection
        player.GoldCollected += OnGoldCollected;

        // Subscribe to standing on entities (for "You see here" messages)
        player.StandingOnEntity += OnStandingOnEntity;

        // Subscribe to status effect messages
        var statusComponent = player.GetNodeOrNull<Components.StatusComponent>("StatusComponent");
        if (statusComponent != null)
        {
            statusComponent.StatusMessage += (message) => _messageLog.AddMessage(message, Palette.ToHex(Palette.HealthFull));
        }

        // Add welcome message
        _messageLog.AddMessage("Welcome to the Pits of Despair. Don't even think about trying to escape.");
    }

    /// <summary>
    /// Checks if any menu is currently open.
    /// </summary>
    public bool IsAnyMenuOpen()
    {
        return _currentMenuState != MenuState.None;
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

    private void OnInventoryCancelled()
    {
        _inventoryPanel.ToggleInventory();
        _currentMenuState = MenuState.None;
    }

    private void OnActivateItemSelected(char key)
    {
        _activateItemPanel.HideMenu();
        _currentMenuState = MenuState.None;

        // Execute ActivateItemAction
        var action = new ActivateItemAction(key);
        _player.ExecuteAction(action, _actionContext);
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
