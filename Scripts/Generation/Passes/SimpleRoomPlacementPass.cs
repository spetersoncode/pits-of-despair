using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Simple Room Placement generation pass.
/// Creates dungeons by randomly placing non-overlapping rooms,
/// then connecting them with corridors using MST (minimum spanning tree).
///
/// Produces more chaotic, less structured layouts than BSP.
/// Role: Base generator
/// </summary>
public class SimpleRoomPlacementPass : IGenerationPass
{
    /// <summary>
    /// Key for storing placed rooms in PassData.
    /// </summary>
    public const string PassDataKeyRooms = "SimpleRooms";

    private readonly PassConfig _passConfig;
    private readonly SimpleRoomPlacementPassConfig _srpConfig;

    public string Name => "SimpleRoomPlacement";
    public int Priority { get; }
    public PassRole Role => PassRole.Base;

    public SimpleRoomPlacementPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? throw new ArgumentNullException(nameof(passConfig));
        _srpConfig = SimpleRoomPlacementPassConfig.FromPassConfig(passConfig);
        Priority = passConfig.Priority;

        if (!_srpConfig.Validate(out var error))
        {
            GD.PushWarning($"[SimpleRoomPlacementPass] Config validation warning: {error}");
        }
    }

    public bool CanExecute(GenerationContext context)
    {
        // Need minimum grid size for reasonable room placement
        int minGridSize = _srpConfig.MaxRoomWidth + _srpConfig.RoomSpacing * 2 + 2;
        if (context.Width < minGridSize || context.Height < minGridSize)
        {
            GD.PushWarning($"[SimpleRoomPlacementPass] Grid too small ({context.Width}x{context.Height}), " +
                          $"need at least {minGridSize}x{minGridSize} for configured room sizes");
            return false;
        }
        return true;
    }

    public void Execute(GenerationContext context)
    {
        GD.Print($"[SimpleRoomPlacementPass] Placing rooms (attempts: {_srpConfig.RoomAttempts}, target: {_srpConfig.MinRooms}-{_srpConfig.MaxRooms})");

        var rooms = new List<PlacedRoom>();

        // Phase 1: Place rooms
        PlaceRooms(context, rooms);

        if (rooms.Count < 2)
        {
            GD.PushWarning($"[SimpleRoomPlacementPass] Only {rooms.Count} room(s) placed, need at least 2 for connections");
            if (rooms.Count == 0)
                return;
        }

        // Phase 2: Connect rooms using MST
        if (rooms.Count >= 2)
        {
            ConnectRoomsMST(context, rooms);

            // Phase 3: Add extra connections for loops
            AddExtraConnections(context, rooms);
        }

        // Ensure border walls
        EnsureBorderWalls(context);

        // Store pass data
        context.SetPassData(PassDataKeyRooms, rooms);

        // Register rooms as regions
        RegisterRoomsAsRegions(rooms, context);

        GD.Print($"[SimpleRoomPlacementPass] Complete: {rooms.Count} rooms placed");
    }

    private void PlaceRooms(GenerationContext context, List<PlacedRoom> rooms)
    {
        int attempts = 0;
        int maxAttempts = _srpConfig.RoomAttempts;

        while (attempts < maxAttempts && rooms.Count < _srpConfig.MaxRooms)
        {
            attempts++;

            // Generate random room dimensions
            int width = context.Random.Next(_srpConfig.MinRoomWidth, _srpConfig.MaxRoomWidth + 1);
            int height = context.Random.Next(_srpConfig.MinRoomHeight, _srpConfig.MaxRoomHeight + 1);

            // Generate random position (accounting for borders)
            int maxX = context.Width - width - 1;
            int maxY = context.Height - height - 1;

            if (maxX < 1 || maxY < 1)
                continue;

            int x = context.Random.Next(1, maxX + 1);
            int y = context.Random.Next(1, maxY + 1);

            var newRoom = new PlacedRoom(x, y, width, height, rooms.Count);

            // Check for overlaps with existing rooms
            if (OverlapsExisting(newRoom, rooms))
                continue;

            // Place the room
            CarveRoom(context, newRoom);
            rooms.Add(newRoom);

            // Reset attempts on success to ensure we keep trying
            if (rooms.Count < _srpConfig.MinRooms)
                attempts = 0;
        }
    }

    private bool OverlapsExisting(PlacedRoom newRoom, List<PlacedRoom> existing)
    {
        int spacing = _srpConfig.RoomSpacing;

        foreach (var room in existing)
        {
            // Check if rooms overlap (with spacing buffer)
            bool overlapsX = newRoom.X < room.X + room.Width + spacing &&
                            newRoom.X + newRoom.Width + spacing > room.X;
            bool overlapsY = newRoom.Y < room.Y + room.Height + spacing &&
                            newRoom.Y + newRoom.Height + spacing > room.Y;

            if (overlapsX && overlapsY)
                return true;
        }

        return false;
    }

    private void CarveRoom(GenerationContext context, PlacedRoom room)
    {
        for (int x = room.X; x < room.X + room.Width; x++)
        {
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                context.SetTile(x, y, TileType.Floor);
            }
        }
    }

    private void ConnectRoomsMST(GenerationContext context, List<PlacedRoom> rooms)
    {
        // Build MST using Prim's algorithm
        var connected = new HashSet<int> { 0 }; // Start with first room
        var edges = new List<(int from, int to, int dist)>();

        while (connected.Count < rooms.Count)
        {
            // Find shortest edge from connected set to unconnected room
            int bestFrom = -1;
            int bestTo = -1;
            int bestDist = int.MaxValue;

            foreach (int fromIdx in connected)
            {
                for (int toIdx = 0; toIdx < rooms.Count; toIdx++)
                {
                    if (connected.Contains(toIdx))
                        continue;

                    int dist = DistanceBetweenRooms(rooms[fromIdx], rooms[toIdx]);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFrom = fromIdx;
                        bestTo = toIdx;
                    }
                }
            }

            if (bestFrom < 0 || bestTo < 0)
                break;

            // Connect rooms
            CreateCorridor(context, rooms[bestFrom], rooms[bestTo]);
            connected.Add(bestTo);
            edges.Add((bestFrom, bestTo, bestDist));
        }
    }

    private void AddExtraConnections(GenerationContext context, List<PlacedRoom> rooms)
    {
        if (_srpConfig.ExtraConnectionChance <= 0 || rooms.Count < 3)
            return;

        // Try to add some extra connections
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 2; j < rooms.Count; j++)
            {
                if (context.Random.Next(100) < _srpConfig.ExtraConnectionChance)
                {
                    // Check if they're not too far apart
                    int dist = DistanceBetweenRooms(rooms[i], rooms[j]);
                    if (dist < context.Width / 3)
                    {
                        CreateCorridor(context, rooms[i], rooms[j]);
                    }
                }
            }
        }
    }

    private int DistanceBetweenRooms(PlacedRoom a, PlacedRoom b)
    {
        int cx1 = a.X + a.Width / 2;
        int cy1 = a.Y + a.Height / 2;
        int cx2 = b.X + b.Width / 2;
        int cy2 = b.Y + b.Height / 2;

        return Math.Abs(cx1 - cx2) + Math.Abs(cy1 - cy2); // Manhattan distance
    }

    private void CreateCorridor(GenerationContext context, PlacedRoom room1, PlacedRoom room2)
    {
        // Get center points
        int x1 = room1.X + room1.Width / 2;
        int y1 = room1.Y + room1.Height / 2;
        int x2 = room2.X + room2.Width / 2;
        int y2 = room2.Y + room2.Height / 2;

        if (_srpConfig.LShaped)
        {
            // L-shaped corridor
            if (context.Random.Next(2) == 0)
            {
                CreateHorizontalTunnel(context, x1, x2, y1);
                CreateVerticalTunnel(context, y1, y2, x2);
            }
            else
            {
                CreateVerticalTunnel(context, y1, y2, x1);
                CreateHorizontalTunnel(context, x1, x2, y2);
            }
        }
        else
        {
            // Direct diagonal-ish corridor using Bresenham-style stepping
            CreateDirectCorridor(context, x1, y1, x2, y2);
        }
    }

    private void CreateHorizontalTunnel(GenerationContext context, int x1, int x2, int y)
    {
        int startX = Math.Min(x1, x2);
        int endX = Math.Max(x1, x2);
        int width = _srpConfig.CorridorWidth;

        for (int x = startX; x <= endX; x++)
        {
            for (int w = 0; w < width; w++)
            {
                int yPos = y + w - width / 2;
                if (yPos >= 1 && yPos < context.Height - 1)
                {
                    context.SetTile(x, yPos, TileType.Floor);
                }
            }
        }
    }

    private void CreateVerticalTunnel(GenerationContext context, int y1, int y2, int x)
    {
        int startY = Math.Min(y1, y2);
        int endY = Math.Max(y1, y2);
        int width = _srpConfig.CorridorWidth;

        for (int y = startY; y <= endY; y++)
        {
            for (int w = 0; w < width; w++)
            {
                int xPos = x + w - width / 2;
                if (xPos >= 1 && xPos < context.Width - 1)
                {
                    context.SetTile(xPos, y, TileType.Floor);
                }
            }
        }
    }

    private void CreateDirectCorridor(GenerationContext context, int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        int x = x1;
        int y = y1;
        int width = _srpConfig.CorridorWidth;

        while (true)
        {
            // Carve at current position with width
            for (int wx = -width / 2; wx <= width / 2; wx++)
            {
                for (int wy = -width / 2; wy <= width / 2; wy++)
                {
                    int px = x + wx;
                    int py = y + wy;
                    if (px >= 1 && px < context.Width - 1 &&
                        py >= 1 && py < context.Height - 1)
                    {
                        context.SetTile(px, py, TileType.Floor);
                    }
                }
            }

            if (x == x2 && y == y2)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
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

    private void RegisterRoomsAsRegions(List<PlacedRoom> rooms, GenerationContext context)
    {
        foreach (var room in rooms)
        {
            var tiles = new List<GridPosition>();
            var edgeTiles = new List<GridPosition>();

            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    tiles.Add(pos);

                    bool isEdge = x == room.X || x == room.X + room.Width - 1 ||
                                  y == room.Y || y == room.Y + room.Height - 1;
                    if (isEdge)
                        edgeTiles.Add(pos);
                }
            }

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
                Source = RegionSource.Detected
            };

            context.Metadata.Regions.Add(region);
        }
    }

    /// <summary>
    /// Room placed by simple room placement algorithm.
    /// </summary>
    public class PlacedRoom
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int Id { get; }

        public PlacedRoom(int x, int y, int width, int height, int id)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Id = id;
        }
    }
}
