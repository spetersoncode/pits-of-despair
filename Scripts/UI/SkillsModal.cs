using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Skills;
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
    public delegate void SkillSelectedEventHandler(string skillId);

    [Signal]
    public delegate void CancelledEventHandler();

    private RichTextLabel _skillsLabel;
    private Player _player;
    private SkillComponent _skillComponent;
    private WillpowerComponent _willpowerComponent;
    private DataLoader _dataLoader;
    private bool _isVisible = false;

    // Currently displayed skills (for number key selection)
    private List<SkillDefinition> _displayedSkills = new();

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
        // Cancel on close key
        if (MenuInputProcessor.IsCloseKey(keyEvent))
        {
            EmitSignal(SignalName.Cancelled);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check for number keys 1-9, 0
        int selectedIndex = GetNumberKeyIndex(keyEvent);

        if (selectedIndex >= 0 && selectedIndex < _displayedSkills.Count)
        {
            var selectedSkill = _displayedSkills[selectedIndex];

            // Only allow activating active skills
            if (selectedSkill.GetCategory() == SkillCategory.Active)
            {
                EmitSignal(SignalName.SkillSelected, selectedSkill.Id);
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private int GetNumberKeyIndex(InputEventKey keyEvent)
    {
        // Keys 1-9 map to indices 0-8
        if (keyEvent.Keycode >= Key.Key1 && keyEvent.Keycode <= Key.Key9)
        {
            return (int)(keyEvent.Keycode - Key.Key1);
        }
        if (keyEvent.Keycode >= Key.Kp1 && keyEvent.Keycode <= Key.Kp9)
        {
            return (int)(keyEvent.Keycode - Key.Kp1);
        }

        // Key 0 maps to index 9
        if (keyEvent.Keycode == Key.Key0 || keyEvent.Keycode == Key.Kp0)
        {
            return 9;
        }

        return -1;
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
        sb.Append(BuildHeader());

        // Group by category
        var activeSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Active).OrderBy(s => s.Name).ToList();
        var passiveSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Passive).OrderBy(s => s.Name).ToList();
        var reactiveSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Reactive).OrderBy(s => s.Name).ToList();
        var auraSkills = learnedSkills.Where(s => s.GetCategory() == SkillCategory.Aura).OrderBy(s => s.Name).ToList();

        // Build displayed skills list (only active skills are selectable)
        _displayedSkills.Clear();
        _displayedSkills.AddRange(activeSkills);

        int currentIndex = 0;
        int currentWP = _willpowerComponent?.CurrentWillpower ?? 0;

        // Active Skills section
        if (activeSkills.Count > 0)
        {
            sb.AppendLine($"\n[color={Palette.ToHex(Palette.Cyan)}][b]─── Active Skills ───[/b][/color]");
            foreach (var skill in activeSkills)
            {
                string keyDisplay = currentIndex < 9 ? (currentIndex + 1).ToString() : "0";
                bool canAfford = currentWP >= skill.WillpowerCost;
                sb.AppendLine(FormatActiveSkill(skill, keyDisplay, canAfford));
                currentIndex++;
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

    private string FormatActiveSkill(SkillDefinition skill, string keyDisplay, bool canAfford)
    {
        string wpCost = skill.WillpowerCost > 0 ? $"[color={Palette.ToHex(Palette.Wizard)}]{skill.WillpowerCost} WP[/color]" : "[color=#888888]Free[/color]";
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
        string wpCost = skill.WillpowerCost > 0 ? $" ({skill.WillpowerCost} WP)" : "";
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
            _skillsLabel.Text = $"[center][b]Skills[/b][/center]\n[center]({closeKey} to close)[/center]\n\n[center][color={Palette.ToHex(Palette.Disabled)}]No skills learned[/color][/center]";
        }
    }

    private string BuildHeader()
    {
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        int currentWP = _willpowerComponent?.CurrentWillpower ?? 0;
        int maxWP = _willpowerComponent?.MaxWillpower ?? 0;
        return $"[center][b]Skills[/b][/center]\n[center]WP: [color={Palette.ToHex(Palette.Wizard)}]{currentWP}/{maxWP}[/color] | ({closeKey} to close)[/center]";
    }
}
