using Godot;

namespace PitsOfDespair.Core;

/// <summary>
/// Centralized color palette for consistent game visuals.
/// Focused collection of 61 intentional colors for roguelike rendering.
/// All colors optimized for high contrast on black backgrounds.
/// </summary>
public static class Palette
{
    #region UI - System & Feedback

    /// <summary>Empty/unexplored space, pure black</summary>
    public static readonly Color Empty = new("#000000");

    /// <summary>Tiles that have been seen but are currently out of view (fog of war)</summary>
    public static readonly Color FogOfWar = new("#404040");

    /// <summary>Default white color for general use</summary>
    public static readonly Color Default = new("#FFFFFF");

    /// <summary>Disabled or inactive UI elements</summary>
    public static readonly Color Disabled = new("#888888");

    /// <summary>Player character color</summary>
    public static readonly Color Player = new("#FFFF00");

    /// <summary>Success message and positive action indicator</summary>
    public static readonly Color Success = new("#66DDAA");

    /// <summary>Danger warning indicator</summary>
    public static readonly Color Danger = new("#EE5555");

    /// <summary>Caution warning indicator</summary>
    public static readonly Color Caution = new("#EEAA55");

    /// <summary>Alert notification indicator</summary>
    public static readonly Color Alert = new("#EEDD77");

    #endregion

    #region UI - Message Log

    /// <summary>Combat damage taken by player (hit messages)</summary>
    public static readonly Color CombatDamage = new("#DD6655");

    /// <summary>Combat damage blocked or deflected (no harm done)</summary>
    public static readonly Color CombatBlocked = new("#99AABB");

    /// <summary>Positive status effects and buffs</summary>
    public static readonly Color StatusBuff = new("#66DD66");

    /// <summary>Negative status effects and debuffs</summary>
    public static readonly Color StatusDebuff = new("#EEAA55");

    /// <summary>Neutral status changes or informational status messages</summary>
    public static readonly Color StatusNeutral = new("#EEDD77");

    /// <summary>Equipment changes (equip/unequip)</summary>
    public static readonly Color Equipment = new("#AABBCC");

    #endregion

    #region UI - Health States

    /// <summary>Health bar at full capacity (>80%)</summary>
    public static readonly Color HealthFull = new("#66DD66");

    /// <summary>Health bar at high level (60-80%)</summary>
    public static readonly Color HealthHigh = new("#99DD66");

    /// <summary>Health bar at medium level (30-60%)</summary>
    public static readonly Color HealthMedium = new("#DDDD66");

    /// <summary>Health bar at low level (15-30%)</summary>
    public static readonly Color HealthLow = new("#DDAA55");

    /// <summary>Health bar at critical level (&lt;15%)</summary>
    public static readonly Color HealthCritical = new("#DD6655");

    #endregion

    #region UI - Targeting System

    /// <summary>Targeting range overlay background</summary>
    public static readonly Color TargetingRangeOverlay = new("#334466");

    /// <summary>Valid target highlight (bright green)</summary>
    public static readonly Color TargetingValid = new("#4DFF4D");

    /// <summary>Invalid target highlight (bright red)</summary>
    public static readonly Color TargetingInvalid = new("#FF4D4D");

    /// <summary>Targeting trace line from player to cursor</summary>
    public static readonly Color TargetingLine = new("#CCCC4D");

    #endregion

    #region Materials - Metals

    /// <summary>Crude, unrefined iron (dark brown-gray)</summary>
    public static readonly Color CrudeIron = new("#665544");

    /// <summary>Iron material (medium gray)</summary>
    public static readonly Color Iron = new("#888888");

    /// <summary>Steel material (blue-gray)</summary>
    public static readonly Color Steel = new("#99AABB");

    /// <summary>Forged steel, quality craftsmanship (medium blue-gray)</summary>
    public static readonly Color ForgedSteel = new("#8899AA");

    /// <summary>Refined steel, highest quality (light blue-gray)</summary>
    public static readonly Color RefinedSteel = new("#AABBCC");

    /// <summary>Copper metal (reddish-brown)</summary>
    public static readonly Color Copper = new("#AA6644");

    /// <summary>Bronze material (golden-brown)</summary>
    public static readonly Color Bronze = new("#BB7755");

    /// <summary>Silver material (light gray-blue)</summary>
    public static readonly Color Silver = new("#9A9AAA");

    /// <summary>Gold material (rich yellow-gold)</summary>
    public static readonly Color Gold = new("#CCAA66");

    #endregion

    #region Materials - Stone

    /// <summary>Obsidian, black volcanic glass</summary>
    public static readonly Color Obsidian = new("#1A1A1A");

    /// <summary>Basalt, dark blue-gray volcanic stone</summary>
    public static readonly Color Basalt = new("#3A4A4A");

    /// <summary>Granite, pink-gray speckled stone</summary>
    public static readonly Color Granite = new("#8A7A7A");

    /// <summary>Slate, blue-gray metamorphic stone</summary>
    public static readonly Color Slate = new("#556677");

    /// <summary>Diorite, salt-and-pepper gray stone</summary>
    public static readonly Color Diorite = new("#6A6A6A");

    /// <summary>Sandstone, tan-orange sedimentary stone</summary>
    public static readonly Color Sandstone = new("#C4A574");

    /// <summary>Limestone, pale tan sedimentary stone</summary>
    public static readonly Color Limestone = new("#C8B89A");

    /// <summary>Marble, white metamorphic stone</summary>
    public static readonly Color Marble = new("#D8D8D0");

    #endregion

    #region Materials - Leather & Hide

    /// <summary>Soft leather, light tan and supple</summary>
    public static readonly Color SoftLeather = new("#D0B090");

    /// <summary>Tough leather, dark brown and hardened</summary>
    public static readonly Color ToughLeather = new("#8B7053");

    /// <summary>Fur, warm medium brown pelts</summary>
    public static readonly Color Fur = new("#8A6A4A");

    /// <summary>Raw hide, gray-brown untreated leather</summary>
    public static readonly Color RawHide = new("#9A8060");

    #endregion

    #region Materials - Cloth

    /// <summary>Linen, natural off-white cloth</summary>
    public static readonly Color Linen = new("#E8E0D0");

    /// <summary>Wool, warm gray-brown fabric</summary>
    public static readonly Color Wool = new("#A08870");

    #endregion

    #region Materials - Wood

    /// <summary>Ash wood, light gray-brown (clubs, simple items)</summary>
    public static readonly Color AshWood = new("#B0A090");

    /// <summary>Oak wood, medium-dark brown (quality items)</summary>
    public static readonly Color Oak = new("#7A6453");

    /// <summary>Mahogany, reddish-brown premium wood</summary>
    public static readonly Color Mahogany = new("#8A5A4A");

    #endregion

    #region Materials - Natural

    /// <summary>Bone material (ivory-cream)</summary>
    public static readonly Color Bone = new("#EEDDCC");

    /// <summary>Ash gray, for undead and worn materials</summary>
    public static readonly Color AshGray = new("#AAAAAA");

    /// <summary>Ochre, yellow-brown earth tone</summary>
    public static readonly Color Ochre = new("#CC9933");

    #endregion

    #region Creatures & Organic

    /// <summary>Coral, pink-orange creature color</summary>
    public static readonly Color Coral = new("#DD8866");

    /// <summary>Clay, earthy reddish-brown</summary>
    public static readonly Color Clay = new("#8B6B51");

    /// <summary>Rust, aggressive red-orange color</summary>
    public static readonly Color Rust = new("#AA5533");

    /// <summary>Blood, dark red organic fluid</summary>
    public static readonly Color Blood = new("#AA3344");

    #endregion

    #region Effects - Elemental

    /// <summary>Fire effect (bright red-orange)</summary>
    public static readonly Color Fire = new("#EE6655");

    /// <summary>Ice effect (frozen blue)</summary>
    public static readonly Color Ice = new("#66AADD");

    /// <summary>Lightning effect (electric yellow)</summary>
    public static readonly Color Lightning = new("#EEEE66");

    /// <summary>Poison effect (toxic green)</summary>
    public static readonly Color Poison = new("#77DD66");

    /// <summary>Acid effect (corrosive bright green)</summary>
    public static readonly Color Acid = new("#99FF88");

    #endregion

    #region Dyes & Pigments

    /// <summary>Cyan dye, classic blue-green (scrolls, magic items)</summary>
    public static readonly Color Cyan = new("#00BBDD");

    /// <summary>Magenta dye, bright pink-purple (teleportation, dimensional magic)</summary>
    public static readonly Color Magenta = new("#EE55DD");

    #endregion

    #region Scrolls

    /// <summary>Violet scroll color (mind-affecting magic)</summary>
    public static readonly Color Violet = new("#9966DD");

    #endregion

    #region Effects - Magical

    /// <summary>Barkskin, olive-brown nature magic (tree-like protection)</summary>
    public static readonly Color Barkskin = new("#7A8A5A");

    #endregion

    #region Utilities

    /// <summary>
    /// Converts a Godot Color to a hex string for use in BBCode.
    /// </summary>
    /// <param name="color">The color to convert</param>
    /// <returns>Hex color string in format #RRGGBB</returns>
    public static string ToHex(Color color) =>
        $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";

    #endregion
}
