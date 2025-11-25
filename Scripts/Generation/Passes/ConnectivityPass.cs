using Godot;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Pipeline;
using PitsOfDespair.Generation.Validators;

namespace PitsOfDespair.Generation.Passes;

/// <summary>
/// Post-processing pass that validates and repairs dungeon connectivity.
/// Ensures all walkable areas are reachable from each other.
/// </summary>
public class ConnectivityPass : IGenerationPass
{
    private readonly PassConfig _passConfig;
    private readonly int _maxCorridorLength;
    private readonly string _repairStrategy;

    public string Name => "Connectivity";
    public int Priority { get; }
    public PassRole Role => PassRole.PostProcess;

    public ConnectivityPass(PassConfig passConfig)
    {
        _passConfig = passConfig ?? new PassConfig { Pass = "connectivity", Priority = 300 };
        Priority = _passConfig.Priority;

        _maxCorridorLength = _passConfig.GetConfigValue("maxCorridorLength", 20);
        _repairStrategy = _passConfig.GetConfigValue("repairStrategy", "mst");
    }

    public bool CanExecute(GenerationContext context)
    {
        // Always run connectivity check
        return true;
    }

    public void Execute(GenerationContext context)
    {
        GD.Print("[ConnectivityPass] Validating dungeon connectivity...");

        // Validate connectivity
        var validation = ConnectivityValidator.Validate(context);

        GD.Print($"[ConnectivityPass] Found {validation.Islands.Count} island(s), {validation.TotalWalkableTiles} walkable tiles");

        if (validation.IsFullyConnected)
        {
            GD.Print("[ConnectivityPass] Dungeon is fully connected.");
            return;
        }

        GD.Print($"[ConnectivityPass] Dungeon has {validation.Islands.Count} disconnected islands. Repairing...");

        // Repair based on strategy
        int corridorsCarved = 0;

        switch (_repairStrategy.ToLowerInvariant())
        {
            case "mst":
            default:
                corridorsCarved = MSTRepairStrategy.Repair(context, validation, _maxCorridorLength);
                break;
        }

        GD.Print($"[ConnectivityPass] Carved {corridorsCarved} corridor(s) to connect islands.");

        // Re-validate
        var revalidation = ConnectivityValidator.Validate(context);
        if (revalidation.IsFullyConnected)
        {
            GD.Print("[ConnectivityPass] Dungeon is now fully connected.");
        }
        else
        {
            GD.PushWarning($"[ConnectivityPass] Still have {revalidation.Islands.Count} disconnected islands after repair!");
        }
    }
}
