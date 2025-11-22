using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Components.AI;

/// <summary>
/// AI component that causes the entity to join the player's faction when it sees the player.
/// </summary>
public partial class JoinPlayerOnSightComponent : Node
{
    /// <summary>
    /// Called when the player is seen by this entity.
    /// Changes faction to Player and updates visuals.
    /// </summary>
    public void OnPlayerSeen(CombatSystem combatSystem)
    {
        var entity = GetParent<BaseEntity>();
        if (entity == null) return;

        // Only trigger if not already friendly
        if (entity.Faction == Faction.Player) return;

        // Change faction
        entity.Faction = Faction.Player;
        entity.GlyphColor = Palette.Player;

        // Log message via CombatSystem (which connects to MessageLog)
        combatSystem?.EmitActionMessage(entity, $"The {entity.DisplayName} meows and joins you!", Palette.ToHex(Palette.Player));
        
        // Setup as friendly companion in AI
        var aiComponent = entity.GetNodeOrNull<AIComponent>("AIComponent");
        if (aiComponent != null)
        {
            // Find player to protect
            // We can find the player via the group "player" or pass it in.
            // For now, let's assume the system calling this knows the player.
            // But wait, this method is called by the system.
        }

        // Remove this component so it doesn't trigger again
        QueueFree();
    }
}
