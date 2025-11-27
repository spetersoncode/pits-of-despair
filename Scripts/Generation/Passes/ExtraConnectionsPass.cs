using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Passes.Config;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Extra Connections modifier pass.
/// Adds additional corridors between nearby rooms to create loops and shortcuts.
/// Works with rooms from BSP or SimpleRoomPlacement base generators.
///
/// Role: Modifier (runs after base generator)
/// </summary>
public class ExtraConnectionsPass : IGenerationPass
{
    private readonly PassConfig _passConfig;
    private readonly ExtraConnectionsPassConfig _ecConfig;

    public string Name => "ExtraConnections";
    public int Priority { get; }
    public PassRole Role => PassRole.Modifier;

    public ExtraConnectionsPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? throw new ArgumentNullException(nameof(passConfig));
        _ecConfig = ExtraConnectionsPassConfig.FromPassConfig(passConfig);
        Priority = passConfig.Priority;

        if (!_ecConfig.Validate(out var error))
        {
            GD.PushWarning($"[ExtraConnectionsPass] Config validation warning: {error}");
        }
    }

    public bool CanExecute(GenerationContext context)
    {
        // Need rooms from a base generator
        return context.Metadata.Regions.Count >= 2;
    }

    public void Execute(GenerationContext context)
    {
        var regions = context.Metadata.Regions;

        if (regions.Count < 2)
        {
            GD.PushWarning("[ExtraConnectionsPass] Not enough regions to create extra connections");
            return;
        }

        int connectionsAdded = 0;

        // Try to add extra connections between non-adjacent rooms
        for (int i = 0; i < regions.Count; i++)
        {
            // Skip some rooms to avoid connecting adjacent BSP siblings
            for (int j = i + 2; j < regions.Count; j++)
            {
                if (context.Random.Next(100) >= _ecConfig.ConnectionChance)
                    continue;

                var regionA = regions[i];
                var regionB = regions[j];

                // Check distance between centroids
                int dist = ManhattanDistance(regionA.Centroid, regionB.Centroid);
                if (dist > _ecConfig.MaxDistance)
                    continue;

                // Create corridor between room centers
                CreateCorridor(context, regionA.Centroid, regionB.Centroid);
                connectionsAdded++;
            }
        }

        GD.Print($"[ExtraConnectionsPass] Added {connectionsAdded} extra connections");
    }

    private int ManhattanDistance(GridPosition a, GridPosition b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private void CreateCorridor(GenerationContext context, GridPosition from, GridPosition to)
    {
        int x1 = from.X;
        int y1 = from.Y;
        int x2 = to.X;
        int y2 = to.Y;

        if (_ecConfig.LShaped)
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
            // Direct diagonal-ish corridor using Bresenham
            CreateDirectCorridor(context, x1, y1, x2, y2);
        }
    }

    private void CreateHorizontalTunnel(GenerationContext context, int x1, int x2, int y)
    {
        int startX = Math.Min(x1, x2);
        int endX = Math.Max(x1, x2);
        int width = _ecConfig.CorridorWidth;

        for (int x = startX; x <= endX; x++)
        {
            for (int w = 0; w < width; w++)
            {
                int yPos = y + w - width / 2;
                if (yPos >= 1 && yPos < context.Height - 1 &&
                    x >= 1 && x < context.Width - 1)
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
        int width = _ecConfig.CorridorWidth;

        for (int y = startY; y <= endY; y++)
        {
            for (int w = 0; w < width; w++)
            {
                int xPos = x + w - width / 2;
                if (xPos >= 1 && xPos < context.Width - 1 &&
                    y >= 1 && y < context.Height - 1)
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
        int width = _ecConfig.CorridorWidth;

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
}
