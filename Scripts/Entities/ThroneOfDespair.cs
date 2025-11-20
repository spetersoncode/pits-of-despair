using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Entities;

/// <summary>
/// Represents the Throne of Despair, the ultimate goal of the dungeon.
/// Reaching the throne triggers victory.
/// </summary>
public partial class ThroneOfDespair : BaseEntity
{
	public override void _Ready()
	{
		DisplayName = "Throne of Despair";
		Glyph = "╬";  // Ornate cross/throne symbol
		GlyphColor = Palette.Throne;
		IsWalkable = true; // Player can walk onto the throne
		Description = "An ancient throne carved from obsidian and bone, pulsing with dark power. Countless souls sought this seat of despair—you are the first to reach it.";
	}

	/// <summary>
	/// Initialize the throne at a specific position.
	/// </summary>
	/// <param name="position">Grid position to place the throne.</param>
	public void Initialize(GridPosition position)
	{
		GridPosition = position;
	}
}
