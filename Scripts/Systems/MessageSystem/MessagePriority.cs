namespace PitsOfDespair.Systems;

/// <summary>
/// Defines the narrative order for message log entries.
/// Lower values are displayed first when messages are flushed.
/// </summary>
public enum MessagePriority
{
    /// <summary>
    /// Discovery messages (e.g., "you spotted a rat").
    /// Displayed first as context for what follows.
    /// </summary>
    Discovery = 0,

    /// <summary>
    /// Primary action damage messages (e.g., "your magic missile hits for 12 damage").
    /// The main combat action that initiates a sequence.
    /// </summary>
    ActionDamage = 10,

    /// <summary>
    /// Damage modifier messages (e.g., "vulnerable to bludgeoning").
    /// Explains why damage was modified.
    /// </summary>
    DamageModifier = 20,

    /// <summary>
    /// Status effect messages (e.g., "rat is burning", "goblin is poisoned").
    /// Ongoing effects applied during combat.
    /// </summary>
    StatusEffect = 30,

    /// <summary>
    /// Death messages (e.g., "skeleton dies!").
    /// Final result of damage - always shown after damage/modifiers.
    /// </summary>
    Death = 40,

    /// <summary>
    /// Reward messages (e.g., "you gained 50 XP").
    /// Shown after combat resolution.
    /// </summary>
    Reward = 50,

    /// <summary>
    /// Generic messages that don't fit other categories.
    /// </summary>
    Generic = 100
}
