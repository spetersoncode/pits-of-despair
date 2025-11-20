using System.Linq;
using PitsOfDespair.Core;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to list all available commands.
/// </summary>
public class HelpCommand : DebugCommand
{
    public override string Name => "help";
    public override string Description => "List all available debug commands";
    public override string Usage => "help";

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        var commands = DebugCommandFactory.GetAllCommands().ToList();

        if (commands.Count == 0)
        {
            return DebugCommandResult.CreateFailure(
                "No debug commands registered!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Build help text with all commands
        var helpLines = new System.Collections.Generic.List<string>
        {
            "[b]Available Debug Commands:[/b]"
        };

        foreach (var cmd in commands)
        {
            helpLines.Add($"  [color={Palette.ToHex(Palette.Alert)}]{cmd.Usage}[/color] - {cmd.Description}");
        }

        string helpText = string.Join("\n", helpLines);

        return DebugCommandResult.CreateSuccess(
            helpText,
            Palette.ToHex(Palette.Default)
        );
    }
}
