using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Skills;
using System.Linq;

namespace PitsOfDespair.UI;

/// <summary>
/// Thin status bar displaying player's active conditions and toggle skills.
/// Shows conditions that have ExamineDescription set (non-mundane effects).
/// </summary>
public partial class StatusBar : PanelContainer
{
    private RichTextLabel _label;
    private Player _player;
    private ToggleSkillProcessor _toggleProcessor;
    private DataLoader _dataLoader;

    public override void _Ready()
    {
        _label = GetNode<RichTextLabel>("MarginContainer/Label");
        _dataLoader = GetNode<DataLoader>("/root/DataLoader");
    }

    /// <summary>
    /// Initializes the status bar with the player reference.
    /// </summary>
    public void Initialize(Player player)
    {
        _player = player;
        _toggleProcessor = _player.GetNodeOrNull<ToggleSkillProcessor>("ToggleSkillProcessor");

        // Connect to condition change signals
        _player.Connect(
            BaseEntity.SignalName.ConditionAdded,
            Callable.From<string>(OnConditionChanged)
        );
        _player.Connect(
            BaseEntity.SignalName.ConditionRemoved,
            Callable.From<string>(OnConditionChanged)
        );

        // Connect to toggle skill signals
        if (_toggleProcessor != null)
        {
            _toggleProcessor.Connect(
                ToggleSkillProcessor.SignalName.ToggleActivated,
                Callable.From<string, string>(OnToggleChanged)
            );
            _toggleProcessor.Connect(
                ToggleSkillProcessor.SignalName.ToggleDeactivated,
                Callable.From<string, string>(OnToggleChanged)
            );
        }

        // Initial update
        UpdateDisplay();
    }

    private void OnConditionChanged(string conditionName)
    {
        UpdateDisplay();
    }

    private void OnToggleChanged(string skillId, string skillName)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_label == null || _player == null)
            return;

        var displayItems = new System.Collections.Generic.List<string>();

        // Get active toggle skill names (shown in cyan)
        if (_toggleProcessor != null && _dataLoader != null)
        {
            foreach (var skillId in _toggleProcessor.GetActiveToggles())
            {
                var skill = _dataLoader.Skills.Get(skillId);
                if (skill != null)
                {
                    displayItems.Add($"[color={Palette.ToHex(Palette.Cyan)}]{skill.Name}[/color]");
                }
            }
        }

        // Get displayable conditions (those with ExamineDescription)
        var conditions = _player.GetActiveConditions()
            .Where(c => c.ExamineDescription != null)
            .Select(c => c.ExamineDescription!)
            .Distinct()
            .ToList();

        // Capitalize each condition for display
        foreach (var condition in conditions)
        {
            string capitalized = char.ToUpper(condition[0]) + condition[1..];
            displayItems.Add(capitalized);
        }

        _label.Text = displayItems.Count > 0 ? string.Join("  ", displayItems) : "";
    }

    public override void _ExitTree()
    {
        if (_player != null)
        {
            _player.Disconnect(
                BaseEntity.SignalName.ConditionAdded,
                Callable.From<string>(OnConditionChanged)
            );
            _player.Disconnect(
                BaseEntity.SignalName.ConditionRemoved,
                Callable.From<string>(OnConditionChanged)
            );
        }

        if (_toggleProcessor != null && IsInstanceValid(_toggleProcessor))
        {
            _toggleProcessor.Disconnect(
                ToggleSkillProcessor.SignalName.ToggleActivated,
                Callable.From<string, string>(OnToggleChanged)
            );
            _toggleProcessor.Disconnect(
                ToggleSkillProcessor.SignalName.ToggleDeactivated,
                Callable.From<string, string>(OnToggleChanged)
            );
        }
    }
}
