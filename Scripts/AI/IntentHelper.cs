using Godot;
using PitsOfDespair.Core;

namespace PitsOfDespair.AI;

/// <summary>
/// Provides display helpers for the Intent enum.
/// Maps intents to colors and display strings for UI presentation.
/// </summary>
public static class IntentHelper
{
    /// <summary>
    /// Gets the display color for an intent.
    /// </summary>
    public static Color GetColor(Intent intent)
    {
        return intent switch
        {
            Intent.Sleeping => Palette.IntentSleeping,
            Intent.Idle => Palette.IntentIdle,
            Intent.Patrolling => Palette.IntentPatrolling,
            Intent.Guarding => Palette.IntentGuarding,
            Intent.Attacking => Palette.IntentAttacking,
            Intent.Fleeing => Palette.IntentFleeing,
            Intent.Following => Palette.IntentFollowing,
            Intent.Scavenging => Palette.IntentScavenging,
            Intent.Wandering => Palette.IntentWandering,
            _ => Palette.IntentIdle
        };
    }

    /// <summary>
    /// Gets a short display string for an intent (for side panel).
    /// </summary>
    public static string GetShortName(Intent intent)
    {
        return intent switch
        {
            Intent.Sleeping => "Zzz",
            Intent.Idle => "",
            Intent.Patrolling => "Patrol",
            Intent.Guarding => "Guard",
            Intent.Attacking => "Attack",
            Intent.Fleeing => "Flee",
            Intent.Following => "Follow",
            Intent.Scavenging => "Loot",
            Intent.Wandering => "Wander",
            _ => ""
        };
    }
}
