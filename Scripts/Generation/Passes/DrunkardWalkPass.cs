using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Passes.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Drunkard's Walk generation pass.
/// Creates winding tunnel systems by simulating random walkers that carve
/// through the grid. Produces organic, meandering passages.
///
/// Role: Base generator (carves tunnels from solid) or Modifier (adds tunnels)
/// </summary>
public class DrunkardWalkPass : IGenerationPass
{
    /// <summary>
    /// Key for storing walker paths in PassData for use by other passes.
    /// </summary>
    public const string PassDataKeyWalkerPaths = "DrunkardWalkerPaths";

    /// <summary>
    /// Key for storing generated rooms in PassData.
    /// </summary>
    public const string PassDataKeyRooms = "DrunkardRooms";

    private readonly PassConfig _passConfig;
    private readonly DrunkardWalkPassConfig _dwConfig;

    // Direction vectors for 4-directional movement
    private static readonly (int dx, int dy)[] Directions = { (0, -1), (1, 0), (0, 1), (-1, 0) };

    public string Name => "DrunkardWalk";
    public int Priority { get; }
    public PassRole Role { get; }

    public DrunkardWalkPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? throw new ArgumentNullException(nameof(passConfig));
        _dwConfig = DrunkardWalkPassConfig.FromPassConfig(passConfig);
        Priority = passConfig.Priority;
        Role = _dwConfig.Role;

        if (!_dwConfig.Validate(out var error))
        {
            GD.PushWarning($"[DrunkardWalkPass] Config validation warning: {error}");
        }
    }

    public bool CanExecute(GenerationContext context)
    {
        // Need minimum grid size for reasonable generation
        if (context.Width < 10 || context.Height < 10)
        {
            GD.PushWarning($"[DrunkardWalkPass] Grid too small ({context.Width}x{context.Height}), need at least 10x10");
            return false;
        }
        return true;
    }

    public void Execute(GenerationContext context)
    {
        GD.Print($"[DrunkardWalkPass] Starting with {_dwConfig.WalkerCount} walker(s), target {_dwConfig.TargetFloorPercent}%");

        var walkerPaths = new List<List<GridPosition>>();
        var rooms = new List<DrunkardRoom>();

        // Calculate target floor count
        int totalTiles = (context.Width - 2) * (context.Height - 2); // Exclude borders
        int targetFloors = (int)(totalTiles * _dwConfig.TargetFloorPercent / 100.0);

        // Track current floor count
        int currentFloors = CountFloorTiles(context);
        int totalSteps = 0;

        // Spawn walkers
        var walkers = new List<Walker>();
        for (int i = 0; i < _dwConfig.WalkerCount; i++)
        {
            var startPos = GetStartPosition(context, i);
            var walker = new Walker(startPos.X, startPos.Y, context.Random.Next(4));
            walkers.Add(walker);
            walkerPaths.Add(new List<GridPosition> { new GridPosition(startPos.X, startPos.Y) });
        }

        // Run walkers until target reached or max steps
        int maxTotalSteps = _dwConfig.MaxStepsPerWalker * _dwConfig.WalkerCount;
        while (currentFloors < targetFloors && totalSteps < maxTotalSteps)
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                var path = walkerPaths[i];

                // Take a step
                int newFloors = TakeStep(context, walker, rooms);
                currentFloors += newFloors;
                totalSteps++;

                path.Add(new GridPosition(walker.X, walker.Y));

                if (currentFloors >= targetFloors)
                    break;
            }
        }

        // Ensure border walls
        EnsureBorderWalls(context);

        // Store pass data
        context.SetPassData(PassDataKeyWalkerPaths, walkerPaths);
        context.SetPassData(PassDataKeyRooms, rooms);

        // Register rooms as regions
        RegisterRoomsAsRegions(rooms, context);

        GD.Print($"[DrunkardWalkPass] Complete: {currentFloors} floor tiles ({100.0 * currentFloors / totalTiles:F1}%), {rooms.Count} rooms, {totalSteps} steps");
    }

    private (int X, int Y) GetStartPosition(GenerationContext context, int walkerIndex)
    {
        if (_dwConfig.StartFromCenter || walkerIndex == 0)
        {
            return (context.Width / 2, context.Height / 2);
        }

        // Random position within bounds
        int x = context.Random.Next(2, context.Width - 2);
        int y = context.Random.Next(2, context.Height - 2);
        return (x, y);
    }

    private int TakeStep(GenerationContext context, Walker walker, List<DrunkardRoom> rooms)
    {
        int floorsCarved = 0;

        // Maybe turn
        if (context.Random.Next(100) < _dwConfig.TurnChance)
        {
            // Turn left or right
            walker.Direction = (walker.Direction + (context.Random.Next(2) == 0 ? 1 : 3)) % 4;
        }

        // Get direction
        var (dx, dy) = Directions[walker.Direction];

        // Calculate new position
        int newX = walker.X + dx;
        int newY = walker.Y + dy;

        // Check bounds (stay within 1-tile border)
        if (newX < 1 || newX >= context.Width - 1 ||
            newY < 1 || newY >= context.Height - 1)
        {
            // Hit boundary - turn around
            walker.Direction = (walker.Direction + 2) % 4;
            return 0;
        }

        // Move walker
        walker.X = newX;
        walker.Y = newY;

        // Carve tunnel
        floorsCarved += CarveTunnel(context, walker.X, walker.Y);

        // Maybe create room
        if (context.Random.Next(100) < _dwConfig.RoomChance)
        {
            floorsCarved += CreateRoom(context, walker.X, walker.Y, rooms);
        }

        return floorsCarved;
    }

    private int CarveTunnel(GenerationContext context, int centerX, int centerY)
    {
        int carved = 0;
        int radius = _dwConfig.TunnelWidth / 2;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                if (x >= 1 && x < context.Width - 1 &&
                    y >= 1 && y < context.Height - 1)
                {
                    if (context.GetTile(x, y) != TileType.Floor)
                    {
                        context.SetTile(x, y, TileType.Floor);
                        carved++;
                    }
                }
            }
        }

        return carved;
    }

    private int CreateRoom(GenerationContext context, int centerX, int centerY, List<DrunkardRoom> rooms)
    {
        int carved = 0;

        // Random room size
        int width = context.Random.Next(_dwConfig.MinRoomSize, _dwConfig.MaxRoomSize + 1);
        int height = context.Random.Next(_dwConfig.MinRoomSize, _dwConfig.MaxRoomSize + 1);

        // Calculate room bounds
        int startX = centerX - width / 2;
        int startY = centerY - height / 2;

        // Clamp to valid bounds
        startX = Math.Max(1, Math.Min(startX, context.Width - width - 1));
        startY = Math.Max(1, Math.Min(startY, context.Height - height - 1));

        int endX = Math.Min(startX + width, context.Width - 1);
        int endY = Math.Min(startY + height, context.Height - 1);

        // Carve room
        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                if (context.GetTile(x, y) != TileType.Floor)
                {
                    context.SetTile(x, y, TileType.Floor);
                    carved++;
                }
            }
        }

        // Record room
        if (carved > 0)
        {
            rooms.Add(new DrunkardRoom(startX, startY, endX - startX, endY - startY, rooms.Count));
        }

        return carved;
    }

    private int CountFloorTiles(GenerationContext context)
    {
        int count = 0;
        for (int x = 1; x < context.Width - 1; x++)
        {
            for (int y = 1; y < context.Height - 1; y++)
            {
                if (context.GetTile(x, y) == TileType.Floor)
                    count++;
            }
        }
        return count;
    }

    private void EnsureBorderWalls(GenerationContext context)
    {
        for (int x = 0; x < context.Width; x++)
        {
            context.SetTile(x, 0, TileType.Wall);
            context.SetTile(x, context.Height - 1, TileType.Wall);
        }
        for (int y = 0; y < context.Height; y++)
        {
            context.SetTile(0, y, TileType.Wall);
            context.SetTile(context.Width - 1, y, TileType.Wall);
        }
    }

    private void RegisterRoomsAsRegions(List<DrunkardRoom> rooms, GenerationContext context)
    {
        foreach (var room in rooms)
        {
            var tiles = new List<GridPosition>();
            var edgeTiles = new List<GridPosition>();

            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    // Only include actual floor tiles
                    if (context.GetTile(x, y) == TileType.Floor)
                    {
                        var pos = new GridPosition(x, y);
                        tiles.Add(pos);

                        bool isEdge = x == room.X || x == room.X + room.Width - 1 ||
                                      y == room.Y || y == room.Y + room.Height - 1;
                        if (isEdge)
                            edgeTiles.Add(pos);
                    }
                }
            }

            if (tiles.Count == 0)
                continue;

            var centroid = new GridPosition(
                room.X + room.Width / 2,
                room.Y + room.Height / 2);

            var region = new Region
            {
                Id = room.Id,
                Tiles = tiles,
                EdgeTiles = edgeTiles,
                BoundingBox = new Rect2I(room.X, room.Y, room.Width, room.Height),
                Centroid = centroid,
                Source = RegionSource.Cave,
                Tag = "tunnel_room"
            };

            context.Metadata.Regions.Add(region);
        }
    }

    /// <summary>
    /// Walker state for random walk algorithm.
    /// </summary>
    private class Walker
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Direction { get; set; }

        public Walker(int x, int y, int direction)
        {
            X = x;
            Y = y;
            Direction = direction;
        }
    }

    /// <summary>
    /// Room created by drunkard's walk during generation.
    /// </summary>
    public class DrunkardRoom
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int Id { get; }

        public DrunkardRoom(int x, int y, int width, int height, int id)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Id = id;
        }
    }
}
