using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Entities;

/// <summary>
/// Represents a pile of gold that can be collected by the player.
/// Gold is automatically collected when the player walks over it.
/// </summary>
public partial class Gold : BaseEntity
{
    public override EntityType Type => EntityType.Gold;
    /// <summary>
    /// Amount of gold in this pile.
    /// </summary>
    public int Amount { get; set; } = 1;

    public override void _Ready()
    {
        // Set gold visual properties
        Glyph = "*";
        GlyphColor = Palette.Gold;
        IsWalkable = true; // Player can walk onto gold tiles to collect it
        Description = "Gleaming coins scattered across the dungeon floor. Someone's fortune, now yours for the taking.";

        // Update display name to show amount
        UpdateDisplayName();
    }

    /// <summary>
    /// Initialize the gold pile with a specific amount and position.
    /// </summary>
    /// <param name="amount">Amount of gold in this pile.</param>
    /// <param name="position">Grid position to place the gold.</param>
    public void Initialize(int amount, GridPosition position)
    {
        Amount = amount;
        GridPosition = position;
        UpdateDisplayName();
    }

    /// <summary>
    /// Updates the display name to reflect the current amount.
    /// </summary>
    private void UpdateDisplayName()
    {
        DisplayName = Amount == 1 ? "1 gold" : $"{Amount} gold";
    }
}
