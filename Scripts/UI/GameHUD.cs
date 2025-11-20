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
    private DebugConsoleModal _debugConsoleModal;
    private LevelUpModal _levelUpModal;
    private Player _player;
    private ActionContext _actionContext;
    private MenuState _currentMenuState = MenuState.None;
    private CursorTargetingSystem _cursorSystem;

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
    /// <param name="actionContext">The action context for executing actions.</param>
    /// <param name="goldManager">The gold manager for tracking player gold.</param>
    /// <param name="visionSystem">The player vision system for checking visible entities.</param>
    /// <param name="debugContext">The debug context for debug commands.</param>
    public void Initialize(Player player, CombatSystem combatSystem, EntityManager entityManager, int floorDepth, ActionContext actionContext, GoldManager goldManager, PlayerVisionSystem visionSystem = null, DebugContext debugContext = null)
    {
        _player = player;
        _actionContext = actionContext;

        _sidePanel.ConnectToPlayer(player);
        _sidePanel.ConnectToGoldManager(goldManager);
        _sidePanel.SetFloorDepth(floorDepth);
        _sidePanel.SetEntityManager(entityManager);
        if (visionSystem != null)
        {
            _sidePanel.SetVisionSystem(visionSystem);
        }

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

        _activateItemPanel.Connect(ActivateItemModal.SignalName.ItemSelected, Callable.From<char>(OnActivateItemSelected));
        _activateItemPanel.Connect(ActivateItemModal.SignalName.Cancelled, Callable.From(OnActivateItemCancelled));
        _dropItemPanel.Connect(DropItemModal.SignalName.ItemSelected, Callable.From<char>(OnDropItemSelected));
        _dropItemPanel.Connect(DropItemModal.SignalName.Cancelled, Callable.From(OnDropItemCancelled));
        _equipPanel.Connect(EquipModal.SignalName.ItemSelected, Callable.From<char>(OnEquipItemSelected));
        _equipPanel.Connect(EquipModal.SignalName.Cancelled, Callable.From(OnEquipItemCancelled));
        _helpModal.Connect(HelpModal.SignalName.Cancelled, Callable.From(OnHelpCancelled));

        _levelUpModal.Connect(LevelUpModal.SignalName.StatChosen, Callable.From<int>(OnStatChosen));

        // Connect to player's StatsComponent for level-up notifications
        var playerStats = player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");
        if (playerStats != null)
        {
            playerStats.Connect(Components.StatsComponent.SignalName.LevelUp, Callable.From<int>(OnLevelUp));
        }

        // Initialize debug console if debug context provided
        if (debugContext != null)
        {
            _debugConsoleModal.Initialize(_messageLog, debugContext);
            _debugConsoleModal.Connect(DebugConsoleModal.SignalName.Cancelled, Callable.From(OnDebugConsoleCancelled));
        }

        entityManager.Connect(EntityManager.SignalName.EntityAdded, Callable.From<BaseEntity>(OnEntityAdded));

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
        player.Connect(Player.SignalName.ItemDropped, Callable.From<string>(OnItemDropped));
        player.Connect(Player.SignalName.ItemEquipped, Callable.From<string>(OnItemEquipped));
        player.Connect(Player.SignalName.ItemUnequipped, Callable.From<string>(OnItemUnequipped));

        player.Connect(Player.SignalName.GoldCollected, Callable.From<int, int>(OnGoldCollected));

        player.Connect(Player.SignalName.StandingOnEntity, Callable.From<string, string, Color>(OnStandingOnEntity));

        var statusComponent = player.GetNodeOrNull<Components.StatusComponent>("StatusComponent");
        if (statusComponent != null)
        {
            statusComponent.Connect(Components.StatusComponent.SignalName.StatusMessage, Callable.From<string, string>((message, color) => _messageLog.AddMessage(message, color)));
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
    /// Checks if any menu is currently open (including help modal, debug console, and level-up modal).
    /// </summary>
    public bool IsAnyMenuOpen()
    {
        return _currentMenuState != MenuState.None || _helpModal.Visible || _debugConsoleModal.Visible || _levelUpModal.Visible;
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

    private void OnItemKeyRebound(char oldKey, char newKey)
    {
        // Get inventory component and perform rebind
        var inventoryComponent = _player.GetNodeOrNull<Components.InventoryComponent>("InventoryComponent");
        if (inventoryComponent != null)
        {
            bool success = inventoryComponent.RebindItemKey(oldKey, newKey);
            if (success)
            {
                var slot = _player.GetInventorySlot(newKey);
                if (slot != null)
                {
                    // Build message explaining what happened
                    string message;
                    if (oldKey == newKey)
                    {
                        message = $"{slot.Item.Template.Name} remains on '{newKey}'.";
                    }
                    else
                    {
                        // Check if there was a swap
                        var oldSlot = _player.GetInventorySlot(oldKey);
                        if (oldSlot != null)
                        {
                            message = $"{slot.Item.Template.Name} rebound to '{newKey}' (swapped with {oldSlot.Item.Template.Name} on '{oldKey}').";
                        }
                        else
                        {
                            message = $"{slot.Item.Template.Name} rebound to '{newKey}'.";
                        }
                    }
                    _messageLog.AddMessage(message, Palette.ToHex(Palette.Success));
                }
            }
        }

        // Close all modals and return to game
        _itemDetailModal.HideMenu();
        _inventoryPanel.ToggleInventory(); // Use toggle to properly reset the _isVisible flag
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

    private void OnItemDropped(string itemName)
    {
        _messageLog.AddMessage($"You drop {itemName}.", Palette.ToHex(Palette.Default));
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
    /// Called when the player levels up.
    /// Increases HP permanently and shows the stat choice modal.
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        // Log level-up message
        _messageLog.AddMessage($"Level Up! You are now level {newLevel}.", Palette.ToHex(Palette.Success));

        // Increase player's BaseMaxHP by 4
        var healthComponent = _player.GetNodeOrNull<Components.HealthComponent>("HealthComponent");
        var statsComponent = _player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");
        if (healthComponent != null && statsComponent != null)
        {
            healthComponent.BaseMaxHP += 4;
            // Trigger HP recalculation by emitting StatsChanged
            statsComponent.EmitSignal(Components.StatsComponent.SignalName.StatsChanged);
        }

        // Show level-up modal for stat choice
        _levelUpModal.ShowForLevel(newLevel);
        _currentMenuState = MenuState.LevelUpChoice;
    }

    /// <summary>
    /// Called when the player chooses a stat to increase.
    /// </summary>
    private void OnStatChosen(int statIndex)
    {
        var statsComponent = _player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");
        if (statsComponent == null) return;

        string statName = "";

        // Increase the chosen stat
        switch (statIndex)
        {
            case 0: // Strength
                statsComponent.BaseStrength++;
                statName = "Strength";
                break;
            case 1: // Agility
                statsComponent.BaseAgility++;
                statName = "Agility";
                break;
            case 2: // Endurance
                statsComponent.BaseEndurance++;
                statName = "Endurance";
                break;
            case 3: // Will
                statsComponent.BaseWill++;
                statName = "Will";
                break;
        }

        // Emit StatsChanged to trigger recalculation
        statsComponent.EmitSignal(Components.StatsComponent.SignalName.StatsChanged);

        // Log stat choice
        _messageLog.AddMessage($"You increased {statName}!", Palette.ToHex(Palette.Success));

        // Hide modal and reset menu state
        _levelUpModal.HideModal();
        _currentMenuState = MenuState.None;
    }
}
