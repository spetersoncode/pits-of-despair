using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Debug;
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
    Equip,
    ItemDetail,
    LevelUpChoice
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
    private ItemDetailModal _itemDetailModal;
    private EntityDetailModal _entityDetailModal;
    private DebugConsoleModal _debugConsoleModal;
    private LevelUpModal _levelUpModal;
    private Player _player;
    private MenuState _currentMenuState = MenuState.None;
    private CursorTargetingSystem _cursorSystem;
    private AutoExploreSystem _autoExploreSystem;
    private Components.StatsComponent _playerStats;
    private Components.ConditionComponent _conditionComponent;
    private Systems.EntityManager _entityManager;

    // New systems for decoupling
    private Systems.LevelUpSystem _levelUpSystem;
    private Systems.PlayerActionHandler _actionHandler;

    public override void _Ready()
    {
        _sidePanel = GetNode<SidePanel>("HBoxContainer/SidePanel");
        _messageLog = GetNode<MessageLog>("HBoxContainer/VBoxContainer/MessageLog");
        _inventoryPanel = GetNode<InventoryModal>("InventoryModal");
        _activateItemPanel = GetNode<ActivateItemModal>("ActivateItemModal");
        _dropItemPanel = GetNode<DropItemModal>("DropItemModal");
        _equipPanel = GetNode<EquipModal>("EquipModal");
        _helpModal = GetNode<HelpModal>("HelpModal");
        _itemDetailModal = GetNode<ItemDetailModal>("ItemDetailModal");
        _entityDetailModal = GetNode<EntityDetailModal>("EntityDetailModal");
        _debugConsoleModal = GetNode<DebugConsoleModal>("DebugConsoleModal");
        _levelUpModal = GetNode<LevelUpModal>("LevelUpModal");
    }

    /// <summary>
    /// Initializes the HUD by connecting to game systems.
    /// Should be called after the HUD is added to the scene tree.
    /// </summary>
    /// <param name="player">The player entity.</param>
    /// <param name="combatSystem">The combat system for attack events.</param>
    /// <param name="entityManager">The entity manager for death notifications.</param>
    /// <param name="floorDepth">The current floor depth.</param>
    /// <param name="goldManager">The gold manager for tracking player gold.</param>
    /// <param name="levelUpSystem">The level-up system for stat rewards.</param>
    /// <param name="actionHandler">The action handler for player actions.</param>
    /// <param name="visionSystem">The player vision system for checking visible entities.</param>
    /// <param name="debugContext">The debug context for debug commands.</param>
    /// <param name="debugModeActive">Initial debug mode state (default: false).</param>
    public void Initialize(Player player, CombatSystem combatSystem, EntityManager entityManager, int floorDepth, GoldManager goldManager, Systems.LevelUpSystem levelUpSystem, Systems.PlayerActionHandler actionHandler, PlayerVisionSystem visionSystem = null, DebugContext debugContext = null, bool debugModeActive = false)
    {
        _player = player;
        _levelUpSystem = levelUpSystem;
        _actionHandler = actionHandler;

        // Note: SidePanel is now initialized separately in GameLevel

        _messageLog.ConnectToCombatSystem(combatSystem);
        _messageLog.SetPlayer(player);
        _messageLog.SetEntityManager(entityManager);

        _inventoryPanel.ConnectToPlayer(player);
        _inventoryPanel.Connect(InventoryModal.SignalName.Cancelled, Callable.From(OnInventoryCancelled));
        _inventoryPanel.Connect(InventoryModal.SignalName.ItemSelected, Callable.From<char>(OnInventoryItemSelected));

        _activateItemPanel.ConnectToPlayer(player);
        _dropItemPanel.ConnectToPlayer(player);
        _equipPanel.ConnectToPlayer(player);

        _itemDetailModal.ConnectToPlayer(player);
        _itemDetailModal.Connect(ItemDetailModal.SignalName.KeyRebound, Callable.From<char, char>(OnItemKeyRebound));
        _itemDetailModal.Connect(ItemDetailModal.SignalName.Cancelled, Callable.From(OnItemDetailCancelled));

        _entityDetailModal.Connect(EntityDetailModal.SignalName.Cancelled, Callable.From(OnEntityDetailCancelled));

        _activateItemPanel.Connect(ActivateItemModal.SignalName.ItemSelected, Callable.From<char>(OnActivateItemSelected));
        _activateItemPanel.Connect(ActivateItemModal.SignalName.Cancelled, Callable.From(OnActivateItemCancelled));
        _dropItemPanel.Connect(DropItemModal.SignalName.ItemSelected, Callable.From<char>(OnDropItemSelected));
        _dropItemPanel.Connect(DropItemModal.SignalName.Cancelled, Callable.From(OnDropItemCancelled));
        _equipPanel.Connect(EquipModal.SignalName.ItemSelected, Callable.From<char>(OnEquipItemSelected));
        _equipPanel.Connect(EquipModal.SignalName.Cancelled, Callable.From(OnEquipItemCancelled));
        _helpModal.Connect(HelpModal.SignalName.Cancelled, Callable.From(OnHelpCancelled));

        _levelUpModal.Connect(LevelUpModal.SignalName.StatChosen, Callable.From<int>(OnStatChosen));

        // Connect to LevelUpSystem for showing level-up modal
        _levelUpSystem.Connect(Systems.LevelUpSystem.SignalName.ShowLevelUpModal, Callable.From<int>(OnShowLevelUpModal));
        _levelUpSystem.Connect(Systems.LevelUpSystem.SignalName.StatIncreased, Callable.From<string, int>(OnStatIncreased));

        // Connect to PlayerActionHandler for action results
        _actionHandler.Connect(Systems.PlayerActionHandler.SignalName.ItemRebound, Callable.From<string>(OnItemReboundMessage));
        _actionHandler.Connect(Systems.PlayerActionHandler.SignalName.StartItemTargeting, Callable.From<char>(OnStartItemTargeting));
        _actionHandler.Connect(Systems.PlayerActionHandler.SignalName.StartReachAttackTargeting, Callable.From<char>(OnStartReachAttackTargeting));

        // Keep reference to player stats for other uses
        _playerStats = player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");

        // Initialize debug console if debug context provided
        if (debugContext != null)
        {
            _debugConsoleModal.Initialize(_messageLog, debugContext, debugModeActive);
            _debugConsoleModal.Connect(DebugConsoleModal.SignalName.Cancelled, Callable.From(OnDebugConsoleCancelled));
        }

        _entityManager = entityManager;
        _entityManager.Connect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));

        foreach (var entity in entityManager.GetAllEntities())
        {
            var healthComponent = entity.GetNodeOrNull<Components.HealthComponent>("HealthComponent");
            if (healthComponent != null)
            {
                _messageLog.ConnectToHealthComponent(healthComponent, entity);
            }
        }

        var playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
        _messageLog.ConnectToHealthComponent(playerHealth, player);

        player.Connect(Player.SignalName.Waited, Callable.From(() => _messageLog.AddMessage("You wait.", Palette.ToHex(Palette.Success))));

        player.Connect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
        player.Connect(Player.SignalName.ItemUsed, Callable.From<string, bool, string>(OnItemUsed));
        player.Connect(Player.SignalName.ItemDropped, Callable.From<string, bool, string>(OnItemDropped));
        player.Connect(Player.SignalName.ItemEquipped, Callable.From<string>(OnItemEquipped));
        player.Connect(Player.SignalName.ItemUnequipped, Callable.From<string>(OnItemUnequipped));

        player.Connect(Player.SignalName.GoldCollected, Callable.From<int, int>(OnGoldCollected));

        player.Connect(Player.SignalName.StandingOnEntity, Callable.From<string, string, Color>(OnStandingOnEntity));

        _conditionComponent = player.GetNodeOrNull<Components.ConditionComponent>("ConditionComponent");
        if (_conditionComponent != null)
        {
            _conditionComponent.Connect(Components.ConditionComponent.SignalName.ConditionMessage, Callable.From<string, string>((message, color) => _messageLog.AddMessage(message, color)));
        }

        _messageLog.AddMessage("Welcome to the Pits of Despair. Don't even think about trying to escape. (? for help)");
    }

    /// <summary>
    /// Connects to the examine system to display entity descriptions.
    /// </summary>
    public void ConnectToCursorTargetingSystem(CursorTargetingSystem cursorSystem)
    {
        _cursorSystem = cursorSystem;

        if (_cursorSystem != null)
        {
            _cursorSystem.Connect(CursorTargetingSystem.SignalName.CursorStarted, Callable.From<int>(OnCursorStarted));
            _cursorSystem.Connect(CursorTargetingSystem.SignalName.CursorMoved, Callable.From<BaseEntity>(OnCursorMoved));
            _cursorSystem.Connect(CursorTargetingSystem.SignalName.CursorCanceled, Callable.From<int>(OnCursorCanceled));
        }
    }

    /// <summary>
    /// Connects to the autoexplore system to display status messages.
    /// </summary>
    public void ConnectToAutoExploreSystem(AutoExploreSystem autoExploreSystem)
    {
        _autoExploreSystem = autoExploreSystem;

        if (_autoExploreSystem != null)
        {
            _autoExploreSystem.Connect(AutoExploreSystem.SignalName.AutoExploreStarted, Callable.From(OnAutoExploreStarted));
            _autoExploreSystem.Connect(AutoExploreSystem.SignalName.ExplorationComplete, Callable.From(OnExplorationComplete));
        }
    }

    private void OnAutoExploreStarted()
    {
        _messageLog.AddMessage("Exploring...", Palette.ToHex(Palette.Default));
    }

    private void OnExplorationComplete()
    {
        _messageLog.AddMessage("Nothing left to explore.", Palette.ToHex(Palette.Default));
    }

    /// <summary>
    /// Checks if any menu is currently open (including help modal, debug console, and level-up modal).
    /// </summary>
    public bool IsAnyMenuOpen()
    {
        return _currentMenuState != MenuState.None || _helpModal.Visible || _debugConsoleModal.Visible || _levelUpModal.Visible;
    }

    /// <summary>
    /// Checks if the entity detail modal is currently open.
    /// Used by InputHandler to prevent cursor ESC from canceling examine mode while modal is open.
    /// </summary>
    public bool IsEntityDetailModalOpen()
    {
        return _entityDetailModal != null && _entityDetailModal.IsOpen;
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

    private void OnInventoryItemSelected(char key)
    {
        // Hide inventory modal to avoid transparency stacking
        _inventoryPanel.Hide();

        // Open ItemDetailModal for the selected item
        _itemDetailModal.ShowMenu(key);
        _currentMenuState = MenuState.ItemDetail;
    }

    private void OnItemDetailCancelled()
    {
        // Close detail modal and return to inventory view
        _itemDetailModal.HideMenu();

        // Show inventory modal again
        _inventoryPanel.Show();
        _currentMenuState = MenuState.Inventory;
    }

    public void ShowEntityDetail(BaseEntity entity)
    {
        _entityDetailModal.ShowDetail(entity);
        // We don't change menu state here because we want to stay in "Examine Mode" context
        // But we need to block other inputs, which the modal does by handling input
    }

    private void OnEntityDetailCancelled()
    {
        _entityDetailModal.HideDetail();
        // Return to examine mode (which is still active in CursorTargetingSystem)
    }

    private void OnItemKeyRebound(char oldKey, char newKey)
    {
        // Delegate to PlayerActionHandler
        _actionHandler.RebindItemKey(oldKey, newKey);

        // Close all modals and return to game
        _itemDetailModal.HideMenu();
        _inventoryPanel.ToggleInventory(); // Use toggle to properly reset the _isVisible flag
        _currentMenuState = MenuState.None;
    }

    /// <summary>
    /// Called when PlayerActionHandler has rebound an item key.
    /// Displays the result message.
    /// </summary>
    private void OnItemReboundMessage(string message)
    {
        _messageLog.AddMessage(message, Palette.ToHex(Palette.Success));
    }

    private void OnActivateItemSelected(char key)
    {
        _activateItemPanel.HideMenu();
        _currentMenuState = MenuState.None;

        // Delegate to PlayerActionHandler
        _actionHandler.ActivateItem(key);
    }

    /// <summary>
    /// Called when PlayerActionHandler signals that item targeting should start.
    /// </summary>
    private void OnStartItemTargeting(char key)
    {
        EmitSignal(SignalName.StartItemTargeting, key);
        EnterTargetingMode();
    }

    /// <summary>
    /// Called when PlayerActionHandler signals that reach attack targeting should start.
    /// </summary>
    private void OnStartReachAttackTargeting(char key)
    {
        EmitSignal(SignalName.StartReachAttackTargeting, key);
        EnterTargetingMode();
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

        // Delegate to PlayerActionHandler
        _actionHandler.DropItem(key);
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

        // Delegate to PlayerActionHandler
        _actionHandler.EquipItem(key);
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
    public void CloseAllMenus()
    {
        _inventoryPanel.Hide();
        _activateItemPanel.HideMenu();
        _dropItemPanel.HideMenu();
        _equipPanel.HideMenu();
        _itemDetailModal.HideMenu();
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

    private void OnItemDropped(string itemName, bool success, string message)
    {
        if (success)
        {
            _messageLog.AddMessage(message, Palette.ToHex(Palette.Default));
        }
        else
        {
            _messageLog.AddMessage(message, Palette.ToHex(Palette.Disabled));
        }
    }

    private void OnItemEquipped(string itemName)
    {
        _messageLog.AddMessage($"You equip {itemName}.", Palette.ToHex(Palette.Equipment));
    }

    private void OnItemUnequipped(string itemName)
    {
        _messageLog.AddMessage($"You unequip {itemName}.", Palette.ToHex(Palette.Equipment));
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
            _messageLog.ConnectToHealthComponent(healthComponent, entity);
        }
    }

    /// <summary>
    /// Called when examine mode starts.
    /// </summary>
    private void OnCursorStarted(int mode)
    {
        var targetingMode = (CursorTargetingSystem.TargetingMode)mode;

        if (targetingMode == CursorTargetingSystem.TargetingMode.Examine)
        {
            _messageLog.AddMessage("[EXAMINE MODE] Use arrow keys to look around. Press X or ESC to exit.", Palette.ToHex(Palette.Alert));
        }
    }

    /// <summary>
    /// Called when the cursor moves to a new position.
    /// For examine mode, displays the entity's description in the message log.
    /// </summary>
    private void OnCursorMoved(BaseEntity entity)
    {
        // Only display examine messages when in examine mode
        if (_cursorSystem != null && _cursorSystem.CurrentMode == CursorTargetingSystem.TargetingMode.Examine)
        {
            // Only display message if there's an entity at the cursor position
            if (entity != null)
            {
                // Format: "EntityName: Description"
                string message = string.IsNullOrEmpty(entity.Description)
                    ? $"You see {entity.DisplayName}."
                    : $"{entity.DisplayName}: {entity.Description}";

                // Convert entity color to hex string for colored display name
                string colorHex = Palette.ToHex(entity.GlyphColor);
                string coloredMessage = $"[color={colorHex}]{message}[/color]";

                _messageLog.AddMessage(coloredMessage, null);
            }
            // Silent for empty tiles (as per user preference)
        }
    }

    /// <summary>
    /// Called when cursor targeting is canceled.
    /// </summary>
    private void OnCursorCanceled(int mode)
    {
        var targetingMode = (CursorTargetingSystem.TargetingMode)mode;

        if (targetingMode == CursorTargetingSystem.TargetingMode.Examine)
        {
            _messageLog.AddMessage("[EXAMINE MODE] Exited.", Palette.ToHex(Palette.Disabled));
        }
    }

    /// <summary>
    /// Toggles debug mode on/off.
    /// </summary>
    public void ToggleDebugMode()
    {
        _debugConsoleModal.SetDebugMode(!_debugConsoleModal.IsDebugModeActive);
    }

    /// <summary>
    /// Requests to open the debug console (if debug mode is enabled).
    /// </summary>
    public void RequestDebugConsole()
    {
        if (_debugConsoleModal.Visible)
        {
            _debugConsoleModal.HideConsole();
        }
        else
        {
            // Close any open menus first
            if (_currentMenuState != MenuState.None)
            {
                CloseAllMenus();
            }
            _debugConsoleModal.ShowConsole();
        }
    }

    private void OnDebugConsoleCancelled()
    {
        // Console handles its own hiding
    }

    /// <summary>
    /// Called when LevelUpSystem requests to show the level-up modal.
    /// </summary>
    private void OnShowLevelUpModal(int newLevel)
    {
        // Log level-up message
        _messageLog.AddMessage($"Level Up! You are now level {newLevel}.", Palette.ToHex(Palette.Success));

        // Show level-up modal for stat choice
        _levelUpModal.ShowForLevel(newLevel, _playerStats.TotalEndurance);
        _currentMenuState = MenuState.LevelUpChoice;
    }

    /// <summary>
    /// Called when the player chooses a stat to increase in the level-up modal.
    /// Delegates to LevelUpSystem for applying the reward.
    /// </summary>
    private void OnStatChosen(int statIndex)
    {
        // Delegate stat application to LevelUpSystem
        _levelUpSystem.ApplyStatChoice(statIndex);

        // Hide modal and reset menu state
        _levelUpModal.HideModal();
        _currentMenuState = MenuState.None;
    }

    /// <summary>
    /// Called when LevelUpSystem has increased a stat.
    /// Logs the stat increase message.
    /// </summary>
    private void OnStatIncreased(string statName, int newLevel)
    {
        _messageLog.AddMessage($"You increased {statName}!", Palette.ToHex(Palette.Success));
    }

    public override void _ExitTree()
    {
        // Disconnect from modal signals
        if (_inventoryPanel != null)
        {
            _inventoryPanel.Disconnect(InventoryModal.SignalName.Cancelled, Callable.From(OnInventoryCancelled));
            _inventoryPanel.Disconnect(InventoryModal.SignalName.ItemSelected, Callable.From<char>(OnInventoryItemSelected));
        }

        if (_itemDetailModal != null)
        {
            _itemDetailModal.Disconnect(ItemDetailModal.SignalName.KeyRebound, Callable.From<char, char>(OnItemKeyRebound));
            _itemDetailModal.Disconnect(ItemDetailModal.SignalName.Cancelled, Callable.From(OnItemDetailCancelled));
        }

        if (_entityDetailModal != null)
        {
            _entityDetailModal.Disconnect(EntityDetailModal.SignalName.Cancelled, Callable.From(OnEntityDetailCancelled));
        }

        if (_activateItemPanel != null)
        {
            _activateItemPanel.Disconnect(ActivateItemModal.SignalName.ItemSelected, Callable.From<char>(OnActivateItemSelected));
            _activateItemPanel.Disconnect(ActivateItemModal.SignalName.Cancelled, Callable.From(OnActivateItemCancelled));
        }

        if (_dropItemPanel != null)
        {
            _dropItemPanel.Disconnect(DropItemModal.SignalName.ItemSelected, Callable.From<char>(OnDropItemSelected));
            _dropItemPanel.Disconnect(DropItemModal.SignalName.Cancelled, Callable.From(OnDropItemCancelled));
        }

        if (_equipPanel != null)
        {
            _equipPanel.Disconnect(EquipModal.SignalName.ItemSelected, Callable.From<char>(OnEquipItemSelected));
            _equipPanel.Disconnect(EquipModal.SignalName.Cancelled, Callable.From(OnEquipItemCancelled));
        }

        if (_helpModal != null)
        {
            _helpModal.Disconnect(HelpModal.SignalName.Cancelled, Callable.From(OnHelpCancelled));
        }

        if (_levelUpModal != null)
        {
            _levelUpModal.Disconnect(LevelUpModal.SignalName.StatChosen, Callable.From<int>(OnStatChosen));
        }

        if (_debugConsoleModal != null)
        {
            _debugConsoleModal.Disconnect(DebugConsoleModal.SignalName.Cancelled, Callable.From(OnDebugConsoleCancelled));
        }

        // Disconnect from player signals
        if (_player != null)
        {
            _player.Disconnect(Player.SignalName.Waited, Callable.From(() => _messageLog.AddMessage("You wait.", Palette.ToHex(Palette.Success))));
            _player.Disconnect(Player.SignalName.ItemPickedUp, Callable.From<string, bool, string>(OnItemPickedUp));
            _player.Disconnect(Player.SignalName.ItemUsed, Callable.From<string, bool, string>(OnItemUsed));
            _player.Disconnect(Player.SignalName.ItemDropped, Callable.From<string, bool, string>(OnItemDropped));
            _player.Disconnect(Player.SignalName.ItemEquipped, Callable.From<string>(OnItemEquipped));
            _player.Disconnect(Player.SignalName.ItemUnequipped, Callable.From<string>(OnItemUnequipped));
            _player.Disconnect(Player.SignalName.GoldCollected, Callable.From<int, int>(OnGoldCollected));
            _player.Disconnect(Player.SignalName.StandingOnEntity, Callable.From<string, string, Color>(OnStandingOnEntity));
        }

        // Disconnect from player components
        if (_conditionComponent != null)
        {
            _conditionComponent.Disconnect(Components.ConditionComponent.SignalName.ConditionMessage, Callable.From<string, string>((message, color) => _messageLog.AddMessage(message, color)));
        }

        // Disconnect from new systems
        if (_levelUpSystem != null)
        {
            _levelUpSystem.Disconnect(Systems.LevelUpSystem.SignalName.ShowLevelUpModal, Callable.From<int>(OnShowLevelUpModal));
            _levelUpSystem.Disconnect(Systems.LevelUpSystem.SignalName.StatIncreased, Callable.From<string, int>(OnStatIncreased));
        }

        if (_actionHandler != null)
        {
            _actionHandler.Disconnect(Systems.PlayerActionHandler.SignalName.ItemRebound, Callable.From<string>(OnItemReboundMessage));
            _actionHandler.Disconnect(Systems.PlayerActionHandler.SignalName.StartItemTargeting, Callable.From<char>(OnStartItemTargeting));
            _actionHandler.Disconnect(Systems.PlayerActionHandler.SignalName.StartReachAttackTargeting, Callable.From<char>(OnStartReachAttackTargeting));
        }

        if (_autoExploreSystem != null)
        {
            _autoExploreSystem.Disconnect(AutoExploreSystem.SignalName.AutoExploreStarted, Callable.From(OnAutoExploreStarted));
            _autoExploreSystem.Disconnect(AutoExploreSystem.SignalName.ExplorationComplete, Callable.From(OnExplorationComplete));
        }

        // Disconnect from entity manager
        if (_entityManager != null)
        {
            _entityManager.Disconnect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));
        }

        // Disconnect from cursor system
        if (_cursorSystem != null)
        {
            _cursorSystem.Disconnect(CursorTargetingSystem.SignalName.CursorStarted, Callable.From<int>(OnCursorStarted));
            _cursorSystem.Disconnect(CursorTargetingSystem.SignalName.CursorMoved, Callable.From<BaseEntity>(OnCursorMoved));
            _cursorSystem.Disconnect(CursorTargetingSystem.SignalName.CursorCanceled, Callable.From<int>(OnCursorCanceled));
        }
    }
}
