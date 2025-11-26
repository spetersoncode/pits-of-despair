using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;
using PitsOfDespair.Generation.Config;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to floor generation configs from YAML files.
/// </summary>
public class FloorConfigLoader
{
    private const string DataPath = "res://Data/Floors/";

    private readonly Dictionary<string, FloorGenerationConfig> _data = new();

    public void Load()
    {
        _data.Clear();

        if (!DirAccess.DirExistsAbsolute(DataPath))
        {
            GD.Print("[FloorConfigLoader] No Floors directory found, floor configs not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(DataPath, _data, "floor config");
        GD.Print($"[FloorConfigLoader] Loaded {_data.Count} floor configs");
    }

    // === Public Accessors ===

    public FloorGenerationConfig Get(string id)
    {
        if (_data.TryGetValue(id, out var config))
        {
            return config;
        }
        GD.PrintErr($"[FloorConfigLoader] Floor config '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out FloorGenerationConfig config)
    {
        return _data.TryGetValue(id, out config);
    }

    /// <summary>
    /// Gets floor generation config for a specific floor depth.
    /// Returns the first config where MinFloor <= depth <= MaxFloor.
    /// Falls back to "default" if no matching config found.
    /// </summary>
    public FloorGenerationConfig GetForDepth(int depth)
    {
        foreach (var config in _data.Values)
        {
            if (depth >= config.MinFloor && depth <= config.MaxFloor)
            {
                return config;
            }
        }

        // Fallback to default
        if (_data.TryGetValue("default", out var defaultConfig))
        {
            return defaultConfig;
        }

        GD.PushWarning($"[FloorConfigLoader] No floor config found for depth {depth}, returning null");
        return null;
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<FloorGenerationConfig> GetAll() => _data.Values;

    public int Count => _data.Count;
}
