using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes.Config;

/// <summary>
/// Configuration for Drunkard's Walk generation pass.
/// Creates winding tunnel systems via random walk algorithm.
/// </summary>
public class DrunkardWalkPassConfig
{
    /// <summary>
    /// Target percentage of floor tiles (0-100).
    /// Walker continues until this percentage is reached.
    /// </summary>
    public int TargetFloorPercent { get; set; } = 40;

    /// <summary>
    /// Number of walkers to spawn.
    /// Multiple walkers create more branching tunnels.
    /// </summary>
    public int WalkerCount { get; set; } = 1;

    /// <summary>
    /// Maximum steps per walker before it stops.
    /// Prevents infinite loops if target can't be reached.
    /// </summary>
    public int MaxStepsPerWalker { get; set; } = 10000;

    /// <summary>
    /// Width of tunnels carved by walkers (in tiles).
    /// 1 = single-tile tunnels, 2+ = wider passages.
    /// </summary>
    public int TunnelWidth { get; set; } = 1;

    /// <summary>
    /// Chance (0-100) that walker turns when moving.
    /// Higher = more winding, lower = straighter tunnels.
    /// </summary>
    public int TurnChance { get; set; } = 50;

    /// <summary>
    /// Whether walkers start from center or random positions.
    /// </summary>
    public bool StartFromCenter { get; set; } = true;

    /// <summary>
    /// Chance (0-100) to create a room at intervals.
    /// Creates occasional open areas along tunnels.
    /// </summary>
    public int RoomChance { get; set; } = 5;

    /// <summary>
    /// Minimum room size when rooms are generated.
    /// </summary>
    public int MinRoomSize { get; set; } = 4;

    /// <summary>
    /// Maximum room size when rooms are generated.
    /// </summary>
    public int MaxRoomSize { get; set; } = 8;

    /// <summary>
    /// Role of this pass - can be Base (generate from scratch)
    /// or Modifier (carve tunnels through existing terrain).
    /// </summary>
    public PassRole Role { get; set; } = PassRole.Base;

    /// <summary>
    /// Create DrunkardWalkPassConfig from PassConfig dictionary.
    /// </summary>
    public static DrunkardWalkPassConfig FromPassConfig(PassConfig passConfig)
    {
        var config = new DrunkardWalkPassConfig();

        if (passConfig?.Config != null)
        {
            config.TargetFloorPercent = passConfig.GetConfigValue("targetFloorPercent", config.TargetFloorPercent);
            config.WalkerCount = passConfig.GetConfigValue("walkerCount", config.WalkerCount);
            config.MaxStepsPerWalker = passConfig.GetConfigValue("maxStepsPerWalker", config.MaxStepsPerWalker);
            config.TunnelWidth = passConfig.GetConfigValue("tunnelWidth", config.TunnelWidth);
            config.TurnChance = passConfig.GetConfigValue("turnChance", config.TurnChance);
            config.StartFromCenter = passConfig.GetConfigValue("startFromCenter", config.StartFromCenter);
            config.RoomChance = passConfig.GetConfigValue("roomChance", config.RoomChance);
            config.MinRoomSize = passConfig.GetConfigValue("minRoomSize", config.MinRoomSize);
            config.MaxRoomSize = passConfig.GetConfigValue("maxRoomSize", config.MaxRoomSize);

            // Parse role
            var roleStr = passConfig.GetConfigValue("role", "base");
            config.Role = roleStr.ToLowerInvariant() switch
            {
                "modifier" => PassRole.Modifier,
                _ => PassRole.Base
            };
        }

        return config;
    }

    /// <summary>
    /// Validate configuration parameters.
    /// </summary>
    public bool Validate(out string error)
    {
        if (TargetFloorPercent < 10 || TargetFloorPercent > 90)
        {
            error = "TargetFloorPercent must be between 10 and 90";
            return false;
        }
        if (WalkerCount < 1)
        {
            error = "WalkerCount must be at least 1";
            return false;
        }
        if (MaxStepsPerWalker < 100)
        {
            error = "MaxStepsPerWalker must be at least 100";
            return false;
        }
        if (TunnelWidth < 1 || TunnelWidth > 5)
        {
            error = "TunnelWidth must be between 1 and 5";
            return false;
        }
        if (TurnChance < 0 || TurnChance > 100)
        {
            error = "TurnChance must be between 0 and 100";
            return false;
        }
        if (RoomChance < 0 || RoomChance > 100)
        {
            error = "RoomChance must be between 0 and 100";
            return false;
        }
        if (MinRoomSize < 3)
        {
            error = "MinRoomSize must be at least 3";
            return false;
        }
        if (MaxRoomSize < MinRoomSize)
        {
            error = "MaxRoomSize must be >= MinRoomSize";
            return false;
        }

        error = null;
        return true;
    }
}
