using Godot;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Systems.Spawning;

/// <summary>
/// Controls global spawn budget for the floor.
/// Simplified to work with budget-based spawning instead of per-room allocation.
/// </summary>
public class SpawnDensityController
{
    private readonly SpawnTableData _spawnTable;

    public SpawnDensityController(SpawnTableData spawnTable)
    {
        _spawnTable = spawnTable;
    }

    /// <summary>
    /// Gets the total creature spawn budget for the floor.
    /// </summary>
    public int GetCreatureBudget()
    {
        return _spawnTable.GetRandomCreatureBudget();
    }

    /// <summary>
    /// Gets the total item spawn budget for the floor.
    /// </summary>
    public int GetItemBudget()
    {
        return _spawnTable.GetRandomItemBudget();
    }

    /// <summary>
    /// Gets spawn table information for debugging.
    /// </summary>
    public string GetDebugInfo()
    {
        return $"SpawnDensityController: {_spawnTable.Name}\n" +
               $"  Creature Budget: {_spawnTable.CreatureBudget.Min}-{_spawnTable.CreatureBudget.Max}\n" +
               $"  Item Budget: {_spawnTable.ItemBudget.Min}-{_spawnTable.ItemBudget.Max}\n" +
               $"  Creature Pools: {_spawnTable.CreaturePools.Count}\n" +
               $"  Item Pools: {_spawnTable.ItemPools.Count}";
    }
}
