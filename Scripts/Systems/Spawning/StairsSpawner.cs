using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Entities;
using PitsOfDespair.Generation.Metadata;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Places stairs (or throne on final floor) in the region farthest from entrance.
/// Can optionally place guardians near the stairs.
/// </summary>
public class StairsSpawner
{
	private readonly EntityManager _entityManager;
	private readonly RandomNumberGenerator _rng;

	private const int FINAL_FLOOR = 10;

	public StairsSpawner(EntityManager entityManager)
	{
		_entityManager = entityManager;
		_rng = new RandomNumberGenerator();
		_rng.Randomize();
	}

	/// <summary>
	/// Places stairs (or throne) in the optimal location.
	/// Returns the position where stairs were placed.
	/// </summary>
	/// <param name="metadata">Dungeon metadata with regions and distance fields</param>
	/// <param name="floorDepth">Current floor (10 = final floor with throne)</param>
	/// <param name="occupiedPositions">Already occupied positions</param>
	/// <returns>Position where stairs/throne was placed, or null if failed</returns>
	public GridPosition? PlaceStairs(
		DungeonMetadata metadata,
		int floorDepth,
		HashSet<Vector2I> occupiedPositions)
	{
		if (metadata?.Regions == null || metadata.Regions.Count == 0)
		{
			GD.PushWarning("StairsSpawner: No regions available for stairs placement");
			return null;
		}

		// Find the best region for stairs (farthest from entrance)
		var stairsRegion = FindStairsRegion(metadata);
		if (stairsRegion == null)
		{
			GD.PushWarning("StairsSpawner: Could not find suitable region for stairs");
			return null;
		}

		// Find the best position within the region
		var position = FindStairsPosition(stairsRegion, metadata, occupiedPositions);
		if (position == null)
		{
			GD.PushWarning("StairsSpawner: Could not find position for stairs in region");
			return null;
		}

		// Create and place the appropriate entity
		BaseEntity stairsEntity;
		if (floorDepth >= FINAL_FLOOR)
		{
			stairsEntity = CreateThrone(position.Value);
		}
		else
		{
			stairsEntity = CreateStairs(position.Value);
		}

		if (stairsEntity == null)
		{
			GD.PushWarning("StairsSpawner: Failed to create stairs/throne entity");
			return null;
		}

		_entityManager.AddEntity(stairsEntity);
		occupiedPositions.Add(new Vector2I(position.Value.X, position.Value.Y));

		// Update metadata
		metadata.ExitPosition = position.Value;

		return position.Value;
	}

	/// <summary>
	/// Finds the region farthest from the entrance for stairs placement.
	/// </summary>
	private Region FindStairsRegion(DungeonMetadata metadata)
	{
		// If we have entrance distance field, use it to find farthest region
		if (metadata.EntranceDistance != null && metadata.EntrancePosition.HasValue)
		{
			return FindFarthestRegionByDistance(metadata);
		}

		// Fallback: find region with centroid farthest from entrance
		return FindFarthestRegionByCentroid(metadata);
	}

	/// <summary>
	/// Finds region with highest average distance from entrance.
	/// </summary>
	private Region FindFarthestRegionByDistance(DungeonMetadata metadata)
	{
		Region farthestRegion = null;
		float maxAvgDistance = 0;

		foreach (var region in metadata.Regions)
		{
			// Skip entrance region
			if (metadata.EntrancePosition.HasValue)
			{
				var entranceRegion = metadata.GetRegionAt(metadata.EntrancePosition.Value);
				if (entranceRegion != null && region.Id == entranceRegion.Id)
					continue;
			}

			// Calculate average distance for this region
			float totalDistance = 0;
			int validTiles = 0;

			foreach (var tile in region.Tiles)
			{
				float dist = metadata.EntranceDistance.GetDistance(tile);
				if (dist >= 0 && dist < float.MaxValue)
				{
					totalDistance += dist;
					validTiles++;
				}
			}

			if (validTiles > 0)
			{
				float avgDistance = totalDistance / validTiles;
				if (avgDistance > maxAvgDistance)
				{
					maxAvgDistance = avgDistance;
					farthestRegion = region;
				}
			}
		}

		return farthestRegion;
	}

	/// <summary>
	/// Fallback: finds region with centroid farthest from entrance.
	/// </summary>
	private Region FindFarthestRegionByCentroid(DungeonMetadata metadata)
	{
		if (!metadata.EntrancePosition.HasValue)
		{
			// No entrance, just pick the largest region
			return metadata.Regions.OrderByDescending(r => r.Area).FirstOrDefault();
		}

		var entrancePos = metadata.EntrancePosition.Value;
		Region farthestRegion = null;
		int maxDistance = 0;

		foreach (var region in metadata.Regions)
		{
			int distance = Mathf.Abs(region.Centroid.X - entrancePos.X) +
						  Mathf.Abs(region.Centroid.Y - entrancePos.Y);

			if (distance > maxDistance)
			{
				maxDistance = distance;
				farthestRegion = region;
			}
		}

		return farthestRegion;
	}

	/// <summary>
	/// Finds the best position for stairs within a region.
	/// Prefers corners and edges (alcove-like positions).
	/// </summary>
	private GridPosition? FindStairsPosition(
		Region region,
		DungeonMetadata metadata,
		HashSet<Vector2I> occupiedPositions)
	{
		// Prefer edge tiles (corners feel more natural for stairs)
		var edgeCandidates = region.EdgeTiles?
			.Where(t => !occupiedPositions.Contains(new Vector2I(t.X, t.Y)))
			.ToList();

		if (edgeCandidates != null && edgeCandidates.Count > 0)
		{
			// Pick the edge tile farthest from entrance
			if (metadata.EntranceDistance != null)
			{
				return edgeCandidates
					.OrderByDescending(t => metadata.EntranceDistance.GetDistance(t))
					.FirstOrDefault();
			}

			// Random edge tile
			return edgeCandidates[_rng.RandiRange(0, edgeCandidates.Count - 1)];
		}

		// Fallback to any available tile
		var availableTiles = region.Tiles
			.Where(t => !occupiedPositions.Contains(new Vector2I(t.X, t.Y)))
			.ToList();

		if (availableTiles.Count == 0)
			return null;

		// Pick farthest from entrance
		if (metadata.EntranceDistance != null)
		{
			return availableTiles
				.OrderByDescending(t => metadata.EntranceDistance.GetDistance(t))
				.FirstOrDefault();
		}

		return availableTiles[_rng.RandiRange(0, availableTiles.Count - 1)];
	}

	private Stairs CreateStairs(GridPosition position)
	{
		var stairs = new Stairs();
		stairs.Initialize(position);
		return stairs;
	}

	private ThroneOfDespair CreateThrone(GridPosition position)
	{
		var throne = new ThroneOfDespair();
		throne.Initialize(position);
		return throne;
	}

	/// <summary>
	/// Gets the region containing stairs/throne.
	/// Useful for spawning guardians near the exit.
	/// </summary>
	public Region GetStairsRegion(DungeonMetadata metadata)
	{
		if (!metadata.ExitPosition.HasValue)
			return null;

		return metadata.GetRegionAt(metadata.ExitPosition.Value);
	}
}
