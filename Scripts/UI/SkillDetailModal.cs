using Godot;
using System.Collections.Generic;
using System.Text;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Skills;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal for viewing detailed skill information.
/// Shows flavor text, description, costs, effects, and category-specific details.
/// </summary>
public partial class SkillDetailModal : CenterContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    private RichTextLabel _contentLabel;
    private DataLoader _dataLoader;
    private bool _isVisible = false;
    private SkillDefinition _currentSkill;
    private char? _currentHotkey;

    public override void _Ready()
    {
        _contentLabel = GetNode<RichTextLabel>("%ContentLabel");
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
        Hide();
    }

    /// <summary>
    /// Shows the modal with details for the specified skill.
    /// </summary>
    public void ShowSkill(string skillId, char? hotkey = null)
    {
        if (_dataLoader == null)
        {
            GD.PrintErr("SkillDetailModal: DataLoader not available");
            return;
        }

        var skill = _dataLoader.Skills.Get(skillId);
        if (skill == null)
        {
            GD.PrintErr($"SkillDetailModal: Skill '{skillId}' not found");
            return;
        }

        ShowSkill(skill, hotkey);
    }

    /// <summary>
    /// Shows the modal with details for the specified skill definition.
    /// </summary>
    public void ShowSkill(SkillDefinition skill, char? hotkey = null)
    {
        _currentSkill = skill;
        _currentHotkey = hotkey;
        _isVisible = true;
        Show();
        UpdateDisplay();
    }

    /// <summary>
    /// Hides the modal.
    /// </summary>
    public void HideModal()
    {
        _isVisible = false;
        _currentSkill = null;
        _currentHotkey = null;
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
            HandleKeyInput(keyEvent);
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        // Cancel on modal close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Block all other keys
        GetViewport().SetInputAsHandled();
    }

    private void UpdateDisplay()
    {
        if (_currentSkill == null || _contentLabel == null)
        {
            return;
        }

        _contentLabel.Text = BuildDisplay(_currentSkill);
    }

    private string BuildDisplay(SkillDefinition skill)
    {
        var sb = new StringBuilder();

        sb.Append(BuildHeader(skill));
        sb.Append(BuildFlavorSection(skill));
        sb.Append(BuildDescriptionSection(skill));
        sb.Append(BuildCostSection(skill));
        sb.Append(BuildTargetingSection(skill));

        // Category-specific sections
        var category = skill.GetCategory();
        switch (category)
        {
            case SkillCategory.Reactive:
                sb.Append(BuildTriggerSection(skill));
                break;
            case SkillCategory.Toggle:
                sb.Append(BuildToggleSection(skill));
                break;
            case SkillCategory.Improvement:
                sb.Append(BuildImprovementSection(skill));
                break;
        }

        sb.Append(BuildEffectsSection(skill));
        sb.Append(BuildPrerequisitesSection(skill));
        sb.Append(BuildCloseHint());

        return sb.ToString();
    }

    private string BuildHeader(SkillDefinition skill)
    {
        var category = skill.GetCategory();
        string categoryColor = GetCategoryColor(category);
        string categoryIndicator = skill.GetCategoryIndicator();

        var sb = new StringBuilder();

        // Centered header: glyph + name + category
        sb.Append($"[center][color={categoryColor}]★[/color]  ");
        sb.Append($"[color={categoryColor}][b]{skill.Name}[/b][/color]  ");
        sb.Append($"[color={Palette.ToHex(Palette.Disabled)}]{categoryIndicator}[/color][/center]");
        sb.AppendLine();

        // Meta line: hotkey if available
        if (_currentHotkey.HasValue)
        {
            sb.Append($"\n[center][color={Palette.ToHex(Palette.Disabled)}]Hotkey:[/color] ");
            sb.Append($"[color={Palette.ToHex(Palette.Default)}]{_currentHotkey.Value}[/color][/center]");
        }

        return sb.ToString();
    }

    private static string BuildFlavorSection(SkillDefinition skill)
    {
        if (string.IsNullOrEmpty(skill.Flavor))
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]FLAVOR[/color]");
        sb.Append($"\n[color={Palette.ToHex(Palette.AshGray)}][i]{skill.Flavor}[/i][/color]");
        return sb.ToString();
    }

    private static string BuildDescriptionSection(SkillDefinition skill)
    {
        if (string.IsNullOrEmpty(skill.Description))
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]DESCRIPTION[/color]");
        sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]{skill.Description}[/color]");
        return sb.ToString();
    }

    private static string BuildCostSection(SkillDefinition skill)
    {
        var category = skill.GetCategory();

        // Passive and improvement skills don't have costs
        if (category == SkillCategory.Passive || category == SkillCategory.Improvement)
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]COST[/color]");

        // Willpower cost
        string wpCost = skill.WillpowerCost > 0
            ? $"[color={Palette.ToHex(Palette.Wizard)}]{skill.WillpowerCost} WP[/color]"
            : $"[color={Palette.ToHex(Palette.Success)}]Free[/color]";
        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Willpower:[/color] {wpCost}");

        // Delay cost
        string delayText = skill.DelayCost switch
        {
            0 => "Instant",
            < 10 => $"Fast ({skill.DelayCost} aut)",
            10 => "Standard (10 aut)",
            > 10 => $"Slow ({skill.DelayCost} aut)"
        };
        string delayColor = skill.DelayCost switch
        {
            0 => Palette.ToHex(Palette.SpeedVeryFast),
            < 10 => Palette.ToHex(Palette.SpeedFast),
            10 => Palette.ToHex(Palette.SpeedAverage),
            _ => Palette.ToHex(Palette.SpeedSlow)
        };
        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Time:[/color] [color={delayColor}]{delayText}[/color]");

        return sb.ToString();
    }

    private static string BuildTargetingSection(SkillDefinition skill)
    {
        // Skip for self-targeting skills
        if (skill.IsSelfTargeting())
            return "";

        var category = skill.GetCategory();
        // Passive and reactive don't have manual targeting
        if (category == SkillCategory.Passive || category == SkillCategory.Reactive || category == SkillCategory.Improvement)
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]TARGETING[/color]");

        string targetType = FormatTargetingType(skill.Targeting);
        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Type:[/color] [color={Palette.ToHex(Palette.Default)}]{targetType}[/color]");

        if (skill.Range > 0)
        {
            sb.Append($"    [color={Palette.ToHex(Palette.Disabled)}]Range:[/color] [color={Palette.ToHex(Palette.Default)}]{skill.Range} tiles[/color]");
        }

        if (skill.Radius > 0)
        {
            sb.Append($"    [color={Palette.ToHex(Palette.Disabled)}]Radius:[/color] [color={Palette.ToHex(Palette.Default)}]{skill.Radius} tiles[/color]");
        }

        return sb.ToString();
    }

    private static string BuildTriggerSection(SkillDefinition skill)
    {
        if (skill.GetCategory() != SkillCategory.Reactive)
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]TRIGGER[/color]");

        string trigger = !string.IsNullOrEmpty(skill.Trigger)
            ? FormatTriggerName(skill.Trigger)
            : "Unknown";
        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Condition:[/color] [color={Palette.ToHex(Palette.Caution)}]{trigger}[/color]");

        if (skill.TriggerCost > 0)
        {
            sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Trigger Cost:[/color] [color={Palette.ToHex(Palette.Wizard)}]{skill.TriggerCost} WP[/color]");
        }

        string autoTrigger = skill.AutoTrigger ? "Yes" : "No (prompts)";
        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Auto-triggers:[/color] [color={Palette.ToHex(Palette.Default)}]{autoTrigger}[/color]");

        return sb.ToString();
    }

    private static string BuildToggleSection(SkillDefinition skill)
    {
        if (skill.GetCategory() != SkillCategory.Toggle || skill.Toggle == null)
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]TOGGLE EFFECTS[/color]");
        sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]When active:[/color]");

        foreach (var modifier in skill.Toggle.Modifiers)
        {
            string sign = modifier.Amount > 0 ? "+" : "";
            string statName = FormatStatName(modifier.Stat);
            string color = modifier.Amount > 0 ? Palette.ToHex(Palette.StatusBuff) : Palette.ToHex(Palette.StatusDebuff);
            sb.Append($"\n  [color={color}]{sign}{modifier.Amount} {statName}[/color]");
        }

        if (skill.Toggle.WpDrainPerTurn > 0)
        {
            sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]WP drain:[/color] [color={Palette.ToHex(Palette.Wizard)}]{skill.Toggle.WpDrainPerTurn}/turn[/color]");
        }

        return sb.ToString();
    }

    private string BuildImprovementSection(SkillDefinition skill)
    {
        if (skill.GetCategory() != SkillCategory.Improvement || skill.Improves == null)
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]IMPROVES[/color]");

        // Get target skill name
        string targetId = skill.Improves.TargetSkill;
        var targetSkill = _dataLoader?.Skills.Get(targetId);
        string targetName = targetSkill?.Name ?? targetId;

        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Target:[/color] [color={Palette.ToHex(Palette.Cyan)}]{targetName}[/color]");

        var mods = skill.Improves.Modifiers;
        if (mods.HasModifiers())
        {
            sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]Modifiers:[/color]");

            if (mods.Damage != 0)
            {
                string sign = mods.Damage > 0 ? "+" : "";
                sb.Append($"\n  [color={Palette.ToHex(Palette.StatusBuff)}]{sign}{mods.Damage} Damage[/color]");
            }
            if (mods.Range != 0)
            {
                string sign = mods.Range > 0 ? "+" : "";
                sb.Append($"\n  [color={Palette.ToHex(Palette.StatusBuff)}]{sign}{mods.Range} Range[/color]");
            }
            if (mods.Radius != 0)
            {
                string sign = mods.Radius > 0 ? "+" : "";
                sb.Append($"\n  [color={Palette.ToHex(Palette.StatusBuff)}]{sign}{mods.Radius} Radius[/color]");
            }
            if (mods.WillpowerCost != 0)
            {
                string sign = mods.WillpowerCost > 0 ? "+" : "";
                string color = mods.WillpowerCost < 0 ? Palette.ToHex(Palette.StatusBuff) : Palette.ToHex(Palette.StatusDebuff);
                sb.Append($"\n  [color={color}]{sign}{mods.WillpowerCost} WP Cost[/color]");
            }
            if (mods.KnockbackDistance != 0)
            {
                string sign = mods.KnockbackDistance > 0 ? "+" : "";
                sb.Append($"\n  [color={Palette.ToHex(Palette.StatusBuff)}]{sign}{mods.KnockbackDistance} Knockback[/color]");
            }
        }

        return sb.ToString();
    }

    private static string BuildEffectsSection(SkillDefinition skill)
    {
        if (skill.Effects == null || skill.Effects.Count == 0)
            return "";

        var effects = new List<string>();

        foreach (var effect in skill.Effects)
        {
            string desc = DescribeEffect(effect);
            if (!string.IsNullOrEmpty(desc))
                effects.Add(desc);
        }

        if (effects.Count == 0)
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]EFFECTS[/color]");
        foreach (var effect in effects)
        {
            sb.Append($"\n[color={Palette.ToHex(Palette.Default)}]• {effect}[/color]");
        }

        return sb.ToString();
    }

    private static string DescribeEffect(SkillEffectDefinition effect)
    {
        return effect.Type.ToLower() switch
        {
            "damage" => DescribeDamageEffect(effect),
            "heal" => DescribeHealEffect(effect),
            "apply_condition" => DescribeConditionEffect(effect),
            "stat_bonus" => DescribeStatBonusEffect(effect),
            "knockback" => $"Knocks target back {effect.Amount} tiles",
            "teleport" => "Teleports to target location",
            "blink" => $"Short-range teleport (up to {effect.Amount} tiles)",
            _ => ""
        };
    }

    private static string DescribeDamageEffect(SkillEffectDefinition effect)
    {
        string dice = effect.Dice ?? $"{effect.Amount}";
        string damageType = effect.DamageType?.ToLower() ?? "physical";

        var sb = new StringBuilder($"Deals {dice} {damageType} damage");

        if (!string.IsNullOrEmpty(effect.ScalingStat))
        {
            string stat = effect.ScalingStat.ToUpper();
            string mult = effect.ScalingMultiplier != 1.0f ? $"×{effect.ScalingMultiplier}" : "";
            sb.Append($" (+{stat}{mult})");
        }

        if (!string.IsNullOrEmpty(effect.SaveStat))
        {
            sb.Append($" ({effect.SaveStat.ToUpper()} save for half)");
        }

        return sb.ToString();
    }

    private static string DescribeHealEffect(SkillEffectDefinition effect)
    {
        if (!string.IsNullOrEmpty(effect.Dice))
            return $"Restores {effect.Dice} HP";
        if (effect.Percent)
            return $"Restores {effect.Amount}% HP";
        return $"Restores {effect.Amount} HP";
    }

    private static string DescribeConditionEffect(SkillEffectDefinition effect)
    {
        string condition = effect.ConditionType ?? "effect";
        string duration = !string.IsNullOrEmpty(effect.Duration) ? $" for {effect.Duration} turns" : "";
        return $"Applies {condition.Replace("_", " ")}{duration}";
    }

    private static string DescribeStatBonusEffect(SkillEffectDefinition effect)
    {
        string stat = FormatStatName(effect.Stat ?? "unknown");
        string sign = effect.Amount > 0 ? "+" : "";
        string duration = !string.IsNullOrEmpty(effect.Duration) ? $" for {effect.Duration} turns" : "";
        return $"Grants {sign}{effect.Amount} {stat}{duration}";
    }

    private static string BuildPrerequisitesSection(SkillDefinition skill)
    {
        if (skill.Prerequisites.IsUniversal())
            return "";

        var sb = new StringBuilder();
        sb.Append($"\n\n[color={Palette.ToHex(Palette.Disabled)}]REQUIREMENTS[/color]");
        sb.Append($"\n[color={Palette.ToHex(Palette.Disabled)}]Prerequisites:[/color] [color={Palette.ToHex(Palette.Default)}]{skill.GetPrerequisiteString()}[/color]");
        return sb.ToString();
    }

    private static string BuildCloseHint()
    {
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        return $"\n\n[right][color={Palette.ToHex(Palette.Disabled)}]{closeKey} to close[/color][/right]";
    }

    private static string GetCategoryColor(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.Active => Palette.ToHex(Palette.Cyan),
            SkillCategory.Passive => Palette.ToHex(Palette.Success),
            SkillCategory.Reactive => Palette.ToHex(Palette.Caution),
            SkillCategory.Toggle => Palette.ToHex(Palette.Cyan),
            SkillCategory.Improvement => Palette.ToHex(Palette.Wizard),
            _ => Palette.ToHex(Palette.Default)
        };
    }

    private static string FormatTargetingType(string targeting)
    {
        return targeting?.ToLower() switch
        {
            "self" => "Self",
            "creature" => "Creature",
            "enemy" => "Enemy",
            "ally" => "Ally",
            "adjacent" => "Adjacent",
            "melee" => "Melee",
            "reach" => "Reach (2 tiles)",
            "ranged" => "Ranged",
            "tile" => "Tile",
            "area" => "Area",
            "line" => "Line",
            "cone" => "Cone",
            "cleave" => "Arc (multiple enemies)",
            _ => targeting ?? "Unknown"
        };
    }

    private static string FormatTriggerName(string trigger)
    {
        return trigger.ToLower() switch
        {
            "on_hit" => "When you are hit",
            "on_kill" => "When you kill an enemy",
            "on_dodge" => "When you dodge an attack",
            "on_block" => "When you block damage",
            "on_miss" => "When an enemy misses you",
            "on_critical" => "When you land a critical hit",
            "on_take_damage" => "When you take damage",
            "on_deal_damage" => "When you deal damage",
            _ => trigger.Replace("_", " ")
        };
    }

    private static string FormatStatName(string stat)
    {
        return stat?.ToLower() switch
        {
            "str" or "strength" => "Strength",
            "agi" or "agility" => "Agility",
            "end" or "endurance" => "Endurance",
            "wil" or "will" => "Will",
            "melee_damage" => "Melee Damage",
            "melee_accuracy" or "accuracy" => "Accuracy",
            "ranged_damage" => "Ranged Damage",
            "ranged_accuracy" => "Ranged Accuracy",
            "armor" => "Armor",
            "evasion" => "Evasion",
            "max_hp" or "max_health" => "Max HP",
            "max_wp" or "max_willpower" => "Max WP",
            "speed" => "Speed",
            "regen" or "regeneration" => "Regeneration",
            _ => stat ?? "Unknown"
        };
    }
}
