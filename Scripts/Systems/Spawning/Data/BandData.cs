using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace PitsOfDespair.Systems.Spawning.Data;

/// <summary>
/// Represents a distance range for follower placement around the leader.
/// </summary>
public class DistanceRange
{
    [YamlMember(Alias = "min")]
    public int Min { get; set; } = 1;

    [YamlMember(Alias = "max")]
    public int Max { get; set; } = 2;
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
    public CountRange Count { get; set; } = new CountRange { Min = 2, Max = 4 };

    /// <summary>
    /// Placement strategy for followers (surrounding, scattered, formation).
    /// </summary>
    [YamlMember(Alias = "placement")]
    public string Placement { get; set; } = "surrounding";

    /// <summary>
    /// Distance range from leader (for surrounding placement).
    /// </summary>
    [YamlMember(Alias = "distance")]
    public DistanceRange Distance { get; set; } = new DistanceRange { Min = 1, Max = 2 };

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
