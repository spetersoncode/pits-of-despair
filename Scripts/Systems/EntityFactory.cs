using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Data;

namespace PitsOfDespair.Systems;

/// <summary>
/// Factory for creating entities from YAML data.
/// Handles component instantiation based on data configuration.
/// </summary>
public partial class EntityFactory : Node
{
    private DataLoader _dataLoader;

    public override void _Ready()
    {
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
    }

    /// <summary>
    /// Create a creature from creature ID and position it on the grid.
    /// </summary>
    /// <param name="creatureId">The creature ID (YAML filename without extension).</param>
    /// <param name="position">The grid position to place the creature.</param>
    /// <returns>The created and configured BaseEntity, or null if creature not found.</returns>
    public BaseEntity CreateCreature(string creatureId, GridPosition position)
    {
        var data = _dataLoader.GetCreature(creatureId);
        if (data == null)
        {
            GD.PrintErr($"EntityFactory: Failed to create creature '{creatureId}' - creature data not found");
            return null;
        }

        return CreateEntityFromCreatureData(data, position, creatureId);
    }

    /// <summary>
    /// Create a new item from item ID and position it on the grid.
    /// Creates a new ItemInstance with randomized charges.
    /// </summary>
    /// <param name="itemId">The item ID (YAML filename without extension).</param>
    /// <param name="position">The grid position to place the item.</param>
    /// <param name="quantity">Optional quantity for stackable items. Defaults to 1.</param>
    /// <returns>The created and configured BaseEntity with ItemData, or null if item not found.</returns>
    public BaseEntity CreateItem(string itemId, GridPosition position, int quantity = 1)
    {
        var data = _dataLoader.GetItem(itemId);
        if (data == null)
        {
            GD.PrintErr($"EntityFactory: Failed to create item '{itemId}' - item data not found");
            return null;
        }

        // Create ItemInstance with randomized/specified charges and quantity
        var itemInstance = new ItemInstance(data)
        {
            Quantity = quantity
        };

        // Set autopickup for consumables and ammo (default for new items)
        string itemType = data.Type?.ToLower() ?? string.Empty;
        if (itemType == "potion" || itemType == "scroll" || itemType == "ammo")
        {
            itemInstance.AutoPickup = true;
        }

        // Delegate to builder for consistent entity construction
        return BuildItemEntity(itemInstance, position);
    }

    /// <summary>
    /// Creates an item entity from an existing ItemInstance.
    /// Used when moving items from inventory to world (e.g., dropping items).
    /// This preserves the item's state (charges, etc.) across the transition.
    /// </summary>
    /// <param name="itemInstance">The existing item instance with template and state.</param>
    /// <param name="position">The grid position to place the item.</param>
    /// <returns>The created and configured BaseEntity with ItemData.</returns>
    public BaseEntity CreateItemFromInstance(ItemInstance itemInstance, GridPosition position)
    {
        return BuildItemEntity(itemInstance, position);
    }

    /// <summary>
    /// Builds a BaseEntity from an existing ItemInstance.
    /// Ensures consistent entity setup whether creating new items or creating entities from dropped inventory items.
    /// </summary>
    /// <param name="itemInstance">The item instance with template and state.</param>
    /// <param name="position">Grid position for the entity.</param>
    /// <returns>Configured BaseEntity ready to add to EntityManager.</returns>
    private BaseEntity BuildItemEntity(ItemInstance itemInstance, GridPosition position)
    {
        var data = itemInstance.Template;

        // Create base entity for the item
        var entity = new BaseEntity
        {
            GridPosition = position,
            DisplayName = data.GetDisplayName(),
            Description = data.Description,
            Glyph = !string.IsNullOrEmpty(data.Glyph) ? data.Glyph : "?",
            GlyphColor = data.GetColor(),
            IsWalkable = true, // Items are walkable (entities can walk over them)
            ItemData = itemInstance, // Store item instance with per-instance state
            Name = data.Name // Set node name for debugging
        };

        return entity;
    }

    /// <summary>
    /// Create an entity from creature data.
    /// </summary>
    /// <param name="data">The creature data.</param>
    /// <param name="position">The grid position to place the creature.</param>
    /// <param name="creatureId">The creature ID for recreation (e.g., "cat").</param>
    private BaseEntity CreateEntityFromCreatureData(CreatureData data, GridPosition position, string creatureId)
    {
        // Create base entity
        var faction = data.GetFaction();
        var entity = new BaseEntity
        {
            GridPosition = position,
            DisplayName = data.Name,
            Description = data.Description,
            Glyph = !string.IsNullOrEmpty(data.Glyph) ? data.Glyph : "?",
            GlyphColor = faction == Faction.Player ? Palette.Player : data.GetColor(),
            Faction = faction,
            CreatureId = creatureId,
            Name = data.Name // Set node name for debugging
        };

        // Add StatsComponent (required for combat system)
        var statsComponent = new StatsComponent
        {
            Name = "StatsComponent",
            BaseStrength = data.Strength,
            BaseAgility = data.Agility,
            BaseEndurance = data.Endurance,
            BaseWill = data.Will,
            Level = data.Level
        };
        entity.AddChild(statsComponent);

        // Conditions are now managed by BaseEntity directly (no separate component needed)

        // Add MovementComponent if entity can move
        if (data.HasMovement)
        {
            var movementComponent = new MovementComponent
            {
                Name = "MovementComponent"
            };
            entity.AddChild(movementComponent);
        }

        // Add VisionComponent if vision range is specified
        if (data.VisionRange > 0)
        {
            var visionComponent = new VisionComponent
            {
                Name = "VisionComponent",
                VisionRange = data.VisionRange
            };
            entity.AddChild(visionComponent);
        }

        // Add SpeedComponent for energy-based turn system
        var speedComponent = new SpeedComponent
        {
            Name = "SpeedComponent",
            BaseSpeed = data.Speed
        };
        entity.AddChild(speedComponent);

        // Add HealthComponent if max HP is specified
        if (data.MaxHealth > 0)
        {
            var healthComponent = new HealthComponent
            {
                Name = "HealthComponent",
                BaseMaxHealth = data.MaxHealth, // Will be modified by Endurance in _Ready
                Immunities = new List<DamageType>(data.Immunities),
                Resistances = new List<DamageType>(data.Resistances),
                Vulnerabilities = new List<DamageType>(data.Vulnerabilities)
            };
            entity.AddChild(healthComponent);
        }

        // Add AttackComponent if entity has attacks OR can equip weapons
        if (data.Attacks.Count > 0 || data.GetCanEquip())
        {
            var naturalAttacks = new Godot.Collections.Array<AttackData>();

            if (data.Attacks.Count > 0)
            {
                // Use defined natural attacks from creature data
                foreach (var attackData in data.Attacks)
                {
                    var attack = new AttackData
                    {
                        Name = attackData.Name,
                        Type = attackData.Type,
                        DiceNotation = attackData.DiceNotation,
                        Range = attackData.Range,
                        DamageType = attackData.DamageType
                    };
                    naturalAttacks.Add(attack);
                }
            }
            else
            {
                // No natural attacks defined - use default punch
                naturalAttacks.Add(GetDefaultNaturalAttack());
            }

            var attackComponent = new AttackComponent
            {
                Name = "AttackComponent",
                NaturalAttacks = naturalAttacks, // Store as natural attacks
                Attacks = naturalAttacks // Current attacks (will be updated if weapons equipped)
            };
            entity.AddChild(attackComponent);
        }

        // Add InventoryComponent and EquipComponent if creature can equip items
        if (data.GetCanEquip())
        {
            // Add InventoryComponent first (equipment items will be stored here)
            var inventoryComponent = new InventoryComponent
            {
                Name = "InventoryComponent",
                MaxInventorySlots = 10 // Creatures have smaller inventories than player
            };
            entity.AddChild(inventoryComponent);

            // Add EquipComponent
            var equipComponent = new Scripts.Components.EquipComponent
            {
                Name = "EquipComponent"
            };
            entity.AddChild(equipComponent);

            // Load and equip creature equipment
            if (data.Equipment != null && data.Equipment.Count > 0)
            {
                foreach (var equipEntry in data.Equipment)
                {
                    // Parse equipment entry - supports both string and object forms
                    string itemId;
                    int quantity = 1;

                    if (equipEntry is string simpleId)
                    {
                        // Simple form: just item ID string
                        itemId = simpleId;
                    }
                    else if (equipEntry is Dictionary<object, object> dict)
                    {
                        // Object form: { id: "item_id", quantity: 20 }
                        if (!dict.TryGetValue("id", out var idObj) || idObj is not string id)
                        {
                            GD.PushWarning($"EntityFactory: Equipment entry missing 'id' for creature '{data.Name}'");
                            continue;
                        }
                        itemId = id;

                        if (dict.TryGetValue("quantity", out var qtyObj))
                        {
                            quantity = Convert.ToInt32(qtyObj);
                        }
                    }
                    else
                    {
                        GD.PushWarning($"EntityFactory: Invalid equipment entry format for creature '{data.Name}'");
                        continue;
                    }

                    var itemData = _dataLoader.GetItem(itemId);
                    if (itemData == null)
                    {
                        GD.PushWarning($"EntityFactory: Equipment item '{itemId}' not found for creature '{data.Name}'");
                        continue;
                    }

                    // Create item instance with specified quantity
                    var itemInstance = new ItemInstance(itemData);
                    itemInstance.Quantity = quantity;

                    // Add item to inventory
                    var key = inventoryComponent.AddItem(itemInstance, out string message, excludeEquipped: false);
                    if (key == null)
                    {
                        GD.PushWarning($"EntityFactory: Failed to add item '{itemId}' to creature '{data.Name}' inventory: {message}");
                        continue;
                    }

                    // Get equipment slot from item (handles rings dynamically)
                    var equipSlot = equipComponent.GetSlotForItem(itemData);
                    if (equipSlot == EquipmentSlot.None)
                    {
                        GD.PushWarning($"EntityFactory: Item '{itemId}' has no equipment slot for creature '{data.Name}'");
                        continue;
                    }

                    // Equip the item
                    equipComponent.Equip(key.Value, equipSlot);
                }
            }
        }

        // Add AIComponent if entity has AI behavior enabled
        if (data.HasAI)
        {
            var aiComponent = new AIComponent
            {
                Name = "AIComponent"
            };
            entity.AddChild(aiComponent);

            // Initialize sets up spawn position and initializes GoalStack with BoredGoal
            aiComponent.Initialize(position);

            // Add AI behavior components from creature data
            AddAIComponents(entity, data.Ai);
        }

        return entity;
    }

    /// <summary>
    /// Configures an entity as a friendly companion that follows and protects a target.
    /// Sets the entity's faction to Player and assigns the protection target.
    /// </summary>
    /// <param name="entity">The entity to configure as friendly.</param>
    /// <param name="protectionTarget">The entity to follow and protect (typically the player).</param>
    public void SetupAsFriendlyCompanion(BaseEntity entity, BaseEntity protectionTarget)
    {
        entity.Faction = Faction.Player;
        entity.GlyphColor = Palette.Player;

        var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            aiComponent.ProtectionTarget = protectionTarget;
        }
    }

    /// <summary>
    /// Adds AI behavior components to an entity based on configuration.
    /// Can be called externally (e.g., by BandSpawnStrategy) to add leader-specific AI.
    /// </summary>
    public void AddAIComponents(BaseEntity entity, List<Dictionary<string, object>> aiComponents)
    {
        if (aiComponents == null || aiComponents.Count == 0)
            return;

        foreach (var componentConfig in aiComponents)
        {
            if (!componentConfig.TryGetValue("type", out var typeObj) || typeObj == null)
            {
                GD.PushWarning($"EntityFactory: AI component missing 'type' field for creature '{entity.DisplayName}'");
                continue;
            }

            string componentName = AIComponentTypes.Resolve(typeObj.ToString());
            Node component = CreateAIComponent(componentName, componentConfig);
            if (component != null)
            {
                component.Name = componentName;
                entity.AddChild(component);
            }
            else
            {
                GD.PushWarning($"EntityFactory: Unknown AI component '{componentName}' for creature '{entity.DisplayName}'");
            }
        }
    }

    /// <summary>
    /// Creates an AI component instance by name and applies configuration.
    /// </summary>
    private Node CreateAIComponent(string componentName, Dictionary<string, object> config)
    {
        Node component = componentName switch
        {
            "CowardlyComponent" => new Components.AI.CowardlyComponent(),
            "YellForHelpComponent" => new Components.AI.YellForHelpComponent(),
            "ShootAndScootComponent" => new Components.AI.ShootAndScootComponent(),
            "ItemUsageComponent" => new Components.AI.ItemUsageComponent(),
            "JoinPlayerOnSightComponent" => new Components.AI.JoinPlayerOnSightComponent(),
            "WanderingComponent" => new Components.AI.WanderingComponent(),
            "ItemCollectComponent" => new Components.AI.ItemCollectComponent(),
            "PatrolComponent" => new Components.AI.PatrolComponent(),
            "FollowLeaderComponent" => new Components.AI.FollowLeaderComponent(),
            _ => null
        };

        if (component == null)
            return null;

        // Apply configuration values via reflection
        ApplyComponentConfig(component, config);

        return component;
    }

    /// <summary>
    /// Applies configuration dictionary values to component properties.
    /// </summary>
    private void ApplyComponentConfig(Node component, Dictionary<string, object> config)
    {
        var type = component.GetType();

        foreach (var kvp in config)
        {
            // Convert camelCase YAML key to PascalCase property name
            string propertyName = char.ToUpper(kvp.Key[0]) + kvp.Key.Substring(1);
            var property = type.GetProperty(propertyName);

            if (property != null && property.CanWrite)
            {
                try
                {
                    object value = ConvertConfigValue(kvp.Value, property.PropertyType);
                    property.SetValue(component, value);
                }
                catch (System.Exception e)
                {
                    GD.PushWarning($"EntityFactory: Failed to set {propertyName} on {type.Name}: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Converts a config value to the expected property type.
    /// </summary>
    private object ConvertConfigValue(object value, System.Type targetType)
    {
        if (value == null)
            return null;

        // Handle common type conversions
        if (targetType == typeof(int))
            return System.Convert.ToInt32(value);
        if (targetType == typeof(float))
            return System.Convert.ToSingle(value);
        if (targetType == typeof(bool))
            return System.Convert.ToBoolean(value);
        if (targetType == typeof(string))
            return value.ToString();

        return value;
    }

    /// <summary>
    /// Creates the default natural attack (punch) for entities with no defined natural attacks.
    /// </summary>
    private AttackData GetDefaultNaturalAttack()
    {
        return new AttackData
        {
            Name = DataDefaults.DefaultAttackName,
            Type = AttackType.Melee,
            DiceNotation = DataDefaults.DefaultAttackDice
        };
    }

    /// <summary>
    /// Initializes the player's inventory with starting equipment.
    /// Adds items to inventory and auto-equips them.
    /// Called by GameLevel after player is initialized.
    /// </summary>
    /// <param name="player">The player entity to equip.</param>
    public void InitializePlayerInventory(Player player)
    {
        var inventoryComponent = player.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        var equipComponent = player.GetNodeOrNull<Scripts.Components.EquipComponent>("EquipComponent");

        if (inventoryComponent == null || equipComponent == null)
        {
            GD.PushError("EntityFactory: Player missing InventoryComponent or EquipComponent!");
            return;
        }

        // Define starting equipment
        string[] startingItems = new string[]
        {
            "weapon_short_sword",
            "armor_padded"
        };

        // Add and equip each item
        foreach (var itemId in startingItems)
        {
            var itemData = _dataLoader.GetItem(itemId);
            if (itemData == null)
            {
                GD.PushWarning($"EntityFactory: Starting item '{itemId}' not found. Player starting without it.");
                continue;
            }

            // Create item instance
            var itemInstance = new ItemInstance(itemData);

            // Add to inventory
            var key = inventoryComponent.AddItem(itemInstance, out string message, excludeEquipped: false);
            if (key == null)
            {
                GD.PushWarning($"EntityFactory: Failed to add starting item '{itemId}': {message}");
                continue;
            }

            // Auto-equip the item (handles rings dynamically)
            var equipSlot = equipComponent.GetSlotForItem(itemData);
            if (equipSlot != EquipmentSlot.None)
            {
                equipComponent.Equip(key.Value, equipSlot);
            }
        }
    }

    /// <summary>
    /// Gives items to an entity and optionally equips them.
    /// Requires the entity to have an InventoryComponent.
    /// </summary>
    /// <param name="entity">The entity to give items to.</param>
    /// <param name="itemDataIds">Array of item data IDs to give.</param>
    /// <param name="autoEquip">If true, automatically equips items to appropriate slots.</param>
    public void GiveItems(BaseEntity entity, string[] itemDataIds, bool autoEquip = false)
    {
        var inventoryComponent = entity.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventoryComponent == null)
        {
            GD.PushWarning($"EntityFactory: Entity '{entity.DisplayName}' has no InventoryComponent!");
            return;
        }

        var equipComponent = autoEquip ? entity.GetNodeOrNull<Scripts.Components.EquipComponent>("EquipComponent") : null;

        foreach (var itemId in itemDataIds)
        {
            var itemData = _dataLoader.GetItem(itemId);
            if (itemData == null)
            {
                GD.PushWarning($"EntityFactory: Item '{itemId}' not found.");
                continue;
            }

            // Create item instance
            var itemInstance = new ItemInstance(itemData);

            // Add to inventory
            var key = inventoryComponent.AddItem(itemInstance, out string message, excludeEquipped: false);
            if (key == null)
            {
                GD.PushWarning($"EntityFactory: Failed to add item '{itemId}' to '{entity.DisplayName}': {message}");
                continue;
            }

            // Auto-equip if requested (handles rings dynamically)
            if (autoEquip && equipComponent != null)
            {
                var equipSlot = equipComponent.GetSlotForItem(itemData);
                if (equipSlot != EquipmentSlot.None)
                {
                    equipComponent.Equip(key.Value, equipSlot);
                }
            }
        }
    }
}
