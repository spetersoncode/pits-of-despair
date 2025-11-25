using PitsOfDespair.Core;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to display spawn statistics for the current floor.
/// </summary>
public class SpawnStatsCommand : DebugCommand
{
    public override string Name => "spawnstats";
    public override string Description => "Display spawn statistics for current floor";
    public override string Usage => "spawnstats";

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (context.SpawnOrchestrator == null)
        {
            return DebugCommandResult.CreateFailure(
                "Spawn orchestrator not available!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var summary = context.SpawnOrchestrator.LastSpawnSummary;
        if (summary == null)
        {
            return DebugCommandResult.CreateFailure(
                "No spawn data available for this floor.",
                Palette.ToHex(Palette.Caution)
            );
        }

        // Output the debug string - it will be printed to the console
        Godot.GD.Print(summary.ToDebugString());

        return DebugCommandResult.CreateSuccess(
            $"Floor {summary.FloorDepth}: {summary.CreaturesSpawned} creatures, {summary.EncountersPlaced} encounters, {summary.PowerBudgetUtilization:F0}% budget used",
            Palette.ToHex(Palette.Success)
        );
    }
}
