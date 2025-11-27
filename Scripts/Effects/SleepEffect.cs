using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that applies the sleep condition to a target.
/// Target skips turns until the duration expires or they take damage.
/// </summary>
public class SleepEffect : Effect
{
	public override string Type => "sleep";
	public override string Name => "Sleep";

	private readonly string _duration;
	private readonly string? _saveStat;
	private readonly string? _attackStat;
	private readonly int _saveModifier;

	public SleepEffect()
	{
		_duration = "4";
	}

	public SleepEffect(EffectDefinition definition)
	{
		_duration = definition.GetDurationString();
		_saveStat = definition.SaveStat;
		_attackStat = definition.AttackStat;
		_saveModifier = definition.SaveModifier;
	}

	public override EffectResult Apply(EffectContext context)
	{
		var target = context.Target;

		// Check saving throw if configured
		if (SavingThrow.TryResist(context, _saveStat, _attackStat, _saveModifier))
		{
			return EffectResult.CreateFailure(string.Empty, Palette.ToHex(Palette.Default));
		}

		// Create sleep condition
		var condition = new SleepCondition(_duration);

		// Apply condition
		var conditionMessage = condition.OnApplied(target);
		target.AddConditionWithoutMessage(condition);

		// Emit message via CombatSystem
		if (!string.IsNullOrEmpty(conditionMessage.Message))
		{
			context.ActionContext.CombatSystem.EmitActionMessage(
				target,
				conditionMessage.Message,
				conditionMessage.Color
			);
		}

		return EffectResult.CreateSuccess(
			string.Empty,
			Palette.ToHex(Palette.StatusDebuff),
			target
		);
	}
}
