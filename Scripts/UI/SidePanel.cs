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

	private ProgressBar _healthBar;
	private Label _healthLabel;
	private Label _levelLabel;
	private ProgressBar _experienceBar;
	private Label _experienceLabel;
	private Label _floorLabel;
	private Label _goldLabel;
	private Label _standingOnLabel;
	private RichTextLabel _equipmentLabel;
	private RichTextLabel _statsLabel;
	private RichTextLabel _visibleEntitiesLabel;

	private int _currentFloorDepth = 1;
	private Entities.Player _player;
	private Systems.EntityManager _entityManager;
	private Systems.PlayerVisionSystem _visionSystem;
	private Systems.GoldManager _goldManager;
	private Components.HealthComponent _playerHealth;
	private Components.StatsComponent _playerStats;
	private EquipComponent _playerEquip;

	public override void _Ready()
	{
		_healthBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/HealthBar");
		_healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthLabel");
		_levelLabel = GetNode<Label>("MarginContainer/VBoxContainer/LevelLabel");
		_experienceBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/ExperienceBar");
		_experienceLabel = GetNode<Label>("MarginContainer/VBoxContainer/ExperienceLabel");
		_floorLabel = GetNode<Label>("MarginContainer/VBoxContainer/FloorLabel");
		_goldLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/GoldLabel");
		_standingOnLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/StandingOnLabel");
		_equipmentLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/EquipmentLabel");
		_statsLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/StatsLabel");
		_visibleEntitiesLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/ScrollContainer/VisibleEntitiesLabel");

		UpdateFloorDisplay();
		UpdateGoldDisplay();
		UpdateStandingOnDisplay();
		UpdateEquipmentDisplay();
		UpdateStatsDisplay();
		UpdateVisibleEntitiesDisplay();
	}

	/// <summary>
	/// Connects to the player's health component to receive updates.
	/// </summary>
	public void ConnectToPlayer(Entities.Player player)
	{
		_player = player;

		_playerHealth = player.GetNode<Components.HealthComponent>("HealthComponent");
		_playerHealth.Connect(Components.HealthComponent.SignalName.HealthChanged, Callable.From<int, int>(OnHealthChanged));

		// Subscribe to stats changes
		_playerStats = player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");
		if (_playerStats != null)
		{
			_playerStats.Connect(Components.StatsComponent.SignalName.StatsChanged, Callable.From(OnStatsChanged));
			_playerStats.Connect(Components.StatsComponent.SignalName.ExperienceGained, Callable.From<int, int, int>(OnExperienceGained));
			_playerStats.Connect(Components.StatsComponent.SignalName.LevelUp, Callable.From<int>(OnLevelUp));
		}

		// Subscribe to player position changes
		player.Connect(Entities.Player.SignalName.PositionChanged, Callable.From<int, int>(OnPlayerPositionChanged));

		// Subscribe to inventory changes (for when items are picked up)
		player.Connect(Entities.Player.SignalName.InventoryChanged, Callable.From(OnInventoryChanged));

		// Subscribe to equipment changes
		_playerEquip = player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (_playerEquip != null)
		{
			_playerEquip.Connect(EquipComponent.SignalName.EquipmentChanged, Callable.From<EquipmentSlot>(OnEquipmentChanged));
		}

		// Initialize display with current values
		OnHealthChanged(_playerHealth.CurrentHP, _playerHealth.MaxHP);
		UpdateExperienceDisplay();
		UpdateStandingOnDisplay();
		UpdateEquipmentDisplay();
		UpdateStatsDisplay();
	}

	/// <summary>
	/// Connects to the gold manager to receive gold updates.
	/// </summary>
	public void ConnectToGoldManager(Systems.GoldManager goldManager)
	{
		_goldManager = goldManager;
		_goldManager.Connect(Systems.GoldManager.SignalName.GoldChanged, Callable.From<int, int>(OnGoldChanged));
		UpdateGoldDisplay();
	}

	/// <summary>
	/// Sets the entity manager reference for querying entities at player position.
	/// </summary>
	public void SetEntityManager(Systems.EntityManager entityManager)
	{
		_entityManager = entityManager;
		UpdateStandingOnDisplay();
		UpdateVisibleEntitiesDisplay();
	}

	/// <summary>
	/// Sets the vision system reference for querying visible tiles.
	/// </summary>
	public void SetVisionSystem(Systems.PlayerVisionSystem visionSystem)
	{
		_visionSystem = visionSystem;
		UpdateVisibleEntitiesDisplay();
	}

	/// <summary>
	/// Sets the current floor depth for display.
	/// </summary>
	public void SetFloorDepth(int depth)
	{
		_currentFloorDepth = depth;
		UpdateFloorDisplay();
	}

	private void OnHealthChanged(int current, int max)
	{
		float healthPercent = (float)current / max;
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

	private void UpdateFloorDisplay()
	{
		if (_floorLabel != null)
		{
			_floorLabel.Text = $"Floor: {_currentFloorDepth}";
			_floorLabel.AddThemeColorOverride("font_color", DefaultTextColor);
		}
	}

	private void OnGoldChanged(int amount, int totalGold)
	{
		UpdateGoldDisplay();
	}

	private void UpdateGoldDisplay()
	{
		if (_goldLabel != null && _goldManager != null)
		{
			_goldLabel.Text = $"Gold: {_goldManager.Gold}";
			_goldLabel.AddThemeColorOverride("font_color", Palette.Gold);
		}
	}

	private void OnPlayerPositionChanged(int x, int y)
	{
		UpdateStandingOnDisplay();
		UpdateVisibleEntitiesDisplay();
	}

	private void OnInventoryChanged()
	{
		// Update display when items are picked up
		UpdateStandingOnDisplay();
		UpdateEquipmentDisplay();
	}

	private void OnEquipmentChanged(EquipmentSlot slot)
	{
		UpdateEquipmentDisplay();
		UpdateStatsDisplay(); // Stats may change when equipment changes
	}

	private void OnStatsChanged()
	{
		UpdateStatsDisplay();
	}

	private void OnExperienceGained(int amount, int current, int toNext)
	{
		UpdateExperienceDisplay();
	}

	private void OnLevelUp(int newLevel)
	{
		// Update the XP display to reflect new level
		UpdateExperienceDisplay();

		// Note: Level-up message is now handled by GameHUD which also shows the stat choice modal
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
		if (_equipmentLabel == null || _player == null)
		{
			return;
		}

		var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent == null)
		{
			_equipmentLabel.Text = "(No equipment system)";
			return;
		}

		var sb = new StringBuilder();

		// Melee weapon
		sb.Append("Melee: ");
		var meleeKey = equipComponent.GetEquippedKey(EquipmentSlot.MeleeWeapon);
		if (meleeKey.HasValue)
		{
			var slot = _player.GetInventorySlot(meleeKey.Value);
			if (slot != null)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]");
			}
			else
			{
				sb.AppendLine("(error)");
			}
		}
		else
		{
			sb.AppendLine("(none)");
		}

		// Ranged weapon
		sb.Append("Ranged: ");
		var rangedKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
		if (rangedKey.HasValue)
		{
			var slot = _player.GetInventorySlot(rangedKey.Value);
			if (slot != null)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]");
			}
			else
			{
				sb.AppendLine("(error)");
			}
		}
		else
		{
			sb.AppendLine("(none)");
		}

		// Armor
		sb.Append("Armor: ");
		var armorKey = equipComponent.GetEquippedKey(EquipmentSlot.Armor);
		if (armorKey.HasValue)
		{
			var slot = _player.GetInventorySlot(armorKey.Value);
			if (slot != null)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]");
			}
			else
			{
				sb.AppendLine("(error)");
			}
		}
		else
		{
			sb.AppendLine("(none)");
		}

		// Ring 1
		sb.Append("Ring: ");
		var ring1Key = equipComponent.GetEquippedKey(EquipmentSlot.Ring1);
		if (ring1Key.HasValue)
		{
			var slot = _player.GetInventorySlot(ring1Key.Value);
			if (slot != null)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]");
			}
			else
			{
				sb.AppendLine("(error)");
			}
		}
		else
		{
			sb.AppendLine("(none)");
		}

		// Ring 2
		sb.Append("Ring: ");
		var ring2Key = equipComponent.GetEquippedKey(EquipmentSlot.Ring2);
		if (ring2Key.HasValue)
		{
			var slot = _player.GetInventorySlot(ring2Key.Value);
			if (slot != null)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]");
			}
			else
			{
				sb.AppendLine("(error)");
			}
		}
		else
		{
			sb.AppendLine("(none)");
		}

		_equipmentLabel.Text = sb.ToString();
	}

	private void UpdateStandingOnDisplay()
	{
		if (_standingOnLabel == null)
			return;

		if (_player == null || _entityManager == null)
		{
			_standingOnLabel.Text = "";
			return;
		}

		// Check for walkable entities at player position
		var entityAtPosition = _entityManager.GetEntityAtPosition(_player.GridPosition);

		if (entityAtPosition != null && entityAtPosition.IsWalkable)
		{
			// Display the item with its glyph
			string displayName = entityAtPosition.ItemData != null
				? entityAtPosition.ItemData.Template.GetDisplayName(1)
				: entityAtPosition.DisplayName;
			_standingOnLabel.Text = $"{entityAtPosition.Glyph} {displayName}";
			_standingOnLabel.AddThemeColorOverride("font_color", entityAtPosition.GlyphColor);
		}
		else
		{
			// Nothing walkable at this position
			_standingOnLabel.Text = "";
		}
	}

	private void UpdateStatsDisplay()
	{
		if (_statsLabel == null || _player == null)
			return;

		var statsComponent = _player.GetNodeOrNull<Components.StatsComponent>("StatsComponent");
		if (statsComponent == null)
		{
			_statsLabel.Text = "(No stats system)";
			return;
		}

		var sb = new StringBuilder();

		// Base stats - full names, one per line
		sb.AppendLine($"Strength: {statsComponent.TotalStrength}");
		sb.AppendLine($"Agility: {statsComponent.TotalAgility}");
		sb.AppendLine($"Endurance: {statsComponent.TotalEndurance}");
		sb.AppendLine($"Will: {statsComponent.TotalWill}");

		sb.AppendLine(); // Blank line for spacing

		// Derived combat values
		int meleeAttack = statsComponent.TotalStrength;
		int rangedAttack = statsComponent.TotalAgility;
		int evasion = statsComponent.TotalAgility + statsComponent.TotalEvasionPenalty;
		int armor = statsComponent.TotalArmor;

		// Get attack component for damage dice
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
					int damageBonus = statsComponent.TotalStrength;
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
		if (_visibleEntitiesLabel == null)
			return;

		// Need all systems to be available
		if (_player == null || _entityManager == null || _visionSystem == null)
		{
			_visibleEntitiesLabel.Text = "(not initialized)";
			return;
		}

		var visibleEntities = new System.Collections.Generic.List<(Entities.BaseEntity entity, int distance)>();
		var playerPos = _player.GridPosition;

		// Get all entities and filter by visibility
		foreach (var entity in _entityManager.GetAllEntities())
		{
			// Skip if not visible
			if (!_visionSystem.IsVisible(entity.GridPosition))
				continue;

			// Calculate Chebyshev distance (grid-based)
			int distance = Helpers.DistanceHelper.ChebyshevDistance(playerPos, entity.GridPosition);

			// Skip if at player position (already shown in "Standing on")
			if (distance == 0)
				continue;

			visibleEntities.Add((entity, distance));
		}

		// Sort by distance and take top 5
		visibleEntities.Sort((a, b) => a.distance.CompareTo(b.distance));
		var nearbyEntities = visibleEntities.Take(5);

		// Build display text with colors and glyphs
		var sb = new StringBuilder();

		if (!nearbyEntities.Any())
		{
			sb.Append("(nothing visible)");
		}
		else
		{
			foreach (var (entity, distance) in nearbyEntities)
			{
				// Convert entity color to hex for BBCode
				string colorHex = $"#{(int)(entity.GlyphColor.R * 255):X2}{(int)(entity.GlyphColor.G * 255):X2}{(int)(entity.GlyphColor.B * 255):X2}";
				sb.AppendLine($"[color={colorHex}]{entity.Glyph} {entity.DisplayName}[/color]");
			}
		}

		_visibleEntitiesLabel.Text = sb.ToString().TrimEnd();
	}

	public override void _ExitTree()
	{
		// Disconnect from player health component
		if (_playerHealth != null)
		{
			_playerHealth.Disconnect(Components.HealthComponent.SignalName.HealthChanged, Callable.From<int, int>(OnHealthChanged));
		}

		// Disconnect from player stats component
		if (_playerStats != null)
		{
			_playerStats.Disconnect(Components.StatsComponent.SignalName.StatsChanged, Callable.From(OnStatsChanged));
			_playerStats.Disconnect(Components.StatsComponent.SignalName.ExperienceGained, Callable.From<int, int, int>(OnExperienceGained));
			_playerStats.Disconnect(Components.StatsComponent.SignalName.LevelUp, Callable.From<int>(OnLevelUp));
		}

		// Disconnect from player signals
		if (_player != null)
		{
			_player.Disconnect(Entities.Player.SignalName.PositionChanged, Callable.From<int, int>(OnPlayerPositionChanged));
			_player.Disconnect(Entities.Player.SignalName.InventoryChanged, Callable.From(OnInventoryChanged));
		}

		// Disconnect from equipment component
		if (_playerEquip != null)
		{
			_playerEquip.Disconnect(EquipComponent.SignalName.EquipmentChanged, Callable.From<EquipmentSlot>(OnEquipmentChanged));
		}

		// Disconnect from gold manager
		if (_goldManager != null)
		{
			_goldManager.Disconnect(Systems.GoldManager.SignalName.GoldChanged, Callable.From<int, int>(OnGoldChanged));
		}
	}
}
