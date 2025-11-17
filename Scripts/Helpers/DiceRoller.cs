using Godot;
using System;
using System.Text.RegularExpressions;

namespace PitsOfDespair.Helpers;

/// <summary>
/// Centralized dice rolling utility for all game randomization.
/// Supports standard dice notation (e.g., "2d6+3", "1d8", "3d4-1").
/// </summary>
public static class DiceRoller
{
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
	/// Rolls dice using standard dice notation or returns a constant value.
	/// </summary>
	/// <param name="diceNotation">Dice notation string or plain number (e.g., "2d6+3", "1d8", "5")</param>
	/// <returns>Total of all dice + modifier, or the constant value</returns>
	/// <exception cref="ArgumentException">Thrown when notation is invalid</exception>
	public static int Roll(string diceNotation)
	{
		if (!TryParse(diceNotation, out int count, out int sides, out int modifier))
		{
			throw new ArgumentException($"Invalid dice notation: {diceNotation}");
		}
		return Roll(count, sides, modifier);
	}

	/// <summary>
	/// Attempts to parse dice notation string into components.
	/// Supports both dice notation (e.g., "2d6+3") and plain numbers (e.g., "5").
	/// </summary>
	/// <param name="notation">Dice notation string or plain number (e.g., "2d6+3", "1d8", "5")</param>
	/// <param name="count">Number of dice (0 for plain numbers)</param>
	/// <param name="sides">Number of sides per die (0 for plain numbers)</param>
	/// <param name="modifier">Modifier to add/subtract (the constant value for plain numbers)</param>
	/// <returns>True if parsing succeeded, false otherwise</returns>
	public static bool TryParse(string notation, out int count, out int sides, out int modifier)
	{
		count = 0;
		sides = 0;
		modifier = 0;

		if (string.IsNullOrWhiteSpace(notation))
		{
			return false;
		}

		var trimmed = notation.Trim();

		// Check if it's a plain number first (e.g., "5" or "-3")
		if (int.TryParse(trimmed, out int constantValue))
		{
			// Plain number - no dice, just return the constant as a modifier
			count = 0;
			sides = 0;
			modifier = constantValue;
			return true;
		}

		// Pattern: [count]d[sides][+/-modifier]
		// Examples: 2d6+3, 1d8, 3d4-1, d20 (implicitly 1d20)
		var match = Regex.Match(trimmed, @"^(\d*)d(\d+)([+\-]\d+)?$", RegexOptions.IgnoreCase);

		if (!match.Success)
		{
			return false;
		}

		// Parse count (defaults to 1 if omitted, e.g., "d20" = "1d20")
		count = string.IsNullOrEmpty(match.Groups[1].Value) ? 1 : int.Parse(match.Groups[1].Value);

		// Parse sides
		sides = int.Parse(match.Groups[2].Value);

		// Parse modifier if present
		if (match.Groups[3].Success)
		{
			modifier = int.Parse(match.Groups[3].Value);
		}

		return count > 0 && sides > 0;
	}
}
