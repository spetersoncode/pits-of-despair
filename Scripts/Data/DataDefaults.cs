using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Data;

/// <summary>
/// Centralized defaults for game data types.
/// Provides fallback values and default constants for data loading.
/// </summary>
public static class DataDefaults
{
    #region Fallback Values

    /// <summary>
    /// Glyph used for unknown or undefined entity types.
    /// </summary>
    public const string UnknownGlyph = "?";

    /// <summary>
    /// Default color for entities when no type-specific color is defined.
    /// </summary>
    public static readonly string DefaultColor = Palette.ToHex(Palette.Default);

    #endregion

    #region Attack Defaults

    /// <summary>
    /// Name of the default unarmed attack when no weapon is equipped.
    /// </summary>
    public const string DefaultAttackName = "punch";

    /// <summary>
    /// Damage dice notation for the default unarmed attack.
    /// </summary>
    public const string DefaultAttackDice = "1d2";

    #endregion
}
