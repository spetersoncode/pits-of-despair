using System;
using Godot;
using PitsOfDespair.Generation.Analyzers;
using PitsOfDespair.Generation.Passes.Config;
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
		// Step 1: Compute wall distance field
		var wallDistance = DistanceFieldComputer.ComputeWallDistance(context);
		context.Metadata.WallDistance = wallDistance;

		// Step 2: Classify all tiles
		var classifications = TileClassifier.ClassifyAll(context, wallDistance);
		context.Metadata.TileClassifications = classifications;

		// Step 3: Detect regions (preserving BSP rooms if present)
		bool preserveExisting = context.Metadata.Regions.Count > 0;
		RegionDetector.DetectRegions(context, classifications, _minRegionSize, preserveExisting);

		// Step 4: Detect passages
		PassageDetector.DetectPassages(context, classifications, wallDistance);

		// Step 5: Detect chokepoints
		ChokepointDetector.DetectChokepoints(context, classifications, wallDistance);

		// Step 6: Build region graph (for adjacency queries, not tile connectivity)
		var graph = new RegionGraph(context.Metadata.Regions, context.Metadata.Passages);

		// Note: Region graph may have gaps when regions connect via wide openings rather than narrow passages.
		// This is normal for BSP-style dungeons and handled by RegionThemeAssigner's adjacency detection.
	}
}
