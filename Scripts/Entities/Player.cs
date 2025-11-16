using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
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
    public delegate void InventoryChangedEventHandler();

    private const int MaxInventorySlots = 26;

    private MovementComponent? _movementComponent;
    private GridPosition _previousPosition;
    private List<InventorySlot> _inventory = new();
    private EntityManager? _entityManager;

    public override void _Ready()
    {
        // Set player properties
        DisplayName = "Player";
        Glyph = '@';
        GlyphColor = Colors.Yellow;

        // Get MovementComponent child
        _movementComponent = GetNode<MovementComponent>("MovementComponent");

        // Initialize attack component with player's default attack
        var attackComponent = GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent != null)
        {
            var playerPunch = new AttackData
            {
                Name = "Punch",
                MinDamage = 1,
                MaxDamage = 4,
                Range = 1
            };
            attackComponent.Attacks = new Godot.Collections.Array<AttackData> { playerPunch };
        }

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

        // If action consumed a turn, emit turn completed signal
        if (result.ConsumesTurn)
        {
            EmitSignal(SignalName.TurnCompleted);
        }

        return result;
    }

    /// <summary>
    /// Handle position changes to emit turn completion.
    /// </summary>
    private void OnPositionChanged(int x, int y)
    {
        // Only emit turn completed if position actually changed
        if (x != _previousPosition.X || y != _previousPosition.Y)
        {
            EmitSignal(SignalName.TurnCompleted);
            _previousPosition = new GridPosition(x, y);
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
    public IReadOnlyList<InventorySlot> Inventory => _inventory.AsReadOnly();

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
        var itemAtPosition = entityAtPosition as Item;

        if (itemAtPosition == null)
        {
            EmitSignal(SignalName.ItemPickedUp, "", false, "Nothing to pick up.");
            return false; // No turn consumed
        }

        // Check if inventory is full (26 unique items)
        if (_inventory.Count >= MaxInventorySlots)
        {
            // Check if we can stack with existing item
            var existingSlot = _inventory.FirstOrDefault(slot =>
                slot.ItemData.DataFileId == itemAtPosition.ItemData.DataFileId);

            if (existingSlot == null)
            {
                EmitSignal(SignalName.ItemPickedUp, itemAtPosition.DisplayName, false,
                    "Inventory full! (26 unique items)");
                return false; // No turn consumed
            }

            // Stack with existing item
            existingSlot.Add(1);
        }
        else
        {
            // Try to find existing slot for stacking
            var existingSlot = _inventory.FirstOrDefault(slot =>
                slot.ItemData.DataFileId == itemAtPosition.ItemData.DataFileId);

            if (existingSlot != null)
            {
                // Stack with existing item
                existingSlot.Add(1);
            }
            else
            {
                // Add new slot with next available key
                char nextKey = GetNextAvailableKey();
                var newSlot = new InventorySlot(nextKey, itemAtPosition.ItemData, 1);
                _inventory.Add(newSlot);
            }
        }

        // Remove item from world
        _entityManager.RemoveEntity(itemAtPosition);
        itemAtPosition.QueueFree();

        // Notify listeners
        string itemName = itemAtPosition.DisplayName;
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
    /// <param name="item">The item to add.</param>
    /// <param name="message">Output message describing the result.</param>
    /// <returns>True if the item was added successfully.</returns>
    public bool AddItemToInventory(Item item, out string message)
    {
        // Check if inventory is full (26 unique items)
        if (_inventory.Count >= MaxInventorySlots)
        {
            // Check if we can stack with existing item
            var existingSlot = _inventory.FirstOrDefault(slot =>
                slot.ItemData.DataFileId == item.ItemData.DataFileId);

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
            // Try to find existing slot for stacking
            var existingSlot = _inventory.FirstOrDefault(slot =>
                slot.ItemData.DataFileId == item.ItemData.DataFileId);

            if (existingSlot != null)
            {
                // Stack with existing item
                existingSlot.Add(1);
            }
            else
            {
                // Add new slot with next available key
                char nextKey = GetNextAvailableKey();
                var newSlot = new InventorySlot(nextKey, item.ItemData, 1);
                _inventory.Add(newSlot);
            }
        }

        message = $"You pick up the {item.DisplayName}.";
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
}
