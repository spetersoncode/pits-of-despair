using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Components;

/// <summary>
/// Manages creature stats with multi-source modifier tracking.
/// Supports base stats (STR, AGI, END, WIL), armor values, and evasion penalties.
/// </summary>
public partial class StatsComponent : Node
{
	#region Signals

	/// <summary>
	/// Emitted when any stat value changes.
	/// </summary>
	[Signal]
	public delegate void StatsChangedEventHandler();

	/// <summary>
	/// Emitted when experience is gained.
	/// Parameters: amount gained, current XP, XP needed for next level
	/// </summary>
	[Signal]
	public delegate void ExperienceGainedEventHandler(int amount, int current, int toNext);

	/// <summary>
	/// Emitted when the entity levels up.
	/// Parameter: new level
	/// </summary>
	[Signal]
	public delegate void LevelUpEventHandler(int newLevel);

	#endregion

	#region Base Stats

	/// <summary>
	/// Base Strength value (before modifiers).
	/// Affects melee attack accuracy and damage.
	/// </summary>
	[Export] public int BaseStrength { get; set; } = 0;

	/// <summary>
	/// Base Agility value (before modifiers).
	/// Affects ranged attack accuracy and evasion.
	/// </summary>
	[Export] public int BaseAgility { get; set; } = 0;

	/// <summary>
	/// Base Endurance value (before modifiers).
	/// Affects hit points (+Endurance HP per level).
	/// </summary>
	[Export] public int BaseEndurance { get; set; } = 0;

	/// <summary>
	/// Base Will value (before modifiers).
	/// Reserved for future magic/abilities system.
	/// </summary>
	[Export] public int BaseWill { get; set; } = 0;

	/// <summary>
	/// Character level (affects HP from Endurance).
	/// HP bonus = Endurance × Level
	/// </summary>
	[Export] public int Level { get; set; } = 1;

	/// <summary>
	/// Current experience points.
	/// </summary>
	public int CurrentExperience { get; private set; } = 0;

	/// <summary>
	/// Experience points required to reach the next level from current level.
	/// Since XP is subtracted on level-up, this shows the gap between levels.
	/// </summary>
	public int ExperienceToNextLevel => GetXPForLevel(Level + 1) - GetXPForLevel(Level);

	#endregion

	#region Multi-Source Modifier Tracking

	// Stat modifiers from equipment, buffs, debuffs, etc.
	private readonly Dictionary<string, int> _strengthModifiers = new();
	private readonly Dictionary<string, int> _agilityModifiers = new();
	private readonly Dictionary<string, int> _enduranceModifiers = new();
	private readonly Dictionary<string, int> _willModifiers = new();

	// Armor value from equipment, buffs, etc.
	private readonly Dictionary<string, int> _armorSources = new();

	// Evasion penalty from armor, debuffs, etc.
	private readonly Dictionary<string, int> _evasionPenaltySources = new();

	#endregion

	#region Computed Total Stats

	/// <summary>
	/// Total Strength including all modifiers.
	/// </summary>
	public int TotalStrength => BaseStrength + _strengthModifiers.Values.Sum();

	/// <summary>
	/// Total Agility including all modifiers.
	/// </summary>
	public int TotalAgility => BaseAgility + _agilityModifiers.Values.Sum();

	/// <summary>
	/// Total Endurance including all modifiers.
	/// </summary>
	public int TotalEndurance => BaseEndurance + _enduranceModifiers.Values.Sum();

	/// <summary>
	/// Total Will including all modifiers.
	/// </summary>
	public int TotalWill => BaseWill + _willModifiers.Values.Sum();

	/// <summary>
	/// Total armor value from all sources.
	/// Reduces incoming damage.
	/// </summary>
	public int TotalArmor => _armorSources.Values.Sum();

	/// <summary>
	/// Total evasion penalty from all sources.
	/// Negative value reduces evasion rolls (heavy armor restricts movement).
	/// </summary>
	public int TotalEvasionPenalty => _evasionPenaltySources.Values.Sum();

	/// <summary>
	/// Melee attack modifier for attack rolls.
	/// Based on total Strength.
	/// </summary>
	public int MeleeAttack => TotalStrength;

	/// <summary>
	/// Ranged attack modifier for attack rolls.
	/// Based on total Agility.
	/// </summary>
	public int RangedAttack => TotalAgility;

	/// <summary>
	/// Total evasion value for defense rolls.
	/// Combines Agility with evasion penalties from armor.
	/// </summary>
	public int TotalEvasion => TotalAgility + TotalEvasionPenalty;

	#endregion

	#region Stat Modifier Management

	/// <summary>
	/// Adds a Strength modifier from a named source.
	/// </summary>
	/// <param name="source">Source identifier (e.g., "equipped_ring", "potion_buff")</param>
	/// <param name="value">Modifier value (can be positive or negative)</param>
	public void AddStrengthModifier(string source, int value)
	{
		_strengthModifiers[source] = value;
		EmitSignal(SignalName.StatsChanged);
	}

	/// <summary>
	/// Removes a Strength modifier by source name.
	/// </summary>
	public void RemoveStrengthModifier(string source)
	{
		if (_strengthModifiers.Remove(source))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	/// <summary>
	/// Adds an Agility modifier from a named source.
	/// </summary>
	public void AddAgilityModifier(string source, int value)
	{
		_agilityModifiers[source] = value;
		EmitSignal(SignalName.StatsChanged);
	}

	/// <summary>
	/// Removes an Agility modifier by source name.
	/// </summary>
	public void RemoveAgilityModifier(string source)
	{
		if (_agilityModifiers.Remove(source))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	/// <summary>
	/// Adds an Endurance modifier from a named source.
	/// </summary>
	public void AddEnduranceModifier(string source, int value)
	{
		_enduranceModifiers[source] = value;
		EmitSignal(SignalName.StatsChanged);
	}

	/// <summary>
	/// Removes an Endurance modifier by source name.
	/// </summary>
	public void RemoveEnduranceModifier(string source)
	{
		if (_enduranceModifiers.Remove(source))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	/// <summary>
	/// Adds a Will modifier from a named source.
	/// </summary>
	public void AddWillModifier(string source, int value)
	{
		_willModifiers[source] = value;
		EmitSignal(SignalName.StatsChanged);
	}

	/// <summary>
	/// Removes a Will modifier by source name.
	/// </summary>
	public void RemoveWillModifier(string source)
	{
		if (_willModifiers.Remove(source))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	#endregion

	#region Armor & Evasion Management

	/// <summary>
	/// Adds armor value from a named source.
	/// </summary>
	/// <param name="source">Source identifier (e.g., "equipped_armor", "ring_of_protection")</param>
	/// <param name="value">Armor value to add</param>
	public void AddArmorSource(string source, int value)
	{
		_armorSources[source] = value;
		EmitSignal(SignalName.StatsChanged);
	}

	/// <summary>
	/// Removes armor value by source name.
	/// </summary>
	public void RemoveArmorSource(string source)
	{
		if (_armorSources.Remove(source))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	/// <summary>
	/// Adds evasion penalty from a named source.
	/// </summary>
	/// <param name="source">Source identifier (e.g., "equipped_armor", "slow_debuff")</param>
	/// <param name="value">Penalty value (typically negative)</param>
	public void AddEvasionPenaltySource(string source, int value)
	{
		_evasionPenaltySources[source] = value;
		EmitSignal(SignalName.StatsChanged);
	}

	/// <summary>
	/// Removes evasion penalty by source name.
	/// </summary>
	public void RemoveEvasionPenaltySource(string source)
	{
		if (_evasionPenaltySources.Remove(source))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Gets the appropriate attack modifier based on weapon type.
	/// </summary>
	/// <param name="isMelee">True for melee weapons, false for ranged</param>
	/// <returns>STR for melee, AGI for ranged</returns>
	public int GetAttackModifier(bool isMelee)
	{
		return isMelee ? TotalStrength : TotalAgility;
	}

	/// <summary>
	/// Gets the defense modifier for evasion rolls.
	/// </summary>
	/// <returns>AGI + evasion penalty (penalty is negative)</returns>
	public int GetDefenseModifier()
	{
		return TotalAgility + TotalEvasionPenalty;
	}

	/// <summary>
	/// Gets the damage bonus for attacks (melee only).
	/// </summary>
	/// <param name="isMelee">True for melee weapons, false for ranged</param>
	/// <returns>STR for melee, 0 for ranged</returns>
	public int GetDamageBonus(bool isMelee)
	{
		return isMelee ? TotalStrength : 0;
	}

	/// <summary>
	/// Gets the HP bonus from Endurance using quadratic scaling.
	/// Formula: (END² + 9×END) / 2
	/// This provides marginal gains of (4 + END value) per point.
	/// Negative END returns 0 (HP floors at base in HealthComponent).
	/// </summary>
	/// <returns>HP bonus from Endurance stat</returns>
	public int GetHPBonus()
	{
		int endurance = TotalEndurance;

		// Negative endurance doesn't reduce HP below base (floor handled in HealthComponent)
		if (endurance <= 0)
			return 0;

		// Quadratic scaling: (END² + 9×END) / 2
		// This gives marginal gains of (4 + new END value) per point
		// END 1: +5, END 2: +6, END 3: +7, etc.
		return (endurance * endurance + 9 * endurance) / 2;
	}

	#endregion

	#region Experience Management

	/// <summary>
	/// Calculates the total cumulative XP required to reach a given level from level 1.
	/// Formula: 50 × (level - 1) × level
	/// Level 1 = 0 XP (starting point)
	/// Level 2 = 100 XP, Level 3 = 300 XP, Level 4 = 600 XP, Level 5 = 1000 XP...
	/// </summary>
	/// <param name="level">Target level</param>
	/// <returns>Cumulative XP threshold for that level</returns>
	public static int GetXPForLevel(int level)
	{
		return 50 * (level - 1) * level;
	}

	/// <summary>
	/// Awards experience points and handles level-ups.
	/// </summary>
	/// <param name="amount">Amount of XP to gain</param>
	public void GainExperience(int amount)
	{
		if (amount <= 0) return;

		CurrentExperience += amount;

		// Check for level-up(s) - handle overflow in case of large XP gains
		bool leveledUp = false;
		while (CurrentExperience >= ExperienceToNextLevel)
		{
			CurrentExperience -= ExperienceToNextLevel;
			Level++;
			leveledUp = true;
			EmitSignal(SignalName.LevelUp, Level);
		}

		// Emit experience gained signal
		EmitSignal(SignalName.ExperienceGained, amount, CurrentExperience, ExperienceToNextLevel);

		// If we leveled up, stats changed (HP will recalculate)
		if (leveledUp)
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	#endregion
}
