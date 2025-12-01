using Godot;
using PitsOfDespair.ItemProperties;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;
using System.Collections.Generic;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal for viewing item details.
/// Opened from InventoryModal when selecting an item by its hotkey.
/// </summary>
public partial class ItemDetailModal : CenterContainer
{
	[Signal]
	public delegate void CancelledEventHandler();

	private RichTextLabel _contentLabel;
	private Player _player;
	private bool _isVisible = false;
	private InventorySlot _currentSlot;

	public override void _Ready()
	{
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		Hide();
	}

	/// <summary>
	/// Connects to the player to access inventory.
	/// </summary>
	public void ConnectToPlayer(Player player)
	{
		_player = player;
	}

	/// <summary>
	/// Shows the modal with details for the specified item.
	/// </summary>
	public void ShowMenu(char itemKey)
	{
		if (_player == null)
		{
			GD.PrintErr("ItemDetailModal: Cannot show menu, player not connected");
			return;
		}

		_currentSlot = _player.GetInventorySlot(itemKey);
		if (_currentSlot == null)
		{
			GD.PrintErr($"ItemDetailModal: No item found for key '{itemKey}'");
			return;
		}

		_isVisible = true;
		Show();
		UpdateDisplay();
	}

	/// <summary>
	/// Hides the modal.
	/// </summary>
	public void HideMenu()
	{
		_isVisible = false;
		_currentSlot = null;
		Hide();
	}

	public override void _Input(InputEvent @event)
	{
		if (!_isVisible)
		{
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			HandleKeyInput(keyEvent);
		}
	}

	private void HandleKeyInput(InputEventKey keyEvent)
	{
		// Cancel on modal close key
		if (MenuInputProcessor.IsCloseKey(keyEvent))
		{
			EmitSignal(SignalName.Cancelled);
			GetViewport().SetInputAsHandled();
			return;
		}

		// Block all other keys
		GetViewport().SetInputAsHandled();
	}

	private void UpdateDisplay()
	{
		if (_currentSlot == null || _contentLabel == null)
		{
			return;
		}

		var itemTemplate = _currentSlot.Item.Template;
		_contentLabel.Text = BuildViewingDisplay(itemTemplate);
	}

	private string BuildViewingDisplay(ItemData itemTemplate)
	{
		var sb = new StringBuilder();
		string disabled = Palette.ToHex(Palette.Disabled);
		string defaultColor = Palette.ToHex(Palette.Default);
		string itemColor = itemTemplate.Color;

		// === HEADER ===
		string glyph = $"[color={itemColor}]{itemTemplate.GetGlyph()}[/color]";
		string displayName = _currentSlot.Item.GetDisplayName(); // Use display name with prefixes/suffixes
		string name = $"[color={itemColor}][b]{displayName}[/b][/color]";
		string countInfo = _currentSlot.Item.Quantity > 1
			? $" [color={Palette.ToHex(Palette.AshGray)}](x{_currentSlot.Item.Quantity})[/color]"
			: "";

		sb.Append($"[center]{glyph}  {name}{countInfo}[/center]");
		sb.Append("\n");

		// === META INFO LINE ===
		var metaParts = new List<string>();

		// Hotkey
		metaParts.Add($"[color={disabled}]Hotkey:[/color] [color={defaultColor}]{_currentSlot.Key}[/color]");

		// Slot and equipped status
		if (itemTemplate.GetIsEquippable())
		{
			var equipSlot = itemTemplate.GetEquipmentSlot();
			string slotName = ItemFormatter.FormatSlotName(equipSlot);
			var equipComponent = _player?.GetNodeOrNull<EquipComponent>("EquipComponent");
			bool isEquipped = equipComponent?.IsEquipped(_currentSlot.Key) ?? false;
			string equippedIndicator = isEquipped
				? $" [color={Palette.ToHex(Palette.Success)}](equipped)[/color]"
				: "";
			metaParts.Add($"[color={disabled}]Slot:[/color] [color={Palette.ToHex(Palette.AshGray)}]{slotName}[/color]{equippedIndicator}");
		}

		// Charges (only shown with Attunement skill)
		int maxCharges = itemTemplate.GetMaxCharges();
		if (maxCharges > 0)
		{
			var skillComponent = _player?.GetNodeOrNull<SkillComponent>("SkillComponent");
			bool hasAttunement = skillComponent?.HasSkill("attunement") ?? false;
			if (hasAttunement)
			{
				string bracket = ItemFormatter.GetChargeBracket(_currentSlot.Item.CurrentCharges, maxCharges);
				metaParts.Add($"[color={disabled}]Charges:[/color] [color={itemColor}]{bracket}[/color]");
			}
		}

		sb.Append($"\n{string.Join("    ", metaParts)}");

		// === COMBAT SECTION (weapons) ===
		string weaponStats = BuildWeaponStats(itemTemplate);
		if (!string.IsNullOrEmpty(weaponStats))
		{
			sb.Append($"\n\n[color={disabled}]COMBAT[/color]");
			sb.Append(weaponStats);
		}

		// === PROPERTIES SECTION (item enchantments) ===
		string propertiesSection = BuildPropertiesSection(_currentSlot.Item);
		if (!string.IsNullOrEmpty(propertiesSection))
		{
			sb.Append($"\n\n[color={disabled}]PROPERTIES[/color]");
			sb.Append(propertiesSection);
		}

		// === EQUIPMENT SECTION (armor, stat modifiers) ===
		string equipmentStats = BuildEquipmentStats(itemTemplate);
		if (!string.IsNullOrEmpty(equipmentStats))
		{
			sb.Append($"\n\n[color={disabled}]EQUIPMENT[/color]");
			sb.Append(equipmentStats);
		}

		// === EFFECTS SECTION ===
		string effectsSection = BuildEffectDescriptions(itemTemplate);
		if (!string.IsNullOrEmpty(effectsSection))
		{
			sb.Append(effectsSection);
		}

		// === DESCRIPTION (flavor text) ===
		if (!string.IsNullOrEmpty(itemTemplate.Description))
		{
			sb.Append($"\n\n[color={disabled}]DESCRIPTION[/color]");
			sb.Append($"\n[color={Palette.ToHex(Palette.AshGray)}][i]{itemTemplate.Description}[/i][/color]");
		}

		// === CLOSE HINT ===
		var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
		sb.Append($"\n\n[right][color={disabled}]{closeKey} to close[/color][/right]");

		return sb.ToString();
	}

	/// <summary>
	/// Builds weapon damage information if this item is a weapon.
	/// Shows Base DPT and Equipped DPT (with player stat bonuses).
	/// </summary>
	private string BuildWeaponStats(ItemData itemTemplate)
	{
		if (itemTemplate.Attack == null)
			return "";

		var attack = itemTemplate.Attack;
		string disabled = Palette.ToHex(Palette.Disabled);
		string defaultColor = Palette.ToHex(Palette.Default);

		var sb = new StringBuilder();

		// Category (e.g., "Short Blades", "Flails")
		if (!string.IsNullOrEmpty(itemTemplate.Category))
		{
			sb.Append($"\n[color={disabled}]Category:[/color] [color={defaultColor}]{itemTemplate.Category}[/color]");
		}

		string damageType = attack.DamageType.ToString().ToLower();
		string strNote = attack.Type == AttackType.Melee ? " (+STR)" : "";
		int maxStrBonus = attack.GetMaxStrengthBonus();

		var (speedText, speedColor) = SpeedStatus.GetWeaponSpeedDisplay(attack.Delay);

		// Calculate Base DPT (weapon only)
		float baseDpt = attack.GetAverageDamagePerTurn();

		// Calculate Equipped DPT (with player stat bonuses, capped by weapon)
		float equippedDpt = baseDpt;
		if (_player != null && attack.Type == AttackType.Melee)
		{
			var statsComponent = _player.GetNodeOrNull<StatsComponent>("StatsComponent");
			if (statsComponent != null)
			{
				int damageBonus = Mathf.Min(statsComponent.GetDamageBonus(true), maxStrBonus);
				float avgDamage = DiceRoller.GetAverage(attack.DiceNotation) + damageBonus;
				equippedDpt = avgDamage / attack.Delay;
			}
		}

		sb.Append($"\n[color={disabled}]Damage:[/color] [color={defaultColor}]{attack.DiceNotation} {damageType}{strNote}[/color]");
		if (attack.Type == AttackType.Melee)
		{
			sb.Append($"\n[color={disabled}]Max STR Bonus:[/color] [color={defaultColor}]+{maxStrBonus}[/color]");
		}
		sb.Append($"\n[color={disabled}]Speed:[/color] [color={Palette.ToHex(speedColor)}]{speedText}[/color]");
		sb.Append($"\n[color={disabled}]Base Damage/Turn:[/color] [color={defaultColor}]{baseDpt:F1}[/color]");
		sb.Append($"\n[color={disabled}]Equipped Damage/Turn:[/color] [color={defaultColor}]{equippedDpt:F1}[/color]");

		return sb.ToString();
	}

	/// <summary>
	/// Builds the properties section for enchanted items.
	/// </summary>
	private static string BuildPropertiesSection(ItemInstance item)
	{
		var properties = item.GetProperties();
		if (properties.Count == 0)
			return "";

		string disabled = Palette.ToHex(Palette.Disabled);
		string defaultColor = Palette.ToHex(Palette.Default);
		string buffColor = Palette.ToHex(Palette.StatusBuff);

		var sb = new StringBuilder();

		foreach (var property in properties)
		{
			string propertyLine = DescribeProperty(property);
			string durationInfo = "";

			if (property.IsTemporary)
			{
				durationInfo = $" [color={disabled}]({property.RemainingTurns} turns)[/color]";
			}

			sb.Append($"\n[color={buffColor}]{property.Name}[/color]: [color={defaultColor}]{propertyLine}[/color]{durationInfo}");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generates a human-readable description of a property's effects.
	/// </summary>
	private static string DescribeProperty(ItemProperty property)
	{
		return property switch
		{
			IDamageProperty dp when dp.GetDamageBonus() != 0 =>
				$"+{dp.GetDamageBonus()} damage",
			IHitProperty hp when hp.GetHitBonus() != 0 =>
				$"+{hp.GetHitBonus()} to hit",
			ElementalProperty ep =>
				$"+1d6 {ep.Name.ToLower()} damage on hit",
			VampiricProperty =>
				"Heals on hit",
			_ => property.Name
		};
	}

	/// <summary>
	/// Builds equipment stat modifiers (armor, evasion, attribute bonuses).
	/// </summary>
	private static string BuildEquipmentStats(ItemData itemTemplate)
	{
		var stats = new List<string>();
		string disabled = Palette.ToHex(Palette.Disabled);
		string defaultColor = Palette.ToHex(Palette.Default);

		// Armor
		if (itemTemplate.Armor is int armor && armor != 0)
		{
			string sign = armor > 0 ? "+" : "";
			stats.Add($"[color={disabled}]Armor:[/color] [color={defaultColor}]{sign}{armor}[/color]");
		}

		// Evasion
		if (itemTemplate.Evasion is int evasion && evasion != 0)
		{
			string sign = evasion > 0 ? "+" : "";
			stats.Add($"[color={disabled}]Evasion:[/color] [color={defaultColor}]{sign}{evasion}[/color]");
		}

		// Attribute modifiers
		if (itemTemplate.Strength is int str && str != 0)
			stats.Add(FormatStatModifier("Strength", str));
		if (itemTemplate.Agility is int agi && agi != 0)
			stats.Add(FormatStatModifier("Agility", agi));
		if (itemTemplate.Endurance is int end && end != 0)
			stats.Add(FormatStatModifier("Endurance", end));
		if (itemTemplate.Will is int will && will != 0)
			stats.Add(FormatStatModifier("Will", will));
		if (itemTemplate.MaxHealth is int maxHp && maxHp != 0)
			stats.Add(FormatStatModifier("Max Health", maxHp));
		if (itemTemplate.MaxWillpower is int maxWp && maxWp != 0)
			stats.Add(FormatStatModifier("Max Willpower", maxWp));
		if (itemTemplate.Regen is int regen && regen != 0)
		{
			float percent = regen / 100f;
			stats.Add($"[color={defaultColor}]+{percent:0.#}% regen/turn[/color]");
		}

		if (stats.Count == 0)
			return "";

		return "\n" + string.Join("\n", stats);
	}

	private static string FormatStatModifier(string statName, int amount)
	{
		string sign = amount > 0 ? "+" : "";
		return $"[color={Palette.ToHex(Palette.Default)}]{sign}{amount} {statName}[/color]";
	}

	/// <summary>
	/// Builds effect descriptions for consumables, wands, staves, etc.
	/// Shows full mechanical detail: dice, damage types, saves, radius.
	/// </summary>
	private string BuildEffectDescriptions(ItemData itemTemplate)
	{
		if (itemTemplate.Effects == null || itemTemplate.Effects.Count == 0)
			return "";

		var descriptions = new List<string>();

		foreach (var effect in itemTemplate.Effects)
		{
			// Handle composed effects (with steps)
			if (effect.Steps != null && effect.Steps.Count > 0)
			{
				foreach (var step in effect.Steps)
				{
					string desc = DescribeStep(step, itemTemplate);
					if (!string.IsNullOrEmpty(desc))
						descriptions.Add(desc);
				}
			}
		}

		if (descriptions.Count == 0)
			return "";

		string disabled = Palette.ToHex(Palette.Disabled);
		string defaultColor = Palette.ToHex(Palette.Default);

		var sb = new StringBuilder();
		sb.Append($"\n\n[color={disabled}]EFFECTS[/color]");
		foreach (var desc in descriptions)
		{
			sb.Append($"\n[color={defaultColor}]{desc}[/color]");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generates a human-readable description for a single effect step.
	/// </summary>
	private static string DescribeStep(StepDefinition step, ItemData itemTemplate)
	{
		return step.Type.ToLower() switch
		{
			"damage" => DescribeDamageStep(step, itemTemplate),
			"heal" => DescribeHealStep(step),
			"heal_caster" => DescribeHealCasterStep(step),
			"apply_condition" => DescribeConditionStep(step),
			"save_check" => DescribeSaveCheckStep(step),
			"blink" => DescribeBlinkStep(step),
			"teleport" => DescribeTeleportStep(step),
			"spawn_hazard" => DescribeHazardStep(step),
			"knockback" => DescribeKnockbackStep(step),
			"clone" => "Creates a clone of the target",
			"magic_mapping" => DescribeMagicMappingStep(step),
			"charm" => "Charms the target to fight for you",
			_ => ""
		};
	}

	private static string DescribeDamageStep(StepDefinition step, ItemData itemTemplate)
	{
		string dice = step.Dice ?? "1d6";
		string damageType = step.DamageType?.ToLower() ?? "physical";

		var sb = new StringBuilder($"Deals {dice} {damageType} damage");

		// Check targeting for area info
		if (itemTemplate.Targeting != null)
		{
			if (itemTemplate.Targeting.Type == "area" && itemTemplate.Targeting.Radius > 0)
				sb.Append($" in {itemTemplate.Targeting.Radius}-tile radius");
			else if (itemTemplate.Targeting.Type == "cone")
				sb.Append($" in a cone");
		}

		if (step.HalfOnSave || step.HalfOnSuccess)
			sb.Append(" (half on save)");

		return sb.ToString();
	}

	private static string DescribeHealStep(StepDefinition step)
	{
		if (!string.IsNullOrEmpty(step.Dice))
			return $"Restores {step.Dice} HP";
		if (step.Amount > 0)
		{
			if (step.Percent)
				return $"Restores {step.Amount}% HP";
			return $"Restores {step.Amount} HP";
		}
		return "Restores HP";
	}

	private static string DescribeHealCasterStep(StepDefinition step)
	{
		if (step.Fraction > 0)
			return $"Heals caster for {step.Fraction * 100:0}% of damage dealt";
		return "Heals caster based on damage dealt";
	}

	private static string DescribeConditionStep(StepDefinition step)
	{
		string condition = FormatConditionName(step.ConditionType ?? "effect");
		string duration = !string.IsNullOrEmpty(step.Duration) ? $" for {step.Duration} turns" : "";

		// Include amount for stat modifiers
		if (step.Amount != 0 && IsStatModifierCondition(step.ConditionType))
		{
			string sign = step.Amount > 0 ? "+" : "";
			return $"Grants {sign}{step.Amount} {condition}{duration}";
		}

		return $"Inflicts {condition}{duration}";
	}

	private static string DescribeSaveCheckStep(StepDefinition step)
	{
		string saveStat = step.SaveStat?.ToUpper() ?? "STAT";
		string result = step.StopOnSuccess ? "negates" : "reduces effect";
		return $"({saveStat} save {result})";
	}

	private static string DescribeBlinkStep(StepDefinition step)
	{
		if (step.Range > 0)
			return $"Short-range teleport (up to {step.Range} tiles)";
		return "Short-range teleport";
	}

	private static string DescribeTeleportStep(StepDefinition step)
	{
		if (step.TeleportCompanions)
			return "Teleports randomly (companions follow)";
		return "Teleports randomly";
	}

	private static string DescribeHazardStep(StepDefinition step)
	{
		string hazard = step.HazardType ?? "hazard";
		string duration = !string.IsNullOrEmpty(step.Duration) ? $" for {step.Duration} turns" : "";
		return $"Creates {hazard}{duration}";
	}

	private static string DescribeKnockbackStep(StepDefinition step)
	{
		return $"Knocks target back {step.Distance} tile(s)";
	}

	private static string DescribeMagicMappingStep(StepDefinition step)
	{
		if (step.Radius > 0)
			return $"Reveals map within {step.Radius} tiles";
		return "Reveals the map";
	}

	/// <summary>
	/// Formats condition type IDs into readable names.
	/// </summary>
	private static string FormatConditionName(string conditionType)
	{
		return conditionType switch
		{
			"strength_modifier" => "Strength",
			"agility_modifier" => "Agility",
			"endurance_modifier" => "Endurance",
			"will_modifier" => "Will",
			"armor_modifier" => "Armor",
			"evasion_modifier" => "Evasion",
			"speed_modifier" => "Speed",
			"confusion" => "confusion",
			"fear" => "fear",
			"sleep" => "sleep",
			"charm" => "charm",
			"acid" => "acid burn",
			"burning" => "burning",
			"poison" => "poison",
			_ => conditionType.Replace("_", " ")
		};
	}

	/// <summary>
	/// Checks if a condition type is a stat modifier (should show +/- amount).
	/// </summary>
	private static bool IsStatModifierCondition(string? conditionType)
	{
		if (string.IsNullOrEmpty(conditionType))
			return false;

		return conditionType.EndsWith("_modifier");
	}
}
