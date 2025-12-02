using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that dazzles an entity (minor blind).
/// Dazzled entities have reduced accuracy and evasion (-2 each).
/// Counts as "distracted" for assassin synergy skills.
/// </summary>
public class DazzledCondition : Condition
{
	private const int AccuracyPenalty = -2;
	private const int EvasionPenalty = -2;
	private const string ModifierSourceId = "condition_dazzled";

	public override string Name => "Dazzled";

	public override string TypeId => "dazzled";

	public override string? ExamineDescription => "dazzled";

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public DazzledCondition()
	{
		Duration = "2d3";
	}

	/// <summary>
	/// Parameterized constructor for creating dazzled with specific duration.
	/// </summary>
	/// <param name="duration">Duration as dice notation (e.g., "2d3", "1d4").</param>
	public DazzledCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
		if (stats == null)
		{
			return ConditionMessage.Empty;
		}

		// Apply accuracy penalties (both melee and ranged)
		stats.AddStatModifier(StatType.MeleeAccuracy, ModifierSourceId, AccuracyPenalty);
		stats.AddStatModifier(StatType.RangedAccuracy, ModifierSourceId, AccuracyPenalty);

		// Apply evasion penalty
		stats.AddStatModifier(StatType.Evasion, ModifierSourceId, EvasionPenalty);

		return new ConditionMessage(
			$"The {target.DisplayName} is dazzled!",
			Palette.ToHex(Palette.StatusDebuff)
		);
	}

	public override ConditionMessage OnTurnProcessed(BaseEntity target)
	{
		// No per-turn effects
		return ConditionMessage.Empty;
	}

	public override ConditionMessage OnRemoved(BaseEntity target)
	{
		var stats = target.GetNodeOrNull<StatsComponent>("StatsComponent");
		if (stats != null)
		{
			// Remove all modifiers
			stats.RemoveStatModifier(StatType.MeleeAccuracy, ModifierSourceId);
			stats.RemoveStatModifier(StatType.RangedAccuracy, ModifierSourceId);
			stats.RemoveStatModifier(StatType.Evasion, ModifierSourceId);
		}

		return new ConditionMessage(
			$"The {target.DisplayName}'s vision clears.",
			Palette.ToHex(Palette.Default)
		);
	}
}
