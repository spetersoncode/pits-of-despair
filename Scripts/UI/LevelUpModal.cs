using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Skills;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;
using PitsOfDespair.Systems.Input.Services;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal displayed when the player levels up.
/// Phase 1: Choose a stat to increase permanently.
/// Phase 2: Choose a skill to learn (on skill-granting levels).
/// Phase 3: Confirm choices before applying.
/// ESC cancels at any point without applying bonuses.
/// </summary>
public partial class LevelUpModal : PanelContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    [Signal]
    public delegate void ConfirmedEventHandler(int statIndex, string skillId);

    [Signal]
    public delegate void SkillPreviewRequestedEventHandler(string skillId);

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
    private enum LevelUpPhase { StatSelection, SkillSelection, Confirmation }
    private LevelUpPhase _currentPhase = LevelUpPhase.StatSelection;

    // Selection tracking (deferred until confirmation)
    private int? _selectedStatIndex = null;
    private string _selectedSkillId = null;

    // Skills available for selection (populated after stat choice)
    private List<SkillDefinition> _availableSkills = new();
    private List<SkillDefinition> _newlyUnlockedSkills = new();
    private Dictionary<char, SkillDefinition> _skillLetterMap = new();

    // Selection navigation state for skill selection
    private int _selectedSkillIndex = 0;

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
        _selectedStatIndex = null;
        _selectedSkillId = null;

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
        _selectedStatIndex = null;
        _selectedSkillId = null;

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

        // Show skills unlocked just by reaching this level
        var levelUnlockedSkills = GetSkillsUnlockedByLevel();
        if (levelUnlockedSkills.Count > 0)
        {
            content.Append($"[center][color={Palette.ToHex(Palette.Success)}]Level {_newLevel} unlocks: ");
            content.Append(string.Join(", ", levelUnlockedSkills.Select(s => s.Name)));
            content.AppendLine("[/color][/center]");
            content.AppendLine();
        }

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
            0 => "+1 Melee Attack, +1 Melee Damage",  // STR
            1 => "+1 Ranged Attack, +1 Evasion",  // AGI
            2 => GetHPPreview(),  // END
            3 => "+5 Max Willpower",  // WIL
            _ => ""
        };
    }

    /// <summary>
    /// Calculates HP gain preview from increasing Endurance.
    /// </summary>
    private string GetHPPreview()
    {
        if (_stats == null) return "+? Max Health";

        int currentEnd = _stats.TotalEndurance;
        int currentHPBonus = _stats.GetHealthBonus();

        // Calculate HP bonus with +1 END using the same formula
        int newEnd = currentEnd + 1;
        int newHPBonus = (newEnd * newEnd + 9 * newEnd) / 2;
        int hpGain = newHPBonus - currentHPBonus;

        return $"+{hpGain} Max Health";
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

        foreach (var skill in _dataLoader.Skills.GetAll())
        {
            // Skip if already learned
            if (_skills.HasSkill(skill.Id))
                continue;

            var prereqs = skill.Prerequisites;

            // Skip if level requirement not met at new level
            if (_newLevel < prereqs.Level)
                continue;

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

    /// <summary>
    /// Gets skills that become available solely by reaching the new level.
    /// These are skills where all stat requirements are already met but level was blocking.
    /// </summary>
    private List<SkillDefinition> GetSkillsUnlockedByLevel()
    {
        var unlocked = new List<SkillDefinition>();

        if (_stats == null || _skills == null || _dataLoader == null)
            return unlocked;

        int previousLevel = _newLevel - 1;

        foreach (var skill in _dataLoader.Skills.GetAll())
        {
            // Skip if already learned
            if (_skills.HasSkill(skill.Id))
                continue;

            // Check if we NOW meet prereqs (at new level) but DIDN'T before
            bool meetsNow = PrerequisiteChecker.MeetsPrerequisites(skill, _stats, _newLevel);
            bool metBefore = PrerequisiteChecker.MeetsPrerequisites(skill, _stats, previousLevel);

            if (meetsNow && !metBefore)
            {
                unlocked.Add(skill);
            }
        }

        return unlocked;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // ESC cancels the entire level-up flow at any point
            if (MenuInputProcessor.IsCloseKey(keyEvent))
            {
                EmitSignal(SignalName.Cancelled);
                HideModal();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_currentPhase == LevelUpPhase.StatSelection)
            {
                HandleStatSelectionInput(keyEvent);
            }
            else if (_currentPhase == LevelUpPhase.SkillSelection)
            {
                HandleSkillSelectionInput(keyEvent);
            }
            else if (_currentPhase == LevelUpPhase.Confirmation)
            {
                HandleConfirmationInput(keyEvent);
            }
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

                // Store selection (don't apply yet)
                _selectedStatIndex = statIndex;

                // Record which skills will be newly unlocked by this stat choice
                _newlyUnlockedSkills = GetSkillsUnlockedByStat(statIndex);

                // Check if this level grants a skill
                if (SkillGrantingLevels.Contains(_newLevel) && _dataLoader != null && _skills != null && _stats != null)
                {
                    // Transition to skill selection phase (pass statIndex to simulate stat increase)
                    TransitionToSkillSelection(statIndex);
                }
                else
                {
                    // No skill selection - go directly to confirmation
                    TransitionToConfirmation();
                }

                GetViewport().SetInputAsHandled();
            }
        }
    }

    /// <summary>
    /// Transitions to skill selection phase after stat choice.
    /// Must be called BEFORE the stat is applied, so we simulate the increase.
    /// </summary>
    /// <param name="statIndex">The stat index being increased (0=STR, 1=AGI, 2=END, 3=WIL)</param>
    private void TransitionToSkillSelection(int statIndex)
    {
        // Get available skills with simulated stat increase (stat hasn't been applied yet)
        _availableSkills = GetAvailableSkillsWithSimulatedStat(statIndex)
            .OrderBy(s => s.GetCategory())
            .ThenBy(s => s.GetTotalPrerequisites())
            .ThenBy(s => s.Name)
            .ToList();

        if (_availableSkills.Count == 0)
        {
            // No skills available - skip to confirmation
            TransitionToConfirmation();
            return;
        }

        // Reset selection index
        _selectedSkillIndex = 0;

        _currentPhase = LevelUpPhase.SkillSelection;
        UpdateSkillSelectionContent();
    }

    /// <summary>
    /// Gets available skills assuming the specified stat will be increased by 1.
    /// Used to check skill availability BEFORE the stat is actually applied.
    /// </summary>
    private List<SkillDefinition> GetAvailableSkillsWithSimulatedStat(int statIndex)
    {
        var available = new List<SkillDefinition>();

        if (_stats == null || _skills == null || _dataLoader == null)
            return available;

        // Simulate stat increase
        int simStr = _stats.BaseStrength + (statIndex == 0 ? 1 : 0);
        int simAgi = _stats.BaseAgility + (statIndex == 1 ? 1 : 0);
        int simEnd = _stats.BaseEndurance + (statIndex == 2 ? 1 : 0);
        int simWil = _stats.BaseWill + (statIndex == 3 ? 1 : 0);

        foreach (var skill in _dataLoader.Skills.GetAll())
        {
            // Skip if already learned
            if (_skills.HasSkill(skill.Id))
                continue;

            var prereqs = skill.Prerequisites;

            // Check if skill would be available after stat increase (including level requirement)
            bool wouldMeet = _newLevel >= prereqs.Level
                          && simStr >= prereqs.Str
                          && simAgi >= prereqs.Agi
                          && simEnd >= prereqs.End
                          && simWil >= prereqs.Wil;

            if (wouldMeet)
            {
                available.Add(skill);
            }
        }

        return available;
    }

    /// <summary>
    /// Updates the modal content for skill selection phase.
    /// Shows multi-line format with flavor text, grouped by category.
    /// </summary>
    private void UpdateSkillSelectionContent()
    {
        var content = new System.Text.StringBuilder();

        // Header
        content.AppendLine($"[center][color={Palette.ToHex(Palette.PotionWill)}]╔══════════════════════════════════════╗");
        content.AppendLine($"║          LEARN A SKILL               ║");
        content.AppendLine($"╚══════════════════════════════════════╝[/color][/center]");
        content.AppendLine();

        // Build letter-to-skill mapping
        _skillLetterMap.Clear();

        if (_availableSkills.Count == 0)
        {
            content.AppendLine($"[center][color={Palette.ToHex(Palette.Disabled)}]No skills available to learn.[/color][/center]");
        }
        else
        {
            char maxLetter = (char)('a' + System.Math.Min(_availableSkills.Count, 26) - 1);
            content.AppendLine($"[center][color={Palette.ToHex(Palette.Alert)}]Choose a skill (a-{maxLetter}, ↑/↓ navigate, ? preview):[/color][/center]");
            content.AppendLine();

            // Assign letters to skills
            for (int i = 0; i < _availableSkills.Count && i < 26; i++)
            {
                char letter = (char)('a' + i);
                _skillLetterMap[letter] = _availableSkills[i];
            }

            // Group by category for display
            var grouped = _availableSkills
                .Take(26)
                .Select((skill, index) => (skill, index, letter: (char)('a' + index)))
                .GroupBy(x => x.skill.GetCategory())
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var group in grouped)
            {
                content.AppendLine($"[color={Palette.ToHex(Palette.Disabled)}]{GetCategoryHeader(group.Key)}[/color]");

                foreach (var (skill, index, letter) in group)
                {
                    bool isSelected = index == _selectedSkillIndex;
                    bool isNewlyUnlocked = _newlyUnlockedSkills.Contains(skill);

                    content.AppendLine(BuildSkillOption(letter, skill, isNewlyUnlocked, isSelected));
                }
                content.AppendLine();
            }
        }

        _contentLabel.Text = content.ToString();
    }

    private static int GetCategoryOrder(SkillCategory cat) => cat switch
    {
        SkillCategory.Active => 0,
        SkillCategory.Toggle => 1,
        SkillCategory.Passive => 2,
        SkillCategory.Reactive => 3,
        SkillCategory.Improvement => 4,
        _ => 99
    };

    private static string GetCategoryHeader(SkillCategory cat) => cat switch
    {
        SkillCategory.Active => "ACTIVE SKILLS",
        SkillCategory.Toggle => "TOGGLE SKILLS",
        SkillCategory.Passive => "PASSIVE SKILLS",
        SkillCategory.Reactive => "REACTIVE SKILLS",
        SkillCategory.Improvement => "IMPROVEMENTS",
        _ => "OTHER"
    };

    /// <summary>
    /// Builds a multi-line skill option for the skill selection UI.
    /// Shows name/category/cost, flavor text, and description on separate lines.
    /// </summary>
    private string BuildSkillOption(char letter, SkillDefinition skill, bool isNewlyUnlocked, bool isSelected)
    {
        var sb = new System.Text.StringBuilder();

        // Colors
        string keyColor = Palette.ToHex(Palette.Default);
        string nameColor = GetSkillCategoryColor(skill.GetCategory());
        string costColor = skill.WillpowerCost > 0 ? Palette.ToHex(Palette.PotionWill) : Palette.ToHex(Palette.Disabled);
        string flavorColor = Palette.ToHex(Palette.AshGray);
        string descColor = Palette.ToHex(Palette.Default);
        string highlightColor = Palette.ToHex(Palette.Alert);

        // Selection indicator
        string selectionIndicator = isSelected ? ">" : " ";

        // Line 1: selection indicator, letter, name, cost, NEW marker
        if (isSelected)
        {
            sb.Append($"[color={highlightColor}]{selectionIndicator}[/color] ");
        }
        else
        {
            sb.Append($"  ");
        }

        sb.Append($"[color={keyColor}]{letter})[/color] ");
        sb.Append($"[color={nameColor}]{skill.Name}[/color]");

        if (skill.WillpowerCost > 0)
        {
            sb.Append($" [color={costColor}]{skill.WillpowerCost} WP[/color]");
        }

        // Reactive trigger info
        if (skill.GetCategory() == SkillCategory.Reactive && !string.IsNullOrEmpty(skill.Trigger))
        {
            string trigger = skill.Trigger.Replace("_", " ");
            sb.Append($" [color={costColor}]({trigger})[/color]");
        }

        if (isNewlyUnlocked)
        {
            sb.Append($" [color={Palette.ToHex(Palette.Success)}]*NEW*[/color]");
        }

        // Line 2: flavor text (indented, italic)
        if (!string.IsNullOrEmpty(skill.Flavor))
        {
            sb.AppendLine();
            sb.Append($"      [color={flavorColor}][i]{skill.Flavor}[/i][/color]");
        }

        // Line 3: description (indented)
        sb.AppendLine();
        sb.Append($"      [color={descColor}]{skill.Description}[/color]");

        return sb.ToString();
    }

    private static string GetSkillCategoryColor(SkillCategory category)
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

    /// <summary>
    /// Handles input during skill selection phase.
    /// Supports letter keys, arrow/j/k navigation, and ? preview.
    /// </summary>
    private void HandleSkillSelectionInput(InputEventKey keyEvent)
    {
        int skillCount = System.Math.Min(_availableSkills.Count, 26);

        // Arrow up or k - move selection up
        if (keyEvent.Keycode == Key.Up || keyEvent.Keycode == Key.K)
        {
            if (_selectedSkillIndex > 0)
            {
                _selectedSkillIndex--;
                UpdateSkillSelectionContent();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // Arrow down or j - move selection down
        if (keyEvent.Keycode == Key.Down || keyEvent.Keycode == Key.J)
        {
            if (_selectedSkillIndex < skillCount - 1)
            {
                _selectedSkillIndex++;
                UpdateSkillSelectionContent();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // ? - preview selected skill (Shift+/)
        if ((keyEvent.Keycode == Key.Slash && keyEvent.ShiftPressed) || keyEvent.Keycode == Key.Question)
        {
            if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _availableSkills.Count)
            {
                var skill = _availableSkills[_selectedSkillIndex];
                EmitSignal(SignalName.SkillPreviewRequested, skill.Id);
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // Enter - select highlighted skill
        if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter)
        {
            if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _availableSkills.Count)
            {
                _selectedSkillId = _availableSkills[_selectedSkillIndex].Id;
                TransitionToConfirmation();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for letter key selection (a-z)
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char selectedKey))
        {
            selectedKey = char.ToLower(selectedKey);

            if (_skillLetterMap.TryGetValue(selectedKey, out var selectedSkill))
            {
                // Store selection (don't apply yet)
                _selectedSkillId = selectedSkill.Id;

                // Transition to confirmation
                TransitionToConfirmation();
                GetViewport().SetInputAsHandled();
            }
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

    /// <summary>
    /// Transitions to confirmation phase after all selections are made.
    /// </summary>
    private void TransitionToConfirmation()
    {
        _currentPhase = LevelUpPhase.Confirmation;
        UpdateConfirmationContent();
    }

    /// <summary>
    /// Updates the modal content for confirmation phase.
    /// Shows summary of selections with confirm/cancel instructions.
    /// </summary>
    private void UpdateConfirmationContent()
    {
        var content = new System.Text.StringBuilder();

        // Header
        content.AppendLine($"[center][color={Palette.ToHex(Palette.Success)}]╔══════════════════════════════════════╗");
        content.AppendLine($"║         CONFIRM LEVEL UP             ║");
        content.AppendLine($"╚══════════════════════════════════════╝[/color][/center]");
        content.AppendLine();
        content.AppendLine($"[center]Level {_newLevel}[/center]");
        content.AppendLine();

        // Stat choice summary
        if (_selectedStatIndex.HasValue)
        {
            string statName = _selectedStatIndex.Value switch
            {
                0 => "Strength",
                1 => "Agility",
                2 => "Endurance",
                3 => "Will",
                _ => "Unknown"
            };

            string shortName = _selectedStatIndex.Value switch
            {
                0 => "STR",
                1 => "AGI",
                2 => "END",
                3 => "WIL",
                _ => "???"
            };

            int currentValue = _stats?.GetBaseStat(_selectedStatIndex.Value) ?? 0;
            int newValue = currentValue + 1;
            string derived = GetDerivedStatPreview(_selectedStatIndex.Value);

            content.Append($"  [color={Palette.ToHex(Palette.Alert)}]Stat:[/color]   ");
            content.Append($"[color={Palette.ToHex(Palette.Default)}]{statName,-11}[/color] ");
            content.Append($"[color={Palette.ToHex(Palette.Success)}]{shortName} {currentValue} → {newValue}[/color]");

            if (!string.IsNullOrEmpty(derived))
            {
                content.Append($"  [color={Palette.ToHex(Palette.Cyan)}]({derived})[/color]");
            }
            content.AppendLine();
        }

        // Skill choice summary (if any)
        if (!string.IsNullOrEmpty(_selectedSkillId) && _dataLoader != null)
        {
            var skillDef = _dataLoader.Skills.Get(_selectedSkillId);
            if (skillDef != null)
            {
                content.AppendLine();
                string category = skillDef.GetCategory() == SkillCategory.Active ? "Active" : "Passive";

                content.Append($"  [color={Palette.ToHex(Palette.Alert)}]Skill:[/color]  ");
                content.Append($"[color={Palette.ToHex(Palette.PotionWill)}]{skillDef.Name}[/color]  ");
                content.Append($"[color={Palette.ToHex(Palette.Cyan)}]{category}[/color]");

                if (skillDef.WillpowerCost > 0)
                {
                    content.Append($"  [color={Palette.ToHex(Palette.PotionWill)}]{skillDef.WillpowerCost} Willpower[/color]");
                }
                content.AppendLine();

                // Skill description
                content.AppendLine($"          [color={Palette.ToHex(Palette.Disabled)}]{skillDef.Description}[/color]");
            }
        }

        content.AppendLine();
        content.AppendLine();
        content.AppendLine($"[center][color={Palette.ToHex(Palette.Success)}][Y][/color] or [color={Palette.ToHex(Palette.Success)}][Enter][/color] Confirm       [color={Palette.ToHex(Palette.Disabled)}][Esc][/color] Cancel[/center]");

        _contentLabel.Text = content.ToString();
    }

    /// <summary>
    /// Handles input during confirmation phase.
    /// </summary>
    private void HandleConfirmationInput(InputEventKey keyEvent)
    {
        // Y or Enter confirms
        if (keyEvent.Keycode == Key.Y || keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter)
        {
            if (_selectedStatIndex.HasValue)
            {
                EmitSignal(SignalName.Confirmed, _selectedStatIndex.Value, _selectedSkillId ?? "");
                HideModal();
                GetViewport().SetInputAsHandled();
            }
        }
        // ESC is handled at the top level of _Input
    }
}
