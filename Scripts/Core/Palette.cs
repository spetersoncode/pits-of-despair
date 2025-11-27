using Godot;

namespace PitsOfDespair.Core;

/// <summary>
/// Centralized color palette for consistent game visuals.
/// Focused collection of intentional colors for roguelike rendering.
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

    /// <summary>Stairs descending to next floor</summary>
    public static readonly Color Stairs = new("#CCAA66");

    /// <summary>Throne of Despair (win condition)</summary>
    public static readonly Color Throne = new("#FFAA33");

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

    #region UI - Projectile System

    /// <summary>Projectile beam/line color (wood-brown arrow)</summary>
    public static readonly Color ProjectileBeam = new("#9B7653");

    #endregion

    #region UI - Examine System

    /// <summary>Examine mode visible tiles overlay background</summary>
    public static readonly Color ExamineRangeOverlay = new("#444444");

    /// <summary>Examine cursor on entity (bright cyan/blue)</summary>
    public static readonly Color ExamineEntity = new("#4DDDFF");

    /// <summary>Examine cursor on empty tile (light gray)</summary>
    public static readonly Color ExamineEmpty = new("#CCCCCC");

    #endregion

    #region Materials - Metals

    /// <summary>Crude, unrefined iron (dark brown-gray)</summary>
    public static readonly Color CrudeIron = new("#665544");

    /// <summary>Rusted iron, corroded and neglected (warm brown)</summary>
    public static readonly Color RustedIron = new("#885544");

    /// <summary>Brutal iron, aggressive weapons like axes (warm red-brown)</summary>
    public static readonly Color BrutalIron = new("#996655");

    /// <summary>Iron material (medium gray)</summary>
    public static readonly Color Iron = new("#888888");

    /// <summary>Heavy iron, weighty bludgeons (cool gray)</summary>
    public static readonly Color HeavyIron = new("#777788");

    /// <summary>Steel material (blue-gray)</summary>
    public static readonly Color Steel = new("#99AABB");

    /// <summary>Forged steel, quality craftsmanship (medium blue-gray)</summary>
    public static readonly Color ForgedSteel = new("#8899AA");

    /// <summary>Dark steel, sinister weapons (dark blue-gray)</summary>
    public static readonly Color DarkSteel = new("#667788");

    /// <summary>Refined steel, highest quality (light blue-gray)</summary>
    public static readonly Color RefinedSteel = new("#AABBCC");

    /// <summary>Polished steel, fine blades (bright blue-gray)</summary>
    public static readonly Color PolishedSteel = new("#BBCCDD");

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

    /// <summary>Fur tan, sandy light brown (feral dogs)</summary>
    public static readonly Color FurTan = new("#C4A878");

    /// <summary>Fur gray, standard wolf gray</summary>
    public static readonly Color FurGray = new("#909098");

    /// <summary>Fur dark, black/charcoal pelts</summary>
    public static readonly Color FurDark = new("#4A4044");

    /// <summary>Fur frost, pale silver-white (arctic/dire wolves)</summary>
    public static readonly Color FurFrost = new("#C8D0D8");

    /// <summary>Fur russet, reddish-brown (foxes, red wolves)</summary>
    public static readonly Color FurRusset = new("#A06040");

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

    /// <summary>Birch, pale almost white wood</summary>
    public static readonly Color Birch = new("#D8C8A8");

    /// <summary>Pine, light yellowish common wood</summary>
    public static readonly Color Pine = new("#C4A060");

    /// <summary>Ash wood, light gray-brown (clubs, simple items)</summary>
    public static readonly Color AshWood = new("#B0A090");

    /// <summary>Yew, greenish-brown bow wood</summary>
    public static readonly Color Yew = new("#8A7A50");

    /// <summary>Oak wood, medium-dark brown (quality items)</summary>
    public static readonly Color Oak = new("#7A6453");

    /// <summary>Mahogany, reddish-brown premium wood</summary>
    public static readonly Color Mahogany = new("#8A5A4A");

    /// <summary>Walnut, dark brown dense wood</summary>
    public static readonly Color Walnut = new("#5A4A3A");

    /// <summary>Darkwood, dark but readable quality wood</summary>
    public static readonly Color Darkwood = new("#6A5A4A");

    /// <summary>Ironwood, gray-brown wood hard as metal</summary>
    public static readonly Color Ironwood = new("#7A7068");

    #endregion

    #region Materials - Natural

    /// <summary>Bone material (ivory-cream)</summary>
    public static readonly Color Bone = new("#EEDDCC");

    /// <summary>Ash gray, for undead and worn materials</summary>
    public static readonly Color AshGray = new("#AAAAAA");

    /// <summary>Ochre, yellow-brown earth tone</summary>
    public static readonly Color Ochre = new("#CC9933");

    #endregion

    #region Materials - Decoration

    /// <summary>Water (puddles, pools)</summary>
    public static readonly Color Water = new("#4466AA");

    /// <summary>Moss (green growth on stone)</summary>
    public static readonly Color Moss = new("#557744");

    /// <summary>Brick (clay containers)</summary>
    public static readonly Color Brick = new("#AA6644");

    #endregion

    #region Creatures & Organic

    /// <summary>CommonCreature, light gray for basic/weak creatures</summary>
    public static readonly Color CommonCreature = new("#BBBBBB");

    /// <summary>Coral, pink-orange creature color</summary>
    public static readonly Color Coral = new("#DD8866");

    /// <summary>Clay, earthy reddish-brown</summary>
    public static readonly Color Clay = new("#8B6B51");

    /// <summary>Rust, aggressive red-orange color</summary>
    public static readonly Color Rust = new("#AA5533");

    /// <summary>Blood, dark red organic fluid</summary>
    public static readonly Color Blood = new("#AA3344");

    /// <summary>Crimson, deep red for critical status</summary>
    public static readonly Color Crimson = new("#DC143C");

    #endregion

    #region Creatures - Role-Based Combat Identity

    // === Core Archetypes (Spawning System) ===
    // These map directly to CreatureArchetype enum for encounter composition

    /// <summary>Tank, high endurance frontline defender</summary>
    public static readonly Color Tank = new("#6699BB");

    /// <summary>Warrior, balanced standard melee combatant</summary>
    public static readonly Color Warrior = new("#AA8866");

    /// <summary>Assassin, stealth-based lethal striker (high AGI, low END)</summary>
    public static readonly Color Assassin = new("#776688");

    /// <summary>Ranged, attacks from distance (bows, thrown weapons)</summary>
    public static readonly Color Ranged = new("#88CC55");

    /// <summary>Support, provides buffs and heals to allies (high WIL)</summary>
    public static readonly Color Support = new("#AADDAA");

    /// <summary>Brute, heavy devastating striker (high STR+END, low AGI)</summary>
    public static readonly Color Brute = new("#AA4422");

    /// <summary>Scout, fast reconnaissance unit (Cowardly/YellForHelp AI)</summary>
    public static readonly Color Scout = new("#77CCAA");

    // === Basic Tiers (Threat Progression) ===

    /// <summary>Minion, weak subordinate creature (basic tier 1)</summary>
    public static readonly Color Minion = new("#AAAAAA");

    /// <summary>Soldier, reliable combatant (basic tier 2)</summary>
    public static readonly Color Soldier = new("#88AA77");

    /// <summary>Elite, above-average threat (basic tier 3)</summary>
    public static readonly Color Elite = new("#DDAA55");

    /// <summary>Champion, exceptional warrior (basic tier 4)</summary>
    public static readonly Color Champion = new("#DD8844");

    /// <summary>Alpha, pack/group dominant (basic tier 5)</summary>
    public static readonly Color Alpha = new("#FFAA33");

    // === Combat Specialists (Melee) ===

    /// <summary>Duelist, skilled one-on-one melee fighter</summary>
    public static readonly Color Duelist = new("#CCBB66");

    /// <summary>Gladiator, arena-trained melee combatant</summary>
    public static readonly Color Gladiator = new("#DD9944");

    /// <summary>Berserker, frenzied aggressive melee attacker</summary>
    public static readonly Color Berserker = new("#DD5533");

    /// <summary>Guardian, defensive protector melee role</summary>
    public static readonly Color Guardian = new("#5577AA");

    // === Combat Specialists (Ranged) ===

    /// <summary>Archer, ranged bow/crossbow combatant</summary>
    public static readonly Color Archer = new("#99DD66");

    /// <summary>Skirmisher, mobile hit-and-run ranged attacker</summary>
    public static readonly Color Skirmisher = new("#77AA55");

    /// <summary>Hunter, wilderness-trained ranged tracker</summary>
    public static readonly Color Hunter = new("#669944");

    /// <summary>Bandit, opportunistic ranged ambusher</summary>
    public static readonly Color Bandit = new("#CCAA55");

    /// <summary>Sniper, precision long-range specialist</summary>
    public static readonly Color Sniper = new("#5599AA");

    // === Magic Users ===

    /// <summary>Shaman, tribal/nature magic caster</summary>
    public static readonly Color Shaman = new("#AA77CC");

    /// <summary>Wizard, arcane scholarly magic user</summary>
    public static readonly Color Wizard = new("#5588DD");

    /// <summary>Illusionist, deception-based magic caster</summary>
    public static readonly Color Illusionist = new("#AA66DD");

    /// <summary>Buffer, support enhancement magic caster</summary>
    public static readonly Color Buffer = new("#66CCDD");

    /// <summary>Hexer, curse/debuff magic caster</summary>
    public static readonly Color Hexer = new("#995588");

    /// <summary>Priest, divine/holy magic caster</summary>
    public static readonly Color Priest = new("#DDCCAA");

    /// <summary>Warlock, dark pact magic caster</summary>
    public static readonly Color Warlock = new("#CC55AA");

    /// <summary>Healer, restorative support magic caster</summary>
    public static readonly Color Healer = new("#88BBAA");

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

    /// <summary>Arcane effect (pure magical energy, reality manipulation)</summary>
    public static readonly Color Arcane = new("#7788EE");

    #endregion

    #region Dyes & Pigments

    /// <summary>Cyan dye, classic blue-green (scrolls, magic items)</summary>
    public static readonly Color Cyan = new("#00BBDD");

    /// <summary>Magenta dye, bright pink-purple (teleportation, dimensional magic)</summary>
    public static readonly Color Magenta = new("#EE55DD");

    #endregion

    #region Scrolls - Translocation

    /// <summary>Short-range spatial magic (blink, phase step)</summary>
    public static readonly Color ScrollBlink = new("#55DDEE");

    /// <summary>Long-range spatial magic (teleport, dimension door)</summary>
    public static readonly Color ScrollTeleport = new("#228877");

    #endregion

    #region Scrolls - Mind-Affecting

    /// <summary>Mental debuffs (confusion, fear, madness)</summary>
    public static readonly Color ScrollConfusion = new("#9966DD");

    /// <summary>Mental control (charm, dominate, compulsion)</summary>
    public static readonly Color ScrollCharm = new("#BB66AA");

    #endregion

    #region Potions - Strength

    /// <summary>Minor strength potion (burnt orange)</summary>
    public static readonly Color PotionStrengthMinor = new("#AA6633");

    /// <summary>Regular strength potion (rich amber-orange)</summary>
    public static readonly Color PotionStrength = new("#CC7733");

    /// <summary>Major strength potion (blazing orange)</summary>
    public static readonly Color PotionStrengthMajor = new("#EE8833");

    #endregion

    #region Potions - Agility

    /// <summary>Minor agility potion (deep teal)</summary>
    public static readonly Color PotionAgilityMinor = new("#448888");

    /// <summary>Regular agility potion (bright teal)</summary>
    public static readonly Color PotionAgility = new("#55AAAA");

    /// <summary>Major agility potion (brilliant cyan)</summary>
    public static readonly Color PotionAgilityMajor = new("#66CCCC");

    #endregion

    #region Potions - Endurance

    /// <summary>Minor endurance potion (dusty rose)</summary>
    public static readonly Color PotionEnduranceMinor = new("#AA5566");

    /// <summary>Regular endurance potion (vibrant rose)</summary>
    public static readonly Color PotionEndurance = new("#CC6677");

    /// <summary>Major endurance potion (brilliant pink)</summary>
    public static readonly Color PotionEnduranceMajor = new("#EE7788");

    #endregion

    #region Potions - Will

    /// <summary>Minor will potion (deep violet)</summary>
    public static readonly Color PotionWillMinor = new("#7755AA");

    /// <summary>Regular will potion (rich purple)</summary>
    public static readonly Color PotionWill = new("#9966CC");

    /// <summary>Major will potion (brilliant lavender)</summary>
    public static readonly Color PotionWillMajor = new("#BB77EE");

    #endregion

    #region Potions - Skin (Armor)

    /// <summary>Barkskin potion (dark olive-brown)</summary>
    public static readonly Color PotionBarkskin = new("#667744");

    /// <summary>Stoneskin potion (cool slate-gray)</summary>
    public static readonly Color PotionStoneskin = new("#778899");

    /// <summary>Ironskin potion (blue-gray steel)</summary>
    public static readonly Color PotionIronskin = new("#99AABB");

    #endregion

    #region Potions - Speed

    /// <summary>Haste potion (energetic yellow-green)</summary>
    public static readonly Color PotionHaste = new("#AADD55");

    #endregion

    #region Intent - AI State Display

    /// <summary>Sleeping intent - creature is inactive</summary>
    public static readonly Color IntentSleeping = new("#666666");

    /// <summary>Idle intent - no current goal</summary>
    public static readonly Color IntentIdle = new("#AAAAAA");

    /// <summary>Patrolling intent - following patrol route</summary>
    public static readonly Color IntentPatrolling = new("#5588CC");

    /// <summary>Guarding intent - holding position, watching threats</summary>
    public static readonly Color IntentGuarding = new("#DDAA55");

    /// <summary>Attacking intent - actively trying to kill target</summary>
    public static readonly Color IntentAttacking = new("#DD5544");

    /// <summary>Fleeing intent - running from threat</summary>
    public static readonly Color IntentFleeing = new("#AA66CC");

    /// <summary>Following intent - following leader/ally</summary>
    public static readonly Color IntentFollowing = new("#55CCCC");

    /// <summary>Scavenging intent - searching for items</summary>
    public static readonly Color IntentScavenging = new("#66BB66");

    /// <summary>Wandering intent - aimlessly moving</summary>
    public static readonly Color IntentWandering = new("#7799AA");

    #endregion

    #region Gems and Jewelry

    /// <summary>Jade (soft green gemstone)</summary>
    public static readonly Color Jade = new("#66BB6A");

    /// <summary>Ruby (deep red gemstone)</summary>
    public static readonly Color Ruby = new("#E53935");

    /// <summary>Sapphire (deep blue gemstone)</summary>
    public static readonly Color Sapphire = new("#1E88E5");

    /// <summary>Emerald (vivid green gemstone)</summary>
    public static readonly Color Emerald = new("#43A047");

    /// <summary>Amethyst (purple gemstone)</summary>
    public static readonly Color Amethyst = new("#8E24AA");

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
