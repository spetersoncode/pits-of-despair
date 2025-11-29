using Godot;
using PitsOfDespair.Brands;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Conditions;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Data;

/// <summary>
/// Captures complete player state for persistence across floor transitions.
/// Includes stats, health, inventory, equipment, and active status effects.
/// </summary>
public class PlayerState
{
	#region Identity State

	/// <summary>
	/// Player's display name.
	/// </summary>
	public string PlayerName { get; set; } = "Player";

	#endregion

	#region Stats State

	public int BaseStrength { get; set; }
	public int BaseAgility { get; set; }
	public int BaseEndurance { get; set; }
	public int BaseWill { get; set; }
	public int Level { get; set; }
	public int CurrentExperience { get; set; }

	// Unified stat modifier tracking
	public Dictionary<StatType, Dictionary<string, int>> StatModifiers { get; set; } = new();

	#endregion

	#region Health State

	public int BaseMaxHealth { get; set; }
	public int CurrentHealth { get; set; }
	public List<DamageType> Immunities { get; set; } = new();
	public List<DamageType> Resistances { get; set; } = new();
	public List<DamageType> Vulnerabilities { get; set; } = new();

	#endregion

	#region Willpower State

	/// <summary>
	/// Current willpower at time of extraction.
	/// Note: On floor transitions, WP is fully restored after state is applied.
	/// </summary>
	public int CurrentWillpower { get; set; }

	#endregion

	#region Inventory State

	/// <summary>
	/// Serializable representation of inventory slots.
	/// </summary>
	public List<SerializableInventorySlot> Inventory { get; set; } = new();

	#endregion

	#region Equipment State

	/// <summary>
	/// Maps equipment slots to inventory keys.
	/// </summary>
	public Dictionary<EquipmentSlot, char> EquippedSlots { get; set; } = new();

	#endregion

	#region Conditions State

	/// <summary>
	/// Active conditions with their remaining turns.
	/// </summary>
	public List<Condition> ActiveConditions { get; set; } = new();

	#endregion

	#region Extraction & Application

	/// <summary>
	/// Extracts complete state from a player entity.
	/// </summary>
	/// <param name="player">The player entity to extract state from.</param>
	/// <returns>PlayerState containing all persistent data.</returns>
	public static PlayerState ExtractFromPlayer(Player player)
	{
		var state = new PlayerState();

		// Extract Player Name
		state.PlayerName = player.DisplayName;

		// Extract Stats
		var statsComponent = player.GetNodeOrNull<StatsComponent>("StatsComponent");
		if (statsComponent != null)
		{
			state.BaseStrength = statsComponent.BaseStrength;
			state.BaseAgility = statsComponent.BaseAgility;
			state.BaseEndurance = statsComponent.BaseEndurance;
			state.BaseWill = statsComponent.BaseWill;
			state.Level = statsComponent.Level;
			state.CurrentExperience = statsComponent.CurrentExperience;

			// Extract stat modifiers using public API
			state.StatModifiers = new Dictionary<StatType, Dictionary<string, int>>();
			foreach (StatType stat in new[] { StatType.Strength, StatType.Agility, StatType.Endurance,
											   StatType.Will, StatType.Armor, StatType.Evasion })
			{
				var modifiers = statsComponent.GetStatModifiers(stat);
				if (modifiers.Count > 0)
				{
					state.StatModifiers[stat] = new Dictionary<string, int>(modifiers);
				}
			}
		}

		// Extract Health
		var healthComponent = player.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (healthComponent != null)
		{
			state.BaseMaxHealth = healthComponent.BaseMaxHealth;
			state.CurrentHealth = healthComponent.CurrentHealth;
			state.Immunities = new List<DamageType>(healthComponent.Immunities);
			state.Resistances = new List<DamageType>(healthComponent.Resistances);
			state.Vulnerabilities = new List<DamageType>(healthComponent.Vulnerabilities);
		}

		// Extract Willpower
		var willpowerComponent = player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
		if (willpowerComponent != null)
		{
			state.CurrentWillpower = willpowerComponent.CurrentWillpower;
		}

		// Extract Inventory
		var inventoryComponent = player.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		if (inventoryComponent != null)
		{
			foreach (var slot in inventoryComponent.Inventory)
			{
				var serializableSlot = new SerializableInventorySlot
				{
					Key = slot.Key,
					ItemDataFileId = slot.Item.Template.DataFileId,
					Quantity = slot.Item.Quantity,
					CurrentCharges = slot.Item.CurrentCharges
				};

				// Serialize brands
				foreach (var brand in slot.Item.GetBrands())
				{
					serializableSlot.Brands.Add(new SerializableBrand
					{
						BrandType = brand.TypeId,
						Amount = GetBrandAmount(brand),
						Duration = brand.Duration,
						RemainingTurns = brand.RemainingTurns,
						SourceId = brand.SourceId
					});
				}

				state.Inventory.Add(serializableSlot);
			}
		}

		// Extract Equipment
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null)
		{
			state.EquippedSlots = GetPrivateDictionary<EquipmentSlot, char>(equipComponent, "_equippedSlots");
		}

		// Extract Conditions (now managed directly by BaseEntity)
		state.ActiveConditions = new List<Condition>(player.GetActiveConditions());

		return state;
	}

	/// <summary>
	/// Applies this state to a player entity.
	/// Must be called after player's components are initialized in _Ready().
	/// </summary>
	/// <param name="player">The player entity to apply state to.</param>
	public void ApplyToPlayer(Player player)
	{
		// Apply Player Name
		player.DisplayName = PlayerName;

		// Apply Stats
		var statsComponent = player.GetNodeOrNull<StatsComponent>("StatsComponent");
		if (statsComponent != null)
		{
			statsComponent.BaseStrength = BaseStrength;
			statsComponent.BaseAgility = BaseAgility;
			statsComponent.BaseEndurance = BaseEndurance;
			statsComponent.BaseWill = BaseWill;
			statsComponent.Level = Level;

			// Set CurrentExperience using reflection to access private setter
			SetPrivateProperty(statsComponent, "CurrentExperience", CurrentExperience);

			// Apply stat modifiers using unified API
			foreach (var statEntry in StatModifiers)
			{
				foreach (var kvp in statEntry.Value)
				{
					statsComponent.AddStatModifier(statEntry.Key, kvp.Key, kvp.Value);
				}
			}

			// Emit signals to update UI
			statsComponent.EmitSignal(StatsComponent.SignalName.StatsChanged);
			statsComponent.EmitSignal(StatsComponent.SignalName.ExperienceGained, 0, CurrentExperience, statsComponent.ExperienceToNextLevel);
		}

		// Apply Health (must be done AFTER stats to get correct MaxHP calculation)
		var healthComponent = player.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (healthComponent != null)
		{
			healthComponent.BaseMaxHealth = BaseMaxHealth;
			healthComponent.Immunities = new List<DamageType>(Immunities);
			healthComponent.Resistances = new List<DamageType>(Resistances);
			healthComponent.Vulnerabilities = new List<DamageType>(Vulnerabilities);

			// Set CurrentHP using reflection to access private setter
			SetPrivateProperty(healthComponent, "CurrentHealth", CurrentHealth);
		}

		// Apply Willpower (fully restore on floor transitions per design)
		var willpowerComponent = player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
		if (willpowerComponent != null)
		{
			// Floor transitions grant full WP restore
			willpowerComponent.FullRestore();
		}

		// Apply Inventory
		var inventoryComponent = player.GetNodeOrNull<InventoryComponent>("InventoryComponent");
		if (inventoryComponent != null)
		{
			inventoryComponent.Clear();

			var dataLoader = player.GetNode<DataLoader>("/root/DataLoader");
			foreach (var slot in Inventory)
			{
				var itemTemplate = dataLoader.Items.Get(slot.ItemDataFileId);
				if (itemTemplate != null)
				{
					var itemInstance = new ItemInstance(itemTemplate)
					{
						CurrentCharges = slot.CurrentCharges,
						Quantity = slot.Quantity
					};

					// Restore brands
					foreach (var serializedBrand in slot.Brands)
					{
						var brand = BrandFactory.Create(
							serializedBrand.BrandType,
							serializedBrand.Amount,
							serializedBrand.Duration,
							serializedBrand.SourceId
						);
						if (brand != null)
						{
							brand.RemainingTurns = serializedBrand.RemainingTurns;
							itemInstance.AddBrand(brand);
						}
					}

					// Manually add to inventory at specific key
					AddItemAtKey(inventoryComponent, slot.Key, itemInstance);
				}
			}
		}

		// Apply Equipment (must be done AFTER inventory is populated)
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null)
		{
			foreach (var kvp in EquippedSlots)
			{
				equipComponent.Equip(kvp.Value, kvp.Key);
			}
		}

		// Apply Conditions (now managed directly by BaseEntity)
		foreach (var condition in ActiveConditions)
		{
			player.AddCondition(condition);
		}
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Gets a copy of a private dictionary field using reflection.
	/// </summary>
	private static Dictionary<TKey, TValue> GetPrivateDictionary<TKey, TValue>(object obj, string fieldName) where TKey : notnull
	{
		var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (field != null)
		{
			var dict = field.GetValue(obj) as Dictionary<TKey, TValue>;
			if (dict != null)
			{
				return new Dictionary<TKey, TValue>(dict);
			}
		}
		return new Dictionary<TKey, TValue>();
	}

	/// <summary>
	/// Sets a private property value using reflection.
	/// </summary>
	private static void SetPrivateProperty(object obj, string propertyName, object value)
	{
		// First try to get the property with all binding flags (public and non-public)
		var property = obj.GetType().GetProperty(propertyName,
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Instance);

		if (property != null)
		{
			// For private setters, we need to get the setter method and invoke it with NonPublic flag
			var setter = property.GetSetMethod(nonPublic: true);
			if (setter != null)
			{
				setter.Invoke(obj, new[] { value });
				return;
			}
		}

		// If property approach didn't work, try as a backing field
		var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (field != null)
		{
			field.SetValue(obj, value);
			return;
		}

		// Last resort: try as a regular private field
		field = obj.GetType().GetField(propertyName,
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (field != null)
		{
			field.SetValue(obj, value);
		}
	}

	/// <summary>
	/// Adds an item to inventory at a specific key (bypasses normal key assignment).
	/// </summary>
	private static void AddItemAtKey(InventoryComponent inventory, char key, ItemInstance item)
	{
		// Access private _inventory list using reflection
		var field = inventory.GetType().GetField("_inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (field != null)
		{
			var list = field.GetValue(inventory) as List<InventorySlot>;
			if (list != null)
			{
				list.Add(new InventorySlot(key, item));
			}
		}
	}

	/// <summary>
	/// Gets the amount value from a brand for serialization purposes.
	/// </summary>
	private static int GetBrandAmount(Brand brand)
	{
		return brand switch
		{
			IDamageBrand db => db.GetDamageBonus(),
			IHitBrand hb => hb.GetHitBonus(),
			_ => 0
		};
	}

	#endregion
}

/// <summary>
/// Serializable representation of an inventory slot.
/// </summary>
public class SerializableInventorySlot
{
	public char Key { get; set; }
	public string ItemDataFileId { get; set; } = "";
	public int Quantity { get; set; }
	public int CurrentCharges { get; set; }
	public List<SerializableBrand> Brands { get; set; } = new();
}

/// <summary>
/// Serializable representation of a brand on an item.
/// </summary>
public class SerializableBrand
{
	public string BrandType { get; set; } = "";
	public int Amount { get; set; }
	public string Duration { get; set; } = "permanent";
	public int RemainingTurns { get; set; }
	public string? SourceId { get; set; }
}
