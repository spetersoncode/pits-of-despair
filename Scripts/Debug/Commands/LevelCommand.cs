using PitsOfDespair.Components;
using PitsOfDespair.Core;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to give the player exactly enough XP to level up.
/// </summary>
public class LevelCommand : DebugCommand
{
    public override string Name => "level";
    public override string Description => "Give player enough XP to level up";
    public override string Usage => "level";

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        var player = context.ActionContext.Player;
        var stats = player.GetNodeOrNull<StatsComponent>("StatsComponent");

        if (stats == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player has no stats component!",
                Palette.ToHex(Palette.Danger)
            );
        }

        int currentLevel = stats.Level;
        int xpNeeded = stats.ExperienceToNextLevel - stats.CurrentExperience;

        if (xpNeeded <= 0)
        {
            // Edge case: already at threshold, give 1 XP to trigger level up
            xpNeeded = 1;
        }

        stats.GainExperience(xpNeeded);

        return DebugCommandResult.CreateSuccess(
            $"Leveled up! {currentLevel} -> {stats.Level}",
            Palette.ToHex(Palette.Success)
        );
    }
}
