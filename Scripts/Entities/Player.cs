using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using PitsOfDespair.Systems;
using PitsOfDespair.UI;
using System.Collections.Generic;
using System.Linq;

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

    private const int MaxInventorySlots = 26;

    private MovementComponent? _movementComponent;
    private GridPosition _previousPosition;
    private List<InventorySlot> _inventory = new();
    private EntityManager? _entityManager;
    private int _score = 0;

    public override void _Ready()
    {
        // Set player properties
        DisplayName = "Player";
        Glyph = "@";
        GlyphColor = Colors.Yellow;

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

        // Add EquipComponent to player
        var equipComponent = new EquipComponent { Name = "EquipComponent" };
        AddChild(equipComponent);

        // Add starting equipment to inventory
        AddStartingEquipment();

        // Track position changes to emit Moved signal for backwards compatibility
        PositionChanged += OnPositionChanged;
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
    /// Adds starting equipment to player's inventory and equips it.
    /// Called during _Ready() to give player initial equipment.
    /// </summary>
    private void AddStartingEquipment()
    {
        var dataLoader = GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
        {
            GD.PushError("Player: DataLoader not found! Cannot add starting equipment.");
            return;
        }

        var equipComponent = GetNodeOrNull<EquipComponent>("EquipComponent");

        // Add short sword to inventory
        var shortSwordData = dataLoader.GetItem("weapon_short_sword");
        if (shortSwordData != null)
        {
            var shortSwordInstance = new ItemInstance(shortSwordData);
            char key = GetNextAvailableKey();
            var slot = new InventorySlot(key, shortSwordInstance, 1);
            _inventory.Add(slot);

            // Auto-equip the short sword
            if (equipComponent != null)
            {
                var equipSlot = shortSwordData.GetEquipmentSlot();
                equipComponent.Equip(key, equipSlot);
            }
        }
        else
        {
            GD.PushWarning("Player: Short sword data not found. Player starting with unarmed.");
        }

        // Add padded armor to inventory
        var paddedArmorData = dataLoader.GetItem("armor_padded");
        if (paddedArmorData != null)
        {
            var paddedArmorInstance = new ItemInstance(paddedArmorData);
            char key = GetNextAvailableKey();
            var slot = new InventorySlot(key, paddedArmorInstance, 1);
            _inventory.Add(slot);

            // Auto-equip the padded armor
            if (equipComponent != null)
            {
                var equipSlot = paddedArmorData.GetEquipmentSlot();
                equipComponent.Equip(key, equipSlot);
            }
        }
        else
        {
            GD.PushWarning("Player: Padded armor data not found. Player starting without armor.");
        }

        EmitSignal(SignalName.InventoryChanged);
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
    /// Wait for one turn, recovering 1 HP.
    /// </summary>
    public void Wait()
    {
        // Heal the player
        var healthComponent = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent != null)
        {
            healthComponent.Heal(1);
        }

        // Emit waited signal for message log
        EmitSignal(SignalName.Waited);

        // End the turn
        EmitSignal(SignalName.TurnCompleted);
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
        foreach (var slot in _inventory)
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
        if (_entityManager == null)
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

        // Add to score
        _score += goldAmount;

        // Remove gold entity from world
        _entityManager.RemoveEntity(entityAtPosition);
        entityAtPosition.QueueFree();

        // Emit signal for UI feedback
        EmitSignal(SignalName.GoldCollected, goldAmount, _score);
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
    public IReadOnlyList<InventorySlot> Inventory => _inventory.AsReadOnly();

    /// <summary>
    /// Gets the player's current score (gold collected).
    /// </summary>
    public int Score => _score;

    /// <summary>
    /// Sets the EntityManager reference for item pickup.
    /// Called by GameLevel during initialization.
    /// </summary>
    public void SetEntityManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Attempts to pick up an item at the player's current position.
    /// Returns true if an item was picked up and turn should be consumed.
    /// </summary>
    public bool TryPickupItem()
    {
        if (_entityManager == null)
        {
            GD.PushWarning("Player: EntityManager not set, cannot pick up items!");
            return false;
        }

        // Check for item at player's current position
        var entityAtPosition = _entityManager.GetEntityAtPosition(GridPosition);
        var itemComponent = entityAtPosition?.GetNodeOrNull<ItemComponent>("ItemComponent");

        if (itemComponent == null)
        {
            EmitSignal(SignalName.ItemPickedUp, "", false, "Nothing to pick up.");
            return false; // No turn consumed
        }

        // Check if item is stackable (consumables only, charged items never stack)
        bool canStack = itemComponent.Item.Template.GetIsConsumable();

        // Check if inventory is full (26 unique items)
        if (_inventory.Count >= MaxInventorySlots)
        {
            // Check if we can stack with existing item
            var existingSlot = canStack ? _inventory.FirstOrDefault(slot =>
                slot.Item.Template.DataFileId == itemComponent.Item.Template.DataFileId) : null;

            if (existingSlot == null)
            {
                EmitSignal(SignalName.ItemPickedUp, entityAtPosition.DisplayName, false,
                    "Inventory full! (26 unique items)");
                return false; // No turn consumed
            }

            // Stack with existing item
            existingSlot.Add(1);
        }
        else
        {
            // Try to find existing slot for stacking
            var existingSlot = canStack ? _inventory.FirstOrDefault(slot =>
                slot.Item.Template.DataFileId == itemComponent.Item.Template.DataFileId) : null;

            if (existingSlot != null)
            {
                // Stack with existing item
                existingSlot.Add(1);
            }
            else
            {
                // Add new slot with next available key
                char nextKey = GetNextAvailableKey();
                var newSlot = new InventorySlot(nextKey, itemComponent.Item, 1);
                _inventory.Add(newSlot);
            }
        }

        // Remove item from world
        _entityManager.RemoveEntity(entityAtPosition);
        entityAtPosition.QueueFree();

        // Notify listeners
        string itemName = entityAtPosition.DisplayName;
        EmitSignal(SignalName.ItemPickedUp, itemName, true, $"You pick up the {itemName}.");
        EmitSignal(SignalName.InventoryChanged);

        return true; // Turn consumed
    }

    /// <summary>
    /// Gets the next available inventory key (a-z).
    /// </summary>
    private char GetNextAvailableKey()
    {
        for (char c = 'a'; c <= 'z'; c++)
        {
            if (!_inventory.Any(slot => slot.Key == c))
            {
                return c;
            }
        }

        // This shouldn't happen due to MaxInventorySlots check, but just in case
        return 'z';
    }

    /// <summary>
    /// Adds an item to the player's inventory.
    /// Used by PickupAction to manage inventory.
    /// </summary>
    /// <param name="itemEntity">The entity with ItemComponent to add.</param>
    /// <param name="message">Output message describing the result.</param>
    /// <returns>True if the item was added successfully.</returns>
    public bool AddItemToInventory(BaseEntity itemEntity, out string message)
    {
        var itemComponent = itemEntity.GetNodeOrNull<ItemComponent>("ItemComponent");
        if (itemComponent == null)
        {
            message = "Not an item!";
            return false;
        }

        // Check if item is stackable (consumables only, charged items never stack)
        bool canStack = itemComponent.Item.Template.GetIsConsumable();

        // Get equipped items to exclude from stacking (equipped items never stack)
        var equipComponent = GetNodeOrNull<EquipComponent>("EquipComponent");

        // Check if inventory is full (26 unique items)
        if (_inventory.Count >= MaxInventorySlots)
        {
            // Check if we can stack with existing item (exclude equipped items)
            var existingSlot = canStack ? _inventory.FirstOrDefault(slot =>
                slot.Item.Template.DataFileId == itemComponent.Item.Template.DataFileId &&
                (equipComponent == null || !equipComponent.IsEquipped(slot.Key))) : null;

            if (existingSlot == null)
            {
                message = "Inventory full! (26 unique items)";
                return false;
            }

            // Stack with existing item
            existingSlot.Add(1);
        }
        else
        {
            // Try to find existing slot for stacking (exclude equipped items)
            var existingSlot = canStack ? _inventory.FirstOrDefault(slot =>
                slot.Item.Template.DataFileId == itemComponent.Item.Template.DataFileId &&
                (equipComponent == null || !equipComponent.IsEquipped(slot.Key))) : null;

            if (existingSlot != null)
            {
                // Stack with existing item
                existingSlot.Add(1);
            }
            else
            {
                // Add new slot with next available key
                char nextKey = GetNextAvailableKey();
                var newSlot = new InventorySlot(nextKey, itemComponent.Item, 1);
                _inventory.Add(newSlot);
            }
        }

        message = $"You pick up the {itemEntity.DisplayName}.";
        return true;
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
    public InventorySlot GetInventorySlot(char key)
    {
        return _inventory.FirstOrDefault(slot => slot.Key == key);
    }

    /// <summary>
    /// Removes items from the player's inventory.
    /// </summary>
    /// <param name="key">The inventory slot key (a-z).</param>
    /// <param name="count">The number of items to remove.</param>
    /// <returns>True if items were removed successfully.</returns>
    public bool RemoveItemFromInventory(char key, int count = 1)
    {
        var slot = GetInventorySlot(key);
        if (slot == null)
        {
            return false;
        }

        // Remove the specified count
        bool removed = slot.Remove(count);

        // If slot is empty, remove it from inventory
        if (slot.Count <= 0)
        {
            _inventory.Remove(slot);
        }

        // Notify listeners
        if (removed)
        {
            EmitSignal(SignalName.InventoryChanged);
        }

        return removed;
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
