using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects;

/// <summary>
/// Effect that heals the target entity.
/// </summary>
public class HealEffect : Effect
{
    /// <summary>
    /// The amount of hit points to restore.
    /// </summary>
    public int Amount { get; set; }

    public override string Name => "Heal";

    public HealEffect()
    {
        Amount = 0;
    }

    public HealEffect(int amount)
    {
        Amount = amount;
    }

    public override EffectResult Apply(BaseEntity target, ActionContext context)
    {
        var healthComponent = target.GetNodeOrNull<HealthComponent>("HealthComponent");

        if (healthComponent == null)
        {
            return new EffectResult(
                false,
                $"{target.DisplayName} cannot be healed.",
                "#888888"
            );
        }

        // Check if already at full health
        if (healthComponent.CurrentHP >= healthComponent.MaxHP)
        {
            return new EffectResult(
                false,
                "You are already at full health.",
                "#888888"
            );
        }

        // Calculate actual healing (don't overheal)
        int oldHealth = healthComponent.CurrentHP;
        healthComponent.Heal(Amount);
        int actualHealing = healthComponent.CurrentHP - oldHealth;

        return new EffectResult(
            true,
            $"You heal {actualHealing} HP.",
            "#66ff66"  // Green for healing
        );
    }
}
