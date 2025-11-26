using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command to spawn a creature as a player ally at a targeted tile.
/// </summary>
public class AllyCommand : DebugCommand
{
    public override string Name => "ally";
    public override string Description => "Spawn a creature as an ally at a targeted tile";
    public override string Usage => "ally [creatureId]";

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

        // Only creature IDs for ally command
        var creatureIds = dataLoader.GetAllCreatureIds().OrderBy(id => id).ToList();

        if (string.IsNullOrEmpty(currentValue))
        {
            return creatureIds;
        }

        var lowerValue = currentValue.ToLower();
        return creatureIds.Where(id => id.Contains(lowerValue, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: ally [creatureId]",
                Palette.ToHex(Palette.Danger)
            );
        }

        string creatureId = args[0];
        var dataLoader = context.DataLoader;

        if (dataLoader == null)
        {
            return DebugCommandResult.CreateFailure(
                "DataLoader not available!",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Validate creature exists
        var creature = dataLoader.GetCreature(creatureId);
        if (creature == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown creature ID: {creatureId}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Return targeting request with makeAlly flag set
        return DebugCommandResult.CreateTargetingRequest(creatureId, SpawnEntityType.Creature, makeAlly: true);
    }
}
