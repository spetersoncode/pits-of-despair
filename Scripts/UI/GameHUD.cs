using Godot;
using PitsOfDespair.Actions;
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
    Drop
}

/// <summary>
/// Main HUD coordinator that manages and wires up UI panels.
/// </summary>
public partial class GameHUD : Control
{
    private StatsPanel _statsPanel;
    private MessageLog _messageLog;
    private InventoryPanel _inventoryPanel;
    private ActivateItemPanel _activateItemPanel;
    private DropItemPanel _dropItemPanel;
    private Player _player;
    private ActionContext _actionContext;
    private MenuState _currentMenuState = MenuState.None;

    public override void _Ready()
    {
        _statsPanel = GetNode<StatsPanel>("StatsPanel");
        _messageLog = GetNode<MessageLog>("MessageLog");
        _inventoryPanel = GetNode<InventoryPanel>("InventoryPanel");
        _activateItemPanel = GetNode<ActivateItemPanel>("ActivateItemPanel");
        _dropItemPanel = GetNode<DropItemPanel>("DropItemPanel");
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
    public void Initialize(Player player, CombatSystem combatSystem, EntityManager entityManager, int floorDepth, ActionContext actionContext)
    {
        _player = player;
        _actionContext = actionContext;

        // Wire up stats panel
        _statsPanel.ConnectToPlayer(player);
        _statsPanel.SetFloorDepth(floorDepth);
        _statsPanel.SetEntityManager(entityManager);

        // Wire up message log
        _messageLog.ConnectToCombatSystem(combatSystem);

        // Wire up inventory panel
        _inventoryPanel.ConnectToPlayer(player);

        // Wire up activate and drop panels
        _activateItemPanel.ConnectToPlayer(player);
        _dropItemPanel.ConnectToPlayer(player);

        // Connect panel signals
        _activateItemPanel.ItemSelected += OnActivateItemSelected;
        _activateItemPanel.Cancelled += OnActivateItemCancelled;
        _dropItemPanel.ItemSelected += OnDropItemSelected;
        _dropItemPanel.Cancelled += OnDropItemCancelled;

        // Subscribe to all entity deaths (including enemies)
        entityManager.EntityRemoved += OnEntityRemoved;

        // Subscribe to player death specifically
        var playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
        _messageLog.ConnectToHealthComponent(playerHealth, player.DisplayName);

        // Subscribe to wait action
        player.Waited += () => _messageLog.AddMessage("You wait.", "#66ff66");

        // Subscribe to item events
        player.ItemPickedUp += OnItemPickedUp;
        player.ItemUsed += OnItemUsed;
        player.ItemDropped += OnItemDropped;

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

    private void OnActivateItemSelected(char key)
    {
        _activateItemPanel.HideMenu();
        _currentMenuState = MenuState.None;

        // Execute UseItemAction
        var action = new UseItemAction(key);
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

    /// <summary>
    /// Closes all open menus and resets menu state.
    /// </summary>
    private void CloseAllMenus()
    {
        _inventoryPanel.Hide();
        _activateItemPanel.HideMenu();
        _dropItemPanel.HideMenu();
        _currentMenuState = MenuState.None;
    }

    private void OnItemPickedUp(string itemName, bool success, string message)
    {
        if (success)
        {
            _messageLog.AddMessage(message, "#66ff66"); // Green for success
        }
        else
        {
            _messageLog.AddMessage(message, "#888888"); // Gray for failure
        }
    }

    private void OnItemUsed(string itemName, bool success, string message)
    {
        if (success)
        {
            // Include item name in the message
            string fullMessage = $"You use {itemName}. {message}";
            _messageLog.AddMessage(fullMessage, "#66ff66"); // Green for success
        }
        else
        {
            _messageLog.AddMessage(message, "#888888"); // Gray for failure
        }
    }

    private void OnItemDropped(string itemName)
    {
        _messageLog.AddMessage($"You drop {itemName}.", "#ffffff"); // White
    }

    private void OnEntityRemoved(BaseEntity entity)
    {
        // Only log death for non-player entities (player death is handled separately)
        if (entity.DisplayName != "Player")
        {
            _messageLog.AddMessage($"{entity.DisplayName} dies!", "#ffff66"); // Yellow
        }
    }
}
