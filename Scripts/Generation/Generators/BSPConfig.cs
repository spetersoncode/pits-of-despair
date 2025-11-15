using Godot;

namespace PitsOfDespair.Generation.Generators;

/// <summary>
/// Configuration for BSP (Binary Space Partitioning) dungeon generation.
/// Controls how the BSP algorithm splits space and creates rooms.
/// </summary>
[GlobalClass]
public partial class BSPConfig : Resource
{
    /// <summary>
    /// Random seed for procedural generation. Use -1 for random seed.
    /// </summary>
    [Export]
    public int Seed { get; set; } = -1;

    /// <summary>
    /// Minimum size a partition can be before it stops splitting (in tiles).
    /// Smaller values create more, smaller rooms.
    /// </summary>
    [Export]
    public int MinPartitionSize { get; set; } = 8;

    /// <summary>
    /// Maximum size a partition should be before it gets split (in tiles).
    /// Partitions larger than this will continue splitting.
    /// </summary>
    [Export]
    public int MaxPartitionSize { get; set; } = 14;

    /// <summary>
    /// Minimum width of a room within a partition (in tiles).
    /// </summary>
    [Export]
    public int MinRoomWidth { get; set; } = 6;

    /// <summary>
    /// Maximum width of a room within a partition (in tiles).
    /// </summary>
    [Export]
    public int MaxRoomWidth { get; set; } = 12;

    /// <summary>
    /// Minimum height of a room within a partition (in tiles).
    /// </summary>
    [Export]
    public int MinRoomHeight { get; set; } = 6;

    /// <summary>
    /// Maximum height of a room within a partition (in tiles).
    /// </summary>
    [Export]
    public int MaxRoomHeight { get; set; } = 12;

    /// <summary>
    /// Width of corridors connecting rooms (in tiles).
    /// </summary>
    [Export]
    public int CorridorWidth { get; set; } = 1;

    /// <summary>
    /// Gets the actual seed to use for generation.
    /// If Seed is -1, generates a random seed based on current time.
    /// </summary>
    public int GetActualSeed()
    {
        if (Seed == -1)
        {
            return (int)Time.GetTicksMsec();
        }
        return Seed;
    }
}
