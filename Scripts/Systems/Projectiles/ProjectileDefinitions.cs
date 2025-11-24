using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.Projectiles;

/// <summary>
/// Static catalog of predefined projectile types.
/// Skills and abilities reference these definitions for consistent visual behavior.
/// All colors are sourced from Palette for visual consistency.
/// </summary>
public static class ProjectileDefinitions
{
    // Shader paths
    private const string FireballShader = "res://Resources/Shaders/Projectiles/fireball.gdshader";
    private const string ArrowShader = "res://Resources/Shaders/Projectiles/arrow.gdshader";
    private const string MagicMissileShader = "res://Resources/Shaders/Projectiles/magic_missile.gdshader";
    private const string IceShardShader = "res://Resources/Shaders/Projectiles/ice_shard.gdshader";
    private const string LightningBoltShader = "res://Resources/Shaders/Projectiles/lightning_bolt.gdshader";
    private const string PoisonBoltShader = "res://Resources/Shaders/Projectiles/poison_bolt.gdshader";

    /// <summary>
    /// Creates a trail color from a Palette color with the specified alpha.
    /// </summary>
    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, alpha);

    #region Physical Projectiles

    /// <summary>
    /// Standard arrow projectile for bows.
    /// </summary>
    public static readonly ProjectileDefinition Arrow = new(
        id: "arrow",
        shaderPath: ArrowShader,
        headColor: Palette.ProjectileBeam,
        trailColor: WithAlpha(Palette.ProjectileBeam, 0.3f),
        speed: 30.0f,
        trailLength: 2,
        size: 1.0f
    );

    /// <summary>
    /// Crossbow bolt - faster and more piercing than arrow.
    /// Uses arrow shader with steel coloring.
    /// </summary>
    public static readonly ProjectileDefinition Bolt = new(
        id: "bolt",
        shaderPath: ArrowShader,
        headColor: Palette.Steel,
        trailColor: WithAlpha(Palette.Steel, 0.2f),
        speed: 35.0f,
        trailLength: 2,
        size: 0.9f
    );

    #endregion

    #region Fire Projectiles

    /// <summary>
    /// Classic fireball - fiery orb with flame trail.
    /// </summary>
    public static readonly ProjectileDefinition Fireball = new(
        id: "fireball",
        shaderPath: FireballShader,
        headColor: Palette.Fire,
        trailColor: WithAlpha(Palette.Fire, 0.4f),
        speed: 20.0f,
        trailLength: 4,
        size: 1.2f
    );

    /// <summary>
    /// Small fire bolt - faster, smaller than fireball.
    /// Uses fireball shader with smaller size.
    /// </summary>
    public static readonly ProjectileDefinition FireBolt = new(
        id: "fire_bolt",
        shaderPath: FireballShader,
        headColor: Palette.Fire,
        trailColor: WithAlpha(Palette.Fire, 0.3f),
        speed: 28.0f,
        trailLength: 3,
        size: 0.8f
    );

    #endregion

    #region Ice Projectiles

    /// <summary>
    /// Ice shard - crystalline projectile with frost trail.
    /// </summary>
    public static readonly ProjectileDefinition IceShard = new(
        id: "ice_shard",
        shaderPath: IceShardShader,
        headColor: Palette.Ice,
        trailColor: WithAlpha(Palette.Ice, 0.3f),
        speed: 32.0f,
        trailLength: 3,
        size: 1.0f
    );

    /// <summary>
    /// Frost bolt - slower, larger ice projectile.
    /// Uses ice shard shader with larger size.
    /// </summary>
    public static readonly ProjectileDefinition FrostBolt = new(
        id: "frost_bolt",
        shaderPath: IceShardShader,
        headColor: Palette.Ice,
        trailColor: WithAlpha(Palette.Ice, 0.4f),
        speed: 22.0f,
        trailLength: 4,
        size: 1.3f
    );

    #endregion

    #region Lightning Projectiles

    /// <summary>
    /// Lightning bolt - crackling electric projectile.
    /// </summary>
    public static readonly ProjectileDefinition LightningBolt = new(
        id: "lightning_bolt",
        shaderPath: LightningBoltShader,
        headColor: Palette.Lightning,
        trailColor: WithAlpha(Palette.Lightning, 0.5f),
        speed: 45.0f,
        trailLength: 5,
        size: 1.2f
    );

    /// <summary>
    /// Spark - small, quick lightning projectile.
    /// Uses lightning shader with smaller size.
    /// </summary>
    public static readonly ProjectileDefinition Spark = new(
        id: "spark",
        shaderPath: LightningBoltShader,
        headColor: Palette.Lightning,
        trailColor: WithAlpha(Palette.Lightning, 0.3f),
        speed: 40.0f,
        trailLength: 2,
        size: 0.7f
    );

    #endregion

    #region Poison/Acid Projectiles

    /// <summary>
    /// Poison bolt - toxic glob with dripping trail.
    /// </summary>
    public static readonly ProjectileDefinition PoisonBolt = new(
        id: "poison_bolt",
        shaderPath: PoisonBoltShader,
        headColor: Palette.Poison,
        trailColor: WithAlpha(Palette.Poison, 0.5f),
        speed: 18.0f,
        trailLength: 5,
        size: 1.0f
    );

    /// <summary>
    /// Acid splash - corrosive bright green projectile.
    /// Uses poison shader with acid coloring.
    /// </summary>
    public static readonly ProjectileDefinition AcidSplash = new(
        id: "acid_splash",
        shaderPath: PoisonBoltShader,
        headColor: Palette.Acid,
        trailColor: WithAlpha(Palette.Acid, 0.4f),
        speed: 20.0f,
        trailLength: 3,
        size: 1.1f
    );

    #endregion

    #region Arcane/Magic Projectiles

    /// <summary>
    /// Magic missile - arcane diamond of energy.
    /// </summary>
    public static readonly ProjectileDefinition MagicMissile = new(
        id: "magic_missile",
        shaderPath: MagicMissileShader,
        headColor: Palette.Cyan,
        trailColor: WithAlpha(Palette.Cyan, 0.4f),
        speed: 25.0f,
        trailLength: 3,
        size: 1.0f
    );

    /// <summary>
    /// Dark bolt - shadow magic projectile.
    /// Uses magic missile shader with magenta coloring.
    /// </summary>
    public static readonly ProjectileDefinition DarkBolt = new(
        id: "dark_bolt",
        shaderPath: MagicMissileShader,
        headColor: Palette.Magenta,
        trailColor: WithAlpha(Palette.Magenta, 0.4f),
        speed: 22.0f,
        trailLength: 4,
        size: 1.1f
    );

    #endregion

    /// <summary>
    /// Gets a projectile definition by ID.
    /// Returns null if not found.
    /// </summary>
    public static ProjectileDefinition? GetById(string id)
    {
        return id?.ToLower() switch
        {
            "arrow" => Arrow,
            "bolt" => Bolt,
            "fireball" => Fireball,
            "fire_bolt" => FireBolt,
            "ice_shard" => IceShard,
            "frost_bolt" => FrostBolt,
            "lightning_bolt" => LightningBolt,
            "spark" => Spark,
            "poison_bolt" => PoisonBolt,
            "acid_splash" => AcidSplash,
            "magic_missile" => MagicMissile,
            "dark_bolt" => DarkBolt,
            _ => null
        };
    }
}
