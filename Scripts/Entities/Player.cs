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
    public delegate void ItemDroppedEventHandler(string itemName, bool success, string message);

    [Signal]
    public delegate void ItemEquippedEventHandler(string itemName);

    [Signal]
    public delegate void ItemUnequippedEventHandler(string itemName);

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
        DisplayName = "Player";
        Description = "A condemned prisoner, exiled to die in these forsaken depths. Weary but determined.";
        Glyph = "@";
        GlyphColor = Palette.Player;
        Faction = Faction.Player;

        // Create all components programmatically
        _movementComponent = new MovementComponent { Name = "MovementComponent" };
        AddChild(_movementComponent);

        var visionComponent = new VisionComponent { Name = "VisionComponent" };
        AddChild(visionComponent);

        var attackComponent = new AttackComponent { Name = "AttackComponent" };
        AddChild(attackComponent);

        var statsComponent = new StatsComponent
        {
            Name = "StatsComponent",
            BaseStrength = 0,
            BaseAgility = 0,
            BaseEndurance = 0,
            BaseWill = 0
        };
        AddChild(statsComponent);

        var healthComponent = new HealthComponent
        {
            Name = "HealthComponent",
            BaseMaxHP = 20
        };
        AddChild(healthComponent);

        var willpowerComponent = new WillpowerComponent { Name = "WillpowerComponent" };
        AddChild(willpowerComponent);

        var skillComponent = new SkillComponent { Name = "SkillComponent" };
        AddChild(skillComponent);

        _inventoryComponent = new InventoryComponent { Name = "InventoryComponent" };
        AddChild(_inventoryComponent);

        _inventoryComponent.Connect(InventoryComponent.SignalName.InventoryChanged, Callable.From(() => EmitSignal(SignalName.InventoryChanged)));

        var equipComponent = new EquipComponent { Name = "EquipComponent" };
        AddChild(equipComponent);

        var conditionComponent = new ConditionComponent
        {
            Name = "ConditionComponent",
            IsPlayerControlled = true
        };
        AddChild(conditionComponent);

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
    /// Handle position changes to auto-collect gold and check for entities.
    /// </summary>
    private void OnPositionChanged(int x, int y)
    {
        if (x != _previousPosition.X || y != _previousPosition.Y)
        {
            _previousPosition = new GridPosition(x, y);

            CheckForThroneVictory();
            TryAutoCollectItems();
            CheckForWalkableEntity();
        }
    }

    /// <summary>
    /// Checks if the player has stepped on the Throne of Despair (win condition).
    /// Must be checked before auto-collect so throne message takes priority.
    /// </summary>
    private void CheckForThroneVictory()
    {
        if (_entityManager == null)
        {
            return;
        }

        // Check all entities at position since cache may only contain one
        var entitiesAtPosition = _entityManager.GetEntitiesAtPosition(GridPosition);
        foreach (var entity in entitiesAtPosition)
        {
            if (entity is ThroneOfDespair)
            {
                // Find GameManager and trigger victory
                var gameManager = GetTree()?.Root.GetNodeOrNull<Systems.GameManager>("GameManager");
                if (gameManager != null)
                {
                    gameManager.OnThroneReached();
                }
                return;
            }
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

        // Use GetItemAtPosition since items are walkable and cache may contain a creature instead
        var itemEntity = _entityManager.GetItemAtPosition(GridPosition);
        if (itemEntity != null && itemEntity.IsWalkable)
        {
            EmitSignal(SignalName.StandingOnEntity, itemEntity.DisplayName, itemEntity.Glyph, itemEntity.GlyphColor);
        }
    }

    /// <summary>
    /// Automatically collects gold and autopickup items at the player's current position.
    /// Called when player moves to a new tile.
    /// </summary>
    private void TryAutoCollectItems()
    {
        if (_entityManager == null || _goldManager == null || _inventoryComponent == null)
        {
            return;
        }

        // Check all entities at position since cache may only contain one
        var entitiesAtPosition = _entityManager.GetEntitiesAtPosition(GridPosition);

        foreach (var entity in entitiesAtPosition)
        {
            // Handle gold (existing behavior)
            if (entity is Gold goldPile)
            {
                int goldAmount = goldPile.Amount;
                if (goldAmount <= 0)
                {
                    goldAmount = 1;
                }

                _goldManager.AddGold(goldAmount);

                // RemoveEntity already calls QueueFree
                _entityManager.RemoveEntity(entity);

                EmitSignal(SignalName.GoldCollected, goldAmount, _goldManager.Gold);
                continue;
            }

            // Handle items with autopickup enabled
            if (entity.ItemData != null && entity.ItemData.AutoPickup)
            {
                var itemInstance = entity.ItemData;
                if (itemInstance == null)
                {
                    continue;
                }

                // Try to add to inventory
                char? slotKey = _inventoryComponent.AddItem(itemInstance, out string message, excludeEquipped: false);

                if (slotKey.HasValue)
                {
                    // Successfully picked up (RemoveEntity already calls QueueFree)
                    _entityManager.RemoveEntity(entity);

                    string displayName = itemInstance.Template.GetDisplayName(itemInstance.Quantity);
                    EmitSignal(SignalName.ItemPickedUp, itemInstance.Template.Name, true, $"You collect {displayName}.");
                }
                else
                {
                    // Inventory full
                    EmitSignal(SignalName.ItemPickedUp, itemInstance.Template.Name, false, message);
                }
            }
        }
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
    /// <param name="key">The key to look up (a-z or A-Z).</param>
    /// <returns>The inventory slot, or null if not found.</returns>
    public InventorySlot? GetInventorySlot(char key)
    {
        return _inventoryComponent?.GetSlot(key);
    }

    /// <summary>
    /// Removes items from the player's inventory.
    /// </summary>
    /// <param name="key">The inventory slot key (a-z or A-Z).</param>
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
    public void EmitItemDropped(string itemName, bool success, string message)
    {
        EmitSignal(SignalName.ItemDropped, itemName, success, message);
    }
}
