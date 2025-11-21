using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

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
	private StatsComponent _statsComponent;
	private GoldManager _goldManager;

	#endregion

	#region Properties - Health

	public int CurrentHP { get; private set; }
	public int MaxHP { get; private set; }

	#endregion

	#region Properties - Level & Experience

	public int Level { get; private set; }
	public int CurrentXP { get; private set; }
	public int XPToNextLevel { get; private set; }

	#endregion

	#region Properties - Base Stats

	public int BaseStrength { get; private set; }
	public int BaseAgility { get; private set; }
	public int BaseEndurance { get; private set; }
	public int BaseWill { get; private set; }

	#endregion

	#region Properties - Total Stats (with modifiers)

	public int TotalStrength { get; private set; }
	public int TotalAgility { get; private set; }
	public int TotalEndurance { get; private set; }
	public int TotalWill { get; private set; }

	#endregion

	#region Properties - Derived Combat Stats

	public int MeleeAttack { get; private set; }
	public int RangedAttack { get; private set; }
	public int TotalArmor { get; private set; }
	public int TotalEvasion { get; private set; }

	#endregion

	#region Properties - Resources

	public int Gold { get; private set; }
	public int FloorDepth { get; private set; }

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
		_statsComponent = _player.GetNodeOrNull<StatsComponent>("StatsComponent");

		if (_healthComponent == null)
		{
			GD.PushError("PlayerStatsViewModel: Player missing HealthComponent.");
			return;
		}

		if (_statsComponent == null)
		{
			GD.PushError("PlayerStatsViewModel: Player missing StatsComponent.");
			return;
		}

		// Connect to component signals
		ConnectToSignals();

		// Initialize properties with current values
		UpdateAllStats();
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

	#region Update Methods

	/// <summary>
	/// Updates all stats from components.
	/// Called during initialization.
	/// </summary>
	private void UpdateAllStats()
	{
		UpdateHealthStats();
		UpdateLevelAndXP();
		UpdateBaseStats();
		UpdateTotalStats();
		UpdateDerivedStats();
		UpdateGold();
	}

	private void UpdateHealthStats()
	{
		CurrentHP = _healthComponent.CurrentHP;
		MaxHP = _healthComponent.MaxHP;
	}

	private void UpdateLevelAndXP()
	{
		Level = _statsComponent.Level;
		CurrentXP = _statsComponent.CurrentExperience;
		XPToNextLevel = _statsComponent.ExperienceToNextLevel;
	}

	private void UpdateBaseStats()
	{
		BaseStrength = _statsComponent.BaseStrength;
		BaseAgility = _statsComponent.BaseAgility;
		BaseEndurance = _statsComponent.BaseEndurance;
		BaseWill = _statsComponent.BaseWill;
	}

	private void UpdateTotalStats()
	{
		TotalStrength = _statsComponent.TotalStrength;
		TotalAgility = _statsComponent.TotalAgility;
		TotalEndurance = _statsComponent.TotalEndurance;
		TotalWill = _statsComponent.TotalWill;
	}

	private void UpdateDerivedStats()
	{
		MeleeAttack = _statsComponent.MeleeAttack;
		RangedAttack = _statsComponent.RangedAttack;
		TotalArmor = _statsComponent.TotalArmor;
		TotalEvasion = _statsComponent.TotalEvasion;
	}

	private void UpdateGold()
	{
		Gold = _goldManager.Gold;
	}

	#endregion

	#region Event Handlers

	private void OnHealthChanged(int current, int max)
	{
		UpdateHealthStats();
		EmitSignal(SignalName.StatsUpdated);
	}

	private void OnStatsChanged()
	{
		UpdateLevelAndXP();
		UpdateBaseStats();
		UpdateTotalStats();
		UpdateDerivedStats();
		EmitSignal(SignalName.StatsUpdated);
	}

	private void OnExperienceGained(int amount, int current, int toNext)
	{
		UpdateLevelAndXP();
		EmitSignal(SignalName.StatsUpdated);
	}

	private void OnLevelUp(int newLevel)
	{
		UpdateLevelAndXP();
		EmitSignal(SignalName.StatsUpdated);
	}

	private void OnGoldChanged(int amount, int totalGold)
	{
		UpdateGold();
		EmitSignal(SignalName.StatsUpdated);
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
