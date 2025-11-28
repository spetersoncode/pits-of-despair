using Godot;
using System.Collections.Generic;
using PitsOfDespair.Core;

namespace PitsOfDespair.Systems.VisualEffects;

/// <summary>
/// Static catalog of predefined visual effect types.
/// Includes stationary effects (explosions, beams) and moving effects (projectiles).
/// All colors are sourced from Palette for visual consistency.
/// </summary>
public static class VisualEffectDefinitions
{
    // Shader paths - Areas
    private const string FireballImpactShader = "res://Resources/Shaders/Areas/fireball.gdshader";

    // Shader paths - Cones
    private const string ConeOfColdShader = "res://Resources/Shaders/Cones/cone_of_cold.gdshader";

    // Shader paths - Beams
    private const string TunnelingBeamShader = "res://Resources/Shaders/Beams/tunneling.gdshader";
    private const string LightningBeamShader = "res://Resources/Shaders/Beams/lightning_beam.gdshader";

    // Shader paths - Projectiles
    private const string FireballShader = "res://Resources/Shaders/Projectiles/fireball.gdshader";
    private const string ArrowShader = "res://Resources/Shaders/Projectiles/arrow.gdshader";
    private const string MagicMissileShader = "res://Resources/Shaders/Projectiles/magic_missile.gdshader";
    private const string IceShardShader = "res://Resources/Shaders/Projectiles/ice_shard.gdshader";
    private const string ChainLightningShader = "res://Resources/Shaders/Projectiles/chain_lightning.gdshader";
    private const string PoisonBoltShader = "res://Resources/Shaders/Projectiles/poison_bolt.gdshader";
    private const string AcidBlastShader = "res://Resources/Shaders/Projectiles/acid_blast.gdshader";

    /// <summary>
    /// Creates a trail color from a Palette color with the specified alpha.
    /// </summary>
    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, alpha);

    #region Impact Effects

    /// <summary>
    /// Fireball impact - dramatic fire explosion with expanding rings.
    /// </summary>
    public static readonly VisualEffectDefinition Fireball = new(
        id: "fireball_impact",
        type: VisualEffectType.Explosion,
        shaderPath: FireballImpactShader,
        duration: 0.6f,
        innerColor: new Color(1.0f, 1.0f, 0.85f, 1.0f),  // Hot white core
        midColor: new Color(
            Mathf.Min(Palette.Fire.R * 1.2f, 1.0f),
            Palette.Fire.G,
            Palette.Fire.B * 0.5f,
            1.0f),  // Bright orange
        outerColor: new Color(0.85f, 0.15f, 0.05f, 1.0f)  // Deep red/crimson
    );

    #endregion

    #region Cone Effects

    /// <summary>
    /// Cone of Cold - icy blast spreading from caster toward target.
    /// </summary>
    public static readonly VisualEffectDefinition ConeOfCold = new(
        id: "cone_of_cold",
        type: VisualEffectType.Cone,
        shaderPath: ConeOfColdShader,
        duration: 0.7f,
        innerColor: new Color(1.0f, 1.0f, 1.0f, 1.0f),      // Bright white core
        midColor: Palette.Ice,                               // Ice blue
        outerColor: new Color(0.2f, 0.4f, 0.8f, 1.0f)       // Deep cold blue
    );

    #endregion

    #region Beam Effects

    /// <summary>
    /// Tunneling beam - earthy beam for wall destruction effects.
    /// </summary>
    public static readonly VisualEffectDefinition Tunneling = new(
        id: "tunneling",
        type: VisualEffectType.Beam,
        shaderPath: TunnelingBeamShader,
        duration: 0.5f,
        innerColor: new Color(1.0f, 0.9f, 0.7f, 1.0f),  // Hot white-yellow core
        midColor: Palette.Ochre,
        outerColor: Palette.Ochre.Darkened(0.4f)
    );

    /// <summary>
    /// Lightning beam - instant electric bolt for lightning bolt skill.
    /// </summary>
    public static readonly VisualEffectDefinition LightningBeam = new(
        id: "lightning_beam",
        type: VisualEffectType.Beam,
        shaderPath: LightningBeamShader,
        duration: 0.4f,
        innerColor: new Color(1.0f, 1.0f, 1.0f, 1.0f),  // White hot core
        midColor: Palette.Lightning,
        outerColor: WithAlpha(Palette.Lightning, 0.6f)
    );

    #endregion

    #region Projectile Effects - Physical

    /// <summary>
    /// Standard arrow projectile for bows.
    /// </summary>
    public static readonly VisualEffectDefinition Arrow = new(
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
    public static readonly VisualEffectDefinition Bolt = new(
        id: "bolt",
        shaderPath: ArrowShader,
        headColor: Palette.Steel,
        trailColor: WithAlpha(Palette.Steel, 0.2f),
        speed: 35.0f,
        trailLength: 2,
        size: 0.9f
    );

    #endregion

    #region Projectile Effects - Fire

    /// <summary>
    /// Classic fireball projectile - fiery orb with flame trail.
    /// </summary>
    public static readonly VisualEffectDefinition FireballProjectile = new(
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
    public static readonly VisualEffectDefinition FireBolt = new(
        id: "fire_bolt",
        shaderPath: FireballShader,
        headColor: Palette.Fire,
        trailColor: WithAlpha(Palette.Fire, 0.3f),
        speed: 28.0f,
        trailLength: 3,
        size: 0.8f
    );

    #endregion

    #region Projectile Effects - Ice

    /// <summary>
    /// Ice shard - crystalline projectile with frost trail.
    /// </summary>
    public static readonly VisualEffectDefinition IceShard = new(
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
    public static readonly VisualEffectDefinition FrostBolt = new(
        id: "frost_bolt",
        shaderPath: IceShardShader,
        headColor: Palette.Ice,
        trailColor: WithAlpha(Palette.Ice, 0.4f),
        speed: 22.0f,
        trailLength: 4,
        size: 1.3f
    );

    #endregion

    #region Projectile Effects - Lightning

    /// <summary>
    /// Chain lightning arc - crackling electric projectile for chain bounces.
    /// </summary>
    public static readonly VisualEffectDefinition ChainLightningArc = new(
        id: "chain_lightning",
        shaderPath: ChainLightningShader,
        headColor: Palette.Lightning,
        trailColor: WithAlpha(Palette.Lightning, 0.5f),
        speed: 45.0f,
        trailLength: 5,
        size: 1.2f
    );

    /// <summary>
    /// Spark - small, quick lightning projectile for chain lightning bounces.
    /// Uses chain lightning shader with smaller size.
    /// </summary>
    public static readonly VisualEffectDefinition Spark = new(
        id: "spark",
        shaderPath: ChainLightningShader,
        headColor: Palette.Lightning,
        trailColor: WithAlpha(Palette.Lightning, 0.3f),
        speed: 40.0f,
        trailLength: 2,
        size: 0.7f
    );

    #endregion

    #region Projectile Effects - Poison/Acid

    /// <summary>
    /// Poison bolt - toxic glob with dripping trail.
    /// </summary>
    public static readonly VisualEffectDefinition PoisonBolt = new(
        id: "poison_bolt",
        shaderPath: PoisonBoltShader,
        headColor: Palette.Poison,
        trailColor: WithAlpha(Palette.Poison, 0.5f),
        speed: 18.0f,
        trailLength: 5,
        size: 1.0f
    );

    /// <summary>
    /// Acid blast - volatile corrosive projectile with custom shader.
    /// Used by the Acid Blast skill.
    /// </summary>
    public static readonly VisualEffectDefinition AcidBlast = new(
        id: "acid_blast",
        shaderPath: AcidBlastShader,
        headColor: Palette.Acid,
        trailColor: WithAlpha(Palette.Acid, 0.5f),
        speed: 22.0f,
        trailLength: 4,
        size: 1.2f
    );

    #endregion

    #region Projectile Effects - Arcane/Magic

    /// <summary>
    /// Magic missile - arcane diamond of energy.
    /// </summary>
    public static readonly VisualEffectDefinition MagicMissile = new(
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
    public static readonly VisualEffectDefinition DarkBolt = new(
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
    /// Gets a visual effect definition by ID.
    /// Returns null if not found.
    /// </summary>
    public static VisualEffectDefinition? GetById(string id)
    {
        return id?.ToLower() switch
        {
            // Impact effects
            "fireball_impact" => Fireball,
            // Cone effects
            "cone_of_cold" => ConeOfCold,
            // Beam effects
            "tunneling" => Tunneling,
            "lightning_beam" => LightningBeam,
            // Projectiles - Physical
            "arrow" => Arrow,
            "bolt" => Bolt,
            // Projectiles - Fire
            "fireball" => FireballProjectile,
            "fire_bolt" => FireBolt,
            // Projectiles - Ice
            "ice_shard" => IceShard,
            "frost_bolt" => FrostBolt,
            // Projectiles - Lightning
            "chain_lightning" => ChainLightningArc,
            "spark" => Spark,
            // Projectiles - Poison/Acid
            "poison_bolt" => PoisonBolt,
            "acid_blast" => AcidBlast,
            // Projectiles - Arcane
            "magic_missile" => MagicMissile,
            "dark_bolt" => DarkBolt,
            _ => null
        };
    }

    /// <summary>
    /// Gets all available visual effect definitions.
    /// </summary>
    public static IEnumerable<VisualEffectDefinition> GetAll()
    {
        // Impact effects
        yield return Fireball;
        // Cone effects
        yield return ConeOfCold;
        // Beam effects
        yield return Tunneling;
        yield return LightningBeam;
        // Projectiles
        yield return Arrow;
        yield return Bolt;
        yield return FireballProjectile;
        yield return FireBolt;
        yield return IceShard;
        yield return FrostBolt;
        yield return ChainLightningArc;
        yield return Spark;
        yield return PoisonBolt;
        yield return AcidBlast;
        yield return MagicMissile;
        yield return DarkBolt;
    }
}
