using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that deals initial acid damage and applies a lingering acid condition.
/// Used by the Wand of Acid Arrow.
/// </summary>
public class AcidEffect : Effect
{
	public override string Type => "acid";
	public override string Name => "Acid";

	private readonly string _initialDamage;
	private readonly string _dotDamage;
	private readonly string _duration;
	private readonly string? _saveStat;
	private readonly string? _attackStat;
	private readonly int _saveModifier;

	public AcidEffect()
	{
		_initialDamage = "1d6";
		_dotDamage = "1d3";
		_duration = "3";
	}

	public AcidEffect(EffectDefinition definition)
	{
		_initialDamage = definition.Dice ?? "1d6";
		_dotDamage = definition.DotDamage ?? "1d3";
		_duration = definition.GetDurationString();
		_saveStat = definition.SaveStat;
		_attackStat = definition.AttackStat;
		_saveModifier = definition.SaveModifier;
	}

	public override EffectResult Apply(EffectContext context)
	{
		var target = context.Target;
		var caster = context.Caster;

		var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (healthComponent == null)
		{
			return EffectResult.CreateFailure(string.Empty, Palette.ToHex(Palette.Default));
		}

		// Check saving throw for reduced effect (half damage, no DoT)
		bool resisted = SavingThrow.TryResist(context, _saveStat, _attackStat, _saveModifier);

		// Roll and apply initial damage
		int initialDamage = DiceRoller.Roll(_initialDamage);
		if (resisted)
		{
			initialDamage /= 2; // Half damage on save
		}

		int actualDamage = healthComponent.TakeDamage(initialDamage, DamageType.Acid, caster);

		// Emit damage message
		if (actualDamage > 0)
		{
			context.ActionContext.CombatSystem.EmitActionMessage(
				target,
				$"Acid splashes the {target.DisplayName} for {actualDamage} damage!",
				Palette.ToHex(Palette.Acid)
			);
		}

		// Apply lingering acid condition if save failed
		if (!resisted)
		{
			var condition = new AcidCondition(_duration, _dotDamage)
			{
				Source = caster
			};

			var conditionMessage = condition.OnApplied(target);
			target.AddConditionWithoutMessage(condition);

			if (!string.IsNullOrEmpty(conditionMessage.Message))
			{
				context.ActionContext.CombatSystem.EmitActionMessage(
					target,
					conditionMessage.Message,
					conditionMessage.Color
				);
			}
		}

		return EffectResult.CreateSuccess(
			string.Empty,
			Palette.ToHex(Palette.Acid),
			target
		);
	}
}
