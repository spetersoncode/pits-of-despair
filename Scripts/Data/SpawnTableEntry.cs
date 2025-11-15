using Godot;

namespace PitsOfDespair.Data;

/// <summary>
/// Single entry in a spawn table defining what can spawn and with what probability.
/// </summary>
[GlobalClass]
public partial class SpawnTableEntry : Resource
{
    /// <summary>
    /// The entity data to spawn (creature, item, furniture, etc.).
    /// </summary>
    [Export]
    public EntityData EntityData { get; set; }

    /// <summary>
    /// Relative spawn probability weight. Higher = more likely.
    /// Example: Weight 2 is twice as likely as Weight 1.
    /// </summary>
    [Export]
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Minimum number to spawn per location (room, area, etc.).
    /// </summary>
    [Export]
    public int MinCount { get; set; } = 1;

    /// <summary>
    /// Maximum number to spawn per location (room, area, etc.).
    /// </summary>
    [Export]
    public int MaxCount { get; set; } = 3;
}
