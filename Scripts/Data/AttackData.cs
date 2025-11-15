using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Resource defining an attack's properties
/// </summary>
[GlobalClass]
public partial class AttackData : Resource
{
    /// <summary>
    /// Display name of the attack (e.g., "Bite", "Claw", "Sword Slash")
    /// </summary>
    [Export] public string AttackName { get; set; } = "Attack";

    /// <summary>
    /// Minimum damage this attack can deal
    /// </summary>
    [Export] public int MinDamage { get; set; } = 1;

    /// <summary>
    /// Maximum damage this attack can deal
    /// </summary>
    [Export] public int MaxDamage { get; set; } = 3;

    /// <summary>
    /// Range of the attack in tiles (1 = melee/adjacent, 2+ = ranged)
    /// </summary>
    [Export] public int Range { get; set; } = 1;
}
