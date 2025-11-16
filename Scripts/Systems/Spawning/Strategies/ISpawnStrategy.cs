using Godot;
using System.Collections.Generic;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning.Strategies;

/// <summary>
/// Result of a spawn operation containing spawned positions and entity count.
/// </summary>
public class SpawnResult
{
    public List<Vector2I> SpawnedPositions { get; set; } = new();
    public int EntityCount { get; set; } = 0;

    public bool Success => EntityCount > 0;
}

/// <summary>
/// Interface for spawn strategies that handle different types of spawning
/// (single creatures, bands, uniques, etc.).
/// </summary>
public interface ISpawnStrategy
{
    /// <summary>
    /// Gets the name of this spawn strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the spawn operation for the given entry.
    /// </summary>
    /// <param name="entry">Spawn entry data</param>
    /// <param name="availableTiles">Available floor tiles in the room</param>
    /// <param name="occupiedPositions">Set of occupied positions to avoid</param>
    /// <returns>Spawn result with spawned positions and entity count</returns>
    SpawnResult Execute(
        SpawnEntryData entry,
        List<Vector2I> availableTiles,
        HashSet<Vector2I> occupiedPositions
    );
}
