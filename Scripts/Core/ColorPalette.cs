using Godot;

namespace PitsOfDespair.Core;

/// <summary>
/// Centralized 128-color color palette for consistent game visuals.
/// Provides a carefully curated collection of colors optimized for roguelike ASCII/glyph rendering
/// with proper contrast on black backgrounds and thematic cohesion for the dark dungeon atmosphere.
/// </summary>
public static class ColorPalette
{
    #region Terrain - Stone & Soil

    /// <summary>Obsidian, black volcanic glass</summary>
    public static readonly Color Obsidian = new("#1A1A1A");

    /// <summary>Basalt, dark blue-gray volcanic stone</summary>
    public static readonly Color Basalt = new("#3A4A4A");

    /// <summary>Gabbro, dark gray igneous stone</summary>
    public static readonly Color Gabbro = new("#2A2A2A");

    /// <summary>Slate, blue-gray metamorphic slabs</summary>
    public static readonly Color Slate = new("#556677");

    /// <summary>Diorite, salt-and-pepper gray igneous stone</summary>
    public static readonly Color Diorite = new("#6A6A6A");

    /// <summary>Schist, greenish-gray metamorphic stone</summary>
    public static readonly Color Schist = new("#6A7A6A");

    /// <summary>Granite, pink-gray speckled igneous stone</summary>
    public static readonly Color Granite = new("#8A7A7A");

    /// <summary>Sandstone, tan-orange sedimentary stone</summary>
    public static readonly Color Sandstone = new("#C4A574");

    /// <summary>Limestone, pale tan sedimentary stone</summary>
    public static readonly Color Limestone = new("#C8B89A");

    /// <summary>Marble, white metamorphic stone</summary>
    public static readonly Color Marble = new("#D8D8D0");

    /// <summary>Mud, dark wet earth</summary>
    public static readonly Color Mud = new("#4A3A28");

    /// <summary>Dark earth, dark dry soil</summary>
    public static readonly Color DarkEarth = new("#5C4033");

    /// <summary>Clay, reddish-brown soil</summary>
    public static readonly Color Clay = new("#8B6B51");

    /// <summary>Gravel, loose gray-tan rock</summary>
    public static readonly Color Gravel = new("#9A9484");

    /// <summary>Grass, green vegetation</summary>
    public static readonly Color Grass = new("#4A7A3A");

    /// <summary>Sand, pale tan loose soil</summary>
    public static readonly Color Sand = new("#D0B88A");

    #endregion

    #region Terrain - Water & Elemental

    /// <summary>Deep water, darkest water color</summary>
    public static readonly Color DeepWater = new("#2A5D5D");

    /// <summary>Standard water color</summary>
    public static readonly Color Water = new("#3A6D6D");

    /// <summary>Shallow water areas</summary>
    public static readonly Color ShallowWater = new("#4A7D7D");

    /// <summary>Pools and standing water</summary>
    public static readonly Color Pool = new("#5A9A9A");

    /// <summary>Coral, pink-orange underwater formations</summary>
    public static readonly Color Coral = new("#DD8866");

    /// <summary>Ice surfaces and frozen areas</summary>
    public static readonly Color Ice = new("#4A6A8A");

    /// <summary>Frost and frozen effects</summary>
    public static readonly Color Frost = new("#5A8AAA");

    /// <summary>Lava surfaces, darkest lava color</summary>
    public static readonly Color Lava = new("#BB4422");

    /// <summary>Molten magma</summary>
    public static readonly Color Magma = new("#DD5533");

    /// <summary>Molten rock, brightest lava color</summary>
    public static readonly Color MoltenRock = new("#EE6644");

    /// <summary>Dark moss on surfaces</summary>
    public static readonly Color DarkMoss = new("#3A5A3A");

    /// <summary>Standard moss color</summary>
    public static readonly Color Moss = new("#4A6A4A");

    /// <summary>Lichen growth on surfaces</summary>
    public static readonly Color Lichen = new("#5A7A5A");

    /// <summary>Desert terrain</summary>
    public static readonly Color Desert = new("#AA8A5A");

    /// <summary>Gold veins in rock</summary>
    public static readonly Color GoldVein = new("#AA8844");

    /// <summary>Silver veins in rock</summary>
    public static readonly Color SilverVein = new("#9A9A9A");

    #endregion

    #region Metals & Materials

    /// <summary>Crude, unrefined iron</summary>
    public static readonly Color CrudeIron = new("#665544");

    /// <summary>Standard iron material</summary>
    public static readonly Color Iron = new("#888888");

    /// <summary>Steel material</summary>
    public static readonly Color Steel = new("#99AABB");

    /// <summary>Refined, high-quality steel</summary>
    public static readonly Color RefinedSteel = new("#AABBCC");

    /// <summary>Copper metal</summary>
    public static readonly Color Copper = new("#AA6644");

    /// <summary>Bronze material</summary>
    public static readonly Color Bronze = new("#BB7755");

    /// <summary>Brass material</summary>
    public static readonly Color Brass = new("#CC9955");

    /// <summary>Gold material</summary>
    public static readonly Color Gold = new("#CCAA66");

    /// <summary>Silver material</summary>
    public static readonly Color Silver = new("#9A9AAA");

    /// <summary>Mithril, fantasy metal</summary>
    public static readonly Color Mithril = new("#AAAACC");

    /// <summary>Adamantine, legendary metal</summary>
    public static readonly Color Adamantine = new("#BBBBDD");

    /// <summary>Platinum material</summary>
    public static readonly Color Platinum = new("#8AAABB");

    /// <summary>Linen, natural off-white cloth</summary>
    public static readonly Color Linen = new("#E8E0D0");

    /// <summary>Wool, warm gray-brown cloth</summary>
    public static readonly Color Wool = new("#A08870");

    /// <summary>Silk, lustrous pale fabric</summary>
    public static readonly Color Silk = new("#E0D8F0");

    /// <summary>Velvet, rich burgundy-purple cloth</summary>
    public static readonly Color Velvet = new("#8B4B6B");

    #endregion

    #region Gems & Precious Stones

    /// <summary>Garnet gemstone, deep red</summary>
    public static readonly Color Garnet = new("#AA3333");

    /// <summary>Ruby gemstone, bright red</summary>
    public static readonly Color Ruby = new("#CC4444");

    /// <summary>Carnelian gemstone, red-orange</summary>
    public static readonly Color Carnelian = new("#DD6633");

    /// <summary>Amber gemstone, orange</summary>
    public static readonly Color Amber = new("#EE8844");

    /// <summary>Topaz gemstone, golden yellow</summary>
    public static readonly Color Topaz = new("#DDAA44");

    /// <summary>Citrine gemstone, yellow</summary>
    public static readonly Color Citrine = new("#DDCC55");

    /// <summary>Peridot gemstone, lime green</summary>
    public static readonly Color Peridot = new("#AADD55");

    /// <summary>Emerald gemstone, bright green</summary>
    public static readonly Color Emerald = new("#66CC66");

    /// <summary>Jade gemstone, muted green</summary>
    public static readonly Color Jade = new("#66AA77");

    /// <summary>Turquoise gemstone, teal</summary>
    public static readonly Color Turquoise = new("#55CCAA");

    /// <summary>Aquamarine gemstone, cyan-blue</summary>
    public static readonly Color Aquamarine = new("#66BBCC");

    /// <summary>Sapphire gemstone, blue</summary>
    public static readonly Color Sapphire = new("#5588DD");

    /// <summary>Lapis Lazuli gemstone, deep blue</summary>
    public static readonly Color LapisLazuli = new("#4466AA");

    /// <summary>Amethyst gemstone, purple</summary>
    public static readonly Color Amethyst = new("#9966CC");

    /// <summary>Kunzite gemstone, lavender</summary>
    public static readonly Color Kunzite = new("#CC99DD");

    /// <summary>Pearl, cream-white</summary>
    public static readonly Color Pearl = new("#FFEEDD");

    #endregion

    #region Organic & Natural

    /// <summary>Blood, dark red organic fluid</summary>
    public static readonly Color Blood = new("#AA3344");

    /// <summary>Flesh, tan-pink skin tone</summary>
    public static readonly Color Flesh = new("#DDAA99");

    /// <summary>Viscera, internal organs and tissue</summary>
    public static readonly Color Viscera = new("#882233");

    /// <summary>Bone, ivory-cream color</summary>
    public static readonly Color Bone = new("#EEDDCC");

    /// <summary>Chitin, dark insect shell material</summary>
    public static readonly Color Chitin = new("#3A2A1A");

    /// <summary>Scale, dragon/reptile scales in teal-green</summary>
    public static readonly Color Scale = new("#5A8A77");

    /// <summary>Fur, brown animal pelt</summary>
    public static readonly Color Fur = new("#8A6A4A");

    /// <summary>Soft leather, supple and light</summary>
    public static readonly Color SoftLeather = new("#D0B090");

    /// <summary>Tough leather, hardened for armor</summary>
    public static readonly Color ToughLeather = new("#6B5033");

    /// <summary>Bark, dark tree bark</summary>
    public static readonly Color Bark = new("#5A4433");

    /// <summary>Foliage, green leaves and plant matter</summary>
    public static readonly Color Foliage = new("#6A9A55");

    /// <summary>Petal, pink-purple flower petals</summary>
    public static readonly Color Petal = new("#DD88AA");

    /// <summary>Fungus, purple mushroom and fungal growth</summary>
    public static readonly Color Fungus = new("#BB77DD");

    /// <summary>Ooze, greenish slime creature</summary>
    public static readonly Color Ooze = new("#88AA66");

    /// <summary>Ectoplasm, pale cyan-green ghostly substance</summary>
    public static readonly Color Ectoplasm = new("#AADDDD");

    /// <summary>Ash gray, undead dust</summary>
    public static readonly Color AshGray = new("#AAAAAA");

    #endregion

    #region Items - Quality & Special

    /// <summary>Common quality item</summary>
    public static readonly Color Common = new("#999999");

    /// <summary>Uncommon quality item</summary>
    public static readonly Color Uncommon = new("#66DD66");

    /// <summary>Rare quality item</summary>
    public static readonly Color Rare = new("#5588FF");

    /// <summary>Epic quality item</summary>
    public static readonly Color Epic = new("#AA55DD");

    /// <summary>Legendary quality item</summary>
    public static readonly Color Legendary = new("#FFAA00");

    /// <summary>Cursed item, dark purple</summary>
    public static readonly Color Cursed = new("#6A4A6A");

    /// <summary>Blessed item, green-cyan</summary>
    public static readonly Color Blessed = new("#77CC99");

    /// <summary>Rusted item, deteriorated</summary>
    public static readonly Color Rusted = new("#7A5A4A");

    /// <summary>Pristine item, perfect condition</summary>
    public static readonly Color Pristine = new("#CCDDEE");

    /// <summary>Artifact, powerful unique item</summary>
    public static readonly Color Artifact = new("#77BBBB");

    /// <summary>Unique item, one-of-a-kind</summary>
    public static readonly Color Unique = new("#BB88DD");

    /// <summary>Quest item, story-related</summary>
    public static readonly Color QuestItem = new("#FFDDAA");

    /// <summary>Rust orange, corroded metal</summary>
    public static readonly Color RustOrange = new("#DD6644");

    /// <summary>Navy blue, deep dyed fabric</summary>
    public static readonly Color NavyBlue = new("#004488");

    /// <summary>Ochre, old parchment and dusty cloth</summary>
    public static readonly Color Ochre = new("#CC9933");

    /// <summary>Burgundy, rich wine and noble fabrics</summary>
    public static readonly Color Burgundy = new("#990033");

    #endregion

    #region Effects - Elemental

    /// <summary>Fire effect, standard flame</summary>
    public static readonly Color Fire = new("#EE6655");

    /// <summary>Inferno effect, intense burning</summary>
    public static readonly Color Inferno = new("#FF8877");

    /// <summary>Ice effect, frozen blue</summary>
    public static readonly Color IceEffect = new("#66AADD");

    /// <summary>Freeze effect, intense cold</summary>
    public static readonly Color Freeze = new("#88CCFF");

    /// <summary>Lightning effect, electric yellow</summary>
    public static readonly Color Lightning = new("#EEEE66");

    /// <summary>Thunder effect, storm energy</summary>
    public static readonly Color Thunder = new("#FFFFAA");

    /// <summary>Poison effect, toxic green</summary>
    public static readonly Color Poison = new("#77DD66");

    /// <summary>Acid effect, corrosive substance</summary>
    public static readonly Color Acid = new("#99FF88");

    /// <summary>Chaos effect, chaotic energy</summary>
    public static readonly Color Chaos = new("#DD66AA");

    /// <summary>Arcane effect, magical energy</summary>
    public static readonly Color Arcane = new("#BB88DD");

    /// <summary>Mystic effect, mystical power</summary>
    public static readonly Color Mystic = new("#CC99EE");

    /// <summary>Divine light, holy radiance</summary>
    public static readonly Color DivineLight = new("#FFFFFF");

    /// <summary>Hellfire, infernal flames</summary>
    public static readonly Color HellFire = new("#FF3300");

    /// <summary>Toxic glow, radioactive poison</summary>
    public static readonly Color ToxicGlow = new("#CCFF00");

    /// <summary>Void energy, shadow magic</summary>
    public static readonly Color VoidEnergy = new("#5500DD");

    /// <summary>Teal energy, enchanted water</summary>
    public static readonly Color Teal = new("#00AAAA");

    #endregion

    #region UI - Status & Feedback

    /// <summary>Floor tiles that have been seen but are currently out of view (fog of war)</summary>
    public static readonly Color RememberedFloor = new("#0F0F0F");

    /// <summary>Wall tiles that have been seen but are currently out of view (fog of war)</summary>
    public static readonly Color RememberedWall = new("#2A2A2A");

    /// <summary>Health bar at full capacity</summary>
    public static readonly Color HealthFull = new("#66DD66");

    /// <summary>Health bar at high level</summary>
    public static readonly Color HealthHigh = new("#88DD66");

    /// <summary>Health bar at medium level</summary>
    public static readonly Color HealthMedium = new("#DDDD66");

    /// <summary>Health bar at low level</summary>
    public static readonly Color HealthLow = new("#DDAA55");

    /// <summary>Health bar at critical level</summary>
    public static readonly Color HealthCritical = new("#DD6655");

    /// <summary>Mana bar at full capacity</summary>
    public static readonly Color ManaFull = new("#5566DD");

    /// <summary>Mana bar at low level</summary>
    public static readonly Color ManaLow = new("#7788DD");

    /// <summary>Positive status effect/buff indicator</summary>
    public static readonly Color Buff = new("#DD66DD");

    /// <summary>Danger warning indicator</summary>
    public static readonly Color Danger = new("#EE5555");

    /// <summary>Caution warning indicator</summary>
    public static readonly Color Caution = new("#EEAA55");

    /// <summary>Alert notification indicator</summary>
    public static readonly Color Alert = new("#EEDD77");

    /// <summary>Success message indicator</summary>
    public static readonly Color Success = new("#66DDAA");

    /// <summary>Player character color</summary>
    public static readonly Color Player = new("#EEEEEE");

    /// <summary>Empty/unexplored space, pure black</summary>
    public static readonly Color Empty = new("#000000");

    #endregion
}
