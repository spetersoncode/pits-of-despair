using System.Collections.Generic;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Effects.Composition;

/// <summary>
/// Accumulates messages during effect execution for batch emission.
/// Provides typed methods for common message patterns.
/// Tracks entity identity to correctly combine messages about different entities with the same name.
/// </summary>
public class MessageCollector
{
    private readonly List<(string Message, string Color, BaseEntity? Entity)> _messages = new();

    /// <summary>
    /// Current entity context for messages. Set this before adding messages about a specific entity.
    /// </summary>
    public BaseEntity? CurrentEntity { get; set; }

    /// <summary>
    /// Adds a generic message with optional color.
    /// </summary>
    public void Add(string message, string? color = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _messages.Add((message, color ?? Palette.ToHex(Palette.Default), CurrentEntity));
    }

    /// <summary>
    /// Adds a damage message for the target.
    /// </summary>
    public void AddDamage(BaseEntity target, int damage, DamageType damageType)
    {
        if (damage <= 0)
            return;

        string color = GetDamageColor(damageType);
        _messages.Add(($"The {target.DisplayName} takes {damage} damage!", color, target));
    }

    /// <summary>
    /// Adds a damage message with custom format.
    /// </summary>
    public void AddDamage(string message, DamageType damageType)
    {
        if (string.IsNullOrEmpty(message))
            return;

        string color = GetDamageColor(damageType);
        _messages.Add((message, color, CurrentEntity));
    }

    /// <summary>
    /// Adds a healing message for the target.
    /// </summary>
    public void AddHeal(BaseEntity target, int amount)
    {
        if (amount <= 0)
            return;

        _messages.Add((
            $"The {target.DisplayName} heals {amount} Health.",
            Palette.ToHex(Palette.Success),
            target
        ));
    }

    /// <summary>
    /// Adds a save resistance message.
    /// </summary>
    public void AddSaveResist(BaseEntity target)
    {
        _messages.Add((
            $"The {target.DisplayName} resists!",
            Palette.ToHex(Palette.Default),
            target
        ));
    }

    /// <summary>
    /// Adds a miss message for attack rolls.
    /// </summary>
    public void AddMiss(BaseEntity target, string? attackName = null)
    {
        string name = string.IsNullOrEmpty(attackName) ? "The attack" : attackName;
        _messages.Add((
            $"{name} misses the {target.DisplayName}!",
            Palette.ToHex(Palette.Disabled),
            target
        ));
    }

    /// <summary>
    /// Adds a condition applied message.
    /// </summary>
    public void AddConditionApplied(BaseEntity target, string message, string? color = null)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _messages.Add((message, color ?? Palette.ToHex(Palette.StatusDebuff), target));
    }

    /// <summary>
    /// Emits all accumulated messages via the combat system.
    /// </summary>
    public void Emit(BaseEntity target, EffectContext context)
    {
        var combatSystem = context.ActionContext.CombatSystem;
        if (combatSystem == null)
            return;

        foreach (var (message, color, _) in _messages)
        {
            combatSystem.EmitActionMessage(target, message, color);
        }
    }

    /// <summary>
    /// Emits messages combined by subject for better narrative flow.
    /// Groups consecutive messages about the same entity and stitches them together.
    /// Example: "Goblin takes 5 damage!" + "Goblin is knocked back!" = "Goblin takes 5 damage and is knocked back!"
    /// </summary>
    public void EmitCombined(BaseEntity target, EffectContext context)
    {
        var combatSystem = context.ActionContext.CombatSystem;
        if (combatSystem == null || _messages.Count == 0)
            return;

        // Group consecutive messages by subject
        var groups = GroupMessagesBySubject();

        foreach (var group in groups)
        {
            if (group.Messages.Count == 0)
                continue;

            if (group.Messages.Count == 1)
            {
                // Single message, emit as-is
                combatSystem.EmitActionMessage(target, group.Messages[0].Message, group.Messages[0].Color);
            }
            else
            {
                // Multiple messages about same subject - combine them
                string combined = CombineMessages(group.Subject, group.Messages);
                // Use the color of the most "important" message (damage > condition > default)
                string color = GetPriorityColor(group.Messages);
                combatSystem.EmitActionMessage(target, combined, color);
            }
        }
    }

    /// <summary>
    /// Groups consecutive messages by entity identity (not just name).
    /// This correctly handles multiple entities with the same DisplayName.
    /// </summary>
    private List<MessageGroup> GroupMessagesBySubject()
    {
        var groups = new List<MessageGroup>();
        if (_messages.Count == 0)
            return groups;

        MessageGroup? currentGroup = null;

        foreach (var (message, color, entity) in _messages)
        {
            string subject = ExtractSubject(message);

            // Group by entity identity if available, otherwise fall back to subject string
            bool sameGroup = currentGroup != null &&
                currentGroup.Subject == subject &&
                (currentGroup.Entity == entity || (currentGroup.Entity == null && entity == null));

            if (!sameGroup)
            {
                // Start a new group
                currentGroup = new MessageGroup { Subject = subject, Entity = entity };
                groups.Add(currentGroup);
            }

            currentGroup.Messages.Add((message, color));
        }

        return groups;
    }

    /// <summary>
    /// Extracts the subject (entity name) from a message.
    /// Assumes messages start with "EntityName verb..." or "The EntityName verb..."
    /// </summary>
    private static string ExtractSubject(string message)
    {
        if (string.IsNullOrEmpty(message))
            return "";

        // Handle "The X" prefix
        string working = message;
        if (working.StartsWith("The "))
            working = working.Substring(4);

        // Find the first verb-like break (space followed by common verbs or "is/are/has/takes/crashes/slams")
        string[] verbIndicators = { " is ", " are ", " has ", " takes ", " crashes ", " slams ", " heals ", " resists" };

        foreach (var verb in verbIndicators)
        {
            int idx = working.IndexOf(verb, System.StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                return working.Substring(0, idx);
            }
        }

        // Fallback: take first two words or up to first space
        int spaceIdx = working.IndexOf(' ');
        if (spaceIdx > 0)
            return working.Substring(0, spaceIdx);

        return working;
    }

    /// <summary>
    /// Combines multiple messages about the same subject into one narrative sentence.
    /// </summary>
    private static string CombineMessages(string subject, List<(string Message, string Color)> messages)
    {
        if (messages.Count == 0)
            return "";

        if (messages.Count == 1)
            return messages[0].Message;

        // Check if first message starts with "The " to preserve it
        bool hasThePrefix = messages[0].Message.StartsWith("The ");

        // Extract the action part of each message (remove subject and "The " prefix)
        var actions = new List<string>();
        foreach (var (message, _) in messages)
        {
            string action = ExtractAction(message, subject);
            if (!string.IsNullOrEmpty(action))
                actions.Add(action);
        }

        if (actions.Count == 0)
            return messages[0].Message;

        // Build combined message: "The Subject action1, action2, and action3!"
        var result = new System.Text.StringBuilder();
        if (hasThePrefix)
            result.Append("The ");
        result.Append(subject);
        result.Append(' ');

        for (int i = 0; i < actions.Count; i++)
        {
            string action = actions[i];

            // Remove trailing punctuation for combining
            action = action.TrimEnd('!', '.');

            if (i == 0)
            {
                result.Append(action);
            }
            else if (i == actions.Count - 1)
            {
                // Oxford comma: use ", and" for 3+ items, just " and" for 2 items
                if (actions.Count > 2)
                    result.Append(", and ");
                else
                    result.Append(" and ");
                result.Append(action);
            }
            else
            {
                result.Append(", ");
                result.Append(action);
            }
        }

        result.Append('!');
        return result.ToString();
    }

    /// <summary>
    /// Extracts the action part of a message (everything after the subject).
    /// </summary>
    private static string ExtractAction(string message, string subject)
    {
        string working = message;

        // Remove "The " prefix if present
        if (working.StartsWith("The "))
            working = working.Substring(4);

        // Remove subject from start
        if (working.StartsWith(subject, System.StringComparison.OrdinalIgnoreCase))
        {
            working = working.Substring(subject.Length).TrimStart();
        }

        return working;
    }

    /// <summary>
    /// Gets the highest priority color from a group of messages.
    /// Priority: damage colors > condition colors > default
    /// </summary>
    private static string GetPriorityColor(List<(string Message, string Color)> messages)
    {
        // Damage color is highest priority
        string damageColor = Palette.ToHex(Palette.CombatDamage);
        string debuffColor = Palette.ToHex(Palette.StatusDebuff);

        string? bestColor = null;
        int bestPriority = 0;

        foreach (var (_, color) in messages)
        {
            int priority = 1; // default

            if (color == damageColor || color == Palette.ToHex(Palette.Fire) ||
                color == Palette.ToHex(Palette.Ice) || color == Palette.ToHex(Palette.Lightning) ||
                color == Palette.ToHex(Palette.Acid) || color == Palette.ToHex(Palette.Poison))
            {
                priority = 3; // damage
            }
            else if (color == debuffColor)
            {
                priority = 2; // condition
            }

            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestColor = color;
            }
        }

        return bestColor ?? Palette.ToHex(Palette.Default);
    }

    private class MessageGroup
    {
        public string Subject { get; set; } = "";
        public BaseEntity? Entity { get; set; }
        public List<(string Message, string Color)> Messages { get; } = new();
    }

    /// <summary>
    /// Gets the count of accumulated messages.
    /// </summary>
    public int Count => _messages.Count;

    /// <summary>
    /// Checks if any messages have been accumulated.
    /// </summary>
    public bool HasMessages => _messages.Count > 0;

    /// <summary>
    /// Clears all accumulated messages.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
    }

    /// <summary>
    /// Gets the appropriate color for a damage type.
    /// </summary>
    private static string GetDamageColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Fire => Palette.ToHex(Palette.Fire),
            DamageType.Cold => Palette.ToHex(Palette.Ice),
            DamageType.Lightning => Palette.ToHex(Palette.Lightning),
            DamageType.Acid => Palette.ToHex(Palette.Acid),
            DamageType.Poison => Palette.ToHex(Palette.Poison),
            DamageType.Necrotic => Palette.ToHex(Palette.Crimson),
            _ => Palette.ToHex(Palette.CombatDamage)
        };
    }
}
