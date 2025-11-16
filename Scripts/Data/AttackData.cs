using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Serializable attack data structure.
/// Embedded within creature definitions.
/// </summary>
public partial class AttackData : Resource
{
    public string Name { get; set; } = string.Empty;

    public int MinDamage { get; set; } = 1;

    public int MaxDamage { get; set; } = 1;

    public int Range { get; set; } = 1;
}
