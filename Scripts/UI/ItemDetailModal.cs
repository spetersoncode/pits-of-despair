using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects.Composition;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Scripts.Components;
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
		string glyph = $"[color={itemTemplate.Color}]{itemTemplate.GetGlyph()}[/color]";
		string name = $"[color={itemTemplate.Color}]{itemTemplate.Name}[/color]";
		string keyInfo = $"[color={Palette.ToHex(Palette.Disabled)}]Hotkey:[/color] [color={Palette.ToHex(Palette.Default)}]{_currentSlot.Key}[/color]";

		// Stack count for consumables
		string countInfo = "";
		if (_currentSlot.Item.Quantity > 1)
		{
			countInfo = $" [color={Palette.ToHex(Palette.AshGray)}](x{_currentSlot.Item.Quantity})[/color]";
		}

		// Charges for charged items (only shown with Attunement skill)
		string chargesInfo = "";
		int maxCharges = itemTemplate.GetMaxCharges();
		if (maxCharges > 0)
		{
			var skillComponent = _player?.GetNodeOrNull<SkillComponent>("SkillComponent");
			bool hasAttunement = skillComponent?.HasSkill("attunement") ?? false;

			if (hasAttunement)
			{
				string bracket = ItemFormatter.GetChargeBracket(_currentSlot.Item.CurrentCharges, maxCharges);
				chargesInfo = $"\n[color={Palette.ToHex(Palette.Disabled)}]Charges:[/color] [color={itemTemplate.Color}]{bracket}[/color]";
			}
		}

		// Equipment slot info and equipped status
		string slotInfo = "";
		if (itemTemplate.GetIsEquippable())
		{
			var equipSlot = itemTemplate.GetEquipmentSlot();
			string slotName = ItemFormatter.FormatSlotName(equipSlot);

			// Check if this item is currently equipped
			var equipComponent = _player?.GetNodeOrNull<EquipComponent>("EquipComponent");
			bool isEquipped = equipComponent?.IsEquipped(_currentSlot.Key) ?? false;
			string equippedIndicator = isEquipped ? $" [color={Palette.ToHex(Palette.Success)}](equipped)[/color]" : "";

			slotInfo = $"\n[color={Palette.ToHex(Palette.Disabled)}]Slot:[/color] [color={Palette.ToHex(Palette.AshGray)}]{slotName}[/color]{equippedIndicator}";
		}

		// Weapon stats section
		string weaponStats = BuildWeaponStats(itemTemplate);

		// Equipment stats section (armor, evasion, stat modifiers)
		string equipmentStats = BuildEquipmentStats(itemTemplate);

		// Effects description section
		string effectsSection = BuildEffectDescriptions(itemTemplate);

		// Description section (flavor text)
		string descriptionSection = "";
		if (!string.IsNullOrEmpty(itemTemplate.Description))
		{
			descriptionSection = $"\n\n[color={Palette.ToHex(Palette.AshGray)}]{itemTemplate.Description}[/color]";
		}

		// Commands section
		var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
		string commands = $"\n\n[center][color={Palette.ToHex(Palette.Disabled)}]({closeKey} to close)[/color][/center]";

		return $"{glyph} {name}{countInfo}\n" +
		       $"{keyInfo}{chargesInfo}{slotInfo}" +
		       $"{weaponStats}{equipmentStats}{effectsSection}" +
		       $"{descriptionSection}" +
		       $"{commands}";
	}

	/// <summary>
	/// Builds weapon damage information if this item is a weapon.
	/// </summary>
	private static string BuildWeaponStats(ItemData itemTemplate)
	{
		if (itemTemplate.Attack == null)
			return "";

		var attack = itemTemplate.Attack;
		string damageType = attack.DamageType.ToString().ToLower();
		string strNote = attack.Type == AttackType.Melee ? " (+STR)" : "";

		// Attack speed
		var (speedText, speedColor) = SpeedStatus.GetWeaponSpeedDisplay(attack.Delay);
		string speedLine = $"\n[color={Palette.ToHex(Palette.Disabled)}]Speed:[/color] " +
		                   $"[color={Palette.ToHex(speedColor)}]{speedText}[/color]";

		return $"\n[color={Palette.ToHex(Palette.Disabled)}]Damage:[/color] " +
		       $"[color={Palette.ToHex(Palette.Default)}]{attack.DiceNotation} {damageType}{strNote}[/color]" +
		       speedLine;
	}

	/// <summary>
	/// Builds equipment stat modifiers (armor, evasion, attribute bonuses).
	/// </summary>
	private static string BuildEquipmentStats(ItemData itemTemplate)
	{
		var stats = new List<string>();

		// Armor
		if (itemTemplate.Armor is int armor && armor != 0)
		{
			string sign = armor > 0 ? "+" : "";
			stats.Add($"[color={Palette.ToHex(Palette.Disabled)}]Armor:[/color] [color={Palette.ToHex(Palette.Default)}]{sign}{armor}[/color]");
		}

		// Evasion
		if (itemTemplate.Evasion is int evasion && evasion != 0)
		{
			string sign = evasion > 0 ? "+" : "";
			stats.Add($"[color={Palette.ToHex(Palette.Disabled)}]Evasion:[/color] [color={Palette.ToHex(Palette.Default)}]{sign}{evasion}[/color]");
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
			// Regen is in basis points (100 = 1% per turn)
			float percent = regen / 100f;
			stats.Add($"[color={Palette.ToHex(Palette.Default)}]+{percent:0.#}% regen/turn[/color]");
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

		var sb = new StringBuilder();
		sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]=== EFFECTS ===[/color]");
		foreach (var desc in descriptions)
		{
			sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]{desc}[/color]");
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
