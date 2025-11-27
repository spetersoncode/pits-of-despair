using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using PitsOfDespair.Systems.Entity;
using System.Linq;
using System.Text;

namespace PitsOfDespair.UI;

/// <summary>
/// Side panel displaying player information including health, stats, equipment, and nearby entities.
/// </summary>
public partial class SidePanel : PanelContainer
{
    private const float HealthGreenThreshold = 0.6f;
    private const float HealthYellowThreshold = 0.3f;

    private static readonly Color HealthColorGreen = Palette.HealthFull;
    private static readonly Color HealthColorYellow = Palette.HealthMedium;
    private static readonly Color HealthColorRed = Palette.HealthCritical;
    private static readonly Color DefaultTextColor = Palette.Default;
    private static readonly Color ExperienceBarColor = Palette.Gold;
    private static readonly Color WillpowerBarColor = new Color(0.4f, 0.4f, 0.8f); // Blue-purple for willpower

    private ProgressBar _healthBar;
    private Label _healthLabel;
    private ProgressBar _willpowerBar;
    private Label _willpowerLabel;
    private ProgressBar _experienceBar;
    private RichTextLabel _xpGoldLabel;
    private RichTextLabel _levelFloorLabel;
    private RichTextLabel _equipmentLabel;
    private RichTextLabel _statsLabel;
    private RichTextLabel _visibleEntitiesLabel;

    private Entities.Player _player;

    // ViewModels for decoupled data access
    private ViewModels.PlayerStatsViewModel _statsViewModel;
    private ViewModels.EquipmentViewModel _equipmentViewModel;
    private NearbyEntitiesTracker _nearbyEntitiesTracker;
    private Systems.TurnManager _turnManager;

    // Pending level-up tracking
    private int _pendingLevelUps = 0;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/HealthBar");
        _healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthLabel");
        _willpowerBar = GetNodeOrNull<ProgressBar>("MarginContainer/VBoxContainer/WillpowerBar");
        _willpowerLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/WillpowerLabel");
        _experienceBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/ExperienceBar");
        _xpGoldLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/XPGoldLabel");
        _levelFloorLabel = GetNode<RichTextLabel>("MarginContainer/VBoxContainer/LevelFloorLabel");
        _equipmentLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/EquipmentLabel");
        _statsLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/StatsLabel");
        _visibleEntitiesLabel = GetNodeOrNull<RichTextLabel>("MarginContainer/VBoxContainer/ScrollContainer/VisibleEntitiesLabel");

        UpdateXPGoldDisplay();
        UpdateLevelFloorDisplay();
        UpdateEquipmentDisplay();
        UpdateStatsDisplay();
        UpdateVisibleEntitiesDisplay();
    }

    /// <summary>
    /// Initializes the side panel with ViewModels and systems.
    /// </summary>
    public void Initialize(
        Entities.Player player,
        ViewModels.PlayerStatsViewModel statsViewModel,
        ViewModels.EquipmentViewModel equipmentViewModel,
        NearbyEntitiesTracker nearbyEntitiesTracker,
        Systems.TurnManager turnManager = null)
    {
        _player = player;
        _statsViewModel = statsViewModel;
        _equipmentViewModel = equipmentViewModel;
        _nearbyEntitiesTracker = nearbyEntitiesTracker;
        _turnManager = turnManager;

        // Connect to ViewModel signals (single source for all stats updates)
        _statsViewModel.Connect(
            ViewModels.PlayerStatsViewModel.SignalName.StatsUpdated,
            Callable.From(OnStatsUpdated)
        );

        _equipmentViewModel.Connect(
            ViewModels.EquipmentViewModel.SignalName.EquipmentDisplayUpdated,
            Callable.From(OnEquipmentUpdated)
        );

        _nearbyEntitiesTracker.Connect(
            NearbyEntitiesTracker.SignalName.NearbyEntitiesChanged,
            Callable.From<Godot.Collections.Array>(OnNearbyEntitiesChanged)
        );

        // Connect to TurnManager to refresh intents after creature turns
        if (_turnManager != null)
        {
            _turnManager.Connect(
                Systems.TurnManager.SignalName.CreatureTurnsEnded,
                Callable.From(OnCreatureTurnsEnded)
            );
        }

        // Initialize display with current values
        UpdateAllDisplays();
    }

    /// <summary>
    /// Called after all creatures have taken their turns.
    /// Refreshes visible entities display to show updated intents.
    /// </summary>
    private void OnCreatureTurnsEnded()
    {
        UpdateVisibleEntitiesDisplay();
    }

    /// <summary>
    /// Updates all display panels.
    /// </summary>
    private void UpdateAllDisplays()
    {
        UpdateHealthDisplay();
        UpdateWillpowerDisplay();
        UpdateXPGoldDisplay();
        UpdateLevelFloorDisplay();
        UpdateStatsDisplay();
        UpdateEquipmentDisplay();
        UpdateVisibleEntitiesDisplay();
    }

    /// <summary>
    /// Called when PlayerStatsViewModel updates.
    /// Refreshes all stat-related displays.
    /// </summary>
    private void OnStatsUpdated()
    {
        UpdateHealthDisplay();
        UpdateWillpowerDisplay();
        UpdateXPGoldDisplay();
        UpdateLevelFloorDisplay();
        UpdateStatsDisplay();
    }

    /// <summary>
    /// Called when EquipmentViewModel updates.
    /// Refreshes equipment display.
    /// </summary>
    private void OnEquipmentUpdated()
    {
        UpdateEquipmentDisplay();
    }

    /// <summary>
    /// Called when NearbyEntitiesTracker updates.
    /// Refreshes visible entities display.
    /// </summary>
    private void OnNearbyEntitiesChanged(Godot.Collections.Array nearbyEntities)
    {
        UpdateVisibleEntitiesDisplay();
    }

    private void UpdateHealthDisplay()
    {
        if (_healthBar == null || _healthLabel == null || _statsViewModel == null)
            return;

        int current = _statsViewModel.CurrentHealth;
        int max = _statsViewModel.MaxHealth;

        float healthPercent = max > 0 ? (float)current / max : 0f;
        Color healthColor = GetHealthColor(healthPercent);

        // Update progress bar
        _healthBar.MaxValue = max;
        _healthBar.Value = current;

        // Create StyleBoxFlat for the progress bar fill with color gradient
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = healthColor;
        _healthBar.AddThemeStyleboxOverride("fill", styleBox);

        // Update text label
        _healthLabel.Text = $"Health: {current}/{max}";
        _healthLabel.AddThemeColorOverride("font_color", healthColor);
    }

    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > HealthGreenThreshold)
        {
            return HealthColorGreen;
        }
        else if (healthPercent > HealthYellowThreshold)
        {
            return HealthColorYellow;
        }
        else
        {
            return HealthColorRed;
        }
    }

    private void UpdateWillpowerDisplay()
    {
        if (_willpowerBar == null || _willpowerLabel == null || _statsViewModel == null)
            return;

        int current = _statsViewModel.CurrentWillpower;
        int max = _statsViewModel.MaxWillpower;

        // Update progress bar
        _willpowerBar.MaxValue = max;
        _willpowerBar.Value = current;

        // Create StyleBoxFlat for the progress bar fill
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = WillpowerBarColor;
        _willpowerBar.AddThemeStyleboxOverride("fill", styleBox);

        // Update text label
        _willpowerLabel.Text = $"Willpower: {current}/{max}";
        _willpowerLabel.AddThemeColorOverride("font_color", WillpowerBarColor);
    }

    private const int SidePanelLabelWidth = 27;

    private void UpdateXPGoldDisplay()
    {
        if (_xpGoldLabel == null || _statsViewModel == null)
            return;

        int currentXP = _statsViewModel.CurrentXP;
        int xpToNext = _statsViewModel.XPToNextLevel;

        _experienceBar.MaxValue = xpToNext;
        _experienceBar.Value = currentXP;
        _experienceBar.AddThemeStyleboxOverride("fill", CreateExperienceBarStyle());

        string xpText = $"XP: {currentXP}/{xpToNext}";
        string goldText = $"Gold: {_statsViewModel.Gold}";
        string colorHex = ColorToHex(ExperienceBarColor);
        string paddedText = PadBetween(xpText, goldText, SidePanelLabelWidth);
        _xpGoldLabel.Text = $"[color={colorHex}]{paddedText}[/color]";
    }

    private void UpdateLevelFloorDisplay()
    {
        if (_levelFloorLabel == null || _statsViewModel == null)
            return;

        int level = _statsViewModel.Level;
        string floorText = $"Floor: {_statsViewModel.FloorDepth}";

        // Show level-up indicator if pending level-ups exist
        if (_pendingLevelUps > 0)
        {
            string levelText = "LEVEL UP! [L]";
            string levelColorHex = ColorToHex(Palette.Alert);
            string floorColorHex = ColorToHex(DefaultTextColor);
            string paddedText = PadBetween(levelText, floorText, SidePanelLabelWidth);
            int levelLen = levelText.Length;
            _levelFloorLabel.Text = $"[color={levelColorHex}]{paddedText.Substring(0, levelLen)}[/color]{paddedText.Substring(levelLen, paddedText.Length - levelLen - floorText.Length)}[color={floorColorHex}]{floorText}[/color]";
        }
        else
        {
            string levelText = $"Level: {level}";
            string colorHex = ColorToHex(DefaultTextColor);
            string paddedText = PadBetween(levelText, floorText, SidePanelLabelWidth);
            _levelFloorLabel.Text = $"[color={colorHex}]{paddedText}[/color]";
        }
    }

    private static string ColorToHex(Color color)
    {
        return $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
    }

    /// <summary>
    /// Sets the number of pending level-ups for the HUD indicator.
    /// </summary>
    /// <param name="count">Number of pending level-ups</param>
    public void SetPendingLevelUps(int count)
    {
        _pendingLevelUps = count;
        UpdateLevelFloorDisplay();
    }

    private StyleBoxFlat CreateExperienceBarStyle()
    {
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = ExperienceBarColor;
        return styleBox;
    }

    /// <summary>
    /// Creates a string with left text and right text separated by space padding to fill totalWidth.
    /// </summary>
    private static string PadBetween(string left, string right, int totalWidth)
    {
        int spacesNeeded = totalWidth - left.Length - right.Length;
        if (spacesNeeded <= 0)
            return left + " " + right;
        return left + new string(' ', spacesNeeded) + right;
    }

    private void UpdateEquipmentDisplay()
    {
        if (_equipmentLabel == null || _equipmentViewModel == null)
            return;

        var sb = new StringBuilder();

        // Use EquipmentViewModel for all equipment data
        foreach (var slot in _equipmentViewModel.AllSlots)
        {
            sb.Append($"{slot.SlotName}: ");

            if (slot.IsEquipped)
            {
                string colorHex = ColorToHex(slot.ItemColor);
                // Show quantity for ammo
                string quantityStr = slot.SlotName == "Ammo" ? $" ({slot.Quantity})" : "";
                sb.AppendLine($"[color={colorHex}]{slot.ItemGlyph} {slot.ItemName}{quantityStr}[/color]");
            }
            else
            {
                sb.AppendLine(slot.ItemName); // "(none)" or "(error)"
            }
        }

        _equipmentLabel.Text = sb.ToString();
    }

    private void UpdateStatsDisplay()
    {
        if (_statsLabel == null || _statsViewModel == null || _player == null)
            return;

        var sb = new StringBuilder();

        // Base stats - full names, one per line (from ViewModel)
        sb.AppendLine($"Strength: {_statsViewModel.TotalStrength}");
        sb.AppendLine($"Agility: {_statsViewModel.TotalAgility}");
        sb.AppendLine($"Endurance: {_statsViewModel.TotalEndurance}");
        sb.AppendLine($"Will: {_statsViewModel.TotalWill}");

        sb.AppendLine(); // Blank line for spacing

        // Derived combat values (from ViewModel)
        int meleeAttack = _statsViewModel.MeleeAttack;
        int rangedAttack = _statsViewModel.RangedAttack;
        int armor = _statsViewModel.TotalArmor;
        int evasion = _statsViewModel.TotalEvasion;

        // Get attack component for damage dice (still needs direct access)
        var attackComponent = _player.GetNodeOrNull<Components.AttackComponent>("AttackComponent");
        string meleeDamage = "-";
        string rangedDamage = "-";

        if (attackComponent != null)
        {
            // Find melee and ranged attacks
            foreach (var attack in attackComponent.Attacks)
            {
                if (attack.Type == Data.AttackType.Melee)
                {
                    int damageBonus = _statsViewModel.TotalStrength;
                    meleeDamage = Helpers.DiceRoller.AddBonus(attack.DiceNotation, damageBonus);
                }
                else if (attack.Type == Data.AttackType.Ranged)
                {
                    rangedDamage = attack.DiceNotation;
                }
            }
        }

        sb.AppendLine($"Melee Attack: {meleeAttack}");
        sb.AppendLine($"Melee Damage: {meleeDamage}");
        sb.AppendLine($"Ranged Attack: {rangedAttack}");
        sb.AppendLine($"Ranged Damage: {rangedDamage}");
        sb.AppendLine($"Armor: {armor}");
        sb.AppendLine($"Evasion: {evasion}");

        _statsLabel.Text = sb.ToString();
    }

    private void UpdateVisibleEntitiesDisplay()
    {
        if (_visibleEntitiesLabel == null || _nearbyEntitiesTracker == null)
            return;

        // Get nearby entities from tracker
        var nearbyEntities = _nearbyEntitiesTracker.GetNearbyEntities();

        // Build display text with colors and glyphs
        var sb = new StringBuilder();

        if (nearbyEntities.Count == 0)
        {
            sb.Append("(nothing visible)");
        }
        else
        {
            foreach (var (entity, distance) in nearbyEntities)
            {
                // Convert entity color to hex for BBCode
                string colorHex = $"#{(int)(entity.GlyphColor.R * 255):X2}{(int)(entity.GlyphColor.G * 255):X2}{(int)(entity.GlyphColor.B * 255):X2}";

                // Use item's display name with quantity for items, otherwise use entity DisplayName
                string displayName = entity.ItemData != null
                    ? entity.ItemData.Template.GetDisplayName(entity.ItemData.Quantity)
                    : entity.DisplayName;

                // Check for AI component to show intent for creatures
                var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
                string intentSuffix = "";
                if (aiComponent != null)
                {
                    var intent = aiComponent.GetIntent();
                    string intentName = IntentHelper.GetShortName(intent);
                    if (!string.IsNullOrEmpty(intentName))
                    {
                        string intentColorHex = Palette.ToHex(IntentHelper.GetColor(intent));
                        intentSuffix = $" [color={intentColorHex}]({intentName})[/color]";
                    }
                }

                sb.AppendLine($"[color={colorHex}]{entity.Glyph} {displayName}[/color]{intentSuffix}");
            }
        }

        _visibleEntitiesLabel.Text = sb.ToString().TrimEnd();
    }

    public override void _ExitTree()
    {
        // Disconnect from ViewModels
        if (_statsViewModel != null)
        {
            _statsViewModel.Disconnect(
                ViewModels.PlayerStatsViewModel.SignalName.StatsUpdated,
                Callable.From(OnStatsUpdated)
            );
        }

        if (_equipmentViewModel != null)
        {
            _equipmentViewModel.Disconnect(
                ViewModels.EquipmentViewModel.SignalName.EquipmentDisplayUpdated,
                Callable.From(OnEquipmentUpdated)
            );
        }

        if (_nearbyEntitiesTracker != null)
        {
            _nearbyEntitiesTracker.Disconnect(
                NearbyEntitiesTracker.SignalName.NearbyEntitiesChanged,
                Callable.From<Godot.Collections.Array>(OnNearbyEntitiesChanged)
            );
        }

        if (_turnManager != null)
        {
            _turnManager.Disconnect(
                Systems.TurnManager.SignalName.CreatureTurnsEnded,
                Callable.From(OnCreatureTurnsEnded)
            );
        }
    }
}
