using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.ItemProperties;

namespace PitsOfDespair.Systems.Entity;

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
		var data = _dataLoader.Creatures.Get(creatureId);
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
	/// <param name="quantity">Optional quantity for stackable items. If 0 or negative, uses type default (e.g., ammo rolls 2d6+8).</param>
	/// <returns>The created and configured BaseEntity with ItemData, or null if item not found.</returns>
	public BaseEntity CreateItem(string itemId, GridPosition position, int quantity = 0)
	{
		var data = _dataLoader.Items.Get(itemId);
		if (data == null)
		{
			GD.PrintErr($"EntityFactory: Failed to create item '{itemId}' - item data not found");
			return null;
		}

		// Use type default quantity if not specified (e.g., ammo gets 2d6+8)
		int actualQuantity = quantity > 0 ? quantity : data.GetDefaultQuantity();

		// Create ItemInstance with randomized/specified charges and quantity
		var itemInstance = new ItemInstance(data)
		{
			Quantity = actualQuantity
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

	#region Ring Generation

	/// <summary>
	/// Static base ring template used for programmatic ring generation.
	/// Rings are generated with properties rather than loaded from YAML.
	/// </summary>
	private static readonly ItemData BaseRingTemplate = new ItemData
	{
		Name = "ring",
		Description = "A simple metal band.",
		Type = "ring",
		Glyph = "=",
		Color = "#FFD700", // Gold
		IntroFloor = 1,
		IsEquippable = true,
		EquipSlot = "Ring",
		NoAutoSpawn = true, // Don't spawn base rings without properties
		DataFileId = "ring_base"
	};

	/// <summary>
	/// Creates a ring entity with a specific property applied.
	/// The ring's name will be derived from the property's suffix (e.g., "ring of true sight").
	/// </summary>
	/// <param name="property">The property to apply to the ring.</param>
	/// <param name="position">The grid position to place the ring.</param>
	/// <param name="colorOverride">Optional color override (property's GetColorOverride takes precedence).</param>
	/// <returns>The created ring entity with the property applied.</returns>
	public BaseEntity CreateRingWithProperty(ItemProperty property, GridPosition position, string? colorOverride = null)
	{
		// Create item instance from base template
		var itemInstance = new ItemInstance(BaseRingTemplate);

		// Apply the property
		itemInstance.AddProperty(property);

		// Determine color: property override > metadata override > default gold
		var ringColor = property.GetColorOverride() ?? ResolveRingColor(colorOverride);

		// Build the entity with appropriate display settings
		var entity = new BaseEntity
		{
			GridPosition = position,
			DisplayName = itemInstance.GetDisplayName(),
			Description = GetRingDescription(property),
			Glyph = BaseRingTemplate.Glyph ?? "=",
			GlyphColor = ringColor,
			IsWalkable = true,
			ItemData = itemInstance,
			Name = itemInstance.GetDisplayName()
		};

		return entity;
	}

	/// <summary>
	/// Creates a ring entity by selecting an appropriate property for the floor.
	/// Uses decay-weighted random selection from eligible ring properties.
	/// </summary>
	/// <param name="currentFloor">The current dungeon floor (for property eligibility).</param>
	/// <param name="position">The grid position to place the ring.</param>
	/// <param name="rng">Random number generator for property selection.</param>
	/// <returns>The created ring entity with a property, or null if no properties available.</returns>
	public BaseEntity? CreateRandomRing(int currentFloor, GridPosition position, RandomNumberGenerator rng)
	{
		// Get eligible ring properties for this floor
		var eligible = ItemPropertyFactory.GetEligibleProperties(currentFloor, ItemType.Ring);
		if (eligible.Count == 0)
		{
			GD.PushWarning($"EntityFactory: No ring properties available for floor {currentFloor}");
			return null;
		}

		// Select property using decay-weighted random selection
		var metadata = ItemPropertyFactory.SelectPropertyWithDecay(eligible, currentFloor, rng);
		if (metadata == null)
			return null;

		// Create the property instance
		var property = ItemPropertyFactory.CreateFromMetadata(metadata, rng);
		if (property == null)
			return null;

		return CreateRingWithProperty(property, position, metadata.ColorOverride);
	}

	/// <summary>
	/// Gets a thematic description for a ring based on its property.
	/// </summary>
	private static string GetRingDescription(ItemProperty property)
	{
		return property.TypeId switch
		{
			"see_invisible" => "A pale ring that seems to shimmer with hidden light. When worn, hidden things become visible to your eyes.",
			"free_action" => "A ring of burnished silver that feels warm to the touch. It grants freedom from bonds and paralysis.",
			"evasion" or "stat_Evasion" => "A pale green ring that shimmers with captured light. When worn, your reflexes sharpen and blows seem to miss you.",
			"regen" or "stat_Regen" => "This deep red ring pulses with a warm inner light. Wounds close faster while you wear it.",
			"armor" or "stat_Armor" => "A sturdy ring of layered metal. It provides a subtle protective ward to the wearer.",
			"max_health" or "stat_MaxHealth" => "A ring that pulses with vibrant energy. You feel heartier while wearing it.",
			"thorns" => "A ring covered in tiny metal barbs. Attackers will feel its bite.",
			_ when property.TypeId.StartsWith("resistance_") => "A ring infused with elemental protection. It shields the wearer from harmful energies.",
			_ => "A metal band with mysterious properties."
		};
	}

	/// <summary>
	/// Resolves a ring color from a color override string.
	/// </summary>
	private Color ResolveRingColor(string? colorOverride)
	{
		if (string.IsNullOrEmpty(colorOverride))
			return Palette.Gold;

		// Handle Palette references
		if (colorOverride.StartsWith("Palette."))
		{
			var colorName = colorOverride.Substring(8);
			var field = typeof(Palette).GetField(colorName,
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			if (field != null && field.FieldType == typeof(Color))
				return (Color)field.GetValue(null)!;
		}

		// Handle hex colors
		if (colorOverride.StartsWith("#"))
			return new Color(colorOverride);

		return Palette.Gold;
	}

	#endregion

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
			Threat = data.Threat
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

		// Add skill components if creature has skills defined
		// Skills imply WillpowerComponent, SkillComponent, and SkillAIComponent
		if (data.Skills != null && data.Skills.Count > 0)
		{
			// Add WillpowerComponent (required for skill costs)
			var willpowerComponent = new WillpowerComponent
			{
				Name = "WillpowerComponent"
			};
			entity.AddChild(willpowerComponent);

			// Add SkillComponent and learn skills
			var skillComponent = new SkillComponent
			{
				Name = "SkillComponent"
			};
			entity.AddChild(skillComponent);

			// Learn each skill
			foreach (var skillId in data.Skills)
			{
				skillComponent.LearnSkill(skillId);
			}

			// Add SkillAIComponent to enable AI skill usage
			var skillAIComponent = new AI.Components.SkillAIComponent
			{
				Name = "SkillAIComponent"
			};
			entity.AddChild(skillAIComponent);
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
			var equipComponent = new EquipComponent
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

					var itemData = _dataLoader.Items.Get(itemId);
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
	/// Adds AI behavior components to an entity based on configuration dictionaries.
	/// Each dictionary must have a "type" key; other keys are applied as component properties.
	/// Can be called externally (e.g., by BandSpawnStrategy) to add leader-specific AI.
	/// </summary>
	public void AddAIComponents(BaseEntity entity, List<Dictionary<string, object>> aiConfigs)
	{
		if (aiConfigs == null || aiConfigs.Count == 0)
			return;

		foreach (var config in aiConfigs)
		{
			if (config == null || !config.TryGetValue("type", out var typeObj))
				continue;

			var typeName = typeObj?.ToString();
			if (string.IsNullOrWhiteSpace(typeName))
				continue;

			string componentName = AIComponentTypes.Resolve(typeName);
			Node component = CreateAIComponent(componentName);
			if (component != null)
			{
				component.Name = componentName;
				ApplyComponentConfig(component, config);
				entity.AddChild(component);
			}
			else
			{
				// Not a warning - could be explicit archetype declaration (e.g., type: Warrior)
				// which has no corresponding component but grants the archetype
			}
		}
	}

	/// <summary>
	/// Applies configuration properties from a dictionary to a component via reflection.
	/// Skips the "type" key as it's used for component selection.
	/// </summary>
	private void ApplyComponentConfig(Node component, Dictionary<string, object> config)
	{
		var componentType = component.GetType();

		foreach (var kvp in config)
		{
			// Skip the type key
			if (kvp.Key.Equals("type", StringComparison.OrdinalIgnoreCase))
				continue;

			// Find property with matching name (case-insensitive)
			var property = componentType.GetProperty(kvp.Key,
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.IgnoreCase);

			if (property != null && property.CanWrite)
			{
				try
				{
					object value;
					var propertyType = property.PropertyType;

					// Handle enum types specially (Convert.ChangeType doesn't work for enums)
					if (propertyType.IsEnum)
					{
						value = Enum.Parse(propertyType, kvp.Value.ToString(), ignoreCase: true);
					}
					else
					{
						value = Convert.ChangeType(kvp.Value, propertyType);
					}

					property.SetValue(component, value);
				}
				catch (Exception ex)
				{
					GD.PushWarning($"EntityFactory: Failed to set {kvp.Key} on {componentType.Name}: {ex.Message}");
				}
			}
		}
	}

	/// <summary>
	/// Creates an AI component instance by name.
	/// </summary>
	private Node CreateAIComponent(string componentName)
	{
		return componentName switch
		{
			"CowardlyComponent" => new AI.Components.CowardlyComponent(),
			"YellForHelpComponent" => new AI.Components.YellForHelpComponent(),
			"ShootAndScootComponent" => new AI.Components.ShootAndScootComponent(),
			"ItemUsageComponent" => new AI.Components.ItemUsageComponent(),
			"JoinPlayerOnSightComponent" => new AI.Components.JoinPlayerOnSightComponent(),
			"WanderingComponent" => new AI.Components.WanderingComponent(),
			"ItemCollectComponent" => new AI.Components.ItemCollectComponent(),
			"PatrolComponent" => new AI.Components.PatrolComponent(),
			"FollowLeaderComponent" => new AI.Components.FollowLeaderComponent(),
			_ => null
		};
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
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");

		if (inventoryComponent == null || equipComponent == null)
		{
			GD.PushError("EntityFactory: Player missing InventoryComponent or EquipComponent!");
			return;
		}

		// Define starting equipment
		string[] startingItems = new string[]
		{
			"weapon_rusty_knife",
			"armor_tattered_rags"
		};

		// Add and equip each item
		foreach (var itemId in startingItems)
		{
			var itemData = _dataLoader.Items.Get(itemId);
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
	/// Creates a decoration entity from decoration data.
	/// Decorations are flavor entities that add visual atmosphere to dungeons.
	/// </summary>
	/// <param name="decorationId">The decoration ID from the DecorationSet entries.</param>
	/// <param name="position">The grid position to place the decoration.</param>
	/// <returns>The created BaseEntity, or null if decoration not found.</returns>
	public BaseEntity CreateDecoration(string decorationId, GridPosition position)
	{
		var data = _dataLoader.Decorations.Get(decorationId);
		if (data == null)
		{
			GD.PushWarning($"EntityFactory: Decoration '{decorationId}' not found");
			return null;
		}

		var entity = new BaseEntity
		{
			GridPosition = position,
			DisplayName = data.Name,
			Description = data.Description,
			Glyph = !string.IsNullOrEmpty(data.Glyph) ? data.Glyph : "?",
			GlyphColor = ResolveColor(data.Color),
			IsWalkable = data.IsWalkable,
			DecorationId = decorationId,
			Name = data.Name
		};

		// Add HealthComponent if health is specified (same pattern as creatures)
		if (data.Health > 0)
		{
			var healthComponent = new HealthComponent
			{
				Name = "HealthComponent",
				BaseMaxHealth = data.Health
			};
			entity.AddChild(healthComponent);
		}

		return entity;
	}

	/// <summary>
	/// Resolves a color string to a Godot Color.
	/// Supports "Palette.ColorName" and "#RRGGBB" formats.
	/// </summary>
	private Color ResolveColor(string colorString)
	{
		if (string.IsNullOrEmpty(colorString))
			return Palette.Default;

		// Handle hex colors
		if (colorString.StartsWith("#"))
			return new Color(colorString);

		// Handle Palette references (already resolved by YAML loader, but fallback)
		if (colorString.StartsWith("Palette."))
		{
			var colorName = colorString.Substring(8);
			var field = typeof(Palette).GetField(colorName,
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			if (field != null && field.FieldType == typeof(Color))
				return (Color)field.GetValue(null);
		}

		// Assume it's already been converted to hex by the YAML loader
		if (colorString.Length == 7 && colorString.StartsWith("#"))
			return new Color(colorString);

		return Palette.Default;
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

		var equipComponent = autoEquip ? entity.GetNodeOrNull<EquipComponent>("EquipComponent") : null;

		foreach (var itemId in itemDataIds)
		{
			var itemData = _dataLoader.Items.Get(itemId);
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
