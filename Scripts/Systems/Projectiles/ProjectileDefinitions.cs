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
    /// <summary>
    /// Creates a trail color from a Palette color with the specified alpha.
    /// </summary>
    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, alpha);

    #region Physical Projectiles

    /// <summary>
    /// Standard arrow projectile for bows (triangle shape, wood color).
    /// </summary>
    public static readonly ProjectileDefinition Arrow = new(
        id: "arrow",
        shape: ProjectileShape.Triangle,
        headColor: Palette.ProjectileBeam,
        trailColor: WithAlpha(Palette.ProjectileBeam, 0.3f),
        speed: 30.0f,
        trailLength: 2,
        size: 8.0f
    );

    /// <summary>
    /// Crossbow bolt - faster and more piercing than arrow.
    /// </summary>
    public static readonly ProjectileDefinition Bolt = new(
        id: "bolt",
        shape: ProjectileShape.Line,
        headColor: Palette.Steel,
        trailColor: WithAlpha(Palette.Steel, 0.2f),
        speed: 35.0f,
        trailLength: 2,
        size: 6.0f,
        lineWidth: 2.0f
    );

    #endregion

    #region Fire Projectiles

    /// <summary>
    /// Classic fireball - circular, bright orange-red, medium speed.
    /// </summary>
    public static readonly ProjectileDefinition Fireball = new(
        id: "fireball",
        shape: ProjectileShape.Circle,
        headColor: Palette.Fire,
        trailColor: WithAlpha(Palette.Fire, 0.4f),
        speed: 20.0f,
        trailLength: 4,
        size: 7.0f
    );

    /// <summary>
    /// Small fire bolt - faster, smaller than fireball.
    /// </summary>
    public static readonly ProjectileDefinition FireBolt = new(
        id: "fire_bolt",
        shape: ProjectileShape.Diamond,
        headColor: Palette.Fire,
        trailColor: WithAlpha(Palette.Fire, 0.3f),
        speed: 28.0f,
        trailLength: 3,
        size: 5.0f
    );

    #endregion

    #region Ice Projectiles

    /// <summary>
    /// Ice shard - diamond shape, cold blue, fast.
    /// </summary>
    public static readonly ProjectileDefinition IceShard = new(
        id: "ice_shard",
        shape: ProjectileShape.Diamond,
        headColor: Palette.Ice,
        trailColor: WithAlpha(Palette.Ice, 0.3f),
        speed: 32.0f,
        trailLength: 3,
        size: 6.0f
    );

    /// <summary>
    /// Frost bolt - slower, larger ice projectile.
    /// </summary>
    public static readonly ProjectileDefinition FrostBolt = new(
        id: "frost_bolt",
        shape: ProjectileShape.Circle,
        headColor: Palette.Ice,
        trailColor: WithAlpha(Palette.Ice, 0.4f),
        speed: 22.0f,
        trailLength: 4,
        size: 7.0f
    );

    #endregion

    #region Lightning Projectiles

    /// <summary>
    /// Lightning bolt - line shape, very fast, electric yellow.
    /// </summary>
    public static readonly ProjectileDefinition LightningBolt = new(
        id: "lightning_bolt",
        shape: ProjectileShape.Line,
        headColor: Palette.Lightning,
        trailColor: WithAlpha(Palette.Lightning, 0.5f),
        speed: 45.0f,
        trailLength: 5,
        size: 8.0f,
        lineWidth: 2.5f
    );

    /// <summary>
    /// Spark - small, quick lightning projectile.
    /// </summary>
    public static readonly ProjectileDefinition Spark = new(
        id: "spark",
        shape: ProjectileShape.Circle,
        headColor: Palette.Lightning,
        trailColor: WithAlpha(Palette.Lightning, 0.3f),
        speed: 40.0f,
        trailLength: 2,
        size: 4.0f
    );

    #endregion

    #region Poison/Acid Projectiles

    /// <summary>
    /// Poison bolt - green, slower with lingering trail.
    /// </summary>
    public static readonly ProjectileDefinition PoisonBolt = new(
        id: "poison_bolt",
        shape: ProjectileShape.Circle,
        headColor: Palette.Poison,
        trailColor: WithAlpha(Palette.Poison, 0.5f),
        speed: 18.0f,
        trailLength: 5,
        size: 6.0f
    );

    /// <summary>
    /// Acid splash - corrosive bright green projectile.
    /// </summary>
    public static readonly ProjectileDefinition AcidSplash = new(
        id: "acid_splash",
        shape: ProjectileShape.Circle,
        headColor: Palette.Acid,
        trailColor: WithAlpha(Palette.Acid, 0.4f),
        speed: 20.0f,
        trailLength: 3,
        size: 7.0f
    );

    #endregion

    #region Arcane/Magic Projectiles

    /// <summary>
    /// Generic magic missile - cyan diamond, reliable speed.
    /// </summary>
    public static readonly ProjectileDefinition MagicMissile = new(
        id: "magic_missile",
        shape: ProjectileShape.Diamond,
        headColor: Palette.Cyan,
        trailColor: WithAlpha(Palette.Cyan, 0.4f),
        speed: 25.0f,
        trailLength: 3,
        size: 5.0f
    );

    /// <summary>
    /// Dark bolt - purple/magenta projectile for shadow magic.
    /// </summary>
    public static readonly ProjectileDefinition DarkBolt = new(
        id: "dark_bolt",
        shape: ProjectileShape.Diamond,
        headColor: Palette.Magenta,
        trailColor: WithAlpha(Palette.Magenta, 0.4f),
        speed: 22.0f,
        trailLength: 4,
        size: 6.0f
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
