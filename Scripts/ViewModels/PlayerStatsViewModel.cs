using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.ViewModels;

/// <summary>
/// View model that aggregates player stats data from multiple components.
/// Provides a single point of access for UI display and emits a unified update signal.
/// Reduces coupling between UI and individual components.
/// </summary>
public partial class PlayerStatsViewModel : Node
{
	#region Signals

	/// <summary>
	/// Emitted when any player stat changes.
	/// UI should refresh its display when this signal is received.
	/// </summary>
	[Signal]
	public delegate void StatsUpdatedEventHandler();

	#endregion

	#region State

	private Player _player;
	private HealthComponent _healthComponent;
	private WillpowerComponent _willpowerComponent;
	private StatsComponent _statsComponent;
	private GoldManager _goldManager;
	private EpithetDefinition _currentEpithet;

	#endregion

	#region Properties - Health

    public int MaxHealth => _healthComponent?.MaxHealth ?? 0;
    public int CurrentHealth => _healthComponent?.CurrentHealth ?? 0;

	#endregion

	#region Properties - Willpower

    public int MaxWillpower => _willpowerComponent?.MaxWillpower ?? 0;
    public int CurrentWillpower => _willpowerComponent?.CurrentWillpower ?? 0;

	#endregion

	#region Properties - Level & Experience

    public int Level => _statsComponent?.Level ?? 1;
    public int CurrentXP => _statsComponent?.CurrentExperience ?? 0;
    public int XPToNextLevel => _statsComponent?.ExperienceToNextLevel ?? 0;

	#endregion

	#region Properties - Base Stats

    public int BaseStrength => _statsComponent?.BaseStrength ?? 0;
    public int BaseAgility => _statsComponent?.BaseAgility ?? 0;
    public int BaseEndurance => _statsComponent?.BaseEndurance ?? 0;
    public int BaseWill => _statsComponent?.BaseWill ?? 0;

	#endregion

	#region Properties - Total Stats (with modifiers)

    public int TotalStrength => _statsComponent?.TotalStrength ?? 0;
    public int TotalAgility => _statsComponent?.TotalAgility ?? 0;
    public int TotalEndurance => _statsComponent?.TotalEndurance ?? 0;
    public int TotalWill => _statsComponent?.TotalWill ?? 0;

	#endregion

	#region Properties - Derived Combat Stats

    public int MeleeAttack => _statsComponent?.MeleeAttack ?? 0;
    public int RangedAttack => _statsComponent?.RangedAttack ?? 0;
    public int TotalArmor => _statsComponent?.TotalArmor ?? 0;
    public int TotalEvasion => _statsComponent?.TotalEvasion ?? 0;
    /// <summary>
    /// Total melee damage bonus (STR + melee damage modifiers from skills/equipment).
    /// </summary>
    public int MeleeDamageBonus => _statsComponent?.GetDamageBonus(true) ?? 0;

	#endregion

	#region Properties - Resources

    public int Gold => _goldManager?.Gold ?? 0;
	public int FloorDepth { get; private set; }
	public string PlayerName => _player?.DisplayName ?? "Player";

	#endregion

	#region Properties - Epithet

	/// <summary>
	/// Current epithet name based on base stat distribution.
	/// </summary>
	public string Epithet => _currentEpithet?.Name ?? "Wanderer";

	/// <summary>
	/// Atmospheric description for the current epithet.
	/// </summary>
	public string EpithetDescription => _currentEpithet?.Description ?? string.Empty;

	/// <summary>
	/// Formatted display name including epithet (e.g., "Hero the Titan").
	/// </summary>
	public string FormattedName => $"{PlayerName} the {Epithet}";

	#endregion

	#region Initialization

	/// <summary>
	/// Initializes the view model with required components and systems.
	/// </summary>
	/// <param name="player">The player entity</param>
	/// <param name="goldManager">Gold management system</param>
	/// <param name="floorDepth">Current floor depth</param>
	public void Initialize(Player player, GoldManager goldManager, int floorDepth)
	{
		if (player == null)
		{
			GD.PushError("PlayerStatsViewModel: Cannot initialize with null player.");
			return;
		}

		if (goldManager == null)
		{
			GD.PushError("PlayerStatsViewModel: Cannot initialize with null gold manager.");
			return;
		}

		_player = player;
		_goldManager = goldManager;
		FloorDepth = floorDepth;

		// Get components
		_healthComponent = _player.GetNodeOrNull<HealthComponent>("HealthComponent");
		_willpowerComponent = _player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
		_statsComponent = _player.GetNodeOrNull<StatsComponent>("StatsComponent");

		if (_healthComponent == null)
		{
			GD.PushError("PlayerStatsViewModel: Player missing HealthComponent.");
			return;
		}

		if (_willpowerComponent == null)
		{
			GD.PushWarning("PlayerStatsViewModel: Player missing WillpowerComponent. Willpower display will be disabled.");
		}

		if (_statsComponent == null)
		{
			GD.PushError("PlayerStatsViewModel: Player missing StatsComponent.");
			return;
		}

		// Connect to component signals
		ConnectToSignals();

		// Initial epithet calculation
		UpdateEpithet();

		// Initial update
		EmitSignal(SignalName.StatsUpdated);
	}

	/// <summary>
	/// Updates the floor depth value.
	/// </summary>
	public void SetFloorDepth(int depth)
	{
		if (FloorDepth != depth)
		{
			FloorDepth = depth;
			EmitSignal(SignalName.StatsUpdated);
		}
	}

	#endregion

	#region Signal Connection

	private void ConnectToSignals()
	{
		// Health changes
		_healthComponent.Connect(
			HealthComponent.SignalName.HealthChanged,
			Callable.From<int, int>(OnHealthChanged)
		);

		// Willpower changes
		if (_willpowerComponent != null)
		{
			_willpowerComponent.Connect(
				WillpowerComponent.SignalName.WillpowerChanged,
				Callable.From<int, int>(OnWillpowerChanged)
			);
		}

		// Stat changes
		_statsComponent.Connect(
			StatsComponent.SignalName.StatsChanged,
			Callable.From(OnStatsChanged)
		);

		_statsComponent.Connect(
			StatsComponent.SignalName.ExperienceGained,
			Callable.From<int, int, int>(OnExperienceGained)
		);

		_statsComponent.Connect(
			StatsComponent.SignalName.LevelUp,
			Callable.From<int>(OnLevelUp)
		);

		// Gold changes
		_goldManager.Connect(
			GoldManager.SignalName.GoldChanged,
			Callable.From<int, int>(OnGoldChanged)
		);
	}

	#endregion

	#region Event Handlers

	private void OnStatsChanged()
	{
		UpdateEpithet();
		EmitSignal(SignalName.StatsUpdated);
	}

	private void OnHealthChanged(int current, int max) => EmitSignal(SignalName.StatsUpdated);
	private void OnWillpowerChanged(int current, int max) => EmitSignal(SignalName.StatsUpdated);
	private void OnExperienceGained(int amount, int current, int toNext) => EmitSignal(SignalName.StatsUpdated);
	private void OnLevelUp(int newLevel) => EmitSignal(SignalName.StatsUpdated);
	private void OnGoldChanged(int amount, int total) => EmitSignal(SignalName.StatsUpdated);

	#endregion

	#region Epithet

	/// <summary>
	/// Updates the current epithet based on base stats.
	/// Also updates the player's description to match the epithet.
	/// </summary>
	private void UpdateEpithet()
	{
		if (_statsComponent == null)
			return;

		_currentEpithet = EpithetResolver.Resolve(
			_statsComponent.BaseStrength,
			_statsComponent.BaseAgility,
			_statsComponent.BaseEndurance,
			_statsComponent.BaseWill
		);

		// Update player description to match epithet
		if (_player != null && _currentEpithet != null)
		{
			_player.Description = _currentEpithet.Description;
		}
	}

	#endregion

	#region Cleanup

	public override void _ExitTree()
	{
		// Disconnect from component signals
		if (_healthComponent != null)
		{
			_healthComponent.Disconnect(
				HealthComponent.SignalName.HealthChanged,
				Callable.From<int, int>(OnHealthChanged)
			);
		}

		if (_willpowerComponent != null)
		{
			_willpowerComponent.Disconnect(
				WillpowerComponent.SignalName.WillpowerChanged,
				Callable.From<int, int>(OnWillpowerChanged)
			);
		}

		if (_statsComponent != null)
		{
			_statsComponent.Disconnect(
				StatsComponent.SignalName.StatsChanged,
				Callable.From(OnStatsChanged)
			);

			_statsComponent.Disconnect(
				StatsComponent.SignalName.ExperienceGained,
				Callable.From<int, int, int>(OnExperienceGained)
			);

			_statsComponent.Disconnect(
				StatsComponent.SignalName.LevelUp,
				Callable.From<int>(OnLevelUp)
			);
		}

		if (_goldManager != null)
		{
			_goldManager.Disconnect(
				GoldManager.SignalName.GoldChanged,
				Callable.From<int, int>(OnGoldChanged)
			);
		}
	}

	#endregion
}
