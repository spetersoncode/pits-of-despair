using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Skills;

namespace PitsOfDespair.Debug.Commands;

/// <summary>
/// Debug command for managing skills.
/// Subcommands: learn, list, available
/// </summary>
public class SkillCommand : DebugCommand
{
    public override string Name => "skill";
    public override string Description => "Manage player skills";
    public override string Usage => "skill [learn|list|available] [skillId]";

    private static readonly string[] Subcommands = { "learn", "list", "available" };

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
            _ => DebugCommandResult.CreateFailure(
                $"Unknown subcommand: {subcommand}. Use learn, list, or available.",
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
}
