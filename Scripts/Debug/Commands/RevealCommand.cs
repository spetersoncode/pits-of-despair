using PitsOfDespair.Core;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to toggle map reveal mode (bypasses fog-of-war).
/// </summary>
public class RevealCommand : DebugCommand
{
    public override string Name => "reveal";
    public override string Description => "Toggle map reveal (show entire dungeon)";
    public override string Usage => "reveal";

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (context.VisionSystem == null)
        {
            return DebugCommandResult.CreateFailure(
                "Vision system not available!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Toggle reveal mode
        context.VisionSystem.ToggleRevealMode();

        // The vision system prints its own status message,
        // but we also return a result for consistency
        return DebugCommandResult.CreateSuccess(
            "Map reveal toggled.",
            Palette.ToHex(Palette.Success)
        );
    }
}
