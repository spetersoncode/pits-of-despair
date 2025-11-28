using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that deals acid damage each turn.
/// Represents lingering corrosive damage that eats away at the target.
/// </summary>
public class AcidCondition : Condition
{
	public override string Name => "Corroding";

	public override string TypeId => "acid";

	public override string? ExamineDescription => "corroding";

	private readonly string _damageDice;

	/// <summary>
	/// The entity that applied this acid effect (for kill attribution).
	/// </summary>
	public BaseEntity? Source { get; set; }

	/// <summary>
	/// Amount of armor to ignore when dealing DoT damage.
	/// </summary>
	public int ArmorPiercing { get; set; } = 0;

	/// <summary>
	/// Parameterless constructor with default values.
	/// </summary>
	public AcidCondition()
	{
		Duration = "3";
		_damageDice = "1d3";
	}

	/// <summary>
	/// Parameterized constructor for creating acid with specific duration and damage.
	/// </summary>
	/// <param name="duration">Duration as dice notation (e.g., "3", "1d4+1").</param>
	/// <param name="damageDice">Damage per turn as dice notation (e.g., "1d3", "2d4").</param>
	public AcidCondition(string duration, string damageDice = "1d3")
	{
		Duration = duration;
		_damageDice = damageDice;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		return new ConditionMessage(
			$"Acid sizzles on the {target.DisplayName}!",
			Palette.ToHex(Palette.Acid)
		);
	}

	public override ConditionMessage OnTurnProcessed(BaseEntity target)
	{
		var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (healthComponent == null)
		{
			return ConditionMessage.Empty;
		}

		// Roll damage
		int damage = DiceRoller.Roll(_damageDice);
		if (damage <= 0)
		{
			return ConditionMessage.Empty;
		}

		// Apply acid damage with source for kill attribution and armor piercing
		int actualDamage = healthComponent.TakeDamage(
			damage,
			DamageType.Acid,
			Source,
			applyArmor: ArmorPiercing > 0,
			armorPiercing: ArmorPiercing
		);

		if (actualDamage > 0)
		{
			return new ConditionMessage(
				$"Acid burns the {target.DisplayName} for {actualDamage} damage!",
				Palette.ToHex(Palette.Acid)
			);
		}

		return ConditionMessage.Empty;
	}

	public override ConditionMessage OnRemoved(BaseEntity target)
	{
		return new ConditionMessage(
			$"The acid on {target.DisplayName} neutralizes.",
			Palette.ToHex(Palette.Default)
		);
	}
}
