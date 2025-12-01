using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Components;
using PitsOfDespair.Helpers;
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

        var sb = new StringBuilder();
        string disabled = Palette.ToHex(Palette.Disabled);
        string entityColor = Palette.ToHex(entity.GlyphColor);

        // === HEADER (centered) ===
        string glyph = $"[color={entityColor}]{entity.Glyph}[/color]";
        string name = $"[color={entityColor}][b]{entity.DisplayName}[/b][/color]";
        sb.Append($"[center]{glyph}  {name}[/center]");
        sb.Append("\n");

        // === HEALTH STATUS LINE ===
        var (healthText, healthColor) = GetHealthStatusParts(entity);
        if (!string.IsNullOrEmpty(healthText))
        {
            sb.Append($"\n[center][color={Palette.ToHex(healthColor)}]{healthText}[/color][/center]");
        }

        // === DESCRIPTION ===
        if (!string.IsNullOrEmpty(entity.Description))
        {
            sb.Append($"\n\n[color={disabled}]DESCRIPTION[/color]");
            sb.Append($"\n[color={Palette.ToHex(Palette.AshGray)}][i]{entity.Description}[/i][/color]");
        }

        // === ENTITY-SPECIFIC SECTIONS ===
        if (entity is Player player)
        {
            sb.Append(BuildPlayerDisplay(player));
        }
        else
        {
            // Get player level for threat comparison
            int playerLevel = 1;
            var playerNode = GetTree().GetFirstNodeInGroup("player") as Player;
            if (playerNode != null)
            {
                var playerStats = playerNode.GetNodeOrNull<StatsComponent>("StatsComponent");
                if (playerStats != null)
                    playerLevel = playerStats.Level;
            }
            sb.Append(BuildCreatureDisplay(entity, playerLevel));
        }

        // === STATUS (conditions + resistances/vulnerabilities) ===
        sb.Append(BuildStatusDisplay(entity));

        // === CLOSE HINT ===
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        sb.Append($"\n\n[right][color={disabled}]{closeKey} to close[/color][/right]");

        _contentLabel.Text = sb.ToString();
    }

    private (string text, Color color) GetHealthStatusParts(BaseEntity entity)
    {
        // Skip health status for players (shown in side panel instead)
        if (entity is Player)
            return ("", Palette.Default);

        var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");

        // Items don't have health - return empty string
        if (health == null)
        {
            // Only show "Unknown" for creatures without health components (shouldn't happen)
            if (entity.ItemData != null)
                return ("", Palette.Default);
            return ("Unknown", Palette.AshGray);
        }

        if (entity.IsDead) return ("Dead", Palette.Crimson);

        var (text, color) = InjuryState.GetInjuryDisplay(health.CurrentHealth, health.MaxHealth);
        return (text, color);
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

        string disabled = Palette.ToHex(Palette.Disabled);
        string combined = string.Join(" ", sentences);
        return $"\n\n[color={disabled}]STATUS[/color]\n[color={Palette.ToHex(Palette.AshGray)}]{combined}[/color]";
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
    /// Builds the player-specific display sections: statistics and skills.
    /// </summary>
    private string BuildPlayerDisplay(Player player)
    {
        var sb = new StringBuilder();

        var stats = player.GetNodeOrNull<StatsComponent>("StatsComponent");
        var health = player.GetNodeOrNull<HealthComponent>("HealthComponent");
        var willpower = player.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
        var speed = player.GetNodeOrNull<SpeedComponent>("SpeedComponent");
        var skills = player.GetNodeOrNull<SkillComponent>("SkillComponent");

        // Primary statistics section
        if (stats != null)
        {
            sb.Append(BuildPrimaryStatisticsSection(stats, player));
        }

        // Secondary statistics section
        sb.Append(BuildSecondaryStatisticsSection(stats, health, willpower, speed, player));

        // Skills section
        if (skills != null)
        {
            sb.Append(BuildSkillsSection(skills));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the primary statistics section (STR, AGI, END, WIL).
    /// </summary>
    private string BuildPrimaryStatisticsSection(StatsComponent stats, Player player)
    {
        var sb = new StringBuilder();

        // No header label - merged with secondary stats
        sb.Append("\n");
        sb.Append(FormatStatWithModifiers("Strength", stats.BaseStrength, stats.TotalStrength, stats.GetStatModifiers(StatType.Strength), player));
        sb.Append(FormatStatWithModifiers("Agility", stats.BaseAgility, stats.TotalAgility, stats.GetStatModifiers(StatType.Agility), player));
        sb.Append(FormatStatWithModifiers("Endurance", stats.BaseEndurance, stats.TotalEndurance, stats.GetStatModifiers(StatType.Endurance), player));
        sb.Append(FormatStatWithModifiers("Will", stats.BaseWill, stats.TotalWill, stats.GetStatModifiers(StatType.Will), player));

        return sb.ToString();
    }

    /// <summary>
    /// Builds the secondary statistics section (Armor, Evasion, Speed, Melee Attack Speed, Regeneration).
    /// </summary>
    private string BuildSecondaryStatisticsSection(
        StatsComponent? stats,
        HealthComponent? health,
        WillpowerComponent? willpower,
        SpeedComponent? speed,
        Player player)
    {
        var sb = new StringBuilder();
        string disabled = Palette.ToHex(Palette.Disabled);
        string defaultColor = Palette.ToHex(Palette.Default);
        string gray = Palette.ToHex(Palette.AshGray);

        // No header label - merged with primary stats

        // Armor
        if (stats != null)
        {
            int totalArmor = stats.TotalArmor;
            var armorMods = stats.GetStatModifiers(StatType.Armor);
            sb.Append($"\n[color={disabled}]Armor:[/color] [color={defaultColor}]{totalArmor}[/color]");
            string armorBreakdown = FormatModifierBreakdown(armorMods, player);
            if (!string.IsNullOrEmpty(armorBreakdown))
            {
                sb.Append($" [color={gray}]{armorBreakdown}[/color]");
            }
        }

        // Evasion (base from AGI + modifiers)
        if (stats != null)
        {
            int totalEvasion = stats.TotalEvasion;
            var evasionMods = stats.GetStatModifiers(StatType.Evasion);
            sb.Append($"\n[color={disabled}]Evasion:[/color] [color={defaultColor}]{totalEvasion}[/color]");

            var parts = new List<string>();
            if (stats.TotalAgility != 0)
            {
                parts.Add($"{stats.TotalAgility} AGI");
            }
            foreach (var mod in evasionMods.Where(m => m.Value != 0))
            {
                string sign = mod.Value >= 0 ? "+" : "";
                string sourceName = FormatSourceId(mod.Key, player);
                parts.Add($"{sign}{mod.Value} {sourceName}");
            }
            if (parts.Count > 0)
            {
                sb.Append($" [color={gray}]{string.Join(", ", parts)}[/color]");
            }
        }

        // Regeneration
        if (health != null)
        {
            var (regenText, regenColor) = RegenStatus.GetRegenDisplay(health.BaseRegenRate);
            sb.Append($"\n[color={disabled}]Regeneration:[/color] [color={Palette.ToHex(regenColor)}]{regenText}[/color]");
        }

        // Speed
        if (speed != null)
        {
            var (speedText, speedColor) = SpeedStatus.GetCreatureSpeedDisplay(speed.EffectiveSpeed);
            sb.Append($"\n[color={disabled}]Speed:[/color] [color={Palette.ToHex(speedColor)}]{speedText}[/color]");
        }

        // Melee Attack Speed (player speed + weapon delay)
        var equipComponent = player.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (speed != null && equipComponent != null)
        {
            var meleeKey = equipComponent.GetEquippedKey(EquipmentSlot.MeleeWeapon);
            if (meleeKey.HasValue)
            {
                var slot = player.GetInventorySlot(meleeKey.Value);
                if (slot?.Item?.Template?.Attack != null)
                {
                    int weaponDelayCost = slot.Item.Template.Attack.GetDelayCost();
                    var (attackText, attackColor) = SpeedStatus.GetCombinedAttackSpeedDisplay(speed.EffectiveSpeed, weaponDelayCost);
                    sb.Append($"\n[color={disabled}]Melee Attack Speed:[/color] [color={Palette.ToHex(attackColor)}]{attackText}[/color]");
                }
            }
        }

        // Ranged Attack Speed (player speed + weapon delay)
        if (speed != null && equipComponent != null)
        {
            var rangedKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
            if (rangedKey.HasValue)
            {
                var slot = player.GetInventorySlot(rangedKey.Value);
                if (slot?.Item?.Template?.Attack != null)
                {
                    int weaponDelayCost = slot.Item.Template.Attack.GetDelayCost();
                    var (attackText, attackColor) = SpeedStatus.GetCombinedAttackSpeedDisplay(speed.EffectiveSpeed, weaponDelayCost);
                    sb.Append($"\n[color={disabled}]Ranged Attack Speed:[/color] [color={Palette.ToHex(attackColor)}]{attackText}[/color]");
                }
                else
                {
                    sb.Append($"\n[color={disabled}]Ranged Attack Speed:[/color] [color={defaultColor}]-[/color]");
                }
            }
            else
            {
                sb.Append($"\n[color={disabled}]Ranged Attack Speed:[/color] [color={defaultColor}]-[/color]");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats modifier breakdown without parens.
    /// Example: "+3 leather armor, +2 shield"
    /// </summary>
    private string FormatModifierBreakdown(IReadOnlyDictionary<string, int> modifiers, Player player)
    {
        var parts = new List<string>();
        foreach (var mod in modifiers.Where(m => m.Value != 0))
        {
            string sign = mod.Value >= 0 ? "+" : "";
            string sourceName = FormatSourceId(mod.Key, player);
            parts.Add($"{sign}{mod.Value} {sourceName}");
        }
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Formats a stat line with its modifier sources.
    /// Example: "Strength: 5 (3), +2 Ring of Strength"
    /// </summary>
    private string FormatStatWithModifiers(string name, int baseValue, int totalValue, IReadOnlyDictionary<string, int> modifiers, Player player)
    {
        string disabled = Palette.ToHex(Palette.Disabled);
        string defaultColor = Palette.ToHex(Palette.Default);
        string gray = Palette.ToHex(Palette.AshGray);

        var sb = new StringBuilder();
        sb.Append($"\n[color={disabled}]{name}:[/color] [color={defaultColor}]{totalValue}[/color]");

        // Build modifier breakdown
        var parts = new List<string> { $"({baseValue})" };
        foreach (var mod in modifiers)
        {
            string sign = mod.Value > 0 ? "+" : "";
            string sourceName = FormatSourceId(mod.Key, player);
            parts.Add($"{sign}{mod.Value} {sourceName}");
        }
        sb.Append($" [color={gray}]{string.Join(", ", parts)}[/color]");

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
        string disabled = Palette.ToHex(Palette.Disabled);
        string defaultColor = Palette.ToHex(Palette.Default);

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={disabled}]SKILLS[/color]");

        // Active skills with keybindings
        if (grouped[SkillCategory.Active].Count > 0)
        {
            var activeNames = grouped[SkillCategory.Active]
                .Select(s =>
                {
                    var key = skills.GetSkillKey(s.Id);
                    return key.HasValue ? $"[{key.Value}] {s.Name}" : s.Name;
                });
            sb.Append($"\n[color={disabled}]Active:[/color] [color={defaultColor}]{string.Join(", ", activeNames)}[/color]");
        }

        // Passive skills
        if (grouped[SkillCategory.Passive].Count > 0)
        {
            var passiveNames = grouped[SkillCategory.Passive].Select(s => s.Name);
            sb.Append($"\n[color={disabled}]Passive:[/color] [color={defaultColor}]{string.Join(", ", passiveNames)}[/color]");
        }

        // Reactive skills
        if (grouped[SkillCategory.Reactive].Count > 0)
        {
            var reactiveNames = grouped[SkillCategory.Reactive].Select(s => s.Name);
            sb.Append($"\n[color={disabled}]Reactive:[/color] [color={defaultColor}]{string.Join(", ", reactiveNames)}[/color]");
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
    /// Builds creature-specific display: threat, intent, speed, and attacks.
    /// </summary>
    private static string BuildCreatureDisplay(BaseEntity entity, int playerLevel)
    {
        var sb = new StringBuilder();
        string disabled = Palette.ToHex(Palette.Disabled);
        string defaultColor = Palette.ToHex(Palette.Default);

        var stats = entity.GetNodeOrNull<StatsComponent>("StatsComponent");
        var ai = entity.GetNodeOrNull<AIComponent>("AIComponent");
        var speed = entity.GetNodeOrNull<SpeedComponent>("SpeedComponent");
        var attacks = entity.GetNodeOrNull<AttackComponent>("AttackComponent");

        // Only show COMBAT section if there's something to display
        bool hasContent = stats != null || ai != null || speed != null || (attacks != null && attacks.Attacks.Count > 0);
        if (!hasContent)
            return "";

        sb.Append($"\n\n[color={disabled}]COMBAT[/color]");

        // Threat (relative to player level)
        if (stats != null && stats.Threat > 0)
        {
            var (threatText, threatColor) = ThreatStatus.GetThreatDisplay(stats.Threat, playerLevel);
            sb.Append($"\n[color={disabled}]Threat:[/color] [color={Palette.ToHex(threatColor)}]{threatText}[/color]");
        }

        // Intent
        if (ai != null)
        {
            var intent = ai.GetIntent();
            var intentColor = IntentHelper.GetColor(intent);
            sb.Append($"\n[color={disabled}]Intent:[/color] [color={Palette.ToHex(intentColor)}]{intent}[/color]");
        }

        // Speed
        if (speed != null)
        {
            var (speedText, speedColor) = SpeedStatus.GetCreatureSpeedDisplay(speed.EffectiveSpeed);
            sb.Append($"\n[color={disabled}]Speed:[/color] [color={Palette.ToHex(speedColor)}]{speedText}[/color]");
        }

        // Attacks
        if (attacks != null && attacks.Attacks.Count > 0)
        {
            var attackDescriptions = attacks.Attacks
                .Select(a => $"{a.Name} ({a.DamageType.ToString().ToLower()})")
                .ToList();

            string attackList = string.Join(", ", attackDescriptions);
            sb.Append($"\n[color={disabled}]Attacks:[/color] [color={defaultColor}]{attackList}[/color]");
        }

        return sb.ToString();
    }

    #endregion
}
