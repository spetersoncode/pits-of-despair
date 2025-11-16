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

    public override void _Ready()
    {
        _statsPanel = GetNode<StatsPanel>("StatsPanel");
        _messageLog = GetNode<MessageLog>("MessageLog");
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

        // Subscribe to all entity deaths (including enemies)
        entityManager.EntityRemoved += OnEntityRemoved;

        // Subscribe to player death specifically
        var playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
        _messageLog.ConnectToHealthComponent(playerHealth, player.DisplayName);

        // Subscribe to wait action
        player.Waited += () => _messageLog.AddMessage("You wait.", "#66ff66");

        // Add welcome message
        _messageLog.AddMessage("Welcome to the Pits of Despair. Don't even think about trying to escape.");
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
