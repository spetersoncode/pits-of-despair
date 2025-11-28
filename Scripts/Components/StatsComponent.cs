using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Conditions;

namespace PitsOfDespair.Components;

/// <summary>
/// Manages creature stats with multi-source modifier tracking.
/// Supports base stats (STR, AGI, END, WIL), armor, and evasion modifiers.
/// </summary>
public partial class StatsComponent : Node
{
	#region Constants

	/// <summary>
	/// Maximum value for any single stat (STR, AGI, END, WIL).
	/// </summary>
	public const int STAT_CAP = 12;

	#endregion

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
	/// Character level (affects HP from Endurance, used for player progression).
	/// HP bonus = Endurance × Level
	/// </summary>
	[Export] public int Level { get; set; } = 1;

	/// <summary>
	/// Threat rating for creatures (affects XP rewards and spawn budgets).
	/// Unbounded scale: 1-5 trivial, 6-15 standard, 16-30 dangerous, 31-50 elite, 51+ boss.
	/// For players, this is typically 0 (unused).
	/// </summary>
	[Export] public int Threat { get; set; } = 0;

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

	/// <summary>
	/// Unified storage for all stat modifiers from equipment, buffs, debuffs, etc.
	/// Key: StatType, Value: Dictionary of source ID to modifier amount.
	/// </summary>
	private readonly Dictionary<StatType, Dictionary<string, int>> _statModifiers = new()
	{
		{ StatType.Strength, new Dictionary<string, int>() },
		{ StatType.Agility, new Dictionary<string, int>() },
		{ StatType.Endurance, new Dictionary<string, int>() },
		{ StatType.Will, new Dictionary<string, int>() },
		{ StatType.Armor, new Dictionary<string, int>() },
		{ StatType.Evasion, new Dictionary<string, int>() },
	};

	#endregion

	#region Computed Total Stats

	/// <summary>
	/// Total Strength including all modifiers.
	/// </summary>
	public int TotalStrength => BaseStrength + GetStatModifierTotal(StatType.Strength);

	/// <summary>
	/// Total Agility including all modifiers.
	/// </summary>
	public int TotalAgility => BaseAgility + GetStatModifierTotal(StatType.Agility);

	/// <summary>
	/// Total Endurance including all modifiers.
	/// </summary>
	public int TotalEndurance => BaseEndurance + GetStatModifierTotal(StatType.Endurance);

	/// <summary>
	/// Total Will including all modifiers.
	/// </summary>
	public int TotalWill => BaseWill + GetStatModifierTotal(StatType.Will);

	/// <summary>
	/// Total armor value from all sources.
	/// Reduces incoming damage.
	/// </summary>
	public int TotalArmor => GetStatModifierTotal(StatType.Armor);

	/// <summary>
	/// Total evasion modifier from all sources.
	/// Positive increases evasion, negative reduces it (heavy armor).
	/// </summary>
	public int TotalEvasionModifier => GetStatModifierTotal(StatType.Evasion);

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
	/// Combines Agility with evasion modifiers from armor/buffs.
	/// </summary>
	public int TotalEvasion => TotalAgility + TotalEvasionModifier;

	#endregion

	#region Stat Modifier Management

	/// <summary>
	/// Adds a stat modifier from a named source.
	/// </summary>
	/// <param name="stat">The stat type to modify</param>
	/// <param name="sourceId">Source identifier (e.g., "equipped_ring", "potion_buff")</param>
	/// <param name="amount">Modifier value (positive for buffs, negative for penalties)</param>
	public void AddStatModifier(StatType stat, string sourceId, int amount)
	{
		if (_statModifiers.TryGetValue(stat, out var modifiers))
		{
			modifiers[sourceId] = amount;
			EmitSignal(SignalName.StatsChanged);
		}
	}

	/// <summary>
	/// Removes a stat modifier by source name.
	/// </summary>
	/// <param name="stat">The stat type to modify</param>
	/// <param name="sourceId">Source identifier to remove</param>
	public void RemoveStatModifier(StatType stat, string sourceId)
	{
		if (_statModifiers.TryGetValue(stat, out var modifiers) && modifiers.Remove(sourceId))
		{
			EmitSignal(SignalName.StatsChanged);
		}
	}

	/// <summary>
	/// Gets the total modifier for a stat from all sources.
	/// </summary>
	/// <param name="stat">The stat type to query</param>
	/// <returns>Sum of all modifiers for the stat</returns>
	public int GetStatModifierTotal(StatType stat)
	{
		return _statModifiers.TryGetValue(stat, out var modifiers) ? modifiers.Values.Sum() : 0;
	}

	/// <summary>
	/// Gets the modifier dictionary for a stat type (for serialization).
	/// </summary>
	/// <param name="stat">The stat type to query</param>
	/// <returns>Dictionary of source IDs to modifier amounts, or empty dictionary if not found</returns>
	public IReadOnlyDictionary<string, int> GetStatModifiers(StatType stat)
	{
		return _statModifiers.TryGetValue(stat, out var modifiers)
			? modifiers
			: new Dictionary<string, int>();
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
	/// <returns>AGI + evasion modifier</returns>
	public int GetDefenseModifier()
	{
		return TotalAgility + TotalEvasionModifier;
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
	/// Checks if a stat is at the cap and cannot be increased.
	/// </summary>
	/// <param name="statIndex">0=STR, 1=AGI, 2=END, 3=WIL</param>
	/// <returns>True if the stat is at or above STAT_CAP</returns>
	public bool IsStatAtCap(int statIndex)
	{
		return statIndex switch
		{
			0 => BaseStrength >= STAT_CAP,
			1 => BaseAgility >= STAT_CAP,
			2 => BaseEndurance >= STAT_CAP,
			3 => BaseWill >= STAT_CAP,
			_ => true
		};
	}

	/// <summary>
	/// Gets the base value of a stat by index.
	/// </summary>
	/// <param name="statIndex">0=STR, 1=AGI, 2=END, 3=WIL</param>
	/// <returns>The base stat value</returns>
	public int GetBaseStat(int statIndex)
	{
		return statIndex switch
		{
			0 => BaseStrength,
			1 => BaseAgility,
			2 => BaseEndurance,
			3 => BaseWill,
			_ => 0
		};
	}

	/// <summary>
	/// Gets the Health bonus from Endurance using flat scaling.
	/// Formula: END × 5
	/// Negative END returns 0 (Health floors at base in HealthComponent).
	/// </summary>
	/// <remarks>
	/// Previous quadratic formula (preserved in case we revert):
	/// Formula: (END² + 9×END) / 2
	/// This provided marginal gains of (4 + END value) per point.
	/// END 1: +5, END 2: +11, END 3: +18, END 4: +26, END 5: +35, etc.
	/// return (endurance * endurance + 9 * endurance) / 2;
	/// </remarks>
	/// <returns>Health bonus from Endurance stat</returns>
	public int GetHealthBonus()
	{
		int endurance = TotalEndurance;

		// Negative endurance doesn't reduce Health below base (floor handled in HealthComponent)
		if (endurance <= 0)
			return 0;

		// Flat scaling: END × 5
		return endurance * 5;
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
