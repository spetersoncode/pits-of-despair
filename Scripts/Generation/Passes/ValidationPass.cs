using System.Collections.Generic;
using Godot;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Post-processing pass that validates generation constraints.
/// This is a read-only pass - it does NOT modify the grid.
/// Useful for debugging and ensuring balanced generation.
/// </summary>
public class ValidationPass : IGenerationPass
{
    private readonly PassConfig _passConfig;

    // Walkable percentage constraints
    private readonly float _minWalkablePercent;
    private readonly float _maxWalkablePercent;

    // Region constraints (0 = no constraint)
    private readonly int _minRegions;
    private readonly int _maxRegions;
    private readonly int _minRegionSize;

    // Behavior
    private readonly bool _warnOnFail;

    public string Name => "Validation";
    public int Priority { get; }
    public PassRole Role => PassRole.PostProcess;

    public ValidationPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? new PassConfig { Pass = "validation", Priority = 250 };
        Priority = _passConfig.Priority;

        // Walkable percentage bounds
        _minWalkablePercent = _passConfig.GetConfigValue("minWalkablePercent", 20f);
        _maxWalkablePercent = _passConfig.GetConfigValue("maxWalkablePercent", 60f);

        // Region constraints (0 means no constraint)
        _minRegions = _passConfig.GetConfigValue("minRegions", 0);
        _maxRegions = _passConfig.GetConfigValue("maxRegions", 0);
        _minRegionSize = _passConfig.GetConfigValue("minRegionSize", 0);

        // If false, throws exception on validation failure instead of warning
        _warnOnFail = _passConfig.GetConfigValue("warnOnFail", true);
    }

    public bool CanExecute(GenerationContext context)
    {
        // Always run validation
        return true;
    }

    public void Execute(GenerationContext context)
    {
        GD.Print("[ValidationPass] Validating generation constraints...");

        var failures = new List<string>();

        // Validate walkable percentage
        ValidateWalkablePercent(context, failures);

        // Validate regions if metadata is available
        ValidateRegions(context, failures);

        // Report results
        if (failures.Count == 0)
        {
            GD.Print("[ValidationPass] All validation checks passed.");
        }
        else
        {
            foreach (var failure in failures)
            {
                if (_warnOnFail)
                {
                    GD.PushWarning($"[ValidationPass] {failure}");
                }
                else
                {
                    GD.PrintErr($"[ValidationPass] {failure}");
                }
            }

            if (!_warnOnFail)
            {
                throw new GenerationValidationException(
                    $"Validation failed with {failures.Count} error(s): {string.Join("; ", failures)}");
            }
        }
    }

    private void ValidateWalkablePercent(GenerationContext context, List<string> failures)
    {
        int totalTiles = context.Width * context.Height;
        int walkableTiles = 0;

        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                if (context.IsWalkable(x, y))
                    walkableTiles++;
            }
        }

        float walkablePercent = (walkableTiles / (float)totalTiles) * 100f;

        GD.Print($"[ValidationPass] Walkable tiles: {walkableTiles}/{totalTiles} ({walkablePercent:F1}%)");

        if (walkablePercent < _minWalkablePercent)
        {
            failures.Add($"Walkable percentage {walkablePercent:F1}% is below minimum {_minWalkablePercent}%");
        }

        if (walkablePercent > _maxWalkablePercent)
        {
            failures.Add($"Walkable percentage {walkablePercent:F1}% exceeds maximum {_maxWalkablePercent}%");
        }
    }

    private void ValidateRegions(GenerationContext context, List<string> failures)
    {
        // Skip region validation if no metadata or no constraints
        if (context.Metadata == null)
        {
            GD.Print("[ValidationPass] No metadata available, skipping region validation.");
            return;
        }

        var regions = context.Metadata.Regions;
        if (regions == null)
        {
            GD.Print("[ValidationPass] No regions in metadata, skipping region validation.");
            return;
        }

        int regionCount = regions.Count;
        GD.Print($"[ValidationPass] Region count: {regionCount}");

        // Validate region count
        if (_minRegions > 0 && regionCount < _minRegions)
        {
            failures.Add($"Region count {regionCount} is below minimum {_minRegions}");
        }

        if (_maxRegions > 0 && regionCount > _maxRegions)
        {
            failures.Add($"Region count {regionCount} exceeds maximum {_maxRegions}");
        }

        // Validate minimum region size
        if (_minRegionSize > 0)
        {
            int undersizedRegions = 0;
            foreach (var region in regions)
            {
                if (region.Area < _minRegionSize)
                {
                    undersizedRegions++;
                }
            }

            if (undersizedRegions > 0)
            {
                failures.Add($"{undersizedRegions} region(s) are smaller than minimum size {_minRegionSize}");
            }
        }
    }
}

/// <summary>
/// Exception thrown when validation fails and warnOnFail is false.
/// </summary>
public class GenerationValidationException : System.Exception
{
    public GenerationValidationException(string message) : base(message) { }
}
