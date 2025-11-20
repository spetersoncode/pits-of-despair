using System.Collections.Generic;
using PitsOfDespair.Helpers;
using YamlDotNet.Serialization;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Represents a distance range for follower placement around the leader using dice notation.
/// The dice notation's natural range becomes the placement range (e.g., "1d2" = 1-2 tiles away).
/// </summary>
public class DistanceRange
{
    [YamlMember(Alias = "dice")]
    public string DiceNotation { get; set; } = "1d2";

    /// <summary>
    /// Gets the minimum possible distance from the dice notation.
    /// </summary>
    public int GetMin()
    {
        if (DiceRoller.TryParse(DiceNotation, out int count, out int sides, out int modifier))
        {
            // Minimum is: count + modifier (rolling all 1s)
            return count + modifier;
        }
        return 1; // Default fallback
    }

    /// <summary>
    /// Gets the maximum possible distance from the dice notation.
    /// </summary>
    public int GetMax()
    {
        if (DiceRoller.TryParse(DiceNotation, out int count, out int sides, out int modifier))
        {
            // Maximum is: (count * sides) + modifier (rolling all max)
            return (count * sides) + modifier;
        }
        return 2; // Default fallback
    }
}

/// <summary>
/// Defines the leader creature in a band.
/// </summary>
public class BandLeaderData
{
    /// <summary>
    /// ID of the leader creature.
    /// </summary>
    [YamlMember(Alias = "creatureId")]
    public string CreatureId { get; set; } = string.Empty;

    /// <summary>
    /// Placement strategy for the leader (center, random, corner).
    /// </summary>
    [YamlMember(Alias = "placement")]
    public string Placement { get; set; } = "center";

    public bool IsValid() => !string.IsNullOrEmpty(CreatureId);
}

/// <summary>
/// Defines a group of follower creatures in a band.
/// </summary>
public class BandFollowerData
{
    /// <summary>
    /// ID of the follower creature type.
    /// </summary>
    [YamlMember(Alias = "creatureId")]
    public string CreatureId { get; set; } = string.Empty;

    /// <summary>
    /// Number of followers to spawn.
    /// </summary>
    [YamlMember(Alias = "count")]
    public CountRange Count { get; set; } = new CountRange { DiceNotation = "1d3+1" };

    /// <summary>
    /// Placement strategy for followers (surrounding, scattered, formation).
    /// </summary>
    [YamlMember(Alias = "placement")]
    public string Placement { get; set; } = "surrounding";

    /// <summary>
    /// Distance range from leader (for surrounding placement).
    /// </summary>
    [YamlMember(Alias = "distance")]
    public DistanceRange Distance { get; set; } = new DistanceRange { DiceNotation = "1d2" };

    public bool IsValid() => !string.IsNullOrEmpty(CreatureId);
}

/// <summary>
/// Defines a monster band/pack with a leader and followers.
/// Inspired by DCSS band spawning (e.g., orc bands, gnoll packs).
/// </summary>
public class BandData
{
    /// <summary>
    /// Name of this band for identification.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Leader creature definition.
    /// </summary>
    [YamlMember(Alias = "leader")]
    public BandLeaderData Leader { get; set; } = new();

    /// <summary>
    /// List of follower groups.
    /// </summary>
    [YamlMember(Alias = "followers")]
    public List<BandFollowerData> Followers { get; set; } = new();

    /// <summary>
    /// Validates that the band has required data.
    /// </summary>
    public bool IsValid()
    {
        if (Leader == null || !Leader.IsValid())
        {
            return false;
        }

        if (Followers == null || Followers.Count == 0)
        {
            return false;
        }

        return Followers.TrueForAll(f => f.IsValid());
    }

    public override string ToString()
    {
        return $"Band '{Name}' (Leader: {Leader?.CreatureId}, Followers: {Followers?.Count ?? 0} groups)";
    }
}
