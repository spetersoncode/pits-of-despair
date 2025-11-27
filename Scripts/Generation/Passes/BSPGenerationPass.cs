using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Passes.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Binary Space Partitioning generation pass.
/// Recursively divides space into partitions, creates rooms in leaf nodes,
/// and connects them with L-shaped corridors.
///
/// Role: Base generator (initializes grid, carves primary topology)
/// </summary>
public class BSPGenerationPass : IGenerationPass
{
    /// <summary>
    /// Key for storing BSP tree in PassData for use by modifier passes.
    /// </summary>
    public const string PassDataKeyBSPTree = "BSPTree";

    /// <summary>
    /// Key for storing list of created rooms in PassData.
    /// </summary>
    public const string PassDataKeyRooms = "BSPRooms";

    private readonly PassConfig _passConfig;
    private readonly BSPPassConfig _bspConfig;

    public string Name => "BSP";
    public int Priority { get; }
    public PassRole Role => PassRole.Base;

    public BSPGenerationPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? throw new ArgumentNullException(nameof(passConfig));
        _bspConfig = BSPPassConfig.FromPassConfig(passConfig);
        Priority = passConfig.Priority;

        // Validate config
        if (!_bspConfig.Validate(out var error))
        {
            GD.PushWarning($"[BSPGenerationPass] Config validation warning: {error}");
        }
    }

    public bool CanExecute(GenerationContext context)
    {
        // BSP can always execute as a base generator
        return true;
    }

    public void Execute(GenerationContext context)
    {
        var rooms = new List<BSPRoom>();

        // Create root partition (1-tile border on all sides)
        var root = new BSPNode(1, 1, context.Width - 2, context.Height - 2);

        // Recursively split the space
        SplitNode(root, 0, context.Random);

        // Create rooms in leaf nodes
        CreateRooms(root, context, rooms);

        // Connect rooms with corridors
        ConnectRooms(root, context);

        // Ensure border walls
        EnsureBorderWalls(context);

        // Store BSP tree and rooms for modifier passes
        context.SetPassData(PassDataKeyBSPTree, root);
        context.SetPassData(PassDataKeyRooms, rooms);

        // Register rooms as Regions in metadata
        RegisterRoomsAsRegions(rooms, context);

        GD.Print($"[BSPGenerationPass] Generated {rooms.Count} rooms");
    }

    private void SplitNode(BSPNode node, int depth, Random random)
    {
        // Safety limit
        if (depth >= _bspConfig.MaxDepth)
            return;

        // Check if partition is already at target size
        bool widthTooLarge = node.Width > _bspConfig.MaxPartitionSize;
        bool heightTooLarge = node.Height > _bspConfig.MaxPartitionSize;

        if (!widthTooLarge && !heightTooLarge)
            return;

        // Check if we can split in each direction
        bool canSplitVertically = node.Width >= _bspConfig.MinPartitionSize * 2;
        bool canSplitHorizontally = node.Height >= _bspConfig.MinPartitionSize * 2;

        if (!canSplitVertically && !canSplitHorizontally)
            return;

        // Determine split orientation
        bool splitHorizontally = DetermineSplitOrientation(
            node, canSplitHorizontally, canSplitVertically,
            widthTooLarge, heightTooLarge, random);

        if (splitHorizontally)
        {
            int minSplit = node.Y + _bspConfig.MinPartitionSize;
            int maxSplit = node.Y + node.Height - _bspConfig.MinPartitionSize;

            if (maxSplit <= minSplit)
                return;

            int splitPos = random.Next(minSplit, maxSplit);
            node.LeftChild = new BSPNode(node.X, node.Y, node.Width, splitPos - node.Y);
            node.RightChild = new BSPNode(node.X, splitPos, node.Width, node.Y + node.Height - splitPos);
        }
        else
        {
            int minSplit = node.X + _bspConfig.MinPartitionSize;
            int maxSplit = node.X + node.Width - _bspConfig.MinPartitionSize;

            if (maxSplit <= minSplit)
                return;

            int splitPos = random.Next(minSplit, maxSplit);
            node.LeftChild = new BSPNode(node.X, node.Y, splitPos - node.X, node.Height);
            node.RightChild = new BSPNode(splitPos, node.Y, node.X + node.Width - splitPos, node.Height);
        }

        // Recursively split children
        SplitNode(node.LeftChild, depth + 1, random);
        SplitNode(node.RightChild, depth + 1, random);
    }

    private bool DetermineSplitOrientation(
        BSPNode node, bool canHorizontal, bool canVertical,
        bool widthTooLarge, bool heightTooLarge, Random random)
    {
        if (!canHorizontal) return false;
        if (!canVertical) return true;
        if (widthTooLarge && !heightTooLarge) return false;
        if (heightTooLarge && !widthTooLarge) return true;
        if (node.Width > node.Height * 1.25) return false;
        if (node.Height > node.Width * 1.25) return true;
        return random.Next(2) == 0;
    }

    private void CreateRooms(BSPNode node, GenerationContext context, List<BSPRoom> rooms)
    {
        if (node.IsLeaf())
        {
            // Create a room in this leaf node
            int maxWidth = Math.Min(_bspConfig.MaxRoomWidth, node.Width - 1);
            int maxHeight = Math.Min(_bspConfig.MaxRoomHeight, node.Height - 1);

            int roomWidth = context.Random.Next(_bspConfig.MinRoomWidth, maxWidth + 1);
            int roomHeight = context.Random.Next(_bspConfig.MinRoomHeight, maxHeight + 1);

            int maxOffsetX = Math.Max(0, node.Width - roomWidth - 1);
            int maxOffsetY = Math.Max(0, node.Height - roomHeight - 1);
            int roomX = node.X + context.Random.Next(0, maxOffsetX + 1);
            int roomY = node.Y + context.Random.Next(0, maxOffsetY + 1);

            var room = new BSPRoom(roomX, roomY, roomWidth, roomHeight, rooms.Count);
            node.Room = room;
            rooms.Add(room);

            // Carve out the room
            for (int x = roomX; x < roomX + roomWidth; x++)
            {
                for (int y = roomY; y < roomY + roomHeight; y++)
                {
                    context.SetTile(x, y, TileType.Floor);
                }
            }
        }
        else
        {
            if (node.LeftChild != null)
                CreateRooms(node.LeftChild, context, rooms);
            if (node.RightChild != null)
                CreateRooms(node.RightChild, context, rooms);
        }
    }

    private void ConnectRooms(BSPNode node, GenerationContext context)
    {
        if (node.IsLeaf())
            return;

        if (node.LeftChild != null)
            ConnectRooms(node.LeftChild, context);
        if (node.RightChild != null)
            ConnectRooms(node.RightChild, context);

        if (node.LeftChild != null && node.RightChild != null)
        {
            var leftRoom = GetRandomRoom(node.LeftChild, context.Random);
            var rightRoom = GetRandomRoom(node.RightChild, context.Random);

            if (leftRoom != null && rightRoom != null)
            {
                CreateCorridor(context, leftRoom, rightRoom);
            }
        }
    }

    private BSPRoom GetRandomRoom(BSPNode node, Random random)
    {
        if (node.IsLeaf())
            return node.Room;

        BSPRoom leftRoom = null;
        BSPRoom rightRoom = null;

        if (node.LeftChild != null)
            leftRoom = GetRandomRoom(node.LeftChild, random);
        if (node.RightChild != null)
            rightRoom = GetRandomRoom(node.RightChild, random);

        if (leftRoom == null) return rightRoom;
        if (rightRoom == null) return leftRoom;
        return random.Next(2) == 0 ? leftRoom : rightRoom;
    }

    private void CreateCorridor(GenerationContext context, BSPRoom room1, BSPRoom room2)
    {
        int x1 = room1.X + room1.Width / 2;
        int y1 = room1.Y + room1.Height / 2;
        int x2 = room2.X + room2.Width / 2;
        int y2 = room2.Y + room2.Height / 2;

        // Create L-shaped corridor
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

    private void CreateHorizontalTunnel(GenerationContext context, int x1, int x2, int y)
    {
        int startX = Math.Min(x1, x2);
        int endX = Math.Max(x1, x2);
        int width = _bspConfig.CorridorWidth;

        for (int x = startX; x <= endX; x++)
        {
            for (int w = 0; w < width; w++)
            {
                int yPos = y + w - width / 2;
                context.SetTile(x, yPos, TileType.Floor);
            }
        }
    }

    private void CreateVerticalTunnel(GenerationContext context, int y1, int y2, int x)
    {
        int startY = Math.Min(y1, y2);
        int endY = Math.Max(y1, y2);
        int width = _bspConfig.CorridorWidth;

        for (int y = startY; y <= endY; y++)
        {
            for (int w = 0; w < width; w++)
            {
                int xPos = x + w - width / 2;
                context.SetTile(xPos, y, TileType.Floor);
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

    private void RegisterRoomsAsRegions(List<BSPRoom> rooms, GenerationContext context)
    {
        foreach (var room in rooms)
        {
            var tiles = new List<GridPosition>();
            var edgeTiles = new List<GridPosition>();

            // Collect tiles and identify edges
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    var pos = new GridPosition(x, y);
                    tiles.Add(pos);

                    // Check if this is an edge tile (adjacent to wall)
                    bool isEdge = x == room.X || x == room.X + room.Width - 1 ||
                                  y == room.Y || y == room.Y + room.Height - 1;
                    if (isEdge)
                        edgeTiles.Add(pos);
                }
            }

            // Calculate centroid
            var centroid = new GridPosition(
                room.X + room.Width / 2,
                room.Y + room.Height / 2);

            // Create region
            var region = new Region
            {
                Id = room.Id,
                Tiles = tiles,
                EdgeTiles = edgeTiles,
                BoundingBox = new Rect2I(room.X, room.Y, room.Width, room.Height),
                Centroid = centroid,
                Source = RegionSource.BSPRoom
            };

            context.Metadata.Regions.Add(region);
        }
    }

    /// <summary>
    /// BSP tree node for space partitioning.
    /// </summary>
    public class BSPNode
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public BSPNode LeftChild { get; set; }
        public BSPNode RightChild { get; set; }
        public BSPRoom Room { get; set; }

        public BSPNode(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool IsLeaf() => LeftChild == null && RightChild == null;
    }

    /// <summary>
    /// Room created by BSP algorithm.
    /// </summary>
    public class BSPRoom
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int Id { get; }

        public BSPRoom(int x, int y, int width, int height, int id)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Id = id;
        }
    }
}
