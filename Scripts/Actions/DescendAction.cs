using System.Linq;
using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Actions;

/// <summary>
/// Action for descending stairs to the next dungeon floor.
/// Can only be executed when standing on stairs.
/// </summary>
public class DescendAction : Action
{
	public override string Name => "Descend";

	public override bool CanExecute(BaseEntity actor, ActionContext context)
	{
		if (actor == null || context == null)
		{
			return false;
		}

		// Check if actor is standing on stairs (use GetEntitiesAtPosition to find stairs
		// underneath the player, since GetEntityAtPosition returns the player first)
		var entitiesAtPosition = context.EntityManager.GetEntitiesAtPosition(actor.GridPosition);
		return entitiesAtPosition.Any(e => e is Stairs);
	}

	public override ActionResult Execute(BaseEntity actor, ActionContext context)
	{
		if (!CanExecute(actor, context))
		{
			return ActionResult.CreateFailure("There are no stairs here.");
		}

		// Only players can descend (for now)
		if (actor is not Player player)
		{
			return ActionResult.CreateFailure("Only the player can descend stairs.");
		}

		// Find GameManager by navigating up the tree
		var gameManager = player.GetTree()?.Root.GetNodeOrNull<GameManager>("GameManager");
		if (gameManager == null)
		{
			GD.PrintErr("DescendAction: GameManager not found in scene tree");
			return ActionResult.CreateFailure("Cannot descend: system error.");
		}

		// Trigger floor transition
		gameManager.DescendToNextFloor();

		// Return success (this action consumes a turn)
		return ActionResult.CreateSuccess("You descend the stairs into darkness...");
	}
}
