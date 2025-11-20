using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Entities;

/// <summary>
/// Represents stairs descending to the next dungeon floor.
/// Walking onto the stairs allows the player to descend deeper into the dungeon.
/// </summary>
public partial class Stairs : BaseEntity
{
	public override void _Ready()
	{
		DisplayName = "stairs down";
		Glyph = ">";
		GlyphColor = Palette.Stairs;
		IsWalkable = true; // Player can walk onto stairs
		Description = "Stone steps descending into darkness. The air grows colder as you peer into the depths below.";
	}

	/// <summary>
	/// Initialize the stairs at a specific position.
	/// </summary>
	/// <param name="position">Grid position to place the stairs.</param>
	public void Initialize(GridPosition position)
	{
		GridPosition = position;
	}
}
