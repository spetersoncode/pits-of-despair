using Godot;
using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Components;

/// <summary>
/// Attached to pack followers. References their leader entity.
/// Followers pursue the leader rather than waypoints directly.
/// </summary>
public partial class PackFollowerComponent : Node
{
    /// <summary>
    /// The pack leader this follower is following.
    /// </summary>
    public BaseEntity Leader { get; set; }

    /// <summary>
    /// How close the follower tries to stay to the leader (in tiles).
    /// </summary>
    [Export] public int FollowDistance { get; set; } = 2;

    /// <summary>
    /// Checks if the leader is still valid (alive and exists).
    /// </summary>
    public bool HasValidLeader => Leader != null && IsInstanceValid(Leader);
}
