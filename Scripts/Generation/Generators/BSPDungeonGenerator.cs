using System;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Generation.Generators;

/// <summary>
/// Binary Space Partitioning dungeon generator.
/// Recursively divides space into partitions, creates rooms in leaf nodes,
/// and connects them with corridors.
/// </summary>
public class BSPDungeonGenerator : IDungeonGenerator
{
    private const int MaxDepth = 10; // Safety limit to prevent infinite recursion

    private readonly BSPConfig _config;
    private Random _random;
    private BSPNode _root;

    /// <summary>
    /// Creates a new BSP dungeon generator with the specified configuration.
    /// </summary>
    public BSPDungeonGenerator(BSPConfig config)
    {
        _config = config ?? new BSPConfig();
    }

    public TileType[,] Generate(int width, int height)
    {
        _random = new Random(_config.GetActualSeed());

        // Initialize map with all walls
        var grid = new TileType[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = TileType.Wall;
            }
        }

        // Create root partition and recursively split
        _root = new BSPNode(1, 1, width - 2, height - 2);
        SplitNode(_root, 0);

        // Create rooms in leaf nodes
        CreateRooms(_root, grid);

        // Connect rooms with corridors
        ConnectRooms(_root, grid);

        // Add border walls (already walls, but ensures clean edges)
        for (int x = 0; x < width; x++)
        {
            grid[x, 0] = TileType.Wall;
            grid[x, height - 1] = TileType.Wall;
        }
        for (int y = 0; y < height; y++)
        {
            grid[0, y] = TileType.Wall;
            grid[width - 1, y] = TileType.Wall;
        }

        return grid;
    }

    private void SplitNode(BSPNode node, int depth)
    {
        // Safety limit to prevent infinite recursion
        if (depth >= MaxDepth)
        {
            return;
        }

        // Check if partition is already at target size
        bool widthTooLarge = node.Width > _config.MaxPartitionSize;
        bool heightTooLarge = node.Height > _config.MaxPartitionSize;

        // Stop if partition is at target size
        if (!widthTooLarge && !heightTooLarge)
        {
            return;
        }

        // Check if we can split in each direction
        bool canSplitVertically = node.Width >= _config.MinPartitionSize * 2;
        bool canSplitHorizontally = node.Height >= _config.MinPartitionSize * 2;

        // Can't split at all - stop
        if (!canSplitVertically && !canSplitHorizontally)
        {
            return;
        }

        // Determine split orientation
        bool splitHorizontally;
        if (!canSplitHorizontally)
        {
            splitHorizontally = false; // Can only split vertically
        }
        else if (!canSplitVertically)
        {
            splitHorizontally = true; // Can only split horizontally
        }
        else if (widthTooLarge && !heightTooLarge)
        {
            splitHorizontally = false; // Width needs splitting
        }
        else if (heightTooLarge && !widthTooLarge)
        {
            splitHorizontally = true; // Height needs splitting
        }
        else if (node.Width > node.Height * 1.25)
        {
            splitHorizontally = false; // Prefer vertical split for wide rooms
        }
        else if (node.Height > node.Width * 1.25)
        {
            splitHorizontally = true; // Prefer horizontal split for tall rooms
        }
        else
        {
            splitHorizontally = _random.Next(2) == 0; // Random choice
        }

        if (splitHorizontally)
        {
            // Split horizontally
            int minSplit = node.Y + _config.MinPartitionSize;
            int maxSplit = node.Y + node.Height - _config.MinPartitionSize;

            if (maxSplit <= minSplit)
                return;

            int splitPos = _random.Next(minSplit, maxSplit);
            node.LeftChild = new BSPNode(node.X, node.Y, node.Width, splitPos - node.Y);
            node.RightChild = new BSPNode(node.X, splitPos, node.Width, node.Y + node.Height - splitPos);
        }
        else
        {
            // Split vertically
            int minSplit = node.X + _config.MinPartitionSize;
            int maxSplit = node.X + node.Width - _config.MinPartitionSize;

            if (maxSplit <= minSplit)
                return;

            int splitPos = _random.Next(minSplit, maxSplit);
            node.LeftChild = new BSPNode(node.X, node.Y, splitPos - node.X, node.Height);
            node.RightChild = new BSPNode(splitPos, node.Y, node.X + node.Width - splitPos, node.Height);
        }

        // Recursively split children
        SplitNode(node.LeftChild, depth + 1);
        SplitNode(node.RightChild, depth + 1);
    }

    private void CreateRooms(BSPNode node, TileType[,] grid)
    {
        if (node.IsLeaf())
        {
            // Create a room in this leaf node - fill partition better to reduce gaps
            // Allow rooms to be as large as partition minus minimal 1-tile buffer
            int maxWidth = Math.Min(_config.MaxRoomWidth, node.Width - 1);
            int maxHeight = Math.Min(_config.MaxRoomHeight, node.Height - 1);

            int roomWidth = _random.Next(_config.MinRoomWidth, maxWidth + 1);
            int roomHeight = _random.Next(_config.MinRoomHeight, maxHeight + 1);

            // Minimal offset - keep rooms close to partition edges
            int maxOffsetX = Math.Max(0, node.Width - roomWidth - 1);
            int maxOffsetY = Math.Max(0, node.Height - roomHeight - 1);
            int roomX = node.X + _random.Next(0, maxOffsetX + 1);
            int roomY = node.Y + _random.Next(0, maxOffsetY + 1);

            node.Room = new Rectangle(roomX, roomY, roomWidth, roomHeight);

            // Carve out the room
            for (int x = roomX; x < roomX + roomWidth; x++)
            {
                for (int y = roomY; y < roomY + roomHeight; y++)
                {
                    if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                    {
                        grid[x, y] = TileType.Floor;
                    }
                }
            }
        }
        else
        {
            // Recursively create rooms in children
            if (node.LeftChild != null)
                CreateRooms(node.LeftChild, grid);
            if (node.RightChild != null)
                CreateRooms(node.RightChild, grid);
        }
    }

    private void ConnectRooms(BSPNode node, TileType[,] grid)
    {
        if (node.IsLeaf())
            return;

        // Connect rooms in children first
        if (node.LeftChild != null)
            ConnectRooms(node.LeftChild, grid);
        if (node.RightChild != null)
            ConnectRooms(node.RightChild, grid);

        // Connect the two subtrees
        if (node.LeftChild != null && node.RightChild != null)
        {
            var leftRoom = GetRandomRoom(node.LeftChild);
            var rightRoom = GetRandomRoom(node.RightChild);

            if (leftRoom != null && rightRoom != null)
            {
                CreateCorridor(grid, leftRoom.Value, rightRoom.Value, _config.CorridorWidth);
            }
        }
    }

    private Rectangle? GetRandomRoom(BSPNode node)
    {
        if (node.IsLeaf())
        {
            return node.Room;
        }

        Rectangle? leftRoom = null;
        Rectangle? rightRoom = null;

        if (node.LeftChild != null)
            leftRoom = GetRandomRoom(node.LeftChild);
        if (node.RightChild != null)
            rightRoom = GetRandomRoom(node.RightChild);

        if (leftRoom == null)
            return rightRoom;
        if (rightRoom == null)
            return leftRoom;

        return _random.Next(2) == 0 ? leftRoom : rightRoom;
    }

    private void CreateCorridor(TileType[,] grid, Rectangle room1, Rectangle room2, int width)
    {
        int x1 = room1.X + room1.Width / 2;
        int y1 = room1.Y + room1.Height / 2;
        int x2 = room2.X + room2.Width / 2;
        int y2 = room2.Y + room2.Height / 2;

        // Create L-shaped corridor
        if (_random.Next(2) == 0)
        {
            // Horizontal then vertical
            CreateHorizontalTunnel(grid, x1, x2, y1, width);
            CreateVerticalTunnel(grid, y1, y2, x2, width);
        }
        else
        {
            // Vertical then horizontal
            CreateVerticalTunnel(grid, y1, y2, x1, width);
            CreateHorizontalTunnel(grid, x1, x2, y2, width);
        }
    }

    private void CreateHorizontalTunnel(TileType[,] grid, int x1, int x2, int y, int width)
    {
        int startX = Math.Min(x1, x2);
        int endX = Math.Max(x1, x2);

        for (int x = startX; x <= endX; x++)
        {
            for (int w = 0; w < width; w++)
            {
                int yPos = y + w - width / 2;
                if (x >= 0 && x < grid.GetLength(0) && yPos >= 0 && yPos < grid.GetLength(1))
                {
                    grid[x, yPos] = TileType.Floor;
                }
            }
        }
    }

    private void CreateVerticalTunnel(TileType[,] grid, int y1, int y2, int x, int width)
    {
        int startY = Math.Min(y1, y2);
        int endY = Math.Max(y1, y2);

        for (int y = startY; y <= endY; y++)
        {
            for (int w = 0; w < width; w++)
            {
                int xPos = x + w - width / 2;
                if (xPos >= 0 && xPos < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                {
                    grid[xPos, y] = TileType.Floor;
                }
            }
        }
    }

    /// <summary>
    /// Represents a node in the BSP tree.
    /// </summary>
    private class BSPNode
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public BSPNode LeftChild { get; set; }
        public BSPNode RightChild { get; set; }
        public Rectangle? Room { get; set; }

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
    /// Simple rectangle structure for rooms.
    /// </summary>
    private struct Rectangle
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
