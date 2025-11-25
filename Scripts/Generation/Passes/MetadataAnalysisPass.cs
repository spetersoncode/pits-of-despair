using System;
using Godot;
using PitsOfDespair.Generation.Analyzers;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Metadata;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Post-processing pass that analyzes the generated dungeon and computes metadata.
/// Should run after all topology-modifying passes.
///
/// Computes: regions, passages, chokepoints, tile classifications, distance fields.
/// </summary>
public class MetadataAnalysisPass : IGenerationPass
{
    private readonly PassConfig _passConfig;
    private readonly int _minRegionSize;
    private readonly int _maxPassageWidth;

    public string Name => "Metadata";
    public int Priority { get; }
    public PassRole Role => PassRole.PostProcess;

    public MetadataAnalysisPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? new PassConfig { Pass = "metadata", Priority = 999 };
        Priority = _passConfig.Priority;

        // Extract config values
        _minRegionSize = _passConfig.GetConfigValue("minRegionSize", 16);
        _maxPassageWidth = _passConfig.GetConfigValue("maxPassageWidth", 2);
    }

    public bool CanExecute(GenerationContext context)
    {
        // Always can execute as post-processor
        return true;
    }

    public void Execute(GenerationContext context)
    {
        GD.Print("[MetadataAnalysisPass] Starting metadata analysis...");

        // Step 1: Compute wall distance field
        GD.Print("[MetadataAnalysisPass] Computing wall distance field...");
        var wallDistance = DistanceFieldComputer.ComputeWallDistance(context);
        context.Metadata.WallDistance = wallDistance;

        // Step 2: Classify all tiles
        GD.Print("[MetadataAnalysisPass] Classifying tiles...");
        var classifications = TileClassifier.ClassifyAll(context, wallDistance);
        context.Metadata.TileClassifications = classifications;

        // Step 3: Detect regions (preserving BSP rooms if present)
        GD.Print("[MetadataAnalysisPass] Detecting regions...");
        bool preserveExisting = context.Metadata.Regions.Count > 0;
        RegionDetector.DetectRegions(context, classifications, _minRegionSize, preserveExisting);

        // Step 4: Detect passages
        GD.Print("[MetadataAnalysisPass] Detecting passages...");
        PassageDetector.DetectPassages(context, classifications, wallDistance);

        // Step 5: Detect chokepoints
        GD.Print("[MetadataAnalysisPass] Detecting chokepoints...");
        ChokepointDetector.DetectChokepoints(context, classifications, wallDistance);

        // Step 6: Build region graph
        GD.Print("[MetadataAnalysisPass] Building region graph...");
        var graph = new RegionGraph(context.Metadata.Regions, context.Metadata.Passages);

        // Log results
        GD.Print($"[MetadataAnalysisPass] Analysis complete:");
        GD.Print($"  - Regions: {context.Metadata.Regions.Count}");
        GD.Print($"  - Alcoves: {context.Metadata.Alcoves.Count}");
        GD.Print($"  - Passages: {context.Metadata.Passages.Count}");
        GD.Print($"  - Chokepoints: {context.Metadata.Chokepoints.Count}");
        GD.Print($"  - Fully connected: {graph.IsFullyConnected()}");
    }
}
