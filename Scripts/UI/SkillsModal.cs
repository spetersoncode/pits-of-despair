using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Skills;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays learned skills and allows activation of active skills.
/// Groups skills by category: Active, Passive, Reactive, Aura.
/// </summary>
public partial class SkillsModal : PanelContainer
{
    [Signal]
    public delegate void SkillSelectedEventHandler(string skillId, char key);

    [Signal]
    public delegate void CancelledEventHandler();

    [Signal]
    public delegate void SkillKeyReboundEventHandler(char oldKey, char newKey);

    private enum State
    {
        Viewing,
        AwaitingRebind
    }

    private RichTextLabel _skillsLabel;
    private Player _player;
    private SkillComponent _skillComponent;
    private WillpowerComponent _willpowerComponent;
    private DataLoader _dataLoader;
    private bool _isVisible = false;
    private State _currentState = State.Viewing;
    private char _rebindSourceKey;
    private string _rebindSkillName;

    // Currently displayed skills (for key selection)
    private Dictionary<char, SkillDefinition> _displayedSkills = new();

    public override void _Ready()
    {
        _skillsLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/SkillsLabel");
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
        Hide();
    }

    /// <summary>
    /// Connects to the player to access skills.
    /// </summary>
    public void ConnectToPlayer(Player player)
    {
        _player = player;
        _skillComponent = player?.GetNodeOrNull<SkillComponent>("SkillComponent");
        _willpowerComponent = player?.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");
    }

    /// <summary>
    /// Shows the skills menu.
    /// </summary>
    public void ShowMenu()
    {
        _isVisible = true;
        Show();
        UpdateDisplay();
    }

    /// <summary>
    /// Hides the skills menu.
    /// </summary>
    public void HideMenu()
    {
        _isVisible = false;
        _currentState = State.Viewing;
        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isVisible)
            return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            HandleKeyInput(keyEvent);
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        switch (_currentState)
        {
            case State.Viewing:
                HandleViewingInput(keyEvent);
                break;
            case State.AwaitingRebind:
                HandleRebindInput(keyEvent);
                break;
        }
    }

    private void HandleViewingInput(InputEventKey keyEvent)
    {
        // Cancel on close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Enter rebind mode on '='
        if (MenuInputProcessor.IsKey(keyEvent, Key.Equal))
        {
            // Find the first active skill to rebind (user will select which one)
            if (_displayedSkills.Count > 0)
            {
                _currentState = State.AwaitingRebind;
                _rebindSourceKey = '\0'; // Will be set when user selects a skill
                UpdateDisplay();
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for letter key selection (a-z)
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char selectedKey))
        {
            selectedKey = char.ToLower(selectedKey);

            if (_displayedSkills.TryGetValue(selectedKey, out var selectedSkill))
            {
                // Only allow activating active skills
                if (selectedSkill.GetCategory() == SkillCategory.Active)
                {
                    EmitSignal(SignalName.SkillSelected, selectedSkill.Id, selectedKey);
                }
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleRebindInput(InputEventKey keyEvent)
    {
        // Cancel rebind on close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            _currentState = State.Viewing;
            UpdateDisplay();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Accept letter keys for rebinding
        if (MenuInputProcessor.TryGetLetterKey(keyEvent, out char selectedKey))
        {
            selectedKey = char.ToLower(selectedKey);

            // If we haven't selected a source skill yet, this is selecting which skill to rebind
            if (_rebindSourceKey == '\0')
            {
                if (_displayedSkills.TryGetValue(selectedKey, out var skill))
                {
                    _rebindSourceKey = selectedKey;
                    _rebindSkillName = skill.Name;
                    UpdateDisplay();
                }
            }
            else
            {
                // We have a source skill, this is the target key
                EmitSignal(SignalName.SkillKeyRebound, _rebindSourceKey, selectedKey);
                _currentState = State.Viewing;
                UpdateDisplay();
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private void UpdateDisplay()
    {
        if (_skillsLabel == null || _player == null || _skillComponent == null || _dataLoader == null)
        {
            return;
        }

        var learnedSkillIds = _skillComponent.LearnedSkills;

        if (learnedSkillIds.Count == 0)
        {
            ShowEmptyMessage();
            return;
        }

        // Load skill definitions
        var learnedSkills = learnedSkillIds
            .Select(id => _dataLoader.GetSkill(id))
            .Where(s => s != null)
            .ToList();

        var sb = new StringBuilder();

        // Show different header based on state
        if (_currentState == State.AwaitingRebind)
        {
            sb.Append(BuildRebindHeader());
        }
        else
        {
            sb.Append(BuildHeader());
        }

        // Group by category
        var activeSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Active).ToList();
        var passiveSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Passive).OrderBy(s => s.Name).ToList();
        var reactiveSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Reactive).OrderBy(s => s.Name).ToList();
        var auraSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Aura).OrderBy(s => s.Name).ToList();

        // Build displayed skills dictionary using persistent keys
        _displayedSkills.Clear();
        foreach (var skill in activeSkills)
        {
            var key = _skillComponent.GetSkillKey(skill.Id);
            if (key.HasValue)
            {
                _displayedSkills[key.Value] = skill;
            }
        }

        int currentWP = _willpowerComponent?.CurrentWillpower ?? 0;

        // Active Skills section - sort by key for display
        if (activeSkills.Count > 0)
        {
            sb.AppendLine($"\n[color={Palette.ToHex(Palette.Cyan)}][b]─── Active Skills ───[/b][/color]");
            foreach (var kvp in _displayedSkills.OrderBy(k => k.Key))
            {
                var skill = kvp.Value;
                bool canAfford = currentWP >= skill.WillpowerCost;
                sb.AppendLine(FormatActiveSkill(skill, kvp.Key, canAfford));
            }
        }

        // Passive Skills section
        if (passiveSkills.Count > 0)
        {
            sb.AppendLine($"\n[color={Palette.ToHex(Palette.Success)}][b]─── Passive Skills ───[/b][/color]");
            foreach (var skill in passiveSkills)
            {
                sb.AppendLine(FormatPassiveSkill(skill));
            }
        }

        // Reactive Skills section
        if (reactiveSkills.Count > 0)
        {
            sb.AppendLine($"\n[color={Palette.ToHex(Palette.Caution)}][b]─── Reactive Skills ───[/b][/color]");
            foreach (var skill in reactiveSkills)
            {
                sb.AppendLine(FormatReactiveSkill(skill));
            }
        }

        // Aura Skills section
        if (auraSkills.Count > 0)
        {
            sb.AppendLine($"\n[color={Palette.ToHex(Palette.Wizard)}][b]─── Aura Skills ───[/b][/color]");
            foreach (var skill in auraSkills)
            {
                sb.AppendLine(FormatAuraSkill(skill));
            }
        }

        _skillsLabel.Text = sb.ToString();
    }

    private string FormatActiveSkill(SkillDefinition skill, char keyDisplay, bool canAfford)
    {
        string wpCost = skill.WillpowerCost > 0 ? $"[color={Palette.ToHex(Palette.Wizard)}]{skill.WillpowerCost} Willpower[/color]" : "[color=#888888]Free[/color]";
        string nameColor = canAfford ? Palette.ToHex(Palette.Default) : Palette.ToHex(Palette.Disabled);

        return $"  [{keyDisplay}] [color={nameColor}]{skill.Name}[/color] - {wpCost} - {skill.Description}";
    }

    private string FormatPassiveSkill(SkillDefinition skill)
    {
        return $"  [color={Palette.ToHex(Palette.Success)}]●[/color] {skill.Name} - [i]Always active[/i] - {skill.Description}";
    }

    private string FormatReactiveSkill(SkillDefinition skill)
    {
        string trigger = !string.IsNullOrEmpty(skill.Trigger) ? $"[i]Triggers: {skill.Trigger}[/i]" : "[i]Auto-triggers[/i]";
        string wpCost = skill.WillpowerCost > 0 ? $" ({skill.WillpowerCost} Willpower)" : "";
        return $"  [color={Palette.ToHex(Palette.Caution)}]⚡[/color] {skill.Name}{wpCost} - {trigger} - {skill.Description}";
    }

    private string FormatAuraSkill(SkillDefinition skill)
    {
        string range = skill.AuraRadius > 0 ? $"Range {skill.AuraRadius}" : "";
        return $"  [color={Palette.ToHex(Palette.Wizard)}]◎[/color] {skill.Name} - [i]{range}[/i] - {skill.Description}";
    }

    private void ShowEmptyMessage()
    {
        if (_skillsLabel != null)
        {
            var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
            _skillsLabel.Text = $"[center][b]Skills (a-z)[/b][/center]\n[center]({closeKey} to close)[/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]No skills learned[/color][/center]";
        }
    }

    private string BuildHeader()
    {
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        int currentWillpower = _willpowerComponent?.CurrentWillpower ?? 0;
        int maxWillpower = _willpowerComponent?.MaxWillpower ?? 0;
        return $"[center][b]Skills (a-z)[/b][/center]\n[center]Willpower: [color={Palette.ToHex(Palette.Wizard)}]{currentWillpower}/{maxWillpower}[/color] | [=] Rebind | ({closeKey} to close)[/center]";
    }

    private string BuildRebindHeader()
    {
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);

        if (_rebindSourceKey == '\0')
        {
            // First step: select which skill to rebind
            return $"[center][b][color={Palette.ToHex(Palette.Caution)}]REBIND MODE[/color][/b][/center]\n[center]Press [color={Palette.ToHex(Palette.Default)}]a-z[/color] to select skill to rebind | ({closeKey} to cancel)[/center]";
        }
        else
        {
            // Second step: select target key
            return $"[center][b][color={Palette.ToHex(Palette.Caution)}]REBIND MODE[/color][/b][/center]\n[center]Rebinding [color={Palette.ToHex(Palette.Wizard)}]{_rebindSkillName}[/color] from [{_rebindSourceKey}][/center]\n[center]Press [color={Palette.ToHex(Palette.Default)}]a-z[/color] to assign new key | ({closeKey} to cancel)[/center]";
        }
    }
}
