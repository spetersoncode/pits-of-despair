namespace PitsOfDespair.Skills;

/// <summary>
/// Defines the categories of skills available in the game.
/// </summary>
public enum SkillCategory
{
    /// <summary>
    /// Player-activated skills that cost Willpower and typically consume a turn.
    /// Examples: Power Attack, Fireball, Heal
    /// </summary>
    Active,

    /// <summary>
    /// Always-on skills that provide permanent bonuses once learned.
    /// Examples: Mighty Thews (+1 melee damage), Tough (+8 max HP)
    /// </summary>
    Passive,

    /// <summary>
    /// Skills that trigger automatically when conditions are met.
    /// May or may not cost Willpower when triggered.
    /// Examples: Riposte (on enemy miss), Die Hard (on lethal damage)
    /// </summary>
    Reactive,

    /// <summary>
    /// Persistent area effects centered on the caster affecting nearby entities.
    /// Examples: Protective Aura (allies take reduced damage)
    /// </summary>
    Aura
}
