namespace PitsOfDespair.Skills;

/// <summary>
/// Defines the categories of skills available in the game.
///
/// Note: "Prepared" skills (Power Attack, Cleave, Pinning Shot) are Active skills that use
/// the apply_prepare effect step. They remain in the Active category but don't consume a turn
/// (consumesTurn: false). The prepared attack triggers on the next successful attack.
/// </summary>
public enum SkillCategory
{
    /// <summary>
    /// Player-activated skills that cost Willpower and typically consume a turn.
    /// Includes "prepared" skills that set up the next attack (consumesTurn: false).
    /// Examples: Fireball, Heal, Power Attack (prepared), Cleave (prepared)
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
    /// On/off sustained skills that provide benefits at a cost while active.
    /// Benefits and penalties are applied via conditions when toggled on.
    /// Can optionally drain Willpower per turn. Auto-deactivates when WP reaches 0.
    /// Multiple toggles can be active simultaneously.
    /// Examples: Power Stance (+2 damage, -2 accuracy)
    /// </summary>
    Toggle,

    /// <summary>
    /// Skills that permanently enhance other learned skills.
    /// When learned, the improvement modifies the target skill's properties (range, damage, etc.).
    /// Requires the target skill to be learned first.
    /// Examples: Shoulder Charge Mastery (+2 range to Shoulder Charge)
    /// </summary>
    Improvement
}
