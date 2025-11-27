namespace PitsOfDespair.Generation.Passes.Config;

/// <summary>
/// Configuration for Simple Room Placement generation pass.
/// Creates dungeons by scattering rooms and connecting them with corridors.
/// Less structured than BSP, producing more chaotic layouts.
/// </summary>
public class SimpleRoomPlacementPassConfig
{
    /// <summary>
    /// Number of room placement attempts.
    /// Not all attempts succeed (overlap rejection).
    /// </summary>
    public int RoomAttempts { get; set; } = 30;

    /// <summary>
    /// Minimum number of rooms to generate.
    /// Generation continues until this many rooms exist.
    /// </summary>
    public int MinRooms { get; set; } = 5;

    /// <summary>
    /// Maximum number of rooms to generate.
    /// Stops placing rooms once this limit is reached.
    /// </summary>
    public int MaxRooms { get; set; } = 15;

    /// <summary>
    /// Minimum room width (in tiles).
    /// </summary>
    public int MinRoomWidth { get; set; } = 5;

    /// <summary>
    /// Maximum room width (in tiles).
    /// </summary>
    public int MaxRoomWidth { get; set; } = 12;

    /// <summary>
    /// Minimum room height (in tiles).
    /// </summary>
    public int MinRoomHeight { get; set; } = 5;

    /// <summary>
    /// Maximum room height (in tiles).
    /// </summary>
    public int MaxRoomHeight { get; set; } = 12;

    /// <summary>
    /// Minimum spacing between rooms (in tiles).
    /// Higher values create more corridor space.
    /// </summary>
    public int RoomSpacing { get; set; } = 2;

    /// <summary>
    /// Width of corridors connecting rooms (in tiles).
    /// </summary>
    public int CorridorWidth { get; set; } = 1;

    /// <summary>
    /// Whether to use L-shaped corridors (true) or direct corridors (false).
    /// L-shaped creates more interesting paths.
    /// </summary>
    public bool LShaped { get; set; } = true;

    /// <summary>
    /// Chance (0-100) to create extra corridor connections.
    /// Creates loops in the dungeon layout.
    /// </summary>
    public int ExtraConnectionChance { get; set; } = 15;

    /// <summary>
    /// Create SimpleRoomPlacementPassConfig from PassConfig dictionary.
    /// </summary>
    public static SimpleRoomPlacementPassConfig FromPassConfig(PassConfig passConfig)
    {
        var config = new SimpleRoomPlacementPassConfig();

        if (passConfig?.Config != null)
        {
            config.RoomAttempts = passConfig.GetConfigValue("roomAttempts", config.RoomAttempts);
            config.MinRooms = passConfig.GetConfigValue("minRooms", config.MinRooms);
            config.MaxRooms = passConfig.GetConfigValue("maxRooms", config.MaxRooms);
            config.MinRoomWidth = passConfig.GetConfigValue("minRoomWidth", config.MinRoomWidth);
            config.MaxRoomWidth = passConfig.GetConfigValue("maxRoomWidth", config.MaxRoomWidth);
            config.MinRoomHeight = passConfig.GetConfigValue("minRoomHeight", config.MinRoomHeight);
            config.MaxRoomHeight = passConfig.GetConfigValue("maxRoomHeight", config.MaxRoomHeight);
            config.RoomSpacing = passConfig.GetConfigValue("roomSpacing", config.RoomSpacing);
            config.CorridorWidth = passConfig.GetConfigValue("corridorWidth", config.CorridorWidth);
            config.LShaped = passConfig.GetConfigValue("lShaped", config.LShaped);
            config.ExtraConnectionChance = passConfig.GetConfigValue("extraConnectionChance", config.ExtraConnectionChance);
        }

        return config;
    }

    /// <summary>
    /// Validate configuration parameters.
    /// </summary>
    public bool Validate(out string error)
    {
        if (RoomAttempts < 1)
        {
            error = "RoomAttempts must be at least 1";
            return false;
        }
        if (MinRooms < 1)
        {
            error = "MinRooms must be at least 1";
            return false;
        }
        if (MaxRooms < MinRooms)
        {
            error = "MaxRooms must be >= MinRooms";
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
        if (RoomSpacing < 0)
        {
            error = "RoomSpacing cannot be negative";
            return false;
        }
        if (CorridorWidth < 1)
        {
            error = "CorridorWidth must be at least 1";
            return false;
        }
        if (ExtraConnectionChance < 0 || ExtraConnectionChance > 100)
        {
            error = "ExtraConnectionChance must be between 0 and 100";
            return false;
        }

        error = null;
        return true;
    }
}
