using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using System.Linq;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to teleport the player to the stairs.
/// </summary>
public class StairsCommand : DebugCommand
{
	public override string Name => "stairs";
	public override string Description => "Teleport to the stairs";
	public override string Usage => "stairs";

	public override DebugCommandResult Execute(DebugContext context, string[] args)
	{
		var player = context.ActionContext.Player;
		var entityManager = context.ActionContext.EntityManager;

		// Find stairs in the entity manager
		var stairs = entityManager.GetAllEntities().FirstOrDefault(e => e is Stairs);

		if (stairs == null)
		{
			return DebugCommandResult.CreateFailure(
				"No stairs found on this floor!",
				Palette.ToHex(Palette.Danger)
			);
		}

		// Teleport player to stairs position
		player.SetGridPosition(stairs.GridPosition);

		return DebugCommandResult.CreateSuccess(
			$"Teleported to stairs at ({stairs.GridPosition.X}, {stairs.GridPosition.Y})",
			Palette.ToHex(Palette.Success)
		);
	}
}
