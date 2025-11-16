using Godot;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays player statistics including health and floor depth.
/// </summary>
public partial class StatsPanel : PanelContainer
{
	private const float HealthGreenThreshold = 0.6f;
	private const float HealthYellowThreshold = 0.3f;

	private static readonly Color HealthColorGreen = new("#66ff66");
	private static readonly Color HealthColorYellow = new("#ffff66");
	private static readonly Color HealthColorRed = new("#ff6666");
	private static readonly Color DefaultTextColor = new("#ffffff");

	private Label _healthLabel;
	private Label _floorLabel;
	private Label _standingOnLabel;
	private RichTextLabel _equipmentLabel;

	private int _currentFloorDepth = 1;
	private Entities.Player _player;
	private Systems.EntityManager _entityManager;

	public override void _Ready()
	{
		_healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthLabel");
		_floorLabel = GetNode<Label>("MarginContainer/VBoxContainer/FloorLabel");
		_standingOnLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/StandingOnLabel");
		_equipmentLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/EquipmentLabel");

		UpdateFloorDisplay();
		UpdateStandingOnDisplay();
		UpdateEquipmentDisplay();
	}

	/// <summary>
	/// Connects to the player's health component to receive updates.
	/// </summary>
	public void ConnectToPlayer(Entities.Player player)
	{
		_player = player;

		var healthComponent = player.GetNode<Components.HealthComponent>("HealthComponent");
		healthComponent.HealthChanged += OnHealthChanged;

		// Subscribe to player position changes
		player.PositionChanged += OnPlayerPositionChanged;

		// Subscribe to inventory changes (for when items are picked up)
		player.InventoryChanged += OnInventoryChanged;

		// Subscribe to equipment changes
		var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null)
		{
			equipComponent.EquipmentChanged += OnEquipmentChanged;
		}

		// Initialize display with current values
		OnHealthChanged(healthComponent.CurrentHP, healthComponent.MaxHP);
		UpdateStandingOnDisplay();
		UpdateEquipmentDisplay();
	}

	/// <summary>
	/// Sets the entity manager reference for querying entities at player position.
	/// </summary>
	public void SetEntityManager(Systems.EntityManager entityManager)
	{
		_entityManager = entityManager;
		UpdateStandingOnDisplay();
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

	private void OnPlayerPositionChanged(int x, int y)
	{
		UpdateStandingOnDisplay();
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
	}

	private void UpdateEquipmentDisplay()
	{
		if (_equipmentLabel == null || _player == null)
			return;

		var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent == null)
		{
			_equipmentLabel.Text = "[b]Equipment:[/b]\n(No equipment system)";
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("[b]Equipment:[/b]");

		// Melee weapon
		sb.Append("Melee: ");
		var meleeKey = equipComponent.GetEquippedKey(EquipmentSlot.MeleeWeapon);
		if (meleeKey.HasValue)
		{
			var slot = _player.GetInventorySlot(meleeKey.Value);
			if (slot != null)
			{
				sb.AppendLine($"{slot.Item.Template.Name}");
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
				sb.AppendLine($"{slot.Item.Template.Name}");
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
				sb.AppendLine($"{slot.Item.Template.Name}");
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

		// Rings
		sb.Append("Rings: ");
		var ring1Key = equipComponent.GetEquippedKey(EquipmentSlot.Ring1);
		var ring2Key = equipComponent.GetEquippedKey(EquipmentSlot.Ring2);
		if (ring1Key.HasValue || ring2Key.HasValue)
		{
			var ringNames = new System.Collections.Generic.List<string>();
			if (ring1Key.HasValue)
			{
				var slot = _player.GetInventorySlot(ring1Key.Value);
				if (slot != null) ringNames.Add(slot.Item.Template.Name);
			}
			if (ring2Key.HasValue)
			{
				var slot = _player.GetInventorySlot(ring2Key.Value);
				if (slot != null) ringNames.Add(slot.Item.Template.Name);
			}
			if (ringNames.Count > 0)
			{
				sb.AppendLine(string.Join(", ", ringNames));
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
			_standingOnLabel.Text = $"{entityAtPosition.Glyph} {entityAtPosition.DisplayName}";
			_standingOnLabel.AddThemeColorOverride("font_color", entityAtPosition.GlyphColor);
		}
		else
		{
			// Nothing walkable at this position
			_standingOnLabel.Text = "";
		}
	}
}
