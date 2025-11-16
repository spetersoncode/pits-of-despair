using Godot;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Data;

/// <summary>
/// Singleton autoload that loads and provides access to game data from YAML files.
/// </summary>
public partial class DataLoader : Node
{
    private const string CreaturesPath = "res://Data/Creatures/";
    private const string SpawnTablesPath = "res://Data/SpawnTables/";
    private const string ItemsPath = "res://Data/Items/";
    private const string BandsPath = "res://Data/Bands/";

    private Dictionary<string, CreatureData> _creatures = new();
    private Dictionary<string, ItemData> _items = new();

    // Spawning system data
    private Dictionary<string, SpawnTableData> _spawnTables = new();
    private Dictionary<string, BandData> _bands = new();

    public override void _Ready()
    {
        LoadAllCreatures();
        LoadAllItems();
        LoadAllSpawnTables();
        LoadAllBands();

        GD.Print($"DataLoader: Loaded {_creatures.Count} creatures, {_items.Count} items, {_spawnTables.Count} spawn tables, {_bands.Count} bands");
    }

    /// <summary>
    /// Gets creature data by ID (filename without extension).
    /// </summary>
    public CreatureData GetCreature(string creatureId)
    {
        if (_creatures.TryGetValue(creatureId, out var creature))
        {
            return creature;
        }

        GD.PrintErr($"DataLoader: Creature '{creatureId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets all loaded creature IDs.
    /// </summary>
    public IEnumerable<string> GetAllCreatureIds()
    {
        return _creatures.Keys;
    }

    /// <summary>
    /// Gets item data by ID (filename without extension).
    /// </summary>
    public ItemData GetItem(string itemId)
    {
        if (_items.TryGetValue(itemId, out var item))
        {
            return item;
        }

        GD.PrintErr($"DataLoader: Item '{itemId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets spawn table data by ID (filename without extension).
    /// </summary>
    public SpawnTableData GetSpawnTable(string tableId)
    {
        if (_spawnTables.TryGetValue(tableId, out var table))
        {
            return table;
        }

        GD.PrintErr($"DataLoader: Spawn table '{tableId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets band data by ID (filename without extension).
    /// </summary>
    public BandData GetBand(string bandId)
    {
        if (_bands.TryGetValue(bandId, out var band))
        {
            return band;
        }

        GD.PrintErr($"DataLoader: Band '{bandId}' not found!");
        return null;
    }

    private void LoadAllCreatures()
    {
        _creatures.Clear();
        LoadYamlFilesRecursive(CreaturesPath, "", _creatures, "creature");
    }

    private void LoadAllItems()
    {
        _items.Clear();
        LoadYamlFilesRecursive(ItemsPath, "", _items, "item", (item, id) =>
        {
            item.DataFileId = id; // Set unique ID for inventory stacking
            item.ApplyDefaults(); // Apply type-based defaults
        });
    }

    private void LoadAllSpawnTables()
    {
        _spawnTables.Clear();
        LoadYamlFilesRecursive(SpawnTablesPath, "", _spawnTables, "spawn table");
    }

    private void LoadAllBands()
    {
        _bands.Clear();

        // Bands folder is optional (not all projects may use bands)
        if (!DirAccess.DirExistsAbsolute(BandsPath))
        {
            GD.Print($"DataLoader: Bands directory not found at {BandsPath}, skipping band loading");
            return;
        }

        LoadYamlFilesRecursive(BandsPath, "", _bands, "band");
    }

    /// <summary>
    /// Recursively loads YAML files from a directory and its subdirectories.
    /// Converts folder paths to IDs (e.g., "Goblins/warrior.yaml" -> "goblins_warrior").
    /// </summary>
    private void LoadYamlFilesRecursive<T>(
        string basePath,
        string currentRelativePath,
        Dictionary<string, T> targetDictionary,
        string fileTypeName,
        Action<T, string> postLoadAction = null) where T : class
    {
        string fullPath = string.IsNullOrEmpty(currentRelativePath)
            ? basePath
            : basePath + currentRelativePath + "/";

        if (!DirAccess.DirExistsAbsolute(fullPath))
        {
            return;
        }

        var dir = DirAccess.Open(fullPath);
        if (dir == null)
        {
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != string.Empty)
        {
            // Skip hidden files/folders
            if (fileName.StartsWith("."))
            {
                fileName = dir.GetNext();
                continue;
            }

            if (dir.CurrentIsDir())
            {
                // Recursively scan subdirectory
                string newRelativePath = string.IsNullOrEmpty(currentRelativePath)
                    ? fileName
                    : currentRelativePath + "/" + fileName;

                LoadYamlFilesRecursive(basePath, newRelativePath, targetDictionary, fileTypeName, postLoadAction);
            }
            else if (fileName.EndsWith(".yaml"))
            {
                // Convert path to ID: "Goblins/warrior.yaml" -> "goblins_warrior"
                string relativePath = string.IsNullOrEmpty(currentRelativePath)
                    ? fileName
                    : currentRelativePath + "/" + fileName;

                string id = ConvertPathToId(relativePath);
                string filePath = fullPath + fileName;

                var data = LoadYamlFile<T>(filePath);
                if (data != null)
                {
                    if (targetDictionary.ContainsKey(id))
                    {
                        GD.PushWarning($"DataLoader: Duplicate {fileTypeName} ID '{id}' from {relativePath}, skipping");
                    }
                    else
                    {
                        // Call post-load action if provided (e.g., to set DataFileId for items)
                        postLoadAction?.Invoke(data, id);

                        targetDictionary[id] = data;
                        GD.Print($"DataLoader: Loaded {fileTypeName} '{id}' from {relativePath}");
                    }
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    /// <summary>
    /// Converts a file path to a valid ID.
    /// Examples: "Goblins/warrior.yaml" -> "goblins_warrior"
    ///           "Items/Potions/health.yaml" -> "items_potions_health"
    /// </summary>
    private string ConvertPathToId(string path)
    {
        // Remove .yaml extension
        string withoutExtension = path.Replace(".yaml", "");

        // Replace path separators with underscores
        string normalized = withoutExtension.Replace("/", "_").Replace("\\", "_");

        // Convert to lowercase
        return normalized.ToLower();
    }

    private T LoadYamlFile<T>(string path) where T : class
    {
        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"DataLoader: Failed to open file {path}");
                return null;
            }

            string yamlText = file.GetAsText();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<T>(yamlText);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"DataLoader: Error loading {path}: {ex.Message}");
            return null;
        }
    }
}
