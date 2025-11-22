using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;

namespace PitsOfDespair.AI;

/// <summary>
/// Per-cell navigation cost map for weighted A* pathfinding.
/// Costs depend on terrain, entities, hazards, and creature capabilities.
/// Higher costs make the pathfinder prefer going around obstacles.
/// </summary>
public class NavigationWeightMap
{
    // Standard weight values
    public const float NormalFloor = 1f;
    public const float Hazard = 3f;              // Webs, shallow water
    public const float OtherCreature = 50f;      // Prefer going around
    public const float DangerousHazard = 100f;   // Fire, acid
    public const float Impassable = 999f;        // Walls (for non-burrowers)
    public const float Door = 2f;                // Slight penalty for doors
    public const float DoorUnintelligent = 999f; // Animals can't open doors
    public const float BurrowWall = 20f;         // Burrowers can dig through walls

    private readonly float[,] _weights;
    private readonly int _width;
    private readonly int _height;

    private NavigationWeightMap(int width, int height)
    {
        _width = width;
        _height = height;
        _weights = new float[width, height];
    }

    /// <summary>
    /// Gets the navigation cost for a cell.
    /// Returns Impassable for out-of-bounds positions.
    /// </summary>
    public float GetWeight(GridPosition pos)
    {
        if (pos.X < 0 || pos.X >= _width || pos.Y < 0 || pos.Y >= _height)
        {
            return Impassable;
        }
        return _weights[pos.X, pos.Y];
    }

    /// <summary>
    /// Gets the navigation cost for a cell by coordinates.
    /// Returns Impassable for out-of-bounds positions.
    /// </summary>
    public float GetWeight(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
        {
            return Impassable;
        }
        return _weights[x, y];
    }

    /// <summary>
    /// Builds a navigation weight map for the given creature.
    /// Weights depend on creature capabilities (intelligence, flight, burrowing).
    /// </summary>
    /// <param name="creature">The creature that will use this map for pathfinding</param>
    /// <param name="mapSystem">The map system for terrain queries</param>
    /// <param name="entityManager">The entity manager for entity position queries</param>
    /// <returns>A new NavigationWeightMap with calculated costs</returns>
    public static NavigationWeightMap Build(
        BaseEntity creature,
        MapSystem mapSystem,
        EntityManager entityManager)
    {
        var capabilities = CreatureCapabilities.FromEntity(creature);
        return Build(creature, mapSystem, entityManager, capabilities);
    }

    /// <summary>
    /// Builds a navigation weight map with explicit capabilities.
    /// </summary>
    public static NavigationWeightMap Build(
        BaseEntity creature,
        MapSystem mapSystem,
        EntityManager entityManager,
        CreatureCapabilities capabilities)
    {
        int width = mapSystem.MapWidth;
        int height = mapSystem.MapHeight;

        var map = new NavigationWeightMap(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new GridPosition(x, y);
                map._weights[x, y] = CalculateCellWeight(
                    pos,
                    mapSystem,
                    entityManager,
                    creature,
                    capabilities);
            }
        }

        return map;
    }

    /// <summary>
    /// Calculates the navigation cost for a single cell.
    /// </summary>
    private static float CalculateCellWeight(
        GridPosition pos,
        MapSystem mapSystem,
        EntityManager entityManager,
        BaseEntity self,
        CreatureCapabilities capabilities)
    {
        // Check terrain walkability
        if (!mapSystem.IsWalkable(pos))
        {
            // Walls: burrowers can dig through (high cost), others can't
            return capabilities.CanBurrow ? BurrowWall : Impassable;
        }

        // TODO: Check for hazards when hazard system is implemented
        // var hazard = mapSystem.GetHazardAt(pos);
        // if (hazard != null && !capabilities.CanFly)
        // {
        //     return hazard.IsDangerous ? DangerousHazard : Hazard;
        // }

        // TODO: Check for doors when door system is implemented
        // var door = mapSystem.GetDoorAt(pos);
        // if (door != null && !door.IsOpen)
        // {
        //     return capabilities.IsIntelligent ? Door : DoorUnintelligent;
        // }

        // Check for other creatures (except self)
        var entity = entityManager.GetEntityAtPosition(pos);
        if (entity != null && entity != self)
        {
            // Can path through creatures but prefer going around
            return OtherCreature;
        }

        return NormalFloor;
    }
}
