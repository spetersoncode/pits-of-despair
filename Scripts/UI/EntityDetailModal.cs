using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Components;
using PitsOfDespair.Skills;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal for viewing entity details in examine mode.
/// </summary>
public partial class EntityDetailModal : CenterContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    // Approximate line width for the modal content area (monospace chars)
    private const int LineWidth = 52;

    private RichTextLabel _contentLabel;
    private bool _isVisible = false;

    /// <summary>
    /// Whether the modal is currently visible/open.
    /// </summary>
    public bool IsOpen => _isVisible;

    public override void _Ready()
    {
        _contentLabel = GetNode<RichTextLabel>("%ContentLabel");
        Hide();
    }

    /// <summary>
    /// Shows the modal with details for the specified entity.
    /// </summary>
    public void ShowDetail(BaseEntity entity)
    {
        if (entity == null)
        {
            GD.PrintErr("EntityDetailModal: Cannot show detail, entity is null");
            return;
        }

        _isVisible = true;
        Show();
        UpdateDisplay(entity);
    }

    /// <summary>
    /// Hides the modal.
    /// </summary>
    public void HideDetail()
    {
        _isVisible = false;
        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isVisible)
        {
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Close only on ESC
            if (MenuInputProcessor.IsCloseKey(keyEvent))
            {
                EmitSignal(SignalName.Cancelled);
                GetViewport().SetInputAsHandled();
            }
            else
            {
                // Block other inputs while modal is open
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void UpdateDisplay(BaseEntity entity)
    {
        if (_contentLabel == null) return;

        string glyph = $"[color={Palette.ToHex(entity.GlyphColor)}]{entity.Glyph}[/color]";
        string name = $"[color={Palette.ToHex(entity.GlyphColor)}]{entity.DisplayName}[/color]";

        // Health Status - get both plain text and colored version
        var (healthText, healthColor) = GetHealthStatusParts(entity);
        string healthStatus = $"[color={Palette.ToHex(healthColor)}]{healthText}[/color]";

        // Name and health on same line with space padding (monospace font)
        int leftLen = entity.Glyph.Length + 1 + entity.DisplayName.Length; // "G Name"
        int rightLen = healthText.Length;
        int padding = Math.Max(1, LineWidth - leftLen - rightLen);
        string headerRow = $"{glyph} {name}{new string(' ', padding)}{healthStatus}";

        // Description
        string description = string.IsNullOrEmpty(entity.Description)
            ? "No description available."
            : entity.Description;
        string descriptionSection = $"[color={Palette.ToHex(Palette.AshGray)}]{description}[/color]";

        // Build entity-specific sections
        string entitySpecificSection;
        if (entity is Player player)
        {
            entitySpecificSection = BuildPlayerDisplay(player);
        }
        else
        {
            entitySpecificSection = BuildCreatureDisplay(entity);
        }

        // Status (conditions + resistances/vulnerabilities) - shared by both
        string statusSection = BuildStatusDisplay(entity);

        // Commands
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        string commands = $"\n\n[color={Palette.ToHex(Palette.Disabled)}]Commands:[/color]\n";
        commands += $"[color={Palette.ToHex(Palette.Default)}]{closeKey}[/color] Close";

        string content = $"{headerRow}\n\n" +
                         $"{descriptionSection}" +
                         $"{entitySpecificSection}" +
                         $"{statusSection}" +
                         $"{commands}";

        _contentLabel.Text = content;
    }

    private (string text, Color color) GetHealthStatusParts(BaseEntity entity)
    {
        var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null) return ("Unknown", Palette.AshGray);

        if (entity.IsDead) return ("Dead", Palette.Crimson);

        float hpPercent = (float)health.CurrentHealth / health.MaxHealth;

        if (hpPercent >= 1.0f) return ("Uninjured", Palette.Success);
        if (hpPercent >= 0.75f) return ("Slightly Wounded", Palette.Success);
        if (hpPercent >= 0.5f) return ("Wounded", Palette.Caution);
        if (hpPercent >= 0.25f) return ("Severely Wounded", Palette.Alert);
        return ("Near Death", Palette.Crimson);
    }

    private string BuildStatusDisplay(BaseEntity entity)
    {
        var sentences = new List<string>();

        // Conditions with visible descriptions (deduplicated)
        var conditions = entity.GetActiveConditions()
            .Where(c => c.ExamineDescription != null)
            .Select(c => c.ExamineDescription!)
            .Distinct()
            .ToList();

        if (conditions.Count > 0)
        {
            string conditionList = FormatStringList(conditions);
            sentences.Add($"{conditionList}.");
        }

        // Resistances/Immunities/Vulnerabilities
        var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health != null)
        {
            if (health.Immunities.Count > 0)
            {
                string types = FormatDamageTypeList(health.Immunities);
                sentences.Add($"Immune to {types} damage.");
            }

            if (health.Resistances.Count > 0)
            {
                string types = FormatDamageTypeList(health.Resistances);
                sentences.Add($"Resistant to {types} damage.");
            }

            if (health.Vulnerabilities.Count > 0)
            {
                string types = FormatDamageTypeList(health.Vulnerabilities);
                sentences.Add($"Vulnerable to {types} damage.");
            }
        }

        if (sentences.Count == 0)
            return "";

        string combined = string.Join(" ", sentences);
        return $"\n\n[color={Palette.ToHex(Palette.AshGray)}]{combined}[/color]";
    }

    private string FormatDamageTypeList(List<DamageType> types)
    {
        var names = types.Select(t => t.ToString().ToLower()).ToList();
        return FormatStringList(names);
    }

    private string FormatStringList(List<string> items)
    {
        if (items.Count == 0)
            return "";
        if (items.Count == 1)
            return Capitalize(items[0]);
        if (items.Count == 2)
            return $"{Capitalize(items[0])} and {items[1]}";

        // Oxford comma for 3+
        return Capitalize(items[0]) + ", " + string.Join(", ", items.Skip(1).Take(items.Count - 2)) + ", and " + items.Last();
    }

    private string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s[1..];
    }

    #region Player Display

    /// <summary>
    /// Builds the player-specific display sections: attributes and skills.
    /// </summary>
    private string BuildPlayerDisplay(Player player)
    {
        var sb = new StringBuilder();

        var stats = player.GetNodeOrNull<StatsComponent>("StatsComponent");
        var skills = player.GetNodeOrNull<SkillComponent>("SkillComponent");

        // Attributes section with modifier breakdown
        if (stats != null)
        {
            sb.Append(BuildAttributesSection(stats, player));
        }

        // Skills section
        if (skills != null)
        {
            sb.Append(BuildSkillsSection(skills));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the attributes section with inline modifier breakdown.
    /// </summary>
    private string BuildAttributesSection(StatsComponent stats, Player player)
    {
        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]=== ATTRIBUTES ===[/color]");

        sb.Append(FormatStatWithModifiers("Strength", stats.BaseStrength, stats.TotalStrength, stats.GetStatModifiers(StatType.Strength), player));
        sb.Append(FormatStatWithModifiers("Agility", stats.BaseAgility, stats.TotalAgility, stats.GetStatModifiers(StatType.Agility), player));
        sb.Append(FormatStatWithModifiers("Endurance", stats.BaseEndurance, stats.TotalEndurance, stats.GetStatModifiers(StatType.Endurance), player));
        sb.Append(FormatStatWithModifiers("Will", stats.BaseWill, stats.TotalWill, stats.GetStatModifiers(StatType.Will), player));

        return sb.ToString();
    }

    /// <summary>
    /// Formats a stat line with its modifier sources.
    /// Example: "Strength: 5 (3), +2 Ring of Strength"
    /// </summary>
    private string FormatStatWithModifiers(string name, int baseValue, int totalValue, IReadOnlyDictionary<string, int> modifiers, Player player)
    {
        var sb = new StringBuilder();
        sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]{name}:[/color] {totalValue}");

        // Build modifier breakdown
        var parts = new List<string> { $"({baseValue})" };
        foreach (var mod in modifiers)
        {
            string sign = mod.Value > 0 ? "+" : "";
            string sourceName = FormatSourceId(mod.Key, player);
            parts.Add($"{sign}{mod.Value} {sourceName}");
        }
        sb.Append($" [color={Palette.ToHex(Palette.AshGray)}]{string.Join(", ", parts)}[/color]");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the skills section grouped by category.
    /// </summary>
    private string BuildSkillsSection(SkillComponent skills)
    {
        var dataLoader = GetNodeOrNull<DataLoader>("/root/DataLoader");
        if (dataLoader == null || skills.LearnedSkills.Count == 0)
            return "";

        var grouped = skills.GetLearnedSkillsByCategory(dataLoader);

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]=== SKILLS ===[/color]");

        // Active skills with keybindings
        if (grouped[SkillCategory.Active].Count > 0)
        {
            var activeNames = grouped[SkillCategory.Active]
                .Select(s =>
                {
                    var key = skills.GetSkillKey(s.Id);
                    return key.HasValue ? $"[{key.Value}] {s.Name}" : s.Name;
                });
            sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]Active:[/color] {string.Join(", ", activeNames)}");
        }

        // Passive skills
        if (grouped[SkillCategory.Passive].Count > 0)
        {
            var passiveNames = grouped[SkillCategory.Passive].Select(s => s.Name);
            sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]Passive:[/color] {string.Join(", ", passiveNames)}");
        }

        // Reactive skills
        if (grouped[SkillCategory.Reactive].Count > 0)
        {
            var reactiveNames = grouped[SkillCategory.Reactive].Select(s => s.Name);
            sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]Reactive:[/color] {string.Join(", ", reactiveNames)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts source IDs to readable names.
    /// Patterns:
    /// - "equipped_{slot}_{itemkey}" -> item name from inventory
    /// - "condition_{type}_{uuid}" -> formatted condition name (e.g., "strength potion")
    /// </summary>
    private static string FormatSourceId(string sourceId, Player player)
    {
        // Check if it's an equipment source: "equipped_{slot}_{key}"
        if (sourceId.StartsWith("equipped_"))
        {
            var parts = sourceId.Split('_');
            if (parts.Length >= 3)
            {
                char itemKey = parts[2][0];
                var slot = player.GetInventorySlot(itemKey);
                if (slot != null)
                {
                    return slot.Item.Template.Name;
                }
            }
        }

        // Check for condition-based sources: "condition_{type}_{uuid}"
        if (sourceId.StartsWith("condition_"))
        {
            // Extract the condition type (between "condition_" and the UUID)
            var withoutPrefix = sourceId["condition_".Length..];
            // Find the last underscore before the UUID (UUIDs have format like 8-4-4-4-12 chars)
            int lastUnderscore = withoutPrefix.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                var conditionType = withoutPrefix[..lastUnderscore];
                return FormatConditionType(conditionType);
            }
            return FormatConditionType(withoutPrefix);
        }

        // Default: clean up underscores and return
        return sourceId.Replace("_", " ");
    }

    /// <summary>
    /// Formats condition type IDs into readable names.
    /// </summary>
    private static string FormatConditionType(string conditionType)
    {
        return conditionType switch
        {
            "strength_modifier" or "strength" => "strength potion",
            "agility_modifier" or "agility" => "agility potion",
            "endurance_modifier" or "endurance" => "endurance potion",
            "will_modifier" or "will" => "will potion",
            "armor_modifier" or "armor" => "armor potion",
            "evasion_modifier" or "evasion" => "evasion buff",
            "speed_modifier" or "speed" => "speed effect",
            _ => conditionType.Replace("_modifier", "").Replace("_", " ")
        };
    }

    #endregion

    #region Creature Display

    /// <summary>
    /// Builds creature-specific display: intent and attack damage types.
    /// </summary>
    private static string BuildCreatureDisplay(BaseEntity entity)
    {
        var sb = new StringBuilder();

        // Intent (for creatures with AI)
        var ai = entity.GetNodeOrNull<AIComponent>("AIComponent");
        if (ai != null)
        {
            var intent = ai.GetIntent();
            var intentColor = IntentHelper.GetColor(intent);
            string intentName = intent.ToString();
            sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]Intent:[/color] ");
            sb.Append($"[color={Palette.ToHex(intentColor)}]{intentName}[/color]");
        }

        // Attacks with name and damage type
        var attacks = entity.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attacks != null && attacks.Attacks.Count > 0)
        {
            var attackDescriptions = attacks.Attacks
                .Select(a => $"{a.Name} ({a.DamageType.ToString().ToLower()})")
                .ToList();

            if (attackDescriptions.Count > 0)
            {
                string attackList = string.Join(", ", attackDescriptions);
                sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Attacks:[/color] ");
                sb.Append($"[color={Palette.ToHex(Palette.Default)}]{attackList}[/color]");
            }
        }

        return sb.ToString();
    }

    #endregion
}
