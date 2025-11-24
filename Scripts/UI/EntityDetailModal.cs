using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Components;
using PitsOfDespair.Systems.Input;
using PitsOfDespair.Systems.Input.Processors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal for viewing entity details in examine mode.
/// </summary>
public partial class EntityDetailModal : CenterContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    // Approximate line width for the modal content area (monospace chars)
    private const int LineWidth = 52;

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

        string glyph = $"[color={Palette.ToHex(entity.GlyphColor)}]{entity.Glyph}[/color]";
        string name = $"[color={Palette.ToHex(entity.GlyphColor)}]{entity.DisplayName}[/color]";

        // Health Status - get both plain text and colored version
        var (healthText, healthColor) = GetHealthStatusParts(entity);
        string healthStatus = $"[color={Palette.ToHex(healthColor)}]{healthText}[/color]";

        // Name and health on same line with space padding (monospace font)
        int leftLen = entity.Glyph.Length + 1 + entity.DisplayName.Length; // "G Name"
        int rightLen = healthText.Length;
        int padding = Math.Max(1, LineWidth - leftLen - rightLen);
        string headerRow = $"{glyph} {name}{new string(' ', padding)}{healthStatus}";

        // Description
        string description = string.IsNullOrEmpty(entity.Description)
            ? "No description available."
            : entity.Description;
        string descriptionSection = $"[color={Palette.ToHex(Palette.AshGray)}]{description}[/color]";

        // Resistances/Vulnerabilities
        string resistanceSection = BuildResistanceDisplay(entity);

        // Commands
        var closeKey = KeybindingConfig.GetKeybindingDisplay(InputAction.ModalClose);
        string commands = $"\n\n[color={Palette.ToHex(Palette.Disabled)}]Commands:[/color]\n";
        commands += $"[color={Palette.ToHex(Palette.Default)}]{closeKey}[/color] Close";

        string content = $"{headerRow}\n\n" +
                         $"{descriptionSection}" +
                         $"{resistanceSection}" +
                         $"{commands}";

        _contentLabel.Text = content;
    }

    private (string text, Color color) GetHealthStatusParts(BaseEntity entity)
    {
        var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null) return ("Unknown", Palette.AshGray);

        if (entity.IsDead) return ("Dead", Palette.Crimson);

        float hpPercent = (float)health.CurrentHealth / health.MaxHealth;

        if (hpPercent >= 1.0f) return ("Uninjured", Palette.Success);
        if (hpPercent >= 0.75f) return ("Slightly Wounded", Palette.Success);
        if (hpPercent >= 0.5f) return ("Wounded", Palette.Caution);
        if (hpPercent >= 0.25f) return ("Severely Wounded", Palette.Alert);
        return ("Near Death", Palette.Crimson);
    }

    private string BuildResistanceDisplay(BaseEntity entity)
    {
        var health = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null) return "";

        var sentences = new List<string>();

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

        if (sentences.Count == 0)
            return "";

        string combined = string.Join(" ", sentences);
        return $"\n\n[color={Palette.ToHex(Palette.AshGray)}]{combined}[/color]";
    }

    private string FormatDamageTypeList(List<DamageType> types)
    {
        var names = types.Select(t => t.ToString().ToLower()).ToList();

        if (names.Count == 1)
            return names[0];
        if (names.Count == 2)
            return $"{names[0]} and {names[1]}";

        // Oxford comma for 3+
        return string.Join(", ", names.Take(names.Count - 1)) + ", and " + names.Last();
    }
}
