using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Systems;
using PitsOfDespair.UI;
using System.Collections.Generic;

namespace PitsOfDespair.Entities;

/// <summary>
/// The player character.
/// Now uses component-based architecture with MovementComponent.
/// </summary>
public partial class Player : BaseEntity
{
    [Signal]
    public delegate void TurnCompletedEventHandler();

    [Signal]
    public delegate void WaitedEventHandler();

    [Signal]
    public delegate void ItemPickedUpEventHandler(string itemName, bool success, string message);

    [Signal]
    public delegate void ItemUsedEventHandler(string itemName, bool success, string message);

    [Signal]
    public delegate void ItemDroppedEventHandler(string itemName);

    [Signal]
    public delegate void InventoryChangedEventHandler();

    [Signal]
    public delegate void GoldCollectedEventHandler(int amount, int totalGold);

    [Signal]
    public delegate void StandingOnEntityEventHandler(string entityName, string entityGlyph, Color entityColor);

    [Signal]
    public delegate void RangedAttackRequestedEventHandler(Vector2I origin, Vector2I target, BaseEntity targetEntity, int attackIndex);

    private MovementComponent? _movementComponent;
    private InventoryComponent? _inventoryComponent;
    private GridPosition _previousPosition;
    private EntityManager? _entityManager;
    private GoldManager? _goldManager;

    public override void _Ready()
    {
        // Set player properties
        DisplayName = "Player";
        Glyph = "@";
        GlyphColor = Palette.Player;

        // Get MovementComponent child
        _movementComponent = GetNode<MovementComponent>("MovementComponent");

        // Add StatsComponent (base-0 stats: player starts average in all stats)
        var statsComponent = new StatsComponent
        {
            Name = "StatsComponent",
            BaseStrength = 0,
            BaseAgility = 0,
            BaseEndurance = 0,
            BaseWill = 0
        };
        AddChild(statsComponent);

        // Add HealthComponent (20 base HP + Endurance bonus)
        var healthComponent = new HealthComponent
        {
            Name = "HealthComponent",
            BaseMaxHP = 20
        };
        AddChild(healthComponent);

        // Note: Attack component with default punch is now created by EntityFactory

        // Add InventoryComponent to player
        _inventoryComponent = new InventoryComponent { Name = "InventoryComponent" };
        AddChild(_inventoryComponent);

        // Relay inventory changed signal for UI compatibility
        _inventoryComponent.Connect(InventoryComponent.SignalName.InventoryChanged, Callable.From(() => EmitSignal(SignalName.InventoryChanged)));

        // Add EquipComponent to player
        var equipComponent = new EquipComponent { Name = "EquipComponent" };
        AddChild(equipComponent);

        // Note: Starting equipment will be added by EntityFactory in GameLevel initialization

        // Track position changes to emit Moved signal for backwards compatibility
        Connect(SignalName.PositionChanged, Callable.From<int, int>(OnPositionChanged));
    }

    /// <summary>
    /// Initialize player at spawn position.
    /// Called by GameLevel after map generation.
    /// </summary>
    /// <param name="spawnPosition">The grid position to spawn at.</param>
    public void Initialize(GridPosition spawnPosition)
    {
        SetGridPosition(spawnPosition);
        _previousPosition = spawnPosition;
    }


    /// <summary>
    /// Attempts to move the player in the specified direction.
    /// Delegates to MovementComponent which emits signals for MovementSystem to validate.
    /// </summary>
    /// <param name="direction">Direction to move (use Vector2I for grid directions).</param>
    public void TryMove(Vector2I direction)
    {
        if (_movementComponent == null)
        {
            GD.PushWarning("Player: MovementComponent not found!");
            return;
        }

        // Store current position to check if move succeeded
        _previousPosition = GridPosition;

        // Request move via component - MovementSystem will validate and update position
        _movementComponent.RequestMove(direction);
    }


    /// <summary>
    /// Execute an action using the action system.
    /// This is the unified entry point for all turn-consuming actions.
    /// Overrides base to emit TurnCompleted signal when appropriate.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">The action context containing game systems and state.</param>
    /// <returns>The result of the action execution.</returns>
    public override ActionResult ExecuteAction(Action action, ActionContext context)
    {
        var result = base.ExecuteAction(action, context);

        // If action consumed a turn, process recharging and emit turn completed signal
        if (result.ConsumesTurn)
        {
            ProcessItemRecharging();
            EmitSignal(SignalName.TurnCompleted);
        }

        return result;
    }

    /// <summary>
    /// Processes recharging for all items in inventory that have recharge capabilities.
    /// Called each turn when an action is successfully executed.
    /// </summary>
    private void ProcessItemRecharging()
    {
        if (_inventoryComponent == null)
            return;

        foreach (var slot in _inventoryComponent.Inventory)
        {
            slot.Item.ProcessTurn();
        }
    }

    /// <summary>
    /// Handle position changes to emit turn completion and auto-collect gold.
    /// </summary>
    private void OnPositionChanged(int x, int y)
    {
        // Only process if position actually changed
        if (x != _previousPosition.X || y != _previousPosition.Y)
        {
            _previousPosition = new GridPosition(x, y);

            // Check for gold at new position and auto-collect
            TryAutoCollectGold();

            // Check for walkable entities to show "You see here" message
            CheckForWalkableEntity();

            EmitSignal(SignalName.TurnCompleted);
        }
    }

    /// <summary>
    /// Checks for walkable entities at the player's position and emits signal for "You see here" message.
    /// Called after gold collection, so it won't show collected gold.
    /// </summary>
    private void CheckForWalkableEntity()
    {
        if (_entityManager == null)
        {
            return;
        }

        var entityAtPosition = _entityManager.GetEntityAtPosition(GridPosition);
        if (entityAtPosition != null && entityAtPosition.IsWalkable)
        {
            EmitSignal(SignalName.StandingOnEntity, entityAtPosition.DisplayName, entityAtPosition.Glyph, entityAtPosition.GlyphColor);
        }
    }

    /// <summary>
    /// Automatically collects gold at the player's current position.
    /// Called when player moves to a new tile.
    /// </summary>
    private void TryAutoCollectGold()
    {
        if (_entityManager == null || _goldManager == null)
        {
            return;
        }

        // Check for entity at player's current position
        var entityAtPosition = _entityManager.GetEntityAtPosition(GridPosition);

        // Check if the entity is a Gold pile
        if (entityAtPosition is not Gold goldPile)
        {
            return;
        }

        // Get gold amount
        int goldAmount = goldPile.Amount;
        if (goldAmount <= 0)
        {
            goldAmount = 1; // Default to 1 if not set
        }

        // Add to gold manager
        _goldManager.AddGold(goldAmount);

        // Remove gold entity from world
        _entityManager.RemoveEntity(entityAtPosition);
        entityAtPosition.QueueFree();

        // Emit signal for UI feedback
        EmitSignal(SignalName.GoldCollected, goldAmount, _goldManager.Gold);
    }

    /// <summary>
    /// Gets the world position for rendering (based on tile size).
    /// </summary>
    public Vector2 GetWorldPosition(int tileSize)
    {
        return GridPosition.ToWorld(tileSize);
    }

    /// <summary>
    /// Property for backwards compatibility with renderer.
    /// </summary>
    public GridPosition CurrentPosition => GridPosition;

    /// <summary>
    /// Gets the player's inventory (read-only).
    /// </summary>
    public IReadOnlyList<InventorySlot> Inventory => _inventoryComponent?.Inventory ?? System.Array.Empty<InventorySlot>();

    /// <summary>
    /// Sets the EntityManager reference for item pickup.
    /// Called by GameLevel during initialization.
    /// </summary>
    public void SetEntityManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Sets the GoldManager reference for gold tracking.
    /// Called by GameLevel during initialization.
    /// </summary>
    public void SetGoldManager(GoldManager goldManager)
    {
        _goldManager = goldManager;
    }



    /// <summary>
    /// Emits item pickup feedback signals.
    /// Used by PickupAction to maintain consistent event signaling.
    /// </summary>
    public void EmitItemPickupFeedback(string itemName, bool success, string message)
    {
        EmitSignal(SignalName.ItemPickedUp, itemName, success, message);
        if (success)
        {
            EmitSignal(SignalName.InventoryChanged);
        }
    }

    /// <summary>
    /// Emits wait action feedback signal.
    /// Used by WaitAction to maintain consistent event signaling.
    /// </summary>
    public void EmitWaitFeedback()
    {
        EmitSignal(SignalName.Waited);
    }

    /// <summary>
    /// Gets an inventory slot by its key binding.
    /// </summary>
    /// <param name="key">The key to look up (a-z).</param>
    /// <returns>The inventory slot, or null if not found.</returns>
    public InventorySlot? GetInventorySlot(char key)
    {
        return _inventoryComponent?.GetSlot(key);
    }

    /// <summary>
    /// Removes items from the player's inventory.
    /// </summary>
    /// <param name="key">The inventory slot key (a-z).</param>
    /// <param name="count">The number of items to remove.</param>
    /// <returns>True if items were removed successfully.</returns>
    public bool RemoveItemFromInventory(char key, int count = 1)
    {
        if (_inventoryComponent == null)
            return false;

        return _inventoryComponent.RemoveItem(key, count);
    }

    /// <summary>
    /// Emits item usage feedback signals.
    /// Used by ActivateItemAction to maintain consistent event signaling.
    /// </summary>
    public void EmitItemUsed(string itemName, bool success, string message)
    {
        EmitSignal(SignalName.ItemUsed, itemName, success, message);
    }

    /// <summary>
    /// Emits item dropped feedback signal.
    /// Used by DropItemAction to maintain consistent event signaling.
    /// </summary>
    public void EmitItemDropped(string itemName)
    {
        EmitSignal(SignalName.ItemDropped, itemName);
    }
}
