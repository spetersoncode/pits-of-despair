using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PitsOfDespair.Data;

/// <summary>
/// Singleton autoload that loads and provides access to game data from JSON files.
/// </summary>
public partial class DataLoader : Node
{
    private const string CreaturesPath = "res://Data/Creatures/";
    private const string SpawnTablesPath = "res://Data/SpawnTables/";

    private Dictionary<string, JsonCreatureData> _creatures = new();
    private Dictionary<string, JsonSpawnTable> _spawnTables = new();

    public override void _Ready()
    {
        LoadAllCreatures();
        LoadAllSpawnTables();

        GD.Print($"DataLoader: Loaded {_creatures.Count} creatures and {_spawnTables.Count} spawn tables");
    }

    /// <summary>
    /// Gets creature data by ID (filename without extension).
    /// </summary>
    public JsonCreatureData GetCreature(string creatureId)
    {
        if (_creatures.TryGetValue(creatureId, out var creature))
        {
            return creature;
        }

        GD.PrintErr($"DataLoader: Creature '{creatureId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets spawn table data by ID (filename without extension).
    /// </summary>
    public JsonSpawnTable GetSpawnTable(string tableId)
    {
        if (_spawnTables.TryGetValue(tableId, out var table))
        {
            return table;
        }

        GD.PrintErr($"DataLoader: Spawn table '{tableId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets all loaded creature IDs.
    /// </summary>
    public IEnumerable<string> GetAllCreatureIds()
    {
        return _creatures.Keys;
    }

    private void LoadAllCreatures()
    {
        _creatures.Clear();

        if (!DirAccess.DirExistsAbsolute(CreaturesPath))
        {
            GD.PrintErr($"DataLoader: Creatures directory not found at {CreaturesPath}");
            return;
        }

        var dir = DirAccess.Open(CreaturesPath);
        if (dir == null)
        {
            GD.PrintErr($"DataLoader: Failed to open creatures directory");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != string.Empty)
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
            {
                string filePath = CreaturesPath + fileName;
                string creatureId = fileName.Replace(".json", "");

                var creature = LoadJsonFile<JsonCreatureData>(filePath);
                if (creature != null)
                {
                    _creatures[creatureId] = creature;
                    GD.Print($"DataLoader: Loaded creature '{creatureId}' - {creature.Name}");
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    private void LoadAllSpawnTables()
    {
        _spawnTables.Clear();

        if (!DirAccess.DirExistsAbsolute(SpawnTablesPath))
        {
            GD.PrintErr($"DataLoader: SpawnTables directory not found at {SpawnTablesPath}");
            return;
        }

        var dir = DirAccess.Open(SpawnTablesPath);
        if (dir == null)
        {
            GD.PrintErr($"DataLoader: Failed to open spawn tables directory");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != string.Empty)
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
            {
                string filePath = SpawnTablesPath + fileName;
                string tableId = fileName.Replace(".json", "");

                var table = LoadJsonFile<JsonSpawnTable>(filePath);
                if (table != null)
                {
                    _spawnTables[tableId] = table;
                    GD.Print($"DataLoader: Loaded spawn table '{tableId}' - {table.Name}");
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    private T LoadJsonFile<T>(string path) where T : class
    {
        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"DataLoader: Failed to open file {path}");
                return null;
            }

            string jsonText = file.GetAsText();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<T>(jsonText, options);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataLoader: Error loading {path}: {ex.Message}");
            return null;
        }
    }
}
