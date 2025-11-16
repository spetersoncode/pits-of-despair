using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.UI;

/// <summary>
/// Main HUD coordinator that manages and wires up UI panels.
/// </summary>
public partial class GameHUD : Control
{
    private StatsPanel _statsPanel;
    private MessageLog _messageLog;
    private InventoryPanel _inventoryPanel;

    public override void _Ready()
    {
        _statsPanel = GetNode<StatsPanel>("StatsPanel");
        _messageLog = GetNode<MessageLog>("MessageLog");
        _inventoryPanel = GetNode<InventoryPanel>("InventoryPanel");
    }

    /// <summary>
    /// Initializes the HUD by connecting to game systems.
    /// Should be called after the HUD is added to the scene tree.
    /// </summary>
    /// <param name="player">The player entity.</param>
    /// <param name="combatSystem">The combat system for attack events.</param>
    /// <param name="entityManager">The entity manager for death notifications.</param>
    /// <param name="floorDepth">The current floor depth.</param>
    public void Initialize(Player player, CombatSystem combatSystem, EntityManager entityManager, int floorDepth)
    {
        // Wire up stats panel
        _statsPanel.ConnectToPlayer(player);
        _statsPanel.SetFloorDepth(floorDepth);
        _statsPanel.SetEntityManager(entityManager);

        // Wire up message log
        _messageLog.ConnectToCombatSystem(combatSystem);

        // Wire up inventory panel
        _inventoryPanel.ConnectToPlayer(player);

        // Subscribe to all entity deaths (including enemies)
        entityManager.EntityRemoved += OnEntityRemoved;

        // Subscribe to player death specifically
        var playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
        _messageLog.ConnectToHealthComponent(playerHealth, player.DisplayName);

        // Subscribe to wait action
        player.Waited += () => _messageLog.AddMessage("You wait.", "#66ff66");

        // Subscribe to item pickup events
        player.ItemPickedUp += OnItemPickedUp;

        // Add welcome message
        _messageLog.AddMessage("Welcome to the Pits of Despair. Don't even think about trying to escape.");
    }

    /// <summary>
    /// Toggles the inventory panel visibility.
    /// Called by InputHandler when 'I' is pressed.
    /// </summary>
    public void ToggleInventory()
    {
        _inventoryPanel.ToggleInventory();
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

    private void OnEntityRemoved(BaseEntity entity)
    {
        // Only log death for non-player entities (player death is handled separately)
        if (entity.DisplayName != "Player")
        {
            _messageLog.AddMessage($"{entity.DisplayName} dies!", "#ffff66"); // Yellow
        }
    }
}
