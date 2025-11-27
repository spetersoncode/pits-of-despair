namespace PitsOfDespair.Generation.Config;

/// <summary>
/// Configuration for Extra Connections modifier pass.
/// Adds additional corridors between nearby rooms to create loops.
/// Works as a modifier after BSP or SimpleRoomPlacement base generators.
/// </summary>
public class ExtraConnectionsPassConfig
{
    /// <summary>
    /// Chance (0-100) for each room pair to get an extra connection.
    /// Higher values create more interconnected dungeons.
    /// </summary>
    public int ConnectionChance { get; set; } = 20;

    /// <summary>
    /// Maximum distance (Manhattan) between room centers to consider connecting.
    /// Rooms further apart won't get extra connections.
    /// </summary>
    public int MaxDistance { get; set; } = 25;

    /// <summary>
    /// Width of corridors (in tiles).
    /// </summary>
    public int CorridorWidth { get; set; } = 1;

    /// <summary>
    /// Whether to use L-shaped corridors (true) or direct/diagonal corridors (false).
    /// </summary>
    public bool LShaped { get; set; } = true;

    /// <summary>
    /// Create ExtraConnectionsPassConfig from PassConfig dictionary.
    /// </summary>
    public static ExtraConnectionsPassConfig FromPassConfig(PassConfig passConfig)
    {
        var config = new ExtraConnectionsPassConfig();

        if (passConfig?.Config != null)
        {
            config.ConnectionChance = passConfig.GetConfigValue("connectionChance", config.ConnectionChance);
            config.MaxDistance = passConfig.GetConfigValue("maxDistance", config.MaxDistance);
            config.CorridorWidth = passConfig.GetConfigValue("corridorWidth", config.CorridorWidth);
            config.LShaped = passConfig.GetConfigValue("lShaped", config.LShaped);
        }

        return config;
    }

    /// <summary>
    /// Validate configuration parameters.
    /// </summary>
    public bool Validate(out string error)
    {
        if (ConnectionChance < 0 || ConnectionChance > 100)
        {
            error = "ConnectionChance must be between 0 and 100";
            return false;
        }
        if (MaxDistance < 1)
        {
            error = "MaxDistance must be at least 1";
            return false;
        }
        if (CorridorWidth < 1)
        {
            error = "CorridorWidth must be at least 1";
            return false;
        }

        error = null;
        return true;
    }
}
