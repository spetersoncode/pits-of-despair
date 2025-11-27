using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;
using PitsOfDespair.Generation.Config;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to pipeline configs from YAML files.
/// Pipelines define map generation strategy and layout-dependent spawn settings.
/// </summary>
public class PipelineConfigLoader
{
    private const string DataPath = "res://Data/Pipelines/";

    private readonly Dictionary<string, PipelineConfig> _data = new();

    public void Load()
    {
        _data.Clear();

        if (!DirAccess.DirExistsAbsolute(DataPath))
        {
            GD.Print("[PipelineConfigLoader] No Pipelines directory found, pipeline configs not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(DataPath, _data, "pipeline config", (config, id) =>
        {
            // Ensure name is set from filename if not specified in YAML
            if (string.IsNullOrEmpty(config.Name))
            {
                config.Name = id;
            }
        });

        GD.Print($"[PipelineConfigLoader] Loaded {_data.Count} pipeline configs");
    }

    // === Public Accessors ===

    public PipelineConfig Get(string id)
    {
        if (_data.TryGetValue(id, out var config))
        {
            return config;
        }
        GD.PrintErr($"[PipelineConfigLoader] Pipeline config '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out PipelineConfig config)
    {
        return _data.TryGetValue(id, out config);
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<PipelineConfig> GetAll() => _data.Values;

    public int Count => _data.Count;
}
