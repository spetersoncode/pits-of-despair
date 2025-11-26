using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Generation.Config;

namespace PitsOfDespair.Generation.Pipeline;

/// <summary>
/// Orchestrates the execution of generation passes in priority order.
/// Validates pipeline structure (exactly one base generator) and
/// executes passes sequentially on a shared context.
/// </summary>
public class GenerationPipeline
{
    private readonly List<IGenerationPass> _passes = new();

    /// <summary>
    /// Add a pass to the pipeline.
    /// </summary>
    public void AddPass(IGenerationPass pass)
    {
        if (pass == null)
            throw new ArgumentNullException(nameof(pass));

        _passes.Add(pass);
    }

    /// <summary>
    /// Get all passes in the pipeline.
    /// </summary>
    public IReadOnlyList<IGenerationPass> Passes => _passes.AsReadOnly();

    /// <summary>
    /// Execute the pipeline with the given configuration.
    /// </summary>
    /// <param name="config">Floor generation configuration.</param>
    /// <returns>Generation result containing grid and metadata.</returns>
    public GenerationResult Execute(FloorGenerationConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        // Validate pipeline structure
        ValidatePipeline();

        // Create shared context
        var context = new GenerationContext(config);
        var executedPasses = new List<string>();

        // Execute passes in priority order
        foreach (var pass in _passes.OrderBy(p => p.Priority))
        {
            if (pass.CanExecute(context))
            {
                pass.Execute(context);
                executedPasses.Add(pass.Name);

                // Track base generator
                if (pass.Role == PassRole.Base)
                    context.BaseGeneratorName = pass.Name;
            }
            else
            {
                GD.Print($"[GenerationPipeline] Skipping pass: {pass.Name} (CanExecute returned false)");
            }
        }

        // Build result
        return new GenerationResult
        {
            Grid = context.Grid,
            Metadata = context.Metadata,
            Seed = context.Seed,
            BaseGenerator = context.BaseGeneratorName,
            PassesExecuted = executedPasses
        };
    }

    /// <summary>
    /// Validate the pipeline has exactly one base generator.
    /// </summary>
    private void ValidatePipeline()
    {
        var baseGenerators = _passes.Where(p => p.Role == PassRole.Base).ToList();

        if (baseGenerators.Count == 0)
        {
            throw new InvalidOperationException(
                "Pipeline must have exactly one base generator. " +
                "Add a pass with Role = PassRole.Base (e.g., BSP, CellularAutomata, DrunkardWalk).");
        }

        if (baseGenerators.Count > 1)
        {
            var names = string.Join(", ", baseGenerators.Select(p => p.Name));
            throw new InvalidOperationException(
                $"Pipeline must have exactly one base generator, found {baseGenerators.Count}: {names}. " +
                "Set Role = PassRole.Modifier for additional topology passes.");
        }
    }

    /// <summary>
    /// Create a pipeline from a floor configuration.
    /// </summary>
    /// <param name="config">Floor generation configuration with pipeline definition.</param>
    /// <returns>Configured pipeline ready to execute.</returns>
    public static GenerationPipeline FromConfig(FloorGenerationConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var pipeline = new GenerationPipeline();

        // Create passes from config
        foreach (var passConfig in config.Pipeline)
        {
            var pass = GenerationPassFactory.Create(passConfig);
            pipeline.AddPass(pass);
        }

        // Auto-add metadata pass if not specified
        bool hasMetadataPass = config.Pipeline.Any(p =>
            p.Pass.Equals("metadata", StringComparison.OrdinalIgnoreCase));

        if (!hasMetadataPass)
        {
            // Metadata pass will be added when implemented in Phase 4
            // For now, just log that it would be added
            GD.Print("[GenerationPipeline] Note: MetadataAnalysisPass would be auto-added (not yet implemented)");
        }

        return pipeline;
    }

    /// <summary>
    /// Get a summary of the pipeline configuration.
    /// </summary>
    public string GetSummary()
    {
        var lines = new List<string> { "Generation Pipeline:" };

        foreach (var pass in _passes.OrderBy(p => p.Priority))
        {
            lines.Add($"  [{pass.Priority}] {pass.Name} ({pass.Role})");
        }

        return string.Join("\n", lines);
    }
}
