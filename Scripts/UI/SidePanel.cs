using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Side panel displaying player information including health, stats, equipment, and nearby entities.
/// </summary>
public partial class SidePanel : PanelContainer
{
	private const float HealthGreenThreshold = 0.6f;
	private const float HealthYellowThreshold = 0.3f;

	private static readonly Color HealthColorGreen = Palette.HealthFull;
	private static readonly Color HealthColorYellow = Palette.HealthMedium;
	private static readonly Color HealthColorRed = Palette.HealthCritical;
	private static readonly Color DefaultTextColor = Palette.Default;
	private static readonly Color ExperienceBarColor = Palette.Gold;
	private static readonly Color WillpowerBarColor = new Color(0.4f, 0.4f, 0.8f); // Blue-purple for willpower

	private ProgressBar _healthBar;
	private Label _healthLabel;
	private ProgressBar _willpowerBar;
	private Label _willpowerLabel;
	private Label _levelLabel;
	private ProgressBar _experienceBar;
	private Label _experienceLabel;
	private Label _floorLabel;
	private Label _goldLabel;
	private RichTextLabel _equipmentLabel;
	private RichTextLabel _statsLabel;
	private RichTextLabel _visibleEntitiesLabel;

	private Entities.Player _player;

	// ViewModels for decoupled data access
	private ViewModels.PlayerStatsViewModel _statsViewModel;
	private ViewModels.EquipmentViewModel _equipmentViewModel;
	private Systems.NearbyEntitiesTracker _nearbyEntitiesTracker;

	// Pending level-up tracking
	private int _pendingLevelUps = 0;

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/HealthBar");
		_healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthLabel");
		_willpowerBar = GetNodeOrNull<ProgressBar>("MarginContainer/VBoxContainer/WillpowerBar");
		_willpowerLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/WillpowerLabel");
		_levelLabel = GetNode<Label>("MarginContainer/VBoxContainer/LevelLabel");
		_experienceBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/ExperienceBar");
		_experienceLabel = GetNode<Label>("MarginContainer/VBoxContainer/ExperienceLabel");
		_floorLabel = GetNode<Label>("MarginContainer/VBoxContainer/FloorLabel");
		_goldLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/GoldLabel");
		_equipmentLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/EquipmentLabel");
		_statsLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/StatsLabel");
		_visibleEntitiesLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/ScrollContainer/VisibleEntitiesLabel");

		UpdateFloorDisplay();
		UpdateGoldDisplay();
		UpdateEquipmentDisplay();
		UpdateStatsDisplay();
		UpdateVisibleEntitiesDisplay();
	}

	/// <summary>
	/// Initializes the side panel with ViewModels and systems.
	/// </summary>
	public void Initialize(
		Entities.Player player,
		ViewModels.PlayerStatsViewModel statsViewModel,
		ViewModels.EquipmentViewModel equipmentViewModel,
		Systems.NearbyEntitiesTracker nearbyEntitiesTracker)
	{
		_player = player;
		_statsViewModel = statsViewModel;
		_equipmentViewModel = equipmentViewModel;
		_nearbyEntitiesTracker = nearbyEntitiesTracker;

		// Connect to ViewModel signals (single source for all stats updates)
		_statsViewModel.Connect(
			ViewModels.PlayerStatsViewModel.SignalName.StatsUpdated,
			Callable.From(OnStatsUpdated)
		);

		_equipmentViewModel.Connect(
			ViewModels.EquipmentViewModel.SignalName.EquipmentDisplayUpdated,
			Callable.From(OnEquipmentUpdated)
		);

		_nearbyEntitiesTracker.Connect(
			Systems.NearbyEntitiesTracker.SignalName.NearbyEntitiesChanged,
			Callable.From<Godot.Collections.Array>(OnNearbyEntitiesChanged)
		);

		// Initialize display with current values
		UpdateAllDisplays();
	}

	/// <summary>
	/// Updates all display panels.
	/// </summary>
	private void UpdateAllDisplays()
	{
		UpdateHealthDisplay();
		UpdateWillpowerDisplay();
		UpdateLevelAndXPDisplay();
		UpdateFloorDisplay();
		UpdateGoldDisplay();
		UpdateStatsDisplay();
		UpdateEquipmentDisplay();
		UpdateVisibleEntitiesDisplay();
	}

	/// <summary>
	/// Called when PlayerStatsViewModel updates.
	/// Refreshes all stat-related displays.
	/// </summary>
	private void OnStatsUpdated()
	{
		UpdateHealthDisplay();
		UpdateWillpowerDisplay();
		UpdateLevelAndXPDisplay();
		UpdateFloorDisplay();
		UpdateGoldDisplay();
		UpdateStatsDisplay();
	}

	/// <summary>
	/// Called when EquipmentViewModel updates.
	/// Refreshes equipment display.
	/// </summary>
	private void OnEquipmentUpdated()
	{
		UpdateEquipmentDisplay();
	}

	/// <summary>
	/// Called when NearbyEntitiesTracker updates.
	/// Refreshes visible entities display.
	/// </summary>
	private void OnNearbyEntitiesChanged(Godot.Collections.Array nearbyEntities)
	{
		UpdateVisibleEntitiesDisplay();
	}

	private void UpdateHealthDisplay()
	{
		if (_healthBar == null || _healthLabel == null || _statsViewModel == null)
			return;

		int current = _statsViewModel.CurrentHP;
		int max = _statsViewModel.MaxHP;

		float healthPercent = max > 0 ? (float)current / max : 0f;
		Color healthColor = GetHealthColor(healthPercent);

		// Update progress bar
		_healthBar.MaxValue = max;
		_healthBar.Value = current;

		// Create StyleBoxFlat for the progress bar fill with color gradient
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = healthColor;
		_healthBar.AddThemeStyleboxOverride("fill", styleBox);

		// Update text label
		_healthLabel.Text = $"HP: {current}/{max}";
		_healthLabel.AddThemeColorOverride("font_color", healthColor);
	}

	private Color GetHealthColor(float healthPercent)
	{
		if (healthPercent > HealthGreenThreshold)
		{
			return HealthColorGreen;
		}
		else if (healthPercent > HealthYellowThreshold)
		{
			return HealthColorYellow;
		}
		else
		{
			return HealthColorRed;
		}
	}

	private void UpdateWillpowerDisplay()
	{
		if (_willpowerBar == null || _willpowerLabel == null || _statsViewModel == null)
			return;

		int current = _statsViewModel.CurrentWP;
		int max = _statsViewModel.MaxWP;

		// Update progress bar
		_willpowerBar.MaxValue = max;
		_willpowerBar.Value = current;

		// Create StyleBoxFlat for the progress bar fill
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = WillpowerBarColor;
		_willpowerBar.AddThemeStyleboxOverride("fill", styleBox);

		// Update text label
		_willpowerLabel.Text = $"WP: {current}/{max}";
		_willpowerLabel.AddThemeColorOverride("font_color", WillpowerBarColor);
	}

	private void UpdateFloorDisplay()
	{
		if (_floorLabel != null && _statsViewModel != null)
		{
			_floorLabel.Text = $"Floor: {_statsViewModel.FloorDepth}";
			_floorLabel.AddThemeColorOverride("font_color", DefaultTextColor);
		}
	}

	private void UpdateGoldDisplay()
	{
		if (_goldLabel != null && _statsViewModel != null)
		{
			_goldLabel.Text = $"Gold: {_statsViewModel.Gold}";
			_goldLabel.AddThemeColorOverride("font_color", Palette.Gold);
		}
	}

	private void UpdateLevelAndXPDisplay()
	{
		if (_levelLabel == null || _experienceBar == null || _experienceLabel == null || _statsViewModel == null)
			return;

		int level = _statsViewModel.Level;
		int currentXP = _statsViewModel.CurrentXP;
		int xpToNext = _statsViewModel.XPToNextLevel;

		// Show level-up indicator if pending level-ups exist
		if (_pendingLevelUps > 0)
		{
			_levelLabel.Text = $"LEVEL UP! [L]";
			_levelLabel.AddThemeColorOverride("font_color", Palette.Alert);
		}
		else
		{
			_levelLabel.Text = $"Level: {level}";
			_levelLabel.AddThemeColorOverride("font_color", DefaultTextColor);
		}

		_experienceBar.MaxValue = xpToNext;
		_experienceBar.Value = currentXP;
		_experienceBar.AddThemeStyleboxOverride("fill", CreateExperienceBarStyle());

		_experienceLabel.Text = $"XP: {currentXP}/{xpToNext}";
		_experienceLabel.AddThemeColorOverride("font_color", ExperienceBarColor);
	}

	/// <summary>
	/// Sets the number of pending level-ups for the HUD indicator.
	/// </summary>
	/// <param name="count">Number of pending level-ups</param>
	public void SetPendingLevelUps(int count)
	{
		_pendingLevelUps = count;
		UpdateLevelAndXPDisplay();
	}

	private StyleBoxFlat CreateExperienceBarStyle()
	{
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = ExperienceBarColor;
		return styleBox;
	}

	private void UpdateExperienceDisplay()
	{
		if (_levelLabel == null || _experienceBar == null || _experienceLabel == null || _player == null)
			return;

		var statsComponent = _player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");
		if (statsComponent == null)
		{
			_levelLabel.Text = "Level: ?";
			_experienceLabel.Text = "XP: ?/?";
			return;
		}

		// Update level label
		_levelLabel.Text = $"Level: {statsComponent.Level}";
		_levelLabel.AddThemeColorOverride("font_color", DefaultTextColor);

		// Update experience bar
		_experienceBar.MaxValue = statsComponent.ExperienceToNextLevel;
		_experienceBar.Value = statsComponent.CurrentExperience;

		// Create StyleBoxFlat for the experience bar fill with gold color
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = ExperienceBarColor;
		_experienceBar.AddThemeStyleboxOverride("fill", styleBox);

		// Update experience label
		_experienceLabel.Text = $"XP: {statsComponent.CurrentExperience}/{statsComponent.ExperienceToNextLevel}";
		_experienceLabel.AddThemeColorOverride("font_color", ExperienceBarColor);
	}

	private void UpdateEquipmentDisplay()
	{
		if (_equipmentLabel == null || _equipmentViewModel == null)
			return;

		var sb = new StringBuilder();

		// Use EquipmentViewModel for all equipment data
		foreach (var slot in _equipmentViewModel.AllSlots)
		{
			sb.Append($"{slot.SlotName}: ");

			if (slot.IsEquipped)
			{
				string colorHex = $"#{(int)(slot.ItemColor.R * 255):X2}{(int)(slot.ItemColor.G * 255):X2}{(int)(slot.ItemColor.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{slot.ItemGlyph} {slot.ItemName}[/color]");
			}
			else
			{
				sb.AppendLine(slot.ItemName); // "(none)" or "(error)"
			}
		}

		_equipmentLabel.Text = sb.ToString();
	}

	private void UpdateStatsDisplay()
	{
		if (_statsLabel == null || _statsViewModel == null || _player == null)
			return;

		var sb = new StringBuilder();

		// Base stats - full names, one per line (from ViewModel)
		sb.AppendLine($"Strength: {_statsViewModel.TotalStrength}");
		sb.AppendLine($"Agility: {_statsViewModel.TotalAgility}");
		sb.AppendLine($"Endurance: {_statsViewModel.TotalEndurance}");
		sb.AppendLine($"Will: {_statsViewModel.TotalWill}");

		sb.AppendLine(); // Blank line for spacing

		// Derived combat values (from ViewModel)
		int meleeAttack = _statsViewModel.MeleeAttack;
		int rangedAttack = _statsViewModel.RangedAttack;
		int armor = _statsViewModel.TotalArmor;
		int evasion = _statsViewModel.TotalEvasion;

		// Get attack component for damage dice (still needs direct access)
		var attackComponent = _player.GetNodeOrNull<Components.AttackComponent>("AttackComponent");
		string meleeDamage = "?";
		string rangedDamage = "?";

		if (attackComponent != null)
		{
			// Find melee and ranged attacks
			foreach (var attack in attackComponent.Attacks)
			{
				if (attack.Type == Data.AttackType.Melee)
				{
					int damageBonus = _statsViewModel.TotalStrength;
					meleeDamage = damageBonus > 0 ? $"{attack.DiceNotation}+{damageBonus}" : attack.DiceNotation;
				}
				else if (attack.Type == Data.AttackType.Ranged)
				{
					rangedDamage = attack.DiceNotation;
				}
			}
		}

		sb.AppendLine($"Melee Attack: {meleeAttack}");
		sb.AppendLine($"Melee Damage: {meleeDamage}");
		sb.AppendLine($"Ranged Attack: {rangedAttack}");
		sb.AppendLine($"Ranged Damage: {rangedDamage}");
		sb.AppendLine($"Armor: {armor}");
		sb.AppendLine($"Evasion: {evasion}");

		_statsLabel.Text = sb.ToString();
	}

	private void UpdateVisibleEntitiesDisplay()
	{
		if (_visibleEntitiesLabel == null || _nearbyEntitiesTracker == null)
			return;

		// Get nearby entities from tracker
		var nearbyEntities = _nearbyEntitiesTracker.GetNearbyEntities();

		// Build display text with colors and glyphs
		var sb = new StringBuilder();

		if (nearbyEntities.Count == 0)
		{
			sb.Append("(nothing visible)");
		}
		else
		{
			foreach (var (entity, distance) in nearbyEntities)
			{
				// Convert entity color to hex for BBCode
				string colorHex = $"#{(int)(entity.GlyphColor.R * 255):X2}{(int)(entity.GlyphColor.G * 255):X2}{(int)(entity.GlyphColor.B * 255):X2}";

				// Use item's display name with quantity for items, otherwise use entity DisplayName
				string displayName = entity.ItemData != null
					? entity.ItemData.Template.GetDisplayName(entity.ItemData.Quantity)
					: entity.DisplayName;

				sb.AppendLine($"[color={colorHex}]{entity.Glyph} {displayName}[/color]");
			}
		}

		_visibleEntitiesLabel.Text = sb.ToString().TrimEnd();
	}

	public override void _ExitTree()
	{
		// Disconnect from ViewModels
		if (_statsViewModel != null)
		{
			_statsViewModel.Disconnect(
				ViewModels.PlayerStatsViewModel.SignalName.StatsUpdated,
				Callable.From(OnStatsUpdated)
			);
		}

		if (_equipmentViewModel != null)
		{
			_equipmentViewModel.Disconnect(
				ViewModels.EquipmentViewModel.SignalName.EquipmentDisplayUpdated,
				Callable.From(OnEquipmentUpdated)
			);
		}

		if (_nearbyEntitiesTracker != null)
		{
			_nearbyEntitiesTracker.Disconnect(
				Systems.NearbyEntitiesTracker.SignalName.NearbyEntitiesChanged,
				Callable.From<Godot.Collections.Array>(OnNearbyEntitiesChanged)
			);
		}
	}
}
