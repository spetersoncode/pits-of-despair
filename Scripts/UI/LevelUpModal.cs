using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Skills;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Services;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal displayed when the player levels up.
/// Phase 1: Choose a stat to increase permanently.
/// Phase 2: Choose a skill to learn (on skill-granting levels).
/// Cannot be cancelled - player must make choices.
/// </summary>
public partial class LevelUpModal : PanelContainer
{
    [Signal]
    public delegate void StatChosenEventHandler(int statIndex);

    [Signal]
    public delegate void SkillChosenEventHandler(string skillId);

    private RichTextLabel _contentLabel;
    private int _newLevel;
    private readonly KeybindingService _keybindingService = KeybindingService.Instance;

    // References for stat/skill preview
    private StatsComponent _stats;
    private SkillComponent _skills;
    private DataLoader _dataLoader;

    // Levels that grant skill choices: 2-10, then 12, 14, 16, 18, 20, 21
    private static readonly HashSet<int> SkillGrantingLevels = new()
    {
        2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 20, 21
    };

    // Phase tracking
    private enum LevelUpPhase { StatSelection, SkillSelection }
    private LevelUpPhase _currentPhase = LevelUpPhase.StatSelection;

    // Skills available for selection (populated after stat choice)
    private List<SkillDefinition> _availableSkills = new();
    private List<SkillDefinition> _newlyUnlockedSkills = new();

    public override void _Ready()
    {
        _contentLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/ContentLabel");

        // Start hidden
        Hide();
    }

    /// <summary>
    /// Shows the level-up modal for the specified level.
    /// </summary>
    /// <param name="level">The new level the player has reached</param>
    /// <param name="stats">Player's StatsComponent for stat values and preview</param>
    /// <param name="skills">Player's SkillComponent for skill tracking</param>
    /// <param name="dataLoader">DataLoader for skill definitions</param>
    public void ShowForLevel(int level, StatsComponent stats, SkillComponent skills, DataLoader dataLoader)
    {
        _newLevel = level;
        _stats = stats;
        _skills = skills;
        _dataLoader = dataLoader;
        _currentPhase = LevelUpPhase.StatSelection;
        _availableSkills.Clear();
        _newlyUnlockedSkills.Clear();

        UpdateStatSelectionContent();

        // Switch to Modal input context so stat selection keys take priority
        _keybindingService.CurrentContext = InputContext.Modal;

        Show();
    }

    /// <summary>
    /// Legacy overload for backwards compatibility.
    /// </summary>
    public void ShowForLevel(int level, int currentEndurance)
    {
        // Fallback to basic display if no references provided
        _newLevel = level;
        _stats = null;
        _skills = null;
        _dataLoader = null;
        _currentPhase = LevelUpPhase.StatSelection;

        UpdateStatSelectionContent();
        _keybindingService.CurrentContext = InputContext.Modal;
        Show();
    }

    /// <summary>
    /// Hides the level-up modal.
    /// </summary>
    public void HideModal()
    {
        // Restore Gameplay input context
        _keybindingService.CurrentContext = InputContext.Gameplay;

        Hide();
    }

    /// <summary>
    /// Updates the modal content for stat selection phase.
    /// Shows current → new values, derived stat preview, and unlockable skills.
    /// </summary>
    private void UpdateStatSelectionContent()
    {
        var content = new System.Text.StringBuilder();

        // Header
        content.AppendLine($"[center][color={Palette.ToHex(Palette.Player)}]╔══════════════════════════════════════╗");
        content.AppendLine($"║            LEVEL UP!                 ║");
        content.AppendLine($"╚══════════════════════════════════════╝[/color][/center]");
        content.AppendLine();
        content.AppendLine($"[center][color={Palette.ToHex(Palette.Success)}]You reached level {_newLevel}![/color][/center]");
        content.AppendLine();
        content.AppendLine($"[center][color={Palette.ToHex(Palette.Alert)}]Choose a stat to increase:[/color][/center]");
        content.AppendLine();

        // Build stat options with preview
        content.AppendLine(BuildStatOption(0, "S", "Strength", "STR"));
        content.AppendLine(BuildStatOption(1, "A", "Agility", "AGI"));
        content.AppendLine(BuildStatOption(2, "E", "Endurance", "END"));
        content.AppendLine(BuildStatOption(3, "W", "Will", "WIL"));

        _contentLabel.Text = content.ToString();
    }

    /// <summary>
    /// Builds a single stat option line with current → new value and derived effects.
    /// </summary>
    private string BuildStatOption(int statIndex, string hotkey, string statName, string shortName)
    {
        bool atCap = _stats?.IsStatAtCap(statIndex) ?? false;
        int currentValue = _stats?.GetBaseStat(statIndex) ?? 0;
        int newValue = currentValue + 1;

        // Color based on availability
        string keyColor = atCap ? Palette.ToHex(Palette.Disabled) : Palette.ToHex(Palette.Default);
        string textColor = atCap ? Palette.ToHex(Palette.Disabled) : Palette.ToHex(Palette.Default);
        string valueColor = atCap ? Palette.ToHex(Palette.Disabled) : Palette.ToHex(Palette.Success);

        // Build the line
        var line = new System.Text.StringBuilder();

        if (atCap)
        {
            line.Append($"  [color={keyColor}][{hotkey}] {statName,-9}[/color] ");
            line.Append($"[color={textColor}]{shortName} {currentValue} [MAX][/color]");
        }
        else
        {
            line.Append($"  [color={keyColor}][{hotkey}][/color] [color={textColor}]{statName,-9}[/color] ");
            line.Append($"[color={textColor}]{shortName}[/color] [color={valueColor}]{currentValue} → {newValue}[/color]");

            // Add derived stat preview
            string derived = GetDerivedStatPreview(statIndex);
            if (!string.IsNullOrEmpty(derived))
            {
                line.Append($" [color={Palette.ToHex(Palette.Cyan)}]({derived})[/color]");
            }

            // Add skill unlock preview
            var unlockableSkills = GetSkillsUnlockedByStat(statIndex);
            if (unlockableSkills.Count > 0)
            {
                line.AppendLine();
                line.Append($"      [color={Palette.ToHex(Palette.PotionWill)}]Unlocks: ");
                line.Append(string.Join(", ", unlockableSkills.Select(s => s.Name)));
                line.Append("[/color]");
            }
        }

        return line.ToString();
    }

    /// <summary>
    /// Gets a preview string of derived stat changes for a stat increase.
    /// </summary>
    private string GetDerivedStatPreview(int statIndex)
    {
        if (_stats == null) return "";

        return statIndex switch
        {
            0 => "+1 Melee Attack",  // STR
            1 => "+1 Ranged Attack, +1 Evasion",  // AGI
            2 => GetHPPreview(),  // END
            3 => "+5 Max WP",  // WIL
            _ => ""
        };
    }

    /// <summary>
    /// Calculates HP gain preview from increasing Endurance.
    /// </summary>
    private string GetHPPreview()
    {
        if (_stats == null) return "+HP";

        int currentEnd = _stats.TotalEndurance;
        int currentHPBonus = _stats.GetHPBonus();

        // Calculate HP bonus with +1 END using the same formula
        int newEnd = currentEnd + 1;
        int newHPBonus = (newEnd * newEnd + 9 * newEnd) / 2;
        int hpGain = newHPBonus - currentHPBonus;

        return $"+{hpGain} HP";
    }

    /// <summary>
    /// Gets skills that would become available if this stat is increased.
    /// </summary>
    private List<SkillDefinition> GetSkillsUnlockedByStat(int statIndex)
    {
        var unlockable = new List<SkillDefinition>();

        if (_stats == null || _skills == null || _dataLoader == null)
            return unlockable;

        // Simulate stat increase
        int simStr = _stats.BaseStrength + (statIndex == 0 ? 1 : 0);
        int simAgi = _stats.BaseAgility + (statIndex == 1 ? 1 : 0);
        int simEnd = _stats.BaseEndurance + (statIndex == 2 ? 1 : 0);
        int simWil = _stats.BaseWill + (statIndex == 3 ? 1 : 0);

        foreach (var skill in _dataLoader.GetAllSkills())
        {
            // Skip if already learned
            if (_skills.HasSkill(skill.Id))
                continue;

            var prereqs = skill.Prerequisites;

            // Check if currently NOT meetable but WOULD BE after stat increase
            bool currentlyMeets = _stats.BaseStrength >= prereqs.Str
                               && _stats.BaseAgility >= prereqs.Agi
                               && _stats.BaseEndurance >= prereqs.End
                               && _stats.BaseWill >= prereqs.Wil;

            bool wouldMeet = simStr >= prereqs.Str
                          && simAgi >= prereqs.Agi
                          && simEnd >= prereqs.End
                          && simWil >= prereqs.Wil;

            if (!currentlyMeets && wouldMeet)
            {
                unlockable.Add(skill);
            }
        }

        return unlockable;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (_currentPhase == LevelUpPhase.StatSelection)
            {
                HandleStatSelectionInput(keyEvent);
            }
            else if (_currentPhase == LevelUpPhase.SkillSelection)
            {
                HandleSkillSelectionInput(keyEvent);
            }
            // Note: ESC does NOT close this modal - player must choose
        }
    }

    /// <summary>
    /// Handles input during stat selection phase.
    /// </summary>
    private void HandleStatSelectionInput(InputEventKey keyEvent)
    {
        if (_keybindingService.TryGetAction(keyEvent, out var action))
        {
            int statIndex = action switch
            {
                InputAction.SelectStrength => 0,
                InputAction.SelectAgility => 1,
                InputAction.SelectEndurance => 2,
                InputAction.SelectWill => 3,
                _ => -1
            };

            if (statIndex >= 0)
            {
                // Block selection of capped stats
                if (_stats != null && _stats.IsStatAtCap(statIndex))
                {
                    // Could play a "can't do that" sound here
                    GetViewport().SetInputAsHandled();
                    return;
                }

                // Record which skills will be newly unlocked by this stat choice
                _newlyUnlockedSkills = GetSkillsUnlockedByStat(statIndex);

                // Emit signal for stat selection
                EmitSignal(SignalName.StatChosen, statIndex);
                GetViewport().SetInputAsHandled();

                // Check if this level grants a skill
                if (SkillGrantingLevels.Contains(_newLevel) && _dataLoader != null && _skills != null && _stats != null)
                {
                    // Transition to skill selection phase
                    TransitionToSkillSelection();
                }
                // If no skill grant, GameHUD will hide modal via HideModal()
            }
        }
    }

    /// <summary>
    /// Transitions to skill selection phase after stat choice.
    /// </summary>
    private void TransitionToSkillSelection()
    {
        // Get available skills (already meets prereqs after stat increase, not learned)
        _availableSkills = _skills.GetAvailableSkills(_stats, _dataLoader)
            .OrderBy(s => s.Tier)
            .ThenBy(s => s.GetCategory())
            .ThenBy(s => s.GetTotalPrerequisites())
            .ThenBy(s => s.Name)
            .ToList();

        if (_availableSkills.Count == 0)
        {
            // No skills available - close modal
            return;
        }

        _currentPhase = LevelUpPhase.SkillSelection;
        UpdateSkillSelectionContent();
    }

    /// <summary>
    /// Updates the modal content for skill selection phase.
    /// </summary>
    private void UpdateSkillSelectionContent()
    {
        var content = new System.Text.StringBuilder();

        // Header
        content.AppendLine($"[center][color={Palette.ToHex(Palette.PotionWill)}]╔══════════════════════════════════════╗");
        content.AppendLine($"║          LEARN A SKILL               ║");
        content.AppendLine($"╚══════════════════════════════════════╝[/color][/center]");
        content.AppendLine();

        if (_availableSkills.Count == 0)
        {
            content.AppendLine($"[center][color={Palette.ToHex(Palette.Disabled)}]No skills available to learn.[/color][/center]");
        }
        else
        {
            content.AppendLine($"[center][color={Palette.ToHex(Palette.Alert)}]Choose a skill (1-{System.Math.Min(_availableSkills.Count, 9)}):[/color][/center]");
            content.AppendLine();

            for (int i = 0; i < _availableSkills.Count && i < 9; i++)
            {
                var skill = _availableSkills[i];
                bool isNewlyUnlocked = _newlyUnlockedSkills.Contains(skill);

                content.AppendLine(BuildSkillOption(i + 1, skill, isNewlyUnlocked));
            }
        }

        _contentLabel.Text = content.ToString();
    }

    /// <summary>
    /// Builds a single skill option line for the skill selection UI.
    /// </summary>
    private string BuildSkillOption(int number, SkillDefinition skill, bool isNewlyUnlocked)
    {
        var line = new System.Text.StringBuilder();

        string keyColor = Palette.ToHex(Palette.Default);
        string nameColor = isNewlyUnlocked ? Palette.ToHex(Palette.Success) : Palette.ToHex(Palette.Default);
        string categoryColor = Palette.ToHex(Palette.Cyan);
        string costColor = skill.WillpowerCost > 0 ? Palette.ToHex(Palette.PotionWill) : Palette.ToHex(Palette.Disabled);
        string descColor = Palette.ToHex(Palette.Disabled);
        string prereqColor = Palette.ToHex(Palette.Cyan);

        // Number and name
        line.Append($"  [color={keyColor}][{number}][/color] ");
        line.Append($"[color={nameColor}]{skill.Name}[/color] ");
        line.Append($"[color={categoryColor}]{skill.GetCategoryIndicator()}[/color]");

        // WP cost if any
        if (skill.WillpowerCost > 0)
        {
            line.Append($" [color={costColor}]{skill.WillpowerCost} WP[/color]");
        }

        // Mark newly unlocked
        if (isNewlyUnlocked)
        {
            line.Append($" [color={Palette.ToHex(Palette.Success)}]*NEW*[/color]");
        }

        line.AppendLine();

        // Description
        line.Append($"      [color={descColor}]{skill.Description}[/color]");

        // Prerequisites (if any)
        if (!skill.Prerequisites.IsUniversal())
        {
            line.AppendLine();
            line.Append($"      [color={prereqColor}]Req: {skill.GetPrerequisiteString()}[/color]");
        }

        return line.ToString();
    }

    /// <summary>
    /// Handles input during skill selection phase.
    /// </summary>
    private void HandleSkillSelectionInput(InputEventKey keyEvent)
    {
        // Check for number keys 1-9
        int selectedIndex = -1;

        if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
        {
            selectedIndex = (int)(keyEvent.Keycode - Key.Key1);
        }
        else if (keyEvent.Keycode >= Key.Kp1 && keyEvent.Keycode <= Key.Kp9)
        {
            selectedIndex = (int)(keyEvent.Keycode - Key.Kp1);
        }

        if (selectedIndex >= 0 && selectedIndex < _availableSkills.Count)
        {
            var selectedSkill = _availableSkills[selectedIndex];
            EmitSignal(SignalName.SkillChosen, selectedSkill.Id);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Returns whether this level grants a skill choice.
    /// </summary>
    public static bool LevelGrantsSkill(int level)
    {
        return SkillGrantingLevels.Contains(level);
    }

    /// <summary>
    /// Returns whether we're in skill selection phase.
    /// </summary>
    public bool IsInSkillSelectionPhase => _currentPhase == LevelUpPhase.SkillSelection;
}
