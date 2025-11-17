using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Attack type classification for combat mechanics.
/// </summary>
public enum AttackType
{
	/// <summary>
	/// Melee attack - uses STR for attack and damage rolls.
	/// </summary>
	Melee,

	/// <summary>
	/// Ranged attack - uses AGI for attack rolls, no stat bonus to damage.
	/// </summary>
	Ranged
}

/// <summary>
/// Serializable attack data structure.
/// Embedded within creature definitions.
/// </summary>
public partial class AttackData : Resource
{
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Type of attack (Melee or Ranged).
	/// Determines which stat is used for attack rolls and damage bonuses.
	/// </summary>
	public AttackType Type { get; set; } = AttackType.Melee;

	public int MinDamage { get; set; } = 1;

	public int MaxDamage { get; set; } = 1;

	public int Range { get; set; } = 1;
}
