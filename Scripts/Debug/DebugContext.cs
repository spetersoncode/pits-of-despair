using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Debug;

/// <summary>
/// Context object providing access to game systems for debug command execution.
/// </summary>
public class DebugContext
{
    public Player Player { get; }
    public EntityManager EntityManager { get; }
    public MapSystem MapSystem { get; }
    public EntityFactory EntityFactory { get; }
    public TurnManager TurnManager { get; }
    public PlayerVisionSystem VisionSystem { get; }

    public DebugContext(
        Player player,
        EntityManager entityManager,
        MapSystem mapSystem,
        EntityFactory entityFactory,
        TurnManager turnManager,
        PlayerVisionSystem visionSystem)
    {
        Player = player;
        EntityManager = entityManager;
        MapSystem = mapSystem;
        EntityFactory = entityFactory;
        TurnManager = turnManager;
        VisionSystem = visionSystem;
    }
}
