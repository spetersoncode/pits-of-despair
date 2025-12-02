using Godot;
using PitsOfDespair.AI;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Helpers;
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
    private const int EquipmentSlotLabelWidth = 6; // Length of "Ranged", the longest slot name

    private static readonly Color HealthColorGreen = Palette.HealthFull;
    private static readonly Color HealthColorYellow = Palette.HealthMedium;
    private static readonly Color HealthColorRed = Palette.HealthCritical;
    private static readonly Color DefaultTextColor = Palette.Default;
    private static readonly Color ExperienceBarColor = Palette.ExperienceBar;
    private static readonly Color WillpowerBarColor = Palette.WillpowerBar;

    private Label _playerNameLabel;
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private ProgressBar _willpowerBar;
    private Label _willpowerLabel;
    private ProgressBar _experienceBar;
    private Label _experienceLabel;
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
        _playerNameLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/PlayerNameLabel");
        _healthBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/HealthBarContainer/HealthBar");
        _healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthBarContainer/HealthLabel");
        _willpowerBar = GetNodeOrNull<ProgressBar>("MarginContainer/VBoxContainer/WillpowerBarContainer/WillpowerBar");
        _willpowerLabel = GetNodeOrNull<Label>("MarginContainer/VBoxContainer/WillpowerBarContainer/WillpowerLabel");
        _experienceBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/ExperienceBarContainer/ExperienceBar");
        _experienceLabel = GetNode<Label>("MarginContainer/VBoxContainer/ExperienceBarContainer/ExperienceLabel");
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
        UpdatePlayerNameDisplay();
        UpdateHealthDisplay();
        UpdateWillpowerDisplay();
        UpdateXPGoldDisplay();
        UpdateLevelFloorDisplay();
        UpdateStatsDisplay();
        UpdateEquipmentDisplay();
        UpdateVisibleEntitiesDisplay();
    }

    private void UpdatePlayerNameDisplay()
    {
        if (_playerNameLabel == null || _statsViewModel == null)
            return;

        _playerNameLabel.Text = _statsViewModel.FormattedName;
        _playerNameLabel.AddThemeColorOverride("font_color", Palette.Player);
    }

    /// <summary>
    /// Called when PlayerStatsViewModel updates.
    /// Refreshes all stat-related displays.
    /// </summary>
    private void OnStatsUpdated()
    {
        UpdatePlayerNameDisplay();
        UpdateHealthDisplay();
        UpdateWillpowerDisplay();
        UpdateXPGoldDisplay();
        UpdateLevelFloorDisplay();
        UpdateStatsDisplay();
    }

    /// <summary>
    /// Called when EquipmentViewModel updates.
    /// Refreshes equipment display and stats (attack speed depends on weapon).
    /// </summary>
    private void OnEquipmentUpdated()
    {
        UpdateEquipmentDisplay();
        UpdateStatsDisplay();
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

        // Update text label (white color set in scene for readability)
        _healthLabel.Text = $"Health: {current}/{max}";
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

        // Update text label (white color set in scene for readability)
        _willpowerLabel.Text = $"Willpower: {current}/{max}";
    }

    private const int SidePanelLabelWidth = 27;

    private void UpdateXPGoldDisplay()
    {
        if (_statsViewModel == null)
            return;

        int currentXP = _statsViewModel.CurrentXP;
        int xpToNext = _statsViewModel.XPToNextLevel;

        // Update progress bar
        if (_experienceBar != null)
        {
            _experienceBar.MaxValue = xpToNext;
            _experienceBar.Value = currentXP;
            _experienceBar.AddThemeStyleboxOverride("fill", CreateExperienceBarStyle());
        }

        // Update label inside the bar
        if (_experienceLabel != null)
        {
            _experienceLabel.Text = $"XP: {currentXP}/{xpToNext}";
        }

        // Update level/floor label below the bar
        if (_xpGoldLabel != null)
        {
            string colorHex = ColorToHex(DefaultTextColor);
            string levelText = $"Level: {_statsViewModel.Level}";
            string floorText = $"Floor: {_statsViewModel.FloorDepth}";
            string paddedText = PadBetween(levelText, floorText, SidePanelLabelWidth);

            // Show level-up indicator if pending level-ups exist
            if (_pendingLevelUps > 0)
            {
                string levelUpText = "LEVEL UP! [L]";
                string levelColorHex = ColorToHex(Palette.Alert);
                _xpGoldLabel.Text = $"[color={colorHex}]{paddedText}[/color]\n[color={levelColorHex}]{levelUpText}[/color]";
            }
            else
            {
                _xpGoldLabel.Text = $"[color={colorHex}]{paddedText}[/color]";
            }
        }
    }

    private void UpdateLevelFloorDisplay()
    {
        if (_levelFloorLabel == null || _statsViewModel == null)
            return;

        string colorHex = ColorToHex(DefaultTextColor);
        _levelFloorLabel.Text = $"[color={colorHex}]Gold: {_statsViewModel.Gold}[/color]";
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
        UpdateXPGoldDisplay();
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
            sb.Append($"{slot.SlotName.PadRight(EquipmentSlotLabelWidth)}: ");

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

        // Base stats - 2 column layout (left-aligned columns)
        const int statColumnWidth = 16;
        sb.AppendLine($"{"Strength: " + _statsViewModel.TotalStrength,-statColumnWidth}Agility: {_statsViewModel.TotalAgility}");
        sb.AppendLine($"{"Endurance: " + _statsViewModel.TotalEndurance,-statColumnWidth}Will: {_statsViewModel.TotalWill}");
        sb.AppendLine($"{"Armor: " + _statsViewModel.TotalArmor,-statColumnWidth}Evasion: {_statsViewModel.TotalEvasion}");

        sb.AppendLine(); // Blank line for spacing

        // Derived combat values (from ViewModel)
        int meleeAttack = _statsViewModel.MeleeAttack;
        int rangedAttack = _statsViewModel.RangedAttack;

        // Get attack component for DPT calculations
        var attackComponent = _player.GetNodeOrNull<Components.AttackComponent>("AttackComponent");
        string meleeDpt = "-";
        string rangedDpt = "-";

        if (attackComponent != null)
        {
            // Find melee and ranged attacks and calculate DPT
            foreach (var attack in attackComponent.Attacks)
            {
                if (attack.Type == Data.AttackType.Melee)
                {
                    int damageBonus = _statsViewModel.MeleeDamageBonus;
                    float avgDamage = Helpers.DiceRoller.GetAverage(attack.DiceNotation) + damageBonus;
                    float dpt = avgDamage / attack.Delay;
                    meleeDpt = dpt.ToString("F1");
                }
                else if (attack.Type == Data.AttackType.Ranged)
                {
                    float avgDamage = Helpers.DiceRoller.GetAverage(attack.DiceNotation);
                    float dpt = avgDamage / attack.Delay;
                    rangedDpt = dpt.ToString("F1");
                }
            }
        }

        sb.AppendLine($"Melee Attack: {meleeAttack}");
        sb.AppendLine($"Melee Damage/Turn: {meleeDpt}");
        sb.AppendLine($"Ranged Attack: {rangedAttack}");
        sb.AppendLine($"Ranged Damage/Turn: {rangedDpt}");

        _statsLabel.Text = sb.ToString();
    }

    /// <summary>
    /// Maximum number of entity lines to display in the nearby panel.
    /// </summary>
    private const int MaxNearbyDisplayLines = 12;

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
            // Group entities by display key (glyph + name + color) for consolidation
            var groupedEntities = GroupEntitiesByType(nearbyEntities);

            int linesDisplayed = 0;
            foreach (var group in groupedEntities)
            {
                if (linesDisplayed >= MaxNearbyDisplayLines)
                    break;

                sb.AppendLine(FormatEntityGroup(group));
                linesDisplayed++;
            }
        }

        _visibleEntitiesLabel.Text = sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Groups entities by their display type (creatures by name, items by template).
    /// Returns groups sorted by closest entity in each group.
    /// </summary>
    private System.Collections.Generic.List<EntityDisplayGroup> GroupEntitiesByType(
        System.Collections.Generic.List<(Entities.BaseEntity entity, int distance)> entities)
    {
        var groups = new System.Collections.Generic.Dictionary<string, EntityDisplayGroup>();

        foreach (var (entity, distance) in entities)
        {
            // Create a grouping key based on entity type
            string groupKey = GetEntityGroupKey(entity);

            if (!groups.TryGetValue(groupKey, out var group))
            {
                group = new EntityDisplayGroup
                {
                    Glyph = entity.Glyph,
                    GlyphColor = entity.GlyphColor,
                    DisplayName = GetEntityDisplayName(entity),
                    MinDistance = distance,
                    Count = 0,
                    Entities = new System.Collections.Generic.List<Entities.BaseEntity>()
                };
                groups[groupKey] = group;
            }

            group.Count++;
            group.Entities.Add(entity);
            if (distance < group.MinDistance)
                group.MinDistance = distance;
        }

        // Sort groups by minimum distance (closest first)
        var sortedGroups = groups.Values.ToList();
        sortedGroups.Sort((a, b) => a.MinDistance.CompareTo(b.MinDistance));
        return sortedGroups;
    }

    /// <summary>
    /// Gets a grouping key for an entity. Creatures group by name, items by template.
    /// </summary>
    private string GetEntityGroupKey(Entities.BaseEntity entity)
    {
        if (entity.ItemData != null)
        {
            // Items: group by template DataFileId and glyph (different item types stay separate)
            return $"item:{entity.ItemData.Template.DataFileId}:{entity.Glyph}";
        }
        else
        {
            // Creatures/other: group by display name and glyph
            return $"creature:{entity.DisplayName}:{entity.Glyph}";
        }
    }

    /// <summary>
    /// Gets the base display name for an entity (without quantity for items).
    /// </summary>
    private string GetEntityDisplayName(Entities.BaseEntity entity)
    {
        if (entity.ItemData != null)
        {
            // For items, use singular name (quantity handled in formatting)
            return entity.ItemData.Template.GetDisplayName(1);
        }
        return entity.DisplayName;
    }

    /// <summary>
    /// Formats an entity group for display (e.g., "3 rats" or "r rat (hostile)").
    /// </summary>
    private string FormatEntityGroup(EntityDisplayGroup group)
    {
        string colorHex = ColorToHex(group.GlyphColor);

        // Build the name with count if multiple
        string displayText;
        if (group.Count > 1)
        {
            // Multiple entities: "3 rats" - pluralize the name
            displayText = Pluralizer.FormatCount(group.DisplayName, group.Count);
        }
        else
        {
            displayText = group.DisplayName;
        }

        // For single creatures, show intent; for groups, skip (too complex)
        string intentSuffix = "";
        if (group.Count == 1 && group.Entities.Count > 0)
        {
            var entity = group.Entities[0];
            var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
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
        }

        return $"[color={colorHex}]{group.Glyph} {displayText}[/color]{intentSuffix}";
    }

    /// <summary>
    /// Helper class for grouping entities by type for display.
    /// </summary>
    private class EntityDisplayGroup
    {
        public string Glyph;
        public Color GlyphColor;
        public string DisplayName;
        public int MinDistance;
        public int Count;
        public System.Collections.Generic.List<Entities.BaseEntity> Entities;
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
