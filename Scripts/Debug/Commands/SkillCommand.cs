using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Skills;

using PitsOfDespair.Targeting;
using TargetingType = PitsOfDespair.Targeting.TargetingType;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command for managing skills.
/// Subcommands: learn, list, available, use
/// </summary>
public class SkillCommand : DebugCommand
{
    public override string Name => "skill";
    public override string Description => "Manage player skills";
    public override string Usage => "skill [learn|list|available|use] [skillId]";

    private static readonly string[] Subcommands = { "learn", "list", "available", "use" };

    public override IReadOnlyList<string> GetArgumentSuggestions(int argIndex, string currentValue)
    {
        if (argIndex == 0)
        {
            if (string.IsNullOrEmpty(currentValue))
                return Subcommands;

            return Subcommands.Where(s => s.StartsWith(currentValue, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // For "learn" subcommand, suggest skill IDs
        if (argIndex == 1)
        {
            var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
            if (dataLoader == null)
                return null;

            var allSkills = dataLoader.GetAllSkillIds().OrderBy(id => id).ToList();

            if (string.IsNullOrEmpty(currentValue))
                return allSkills;

            return allSkills.Where(id => id.StartsWith(currentValue, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return null;
    }

    public override DebugCommandResult Execute(DebugContext context, string[] args)
    {
        if (args.Length == 0)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: skill [learn|list|available] [skillId]",
                Palette.ToHex(Palette.Danger)
            );
        }

        string subcommand = args[0].ToLower();

        return subcommand switch
        {
            "learn" => ExecuteLearn(context, args),
            "list" => ExecuteList(context),
            "available" => ExecuteAvailable(context),
            "use" => ExecuteUse(context, args),
            _ => DebugCommandResult.CreateFailure(
                $"Unknown subcommand: {subcommand}. Use learn, list, available, or use.",
                Palette.ToHex(Palette.Danger)
            )
        };
    }

    private DebugCommandResult ExecuteLearn(DebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: skill learn [skillId]",
                Palette.ToHex(Palette.Danger)
            );
        }

        string skillId = args[1];
        var player = context.ActionContext.Player;
        var skillComponent = player.GetNodeOrNull<SkillComponent>("SkillComponent");
        var statsComponent = player.GetNodeOrNull<StatsComponent>("StatsComponent");

        if (skillComponent == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player has no SkillComponent!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
        {
            return DebugCommandResult.CreateFailure(
                "DataLoader not found!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var skill = dataLoader.GetSkill(skillId);
        if (skill == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown skill ID: {skillId}",
                Palette.ToHex(Palette.Danger)
            );
        }

        if (skillComponent.HasSkill(skillId))
        {
            return DebugCommandResult.CreateFailure(
                $"Already know skill: {skill.Name}",
                Palette.ToHex(Palette.Caution)
            );
        }

        // Check prerequisites (for info, but allow learning anyway in debug)
        bool meetsPrereqs = PrerequisiteChecker.MeetsPrerequisites(skill, statsComponent);
        string prereqWarning = meetsPrereqs ? "" : " [color=#ffaa00](prereqs not met)[/color]";

        skillComponent.LearnSkill(skillId);

        return DebugCommandResult.CreateSuccess(
            $"Learned [b]{skill.Name}[/b]{prereqWarning}",
            Palette.ToHex(Palette.Success)
        );
    }

    private DebugCommandResult ExecuteList(DebugContext context)
    {
        var player = context.ActionContext.Player;
        var skillComponent = player.GetNodeOrNull<SkillComponent>("SkillComponent");

        if (skillComponent == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player has no SkillComponent!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
        {
            return DebugCommandResult.CreateFailure(
                "DataLoader not found!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var learned = skillComponent.LearnedSkills;
        if (learned.Count == 0)
        {
            return DebugCommandResult.CreateSuccess(
                "No skills learned yet.",
                Palette.ToHex(Palette.Default)
            );
        }

        var lines = new List<string> { $"Learned skills ({learned.Count}):" };
        foreach (var skillId in learned)
        {
            var skill = dataLoader.GetSkill(skillId);
            if (skill != null)
            {
                lines.Add($"  {skill.GetCategoryIndicator()} {skill.Name} - {skill.Description}");
            }
        }

        return DebugCommandResult.CreateSuccess(
            string.Join("\n", lines),
            Palette.ToHex(Palette.Default)
        );
    }

    private DebugCommandResult ExecuteAvailable(DebugContext context)
    {
        var player = context.ActionContext.Player;
        var skillComponent = player.GetNodeOrNull<SkillComponent>("SkillComponent");
        var statsComponent = player.GetNodeOrNull<StatsComponent>("StatsComponent");

        if (skillComponent == null || statsComponent == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player missing SkillComponent or StatsComponent!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
        {
            return DebugCommandResult.CreateFailure(
                "DataLoader not found!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var available = skillComponent.GetAvailableSkills(statsComponent, dataLoader);
        if (available.Count == 0)
        {
            return DebugCommandResult.CreateSuccess(
                "No skills available to learn (check stat requirements).",
                Palette.ToHex(Palette.Default)
            );
        }

        var lines = new List<string> { $"Available skills ({available.Count}):" };
        foreach (var skill in available.OrderBy(s => s.Tier).ThenBy(s => s.Name))
        {
            string prereqs = skill.Prerequisites.IsUniversal() ? "" : $" ({skill.GetPrerequisiteString()})";
            lines.Add($"  {skill.Id}: {skill.Name}{prereqs}");
        }

        return DebugCommandResult.CreateSuccess(
            string.Join("\n", lines),
            Palette.ToHex(Palette.Default)
        );
    }

    private DebugCommandResult ExecuteUse(DebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return DebugCommandResult.CreateFailure(
                "Usage: skill use [skillId]",
                Palette.ToHex(Palette.Danger)
            );
        }

        string skillId = args[1];
        var player = context.ActionContext.Player;
        var skillComponent = player.GetNodeOrNull<SkillComponent>("SkillComponent");
        var willpowerComponent = player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        if (skillComponent == null)
        {
            return DebugCommandResult.CreateFailure(
                "Player has no SkillComponent!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
        {
            return DebugCommandResult.CreateFailure(
                "DataLoader not found!",
                Palette.ToHex(Palette.Danger)
            );
        }

        var skill = dataLoader.GetSkill(skillId);
        if (skill == null)
        {
            return DebugCommandResult.CreateFailure(
                $"Unknown skill ID: {skillId}",
                Palette.ToHex(Palette.Danger)
            );
        }

        // Check if skill is learned
        if (!skillComponent.HasSkill(skillId))
        {
            return DebugCommandResult.CreateFailure(
                $"Skill not learned: {skill.Name}. Use 'skill learn {skillId}' first.",
                Palette.ToHex(Palette.Caution)
            );
        }

        // Check WP cost
        if (willpowerComponent != null && willpowerComponent.CurrentWillpower < skill.WillpowerCost)
        {
            return DebugCommandResult.CreateFailure(
                $"Not enough Willpower ({skill.WillpowerCost} required, have {willpowerComponent.CurrentWillpower}).",
                Palette.ToHex(Palette.Caution)
            );
        }

        // Resolve targets based on targeting type
        var definition = TargetingDefinition.FromSkill(skill);
        var targetingHandler = TargetingHandler.CreateForDefinition(definition);
        List<BaseEntity> targets;

        if (skill.GetTargetingType() == TargetingType.Self)
        {
            targets = new List<BaseEntity> { player };
        }
        else
        {
            // For debug, find nearest enemy for enemy-targeting skills
            targets = FindDebugTargets(player, skill, definition, targetingHandler, context);
        }

        if (targets.Count == 0 && skill.GetTargetingType() != TargetingType.Self)
        {
            return DebugCommandResult.CreateFailure(
                $"No valid targets found for {skill.Name}.",
                Palette.ToHex(Palette.Caution)
            );
        }

        // Execute the skill
        var result = SkillExecutor.ExecuteSkill(player, skill, targets, context.ActionContext);

        if (result.Success)
        {
            string targetInfo = targets.Count > 0 ? $" on {string.Join(", ", targets.Select(t => t.DisplayName))}" : "";
            return DebugCommandResult.CreateSuccess(
                $"Used [b]{skill.Name}[/b]{targetInfo}\n{result.GetCombinedMessage()}",
                Palette.ToHex(Palette.Success)
            );
        }
        else
        {
            return DebugCommandResult.CreateFailure(
                $"Failed to use {skill.Name}: {result.GetCombinedMessage()}",
                Palette.ToHex(Palette.Danger)
            );
        }
    }

    private List<BaseEntity> FindDebugTargets(
        BaseEntity player,
        SkillDefinition skill,
        TargetingDefinition definition,
        TargetingHandler handler,
        DebugContext context)
    {
        var validPositions = handler.GetValidTargetPositions(player, definition, context.ActionContext);

        if (validPositions.Count == 0)
            return new List<BaseEntity>();

        // Sort by distance and return entities at closest valid positions
        var playerPos = player.GridPosition;
        var sortedPositions = validPositions
            .OrderBy(p => DistanceHelper.ChebyshevDistance(playerPos, p))
            .ToList();

        // For enemy targeting, find the closest enemy
        foreach (var pos in sortedPositions)
        {
            var entity = context.ActionContext.EntityManager.GetEntityAtPosition(pos);
            if (entity != null)
            {
                return handler.GetAffectedEntities(player, pos, definition, context.ActionContext);
            }
        }

        // If no entity found but positions exist (e.g., tile targeting), return empty
        // or handle based on targeting type
        if (skill.GetTargetingType() == TargetingType.Tile && sortedPositions.Count > 0)
        {
            // For tile targeting without a target entity, still return the position's entities
            return handler.GetAffectedEntities(player, sortedPositions[0], definition, context.ActionContext);
        }

        return new List<BaseEntity>();
    }
}
