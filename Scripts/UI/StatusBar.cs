using Godot;
using PitsOfDespair.Entities;
using System.Linq;

namespace PitsOfDespair.UI;

/// <summary>
/// Thin status bar displaying player's active conditions.
/// Shows conditions that have ExamineDescription set (non-mundane effects).
/// </summary>
public partial class StatusBar : PanelContainer
{
    private RichTextLabel _label;
    private Player _player;

    public override void _Ready()
    {
        _label = GetNode<RichTextLabel>("MarginContainer/Label");
    }

    /// <summary>
    /// Initializes the status bar with the player reference.
    /// </summary>
    public void Initialize(Player player)
    {
        _player = player;

        // Connect to condition change signals
        _player.Connect(
            BaseEntity.SignalName.ConditionAdded,
            Callable.From<string>(OnConditionChanged)
        );
        _player.Connect(
            BaseEntity.SignalName.ConditionRemoved,
            Callable.From<string>(OnConditionChanged)
        );

        // Initial update
        UpdateDisplay();
    }

    private void OnConditionChanged(string conditionName)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_label == null || _player == null)
            return;

        // Get displayable conditions (those with ExamineDescription)
        var conditions = _player.GetActiveConditions()
            .Where(c => c.ExamineDescription != null)
            .Select(c => c.ExamineDescription!)
            .Distinct()
            .ToList();

        if (conditions.Count == 0)
        {
            _label.Text = "";
        }
        else
        {
            // Capitalize each condition for display
            var capitalized = conditions.Select(c => char.ToUpper(c[0]) + c[1..]);
            _label.Text = string.Join("  ", capitalized);
        }
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
    }
}
