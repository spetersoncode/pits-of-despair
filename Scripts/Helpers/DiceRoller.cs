using Godot;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized dice rolling utility for combat and other game systems.
/// Provides 2d6 rolls with proper bell curve distribution.
/// </summary>
public static class DiceRoller
{
	/// <summary>
	/// Rolls 2d6 with an optional modifier.
	/// Uses two separate d6 rolls to create a bell curve distribution (7 average).
	/// </summary>
	/// <param name="modifier">Modifier to add to the roll (can be positive or negative)</param>
	/// <returns>Total of 2d6 + modifier</returns>
	public static int Roll2d6(int modifier = 0)
	{
		int die1 = GD.RandRange(1, 6);
		int die2 = GD.RandRange(1, 6);
		return die1 + die2 + modifier;
	}

	/// <summary>
	/// Rolls a specified number of dice with a given number of sides.
	/// </summary>
	/// <param name="count">Number of dice to roll</param>
	/// <param name="sides">Number of sides per die</param>
	/// <param name="modifier">Modifier to add to the total</param>
	/// <returns>Total of all dice + modifier</returns>
	public static int Roll(int count, int sides, int modifier = 0)
	{
		int total = modifier;
		for (int i = 0; i < count; i++)
		{
			total += GD.RandRange(1, sides);
		}
		return total;
	}

	/// <summary>
	/// Rolls damage within a min-max range (simulating weapon damage dice).
	/// </summary>
	/// <param name="minDamage">Minimum damage value</param>
	/// <param name="maxDamage">Maximum damage value</param>
	/// <returns>Random damage value between min and max (inclusive)</returns>
	public static int RollDamage(int minDamage, int maxDamage)
	{
		return GD.RandRange(minDamage, maxDamage);
	}
}
