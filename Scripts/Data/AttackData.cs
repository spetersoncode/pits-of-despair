using Godot;
using YamlDotNet.Serialization;

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

	/// <summary>
	/// Dice notation for weapon damage (e.g., "1d6", "2d4+1").
	/// </summary>
	[YamlMember(Alias = "dice")]
	public string DiceNotation { get; set; } = "1d4";

	/// <summary>
	/// Type of damage dealt by this attack (Bludgeoning, Slashing, or Piercing).
	/// Defaults to Bludgeoning for backwards compatibility.
	/// </summary>
	public DamageType DamageType { get; set; } = DamageType.Bludgeoning;

	public int Range { get; set; } = 1;

	/// <summary>
	/// Type of ammo required for this attack (e.g., "arrow", "bolt").
	/// If set, the attack requires ammunition to be consumed.
	/// Only applicable for ranged attacks.
	/// </summary>
	[YamlMember(Alias = "ammoType")]
	public string? AmmoType { get; set; } = null;
}
