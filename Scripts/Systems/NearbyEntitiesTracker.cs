using Godot;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Systems;

/// <summary>
/// Tracks nearby visible entities relative to the player.
/// Queries, filters by visibility, sorts by distance, and provides the top N nearest entities.
/// </summary>
public partial class NearbyEntitiesTracker : Node
{
	#region Signals

	/// <summary>
	/// Emitted when the list of nearby entities changes.
	/// Parameter: list of (entity, distance) tuples
	/// </summary>
	[Signal]
	public delegate void NearbyEntitiesChangedEventHandler(Godot.Collections.Array nearbyEntities);

	#endregion

	#region Constants

	/// <summary>
	/// Maximum number of nearby entities to track.
	/// </summary>
	private const int MAX_NEARBY_ENTITIES = 5;

	#endregion

	#region State

	private Player _player;
	private EntityManager _entityManager;
	private PlayerVisionSystem _visionSystem;
	private List<(BaseEntity entity, int distance)> _cachedNearbyEntities = new();

	#endregion

	#region Initialization

	/// <summary>
	/// Initializes the tracker with required systems.
	/// </summary>
	/// <param name="player">The player entity</param>
	/// <param name="entityManager">Entity management system</param>
	/// <param name="visionSystem">Player vision system for visibility checks</param>
	public void Initialize(Player player, EntityManager entityManager, PlayerVisionSystem visionSystem)
	{
		if (player == null)
		{
			GD.PushError("NearbyEntitiesTracker: Cannot initialize with null player.");
			return;
		}

		if (entityManager == null)
		{
			GD.PushError("NearbyEntitiesTracker: Cannot initialize with null entity manager.");
			return;
		}

		if (visionSystem == null)
		{
			GD.PushError("NearbyEntitiesTracker: Cannot initialize with null vision system.");
			return;
		}

		_player = player;
		_entityManager = entityManager;
		_visionSystem = visionSystem;

		// Listen to player position changes
		_player.Connect(
			Player.SignalName.PositionChanged,
			Callable.From<Vector2I>(OnPlayerPositionChanged)
		);

		// Listen to entity additions (new entities appearing)
		_entityManager.Connect(
			EntityManager.SignalName.EntityAdded,
			Callable.From<BaseEntity>(OnEntityAdded)
		);

		// Listen to entity removals (entities disappearing)
		_entityManager.Connect(
			EntityManager.SignalName.EntityRemoved,
			Callable.From<BaseEntity>(OnEntityRemoved)
		);

		// Perform initial query
		UpdateNearbyEntities();
	}

	#endregion

	#region Query Methods

	/// <summary>
	/// Gets the current list of nearby entities.
	/// Returns up to MAX_NEARBY_ENTITIES entities sorted by distance.
	/// </summary>
	public List<(BaseEntity entity, int distance)> GetNearbyEntities()
	{
		return new List<(BaseEntity, int)>(_cachedNearbyEntities);
	}

	/// <summary>
	/// Updates the cached list of nearby entities.
	/// Queries all entities, filters by visibility, sorts by distance, and takes top N.
	/// </summary>
	private void UpdateNearbyEntities()
	{
		if (_player == null || _entityManager == null || _visionSystem == null)
		{
			return;
		}

		var visibleEntities = new List<(BaseEntity entity, int distance)>();
		var playerPos = _player.GridPosition;

		// Query all entities and filter by visibility
		foreach (var entity in _entityManager.GetAllEntities())
		{
			// Skip if entity is no longer valid
			if (!GodotObject.IsInstanceValid(entity))
			{
				continue;
			}

			// Skip if not visible
			if (!_visionSystem.IsVisible(entity.GridPosition))
			{
				continue;
			}

			// Calculate Chebyshev distance (grid-based, roguelike standard)
			int distance = DistanceHelper.ChebyshevDistance(playerPos, entity.GridPosition);

			// Skip if at player position (distance 0)
			if (distance == 0)
			{
				continue;
			}

			visibleEntities.Add((entity, distance));
		}

		// Sort by distance and take top N
		visibleEntities.Sort((a, b) => a.distance.CompareTo(b.distance));
		var newNearbyEntities = visibleEntities.Take(MAX_NEARBY_ENTITIES).ToList();

		// Check if the list changed
		if (HasListChanged(_cachedNearbyEntities, newNearbyEntities))
		{
			_cachedNearbyEntities = newNearbyEntities;
			EmitNearbyEntitiesChanged();
		}
	}

	/// <summary>
	/// Checks if two nearby entity lists are different.
	/// </summary>
	private bool HasListChanged(
		List<(BaseEntity entity, int distance)> oldList,
		List<(BaseEntity entity, int distance)> newList)
	{
		if (oldList.Count != newList.Count)
		{
			return true;
		}

		for (int i = 0; i < oldList.Count; i++)
		{
			if (oldList[i].entity != newList[i].entity || oldList[i].distance != newList[i].distance)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Emits the NearbyEntitiesChanged signal with the current list.
	/// Converts to Godot-compatible array format.
	/// </summary>
	private void EmitNearbyEntitiesChanged()
	{
		// Convert to Godot Collections.Array for signal emission
		var godotArray = new Godot.Collections.Array();
		foreach (var (entity, distance) in _cachedNearbyEntities)
		{
			var dict = new Godot.Collections.Dictionary
			{
				{ "entity", entity },
				{ "distance", distance }
			};
			godotArray.Add(dict);
		}

		EmitSignal(SignalName.NearbyEntitiesChanged, godotArray);
	}

	#endregion

	#region Event Handlers

	/// <summary>
	/// Called when the player's position changes.
	/// </summary>
	private void OnPlayerPositionChanged(Vector2I newPosition)
	{
		UpdateNearbyEntities();
	}

	/// <summary>
	/// Called when a new entity is added to the world.
	/// </summary>
	private void OnEntityAdded(BaseEntity entity)
	{
		// New entity might be nearby, update the list
		UpdateNearbyEntities();
	}

	/// <summary>
	/// Called when an entity is removed from the world.
	/// </summary>
	private void OnEntityRemoved(BaseEntity entity)
	{
		// Removed entity might have been in the nearby list, update it
		UpdateNearbyEntities();
	}

	#endregion

	#region Cleanup

	public override void _ExitTree()
	{
		// Disconnect from player signal
		if (_player != null)
		{
			_player.Disconnect(
				Player.SignalName.PositionChanged,
				Callable.From<Vector2I>(OnPlayerPositionChanged)
			);
		}

		// Disconnect from entity manager signals
		if (_entityManager != null)
		{
			_entityManager.Disconnect(
				EntityManager.SignalName.EntityAdded,
				Callable.From<BaseEntity>(OnEntityAdded)
			);
			_entityManager.Disconnect(
				EntityManager.SignalName.EntityRemoved,
				Callable.From<BaseEntity>(OnEntityRemoved)
			);
		}
	}

	#endregion
}
