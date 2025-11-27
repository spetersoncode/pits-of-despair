using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;
using PitsOfDespair.Generation.Config;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to floor configs from YAML files.
/// Floors define difficulty curve, content composition, and pipeline selection.
/// </summary>
public class FloorConfigLoader
{
    private const string DataPath = "res://Data/Floors/";

    private readonly Dictionary<string, FloorConfig> _data = new();

    public void Load()
    {
        _data.Clear();

        if (!DirAccess.DirExistsAbsolute(DataPath))
        {
            GD.Print("[FloorConfigLoader] No Floors directory found, floor configs not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(DataPath, _data, "floor config", (config, id) =>
        {
            // Ensure name is set from filename if not specified in YAML
            if (string.IsNullOrEmpty(config.Name))
            {
                config.Name = id;
            }
        });

        GD.Print($"[FloorConfigLoader] Loaded {_data.Count} floor configs");
    }

    // === Public Accessors ===

    public FloorConfig Get(string id)
    {
        if (_data.TryGetValue(id, out var config))
        {
            return config;
        }
        GD.PrintErr($"[FloorConfigLoader] Floor config '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out FloorConfig config)
    {
        return _data.TryGetValue(id, out config);
    }

    /// <summary>
    /// Gets floor config for a specific floor depth.
    /// Throws if no config found for the depth.
    /// </summary>
    public FloorConfig GetForDepth(int depth)
    {
        foreach (var config in _data.Values)
        {
            if (config.Floor == depth)
            {
                return config;
            }
        }

        throw new System.InvalidOperationException($"[FloorConfigLoader] No floor config found for depth {depth}. Create Data/Floors/floor_{depth}.yaml");
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<FloorConfig> GetAll() => _data.Values;

    public int Count => _data.Count;
}
