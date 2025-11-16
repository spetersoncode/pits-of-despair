using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Result of spawn budget allocation for a room.
/// </summary>
public class RoomSpawnAllocation
{
    public int RoomIndex { get; set; }
    public bool IsEmpty { get; set; }
    public bool IsOutOfDepth { get; set; }
    public int SpawnBudget { get; set; }
}

/// <summary>
/// Controls spawn density, empty rooms, and out-of-depth spawns.
/// Manages spawn budgets and pacing across the dungeon.
/// </summary>
public class SpawnDensityController
{
    private readonly SpawnTableData _spawnTable;

    public SpawnDensityController(SpawnTableData spawnTable)
    {
        _spawnTable = spawnTable;
    }

    /// <summary>
    /// Allocates spawn budget across all rooms.
    /// Determines which rooms are empty and which have out-of-depth spawns.
    /// </summary>
    /// <param name="roomCount">Total number of rooms in the dungeon</param>
    /// <returns>List of room allocations</returns>
    public List<RoomSpawnAllocation> AllocateSpawnBudget(int roomCount)
    {
        var allocations = new List<RoomSpawnAllocation>();

        if (roomCount <= 0)
        {
            return allocations;
        }

        // Get total spawn budget for the floor
        int totalBudget = _spawnTable.GetRandomSpawnBudget();

        // Determine which rooms are empty
        var emptyRooms = DetermineEmptyRooms(roomCount);

        // Determine which rooms have out-of-depth spawns
        var outOfDepthRooms = DetermineOutOfDepthRooms(roomCount, emptyRooms);

        // Calculate populated room count
        int populatedRoomCount = roomCount - emptyRooms.Count;

        if (populatedRoomCount <= 0)
        {
            // All rooms are empty
            for (int i = 0; i < roomCount; i++)
            {
                allocations.Add(new RoomSpawnAllocation
                {
                    RoomIndex = i,
                    IsEmpty = true,
                    IsOutOfDepth = false,
                    SpawnBudget = 0
                });
            }
            return allocations;
        }

        // Distribute budget across populated rooms
        var budgets = DistributeBudget(totalBudget, populatedRoomCount);

        int budgetIndex = 0;
        for (int i = 0; i < roomCount; i++)
        {
            bool isEmpty = emptyRooms.Contains(i);
            bool isOutOfDepth = outOfDepthRooms.Contains(i);

            allocations.Add(new RoomSpawnAllocation
            {
                RoomIndex = i,
                IsEmpty = isEmpty,
                IsOutOfDepth = isOutOfDepth,
                SpawnBudget = isEmpty ? 0 : budgets[budgetIndex++]
            });
        }

        return allocations;
    }

    /// <summary>
    /// Determines which rooms should remain empty based on empty room chance.
    /// </summary>
    private HashSet<int> DetermineEmptyRooms(int roomCount)
    {
        var emptyRooms = new HashSet<int>();

        for (int i = 0; i < roomCount; i++)
        {
            float roll = GD.Randf();
            if (roll < _spawnTable.EmptyRoomChance)
            {
                emptyRooms.Add(i);
            }
        }

        return emptyRooms;
    }

    /// <summary>
    /// Determines which rooms should have out-of-depth spawns.
    /// Only populated rooms can have OOD spawns.
    /// </summary>
    private HashSet<int> DetermineOutOfDepthRooms(int roomCount, HashSet<int> emptyRooms)
    {
        var outOfDepthRooms = new HashSet<int>();

        // Only check if we have out-of-depth pools configured
        if (_spawnTable.OutOfDepthPools == null || _spawnTable.OutOfDepthPools.Count == 0)
        {
            return outOfDepthRooms;
        }

        for (int i = 0; i < roomCount; i++)
        {
            // Skip empty rooms
            if (emptyRooms.Contains(i))
            {
                continue;
            }

            float roll = GD.Randf();
            if (roll < _spawnTable.OutOfDepthChance)
            {
                outOfDepthRooms.Add(i);
            }
        }

        return outOfDepthRooms;
    }

    /// <summary>
    /// Distributes total budget across rooms with some randomization.
    /// Ensures variety while staying within total budget.
    /// </summary>
    private List<int> DistributeBudget(int totalBudget, int roomCount)
    {
        var budgets = new List<int>();

        if (roomCount <= 0)
        {
            return budgets;
        }

        // Calculate base budget per room
        int baseBudget = Mathf.Max(1, totalBudget / roomCount);
        int remainingBudget = totalBudget;

        for (int i = 0; i < roomCount; i++)
        {
            // For last room, use all remaining budget
            if (i == roomCount - 1)
            {
                budgets.Add(Mathf.Max(0, remainingBudget));
            }
            else
            {
                // Randomize budget: 50% to 150% of base budget
                int minBudget = Mathf.Max(1, baseBudget / 2);
                int maxBudget = Mathf.Max(minBudget + 1, baseBudget * 3 / 2);

                // Ensure we don't exceed remaining budget
                maxBudget = Mathf.Min(maxBudget, remainingBudget - (roomCount - i - 1));

                int roomBudget = GD.RandRange(minBudget, maxBudget);
                budgets.Add(roomBudget);
                remainingBudget -= roomBudget;
            }
        }

        return budgets;
    }

    /// <summary>
    /// Gets spawn table information for debugging.
    /// </summary>
    public string GetDebugInfo()
    {
        return $"SpawnDensityController: {_spawnTable.Name}\n" +
               $"  Budget: {_spawnTable.SpawnBudget.Min}-{_spawnTable.SpawnBudget.Max}\n" +
               $"  Empty Room Chance: {_spawnTable.EmptyRoomChance:P0}\n" +
               $"  OOD Chance: {_spawnTable.OutOfDepthChance:P0}\n" +
               $"  Creature Pools: {_spawnTable.CreaturePools.Count}\n" +
               $"  Item Pools: {_spawnTable.ItemPools.Count}\n" +
               $"  OOD Pools: {_spawnTable.OutOfDepthPools?.Count ?? 0}";
    }
}
