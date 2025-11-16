using Godot;

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

	private int _currentFloorDepth = 1;
	private Entities.Player _player;
	private Systems.EntityManager _entityManager;

	public override void _Ready()
	{
		_healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthLabel");
		_floorLabel = GetNode<Label>("MarginContainer/VBoxContainer/FloorLabel");
		_standingOnLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/StandingOnLabel");

		UpdateFloorDisplay();
		UpdateStandingOnDisplay();
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

		// Initialize display with current values
		OnHealthChanged(healthComponent.CurrentHP, healthComponent.MaxHP);
		UpdateStandingOnDisplay();
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

	private void UpdateStandingOnDisplay()
	{
		if (_standingOnLabel == null)
			return;

		if (_player == null || _entityManager == null)
		{
			_standingOnLabel.Text = "";
			return;
		}

		// Check for passable entities at player position
		var entityAtPosition = _entityManager.GetEntityAtPosition(_player.GridPosition);

		if (entityAtPosition != null && entityAtPosition.Passable)
		{
			// Display the item with its glyph
			_standingOnLabel.Text = $"{entityAtPosition.Glyph} {entityAtPosition.DisplayName}";
			_standingOnLabel.AddThemeColorOverride("font_color", entityAtPosition.GlyphColor);
		}
		else
		{
			// Nothing passable at this position
			_standingOnLabel.Text = "";
		}
	}
}
