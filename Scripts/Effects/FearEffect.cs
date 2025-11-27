using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that applies the fear condition to a target, causing them to flee from the caster.
/// </summary>
public class FearEffect : Effect
{
	public override string Type => "fear";
	public override string Name => "Fear";

	private readonly string _duration;
	private readonly string? _saveStat;
	private readonly string? _attackStat;
	private readonly int _saveModifier;

	public FearEffect()
	{
		_duration = "4";
	}

	public FearEffect(EffectDefinition definition)
	{
		_duration = definition.GetDurationString();
		_saveStat = definition.SaveStat;
		_attackStat = definition.AttackStat;
		_saveModifier = definition.SaveModifier;
	}

	public override EffectResult Apply(EffectContext context)
	{
		var target = context.Target;
		var caster = context.Caster;

		// Check saving throw if configured
		if (SavingThrow.TryResist(context, _saveStat, _attackStat, _saveModifier))
		{
			return EffectResult.CreateFailure(string.Empty, Palette.ToHex(Palette.Default));
		}

		// Create fear condition with caster as the fear source
		var condition = new FearCondition(_duration)
		{
			FearSource = caster
		};

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
