using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to spawn a creature or item at a targeted tile.
/// </summary>
public class SpawnCommand : DebugCommand
{
    public override string Name => "spawn";
    public override string Description => "Spawn a creature or item at a targeted tile";
    public override string Usage => "spawn [entityId]";

    public override IReadOnlyList<string> GetArgumentSuggestions(int argIndex, string currentValue)
    {
        if (argIndex != 0)
        {
            return null;
        }

        var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
        {
            return null;
        }

        // Combine creature and item IDs
        var creatureIds = dataLoader.GetAllCreatureIds().Select(id => id);
        var itemIds = dataLoader.GetAllItemIds().Select(id => id);
        var allIds = creatureIds.Concat(itemIds).OrderBy(id => id).ToList();

        if (string.IsNullOrEmpty(currentValue))
        {
            return allIds;
        }

        var lowerValue = currentValue.ToLower();
        return allIds.Where(id => id.Contains(lowerValue, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: spawn [entityId]",
                Palette.ToHex(Palette.Danger)
            );
        }

        string entityId = args[0];
        var dataLoader = context.DataLoader;

        if (dataLoader == null)
        {
            return DebugCommandResult.CreateFailure(
                "DataLoader not available!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Check if it's a creature
        var creature = dataLoader.GetCreature(entityId);
        if (creature != null)
        {
            return DebugCommandResult.CreateTargetingRequest(entityId, SpawnEntityType.Creature);
        }

        // Check if it's an item
        var item = dataLoader.GetItem(entityId);
        if (item != null)
        {
            return DebugCommandResult.CreateTargetingRequest(entityId, SpawnEntityType.Item);
        }

        return DebugCommandResult.CreateFailure(
            $"Unknown entity ID: {entityId}",
            Palette.ToHex(Palette.Danger)
        );
    }
}
