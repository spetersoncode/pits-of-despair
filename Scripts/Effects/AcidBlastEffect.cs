using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect for the Acid Blast skill.
/// Uses WIL as attack bonus for an opposed roll (not auto-hit).
/// Deals armor-piercing acid damage and applies acid DoT on hit.
/// </summary>
public class AcidBlastEffect : Effect
{
    public override string Type => "acid_blast";
    public override string Name => "Acid Blast";

    private readonly string _damageDice;
    private readonly string _dotDamage;
    private readonly string _duration;
    private readonly int _armorPiercing;
    private readonly string? _attackStat;

    public AcidBlastEffect()
    {
        _damageDice = "2d6";
        _dotDamage = "1d3";
        _duration = "3";
        _armorPiercing = 3;
        _attackStat = "wil";
    }

    public AcidBlastEffect(EffectDefinition definition)
    {
        _damageDice = definition.Dice ?? "2d6";
        _dotDamage = definition.DotDamage ?? "1d3";
        _duration = definition.GetDurationString();
        _armorPiercing = definition.ArmorPiercing;
        _attackStat = definition.AttackStat ?? "wil";
    }

    public override EffectResult Apply(EffectContext context)
    {
        var target = context.Target;
        var caster = context.Caster;

        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (healthComponent == null)
        {
            return EffectResult.CreateFailure(
                $"{target.DisplayName} cannot be damaged.",
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Get caster and target stats for opposed roll
        var casterStats = caster?.GetNodeOrNull<StatsComponent>("StatsComponent");
        var targetStats = target.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate attack modifier from attack stat (usually WIL)
        int attackMod = context.GetCasterStat(_attackStat);

        // Targets without stats (decorations/objects) have -10 defense - essentially auto-hit
        int defenseMod = targetStats?.GetDefenseModifier() ?? -10;

        // Opposed roll: 2d6 + attack vs 2d6 + defense
        // Attacker wins ties (unlike saves where defender wins)
        int attackRoll = DiceRoller.Roll(2, 6, attackMod);
        int defenseRoll = DiceRoller.Roll(2, 6, defenseMod);
        bool hit = attackRoll >= defenseRoll;

        if (!hit)
        {
            // Attack missed
            context.ActionContext.CombatSystem.EmitActionMessage(
                target,
                $"Acid blast misses the {target.DisplayName}!",
                Palette.ToHex(Palette.Disabled)
            );

            return EffectResult.CreateFailure(
                string.Empty,
                Palette.ToHex(Palette.Disabled)
            );
        }

        // Roll damage
        int damage = DiceRoller.Roll(_damageDice);

        // Apply damage with armor piercing
        int actualDamage = healthComponent.TakeDamage(
            damage,
            DamageType.Acid,
            caster,
            applyArmor: true,
            armorPiercing: _armorPiercing
        );

        // Emit damage message
        if (actualDamage > 0)
        {
            context.ActionContext.CombatSystem.EmitActionMessage(
                target,
                $"Acid blasts the {target.DisplayName} for {actualDamage} damage!",
                Palette.ToHex(Palette.Acid)
            );

            // Apply acid DoT condition only if we dealt damage
            var condition = new AcidCondition(_duration, _dotDamage)
            {
                Source = caster,
                ArmorPiercing = _armorPiercing
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
        else
        {
            // Damage was fully absorbed by armor
            context.ActionContext.CombatSystem.EmitActionMessage(
                target,
                $"Acid splashes harmlessly against the {target.DisplayName}'s armor!",
                Palette.ToHex(Palette.Default)
            );
        }

        var result = EffectResult.CreateSuccess(
            string.Empty,
            Palette.ToHex(Palette.Acid),
            target
        );
        result.DamageDealt = actualDamage;
        return result;
    }
}
