using PitsOfDespair.Data;

namespace PitsOfDespair.Effects;

/// <summary>
/// Magic missile damage effect. Thin wrapper around DamageEffect for sound hookup.
/// TODO: Refactor so damage effects can specify sound without custom effect types.
/// </summary>
public class MagicMissileEffect : DamageEffect
{
    public override string Type => "magic_missile";
    public override string Name => "Magic Missile";

    public MagicMissileEffect() { }

    public MagicMissileEffect(EffectDefinition definition) : base(definition) { }

    public override EffectResult Apply(EffectContext context)
    {
        PlayEffectSound();
        return base.Apply(context);
    }
}
