namespace PitsOfDespair.Generation.Passes.Config;

/// <summary>
/// Configuration for BSP (Binary Space Partitioning) generation pass.
/// Extracted from PassConfig dictionary for type-safe access.
/// </summary>
public class BSPPassConfig
{
    /// <summary>
    /// Minimum size a partition can be before it stops splitting (in tiles).
    /// Smaller values create more, smaller rooms.
    /// </summary>
    public int MinPartitionSize { get; set; } = 8;

    /// <summary>
    /// Maximum size a partition should be before it gets split (in tiles).
    /// Partitions larger than this will continue splitting.
    /// </summary>
    public int MaxPartitionSize { get; set; } = 14;

    /// <summary>
    /// Minimum width of a room within a partition (in tiles).
    /// </summary>
    public int MinRoomWidth { get; set; } = 6;

    /// <summary>
    /// Maximum width of a room within a partition (in tiles).
    /// </summary>
    public int MaxRoomWidth { get; set; } = 12;

    /// <summary>
    /// Minimum height of a room within a partition (in tiles).
    /// </summary>
    public int MinRoomHeight { get; set; } = 6;

    /// <summary>
    /// Maximum height of a room within a partition (in tiles).
    /// </summary>
    public int MaxRoomHeight { get; set; } = 12;

    /// <summary>
    /// Width of corridors connecting rooms (in tiles).
    /// </summary>
    public int CorridorWidth { get; set; } = 1;

    /// <summary>
    /// Maximum recursion depth for BSP splits (safety limit).
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Create BSPPassConfig from PassConfig dictionary.
    /// </summary>
    public static BSPPassConfig FromPassConfig(PassConfig passConfig)
    {
        var config = new BSPPassConfig();

        if (passConfig?.Config != null)
        {
            config.MinPartitionSize = passConfig.GetConfigValue("minPartitionSize", config.MinPartitionSize);
            config.MaxPartitionSize = passConfig.GetConfigValue("maxPartitionSize", config.MaxPartitionSize);
            config.MinRoomWidth = passConfig.GetConfigValue("minRoomWidth", config.MinRoomWidth);
            config.MaxRoomWidth = passConfig.GetConfigValue("maxRoomWidth", config.MaxRoomWidth);
            config.MinRoomHeight = passConfig.GetConfigValue("minRoomHeight", config.MinRoomHeight);
            config.MaxRoomHeight = passConfig.GetConfigValue("maxRoomHeight", config.MaxRoomHeight);
            config.CorridorWidth = passConfig.GetConfigValue("corridorWidth", config.CorridorWidth);
            config.MaxDepth = passConfig.GetConfigValue("maxDepth", config.MaxDepth);
        }

        return config;
    }

    /// <summary>
    /// Validate configuration parameters.
    /// </summary>
    public bool Validate(out string error)
    {
        if (MinPartitionSize < 4)
        {
            error = "MinPartitionSize must be at least 4";
            return false;
        }
        if (MaxPartitionSize < MinPartitionSize)
        {
            error = "MaxPartitionSize must be >= MinPartitionSize";
            return false;
        }
        if (MinRoomWidth < 3 || MinRoomHeight < 3)
        {
            error = "MinRoomWidth and MinRoomHeight must be at least 3";
            return false;
        }
        if (MaxRoomWidth < MinRoomWidth || MaxRoomHeight < MinRoomHeight)
        {
            error = "MaxRoom dimensions must be >= MinRoom dimensions";
            return false;
        }
        if (CorridorWidth < 1)
        {
            error = "CorridorWidth must be at least 1";
            return false;
        }
        if (MaxDepth < 1)
        {
            error = "MaxDepth must be at least 1";
            return false;
        }

        error = null;
        return true;
    }
}
