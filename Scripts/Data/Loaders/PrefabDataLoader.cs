using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;
using PitsOfDespair.Generation.Prefabs;
using PitsOfDespair.Generation.Passes;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to prefab data from YAML files.
/// Also maintains the static PrefabLoader bridge for generation passes.
/// </summary>
public class PrefabDataLoader
{
    private const string DataPath = "res://Data/Prefabs/";

    private readonly Dictionary<string, PrefabData> _data = new();

    public void Load()
    {
        _data.Clear();

        if (!DirAccess.DirExistsAbsolute(DataPath))
        {
            GD.Print("[PrefabDataLoader] No Prefabs directory found, prefabs not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(DataPath, _data, "prefab", (prefab, id) =>
        {
            // Set the name from the file ID if not specified
            if (string.IsNullOrEmpty(prefab.Name))
            {
                prefab.Name = id;
            }
        });

        // Register prefabs with the static accessor for generation passes
        PrefabLoader.SetPrefabs(_data);

        GD.Print($"[PrefabDataLoader] Loaded {_data.Count} prefabs");
    }

    // === Public Accessors ===

    public PrefabData Get(string id)
    {
        if (_data.TryGetValue(id, out var prefab))
        {
            return prefab;
        }
        GD.PrintErr($"[PrefabDataLoader] Prefab '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out PrefabData prefab)
    {
        return _data.TryGetValue(id, out prefab);
    }

    /// <summary>
    /// Gets all prefabs valid for a specific floor depth.
    /// </summary>
    public List<PrefabData> GetForFloor(int depth)
    {
        var result = new List<PrefabData>();
        foreach (var prefab in _data.Values)
        {
            if (depth >= prefab.Placement.MinFloor && depth <= prefab.Placement.MaxFloor)
            {
                result.Add(prefab);
            }
        }
        return result;
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<PrefabData> GetAll() => _data.Values;

    public int Count => _data.Count;
}
