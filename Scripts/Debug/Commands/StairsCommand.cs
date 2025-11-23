using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to teleport the player to the stairs.
/// Brings player faction companions along and updates vision.
/// </summary>
public class StairsCommand : DebugCommand
{
	private const int CompanionTeleportRange = 3;

	public override string Name => "stairs";
	public override string Description => "Teleport to the stairs";
	public override string Usage => "stairs";

	public override DebugCommandResult Execute(DebugContext context, string[] args)
	{
		var player = context.ActionContext.Player;
		var entityManager = context.ActionContext.EntityManager;
		var mapSystem = context.ActionContext.MapSystem;

		// Find stairs in the entity manager
		var stairs = entityManager.GetAllEntities().FirstOrDefault(e => e is Stairs);

		if (stairs == null)
		{
			return DebugCommandResult.CreateFailure(
				"No stairs found on this floor!",
				Palette.ToHex(Palette.Danger)
			);
		}

		var stairsPos = stairs.GridPosition;

		// Teleport player to stairs position
		player.SetGridPosition(stairsPos);

		// Teleport companions nearby
		TeleportCompanionsNearby(player, stairsPos, context);

		// Update vision at new location
		context.VisionSystem?.ForceRecalculateVision();

		return DebugCommandResult.CreateSuccess(
			$"Teleported to stairs at ({stairsPos.X}, {stairsPos.Y})",
			Palette.ToHex(Palette.Success)
		);
	}

	private void TeleportCompanionsNearby(Player player, GridPosition targetPos, DebugContext context)
	{
		var entityManager = context.ActionContext.EntityManager;
		var mapSystem = context.ActionContext.MapSystem;

		// Find all player faction entities (excluding the player)
		var companions = entityManager.GetAllEntities()
			.Where(e => e != player && e.Faction == Faction.Player)
			.ToList();

		if (companions.Count == 0)
			return;

		// Find valid positions near the target
		var nearbyPositions = FindNearbyValidPositions(targetPos, CompanionTeleportRange, mapSystem, entityManager);

		// Teleport each companion to a nearby position
		foreach (var companion in companions)
		{
			if (nearbyPositions.Count == 0)
				break;

			int index = GD.RandRange(0, nearbyPositions.Count - 1);
			var companionNewPos = nearbyPositions[index];
			nearbyPositions.RemoveAt(index);

			companion.SetGridPosition(companionNewPos);
		}
	}

	private List<GridPosition> FindNearbyValidPositions(
		GridPosition center,
		int range,
		Systems.MapSystem mapSystem,
		Systems.EntityManager entityManager)
	{
		var validPositions = new List<GridPosition>();

		for (int dx = -range; dx <= range; dx++)
		{
			for (int dy = -range; dy <= range; dy++)
			{
				if (dx == 0 && dy == 0)
					continue;

				var checkPos = new GridPosition(center.X + dx, center.Y + dy);

				if (DistanceHelper.ChebyshevDistance(center, checkPos) > range)
					continue;

				if (!mapSystem.IsWalkable(checkPos))
					continue;

				if (entityManager.IsPositionOccupied(checkPos))
					continue;

				validPositions.Add(checkPos);
			}
		}

		return validPositions;
	}
}
