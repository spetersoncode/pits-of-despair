using System;
using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Cellular Automata generation pass.
/// Supports two modes:
/// - Base: Generates cave-like terrain from random noise
/// - Modifier: Transforms existing regions into organic shapes
/// </summary>
public class CellularAutomataPass : IGenerationPass
{
    private readonly PassConfig _passConfig;
    private readonly CellularAutomataPassConfig _caConfig;

    public string Name => "CellularAutomata";
    public int Priority { get; }
    public PassRole Role { get; }

    public CellularAutomataPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? throw new ArgumentNullException(nameof(passConfig));
        _caConfig = CellularAutomataPassConfig.FromPassConfig(passConfig);
        Priority = passConfig.Priority;
        Role = _caConfig.Role;
    }

    public bool CanExecute(GenerationContext context)
    {
        if (Role == PassRole.Modifier)
        {
            // Modifier mode requires existing regions
            return context.Metadata.Regions.Count > 0;
        }
        return true;
    }

    public void Execute(GenerationContext context)
    {
        if (Role == PassRole.Base)
        {
            ExecuteAsBase(context);
        }
        else
        {
            ExecuteAsModifier(context);
        }
    }

    /// <summary>
    /// Execute as base generator - creates cave terrain from scratch.
    /// </summary>
    private void ExecuteAsBase(GenerationContext context)
    {
        GD.Print($"[CellularAutomataPass] Generating cave terrain (fill: {_caConfig.FillPercent}%, iterations: {_caConfig.Iterations})");

        // Initialize with random noise (leaving 1-tile border)
        InitializeWithNoise(context);

        // Run CA iterations
        for (int i = 0; i < _caConfig.Iterations; i++)
        {
            RunCAIteration(context, 1, 1, context.Width - 2, context.Height - 2);
        }

        // Smoothing passes
        for (int i = 0; i < _caConfig.SmoothingIterations; i++)
        {
            RunSmoothingIteration(context, 1, 1, context.Width - 2, context.Height - 2);
        }

        // Ensure border walls
        EnsureBorderWalls(context);

        GD.Print($"[CellularAutomataPass] Cave generation complete");
    }

    /// <summary>
    /// Execute as modifier - transforms target regions.
    /// </summary>
    private void ExecuteAsModifier(GenerationContext context)
    {
        var targetRegions = SelectTargetRegions(context);
        GD.Print($"[CellularAutomataPass] Modifying {targetRegions.Count} region(s)");

        foreach (var region in targetRegions)
        {
            TransformRegion(context, region);
        }
    }

    /// <summary>
    /// Initialize grid with random floor/wall based on fill percentage.
    /// </summary>
    private void InitializeWithNoise(GenerationContext context)
    {
        for (int x = 1; x < context.Width - 1; x++)
        {
            for (int y = 1; y < context.Height - 1; y++)
            {
                if (context.Random.Next(100) < _caConfig.FillPercent)
                    context.SetTile(x, y, TileType.Floor);
                else
                    context.SetTile(x, y, TileType.Wall);
            }
        }
    }

    /// <summary>
    /// Run one iteration of cellular automata rules.
    /// </summary>
    private void RunCAIteration(GenerationContext context, int startX, int startY, int endX, int endY)
    {
        // Create copy to read from while writing
        var newTiles = new TileType[context.Width, context.Height];
        Array.Copy(context.Grid, newTiles, context.Grid.Length);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                int floorNeighbors = CountFloorNeighbors(context, x, y);

                if (context.GetTile(x, y) == TileType.Wall)
                {
                    // Birth rule: wall becomes floor
                    if (floorNeighbors >= _caConfig.BirthLimit)
                        newTiles[x, y] = TileType.Floor;
                }
                else
                {
                    // Death rule: floor becomes wall
                    if (floorNeighbors < _caConfig.DeathLimit)
                        newTiles[x, y] = TileType.Wall;
                }
            }
        }

        // Copy back
        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                context.Grid[x, y] = newTiles[x, y];
            }
        }
    }

    /// <summary>
    /// Smoothing iteration - removes isolated floor/wall tiles.
    /// </summary>
    private void RunSmoothingIteration(GenerationContext context, int startX, int startY, int endX, int endY)
    {
        var newTiles = new TileType[context.Width, context.Height];
        Array.Copy(context.Grid, newTiles, context.Grid.Length);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                int floorNeighbors = CountFloorNeighbors(context, x, y);

                // Remove isolated tiles (less than 2 same-type neighbors)
                if (context.GetTile(x, y) == TileType.Floor && floorNeighbors < 2)
                    newTiles[x, y] = TileType.Wall;
                else if (context.GetTile(x, y) == TileType.Wall && floorNeighbors > 6)
                    newTiles[x, y] = TileType.Floor;
            }
        }

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                context.Grid[x, y] = newTiles[x, y];
            }
        }
    }

    /// <summary>
    /// Count floor neighbors in 8 directions.
    /// </summary>
    private int CountFloorNeighbors(GenerationContext context, int x, int y)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (context.GetTile(x + dx, y + dy) == TileType.Floor)
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Select regions to transform based on config.
    /// </summary>
    private List<Region> SelectTargetRegions(GenerationContext context)
    {
        var regions = context.Metadata.Regions;
        var targets = new List<Region>();

        if (_caConfig.TargetRegions == null)
        {
            // Default: select one random region
            if (regions.Count > 0)
                targets.Add(regions[context.Random.Next(regions.Count)]);
            return targets;
        }

        var config = _caConfig.TargetRegions;

        // Filter by minimum area
        var candidates = new List<Region>();
        foreach (var region in regions)
        {
            if (region.Area >= config.MinArea)
                candidates.Add(region);
        }

        switch (config.Type.ToLowerInvariant())
        {
            case "all":
                targets.AddRange(candidates);
                break;

            case "tagged":
                foreach (var region in candidates)
                {
                    if (region.Tag == config.Tag)
                        targets.Add(region);
                }
                break;

            case "random":
            default:
                // Shuffle and take N
                var shuffled = new List<Region>(candidates);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = context.Random.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }
                for (int i = 0; i < config.Count && i < shuffled.Count; i++)
                {
                    targets.Add(shuffled[i]);
                }
                break;
        }

        return targets;
    }

    /// <summary>
    /// Transform a single region using CA rules.
    /// </summary>
    private void TransformRegion(GenerationContext context, Region region)
    {
        var bbox = region.BoundingBox;

        // Expand bounding box slightly for CA to work at edges
        int startX = Math.Max(1, bbox.Position.X - 1);
        int startY = Math.Max(1, bbox.Position.Y - 1);
        int endX = Math.Min(context.Width - 1, bbox.Position.X + bbox.Size.X + 1);
        int endY = Math.Min(context.Height - 1, bbox.Position.Y + bbox.Size.Y + 1);

        // Run CA iterations on region bounds
        for (int i = 0; i < _caConfig.Iterations; i++)
        {
            RunCAIteration(context, startX, startY, endX, endY);
        }

        // Update region source
        region.Source = RegionSource.Cave;

        GD.Print($"[CellularAutomataPass] Transformed region {region.Id} to cave");
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
}
