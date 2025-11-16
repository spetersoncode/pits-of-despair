using Godot;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PitsOfDespair.Data;

/// <summary>
/// Singleton autoload that loads and provides access to game data from YAML files.
/// </summary>
public partial class DataLoader : Node
{
    private const string CreaturesPath = "res://Data/Creatures/";
    private const string SpawnTablesPath = "res://Data/SpawnTables/";
    private const string ItemsPath = "res://Data/Items/";

    private Dictionary<string, CreatureData> _creatures = new();
    private Dictionary<string, CreatureSpawnTable> _spawnTables = new();
    private Dictionary<string, ItemData> _items = new();
    private Dictionary<string, ItemSpawnTable> _itemSpawnTables = new();

    public override void _Ready()
    {
        LoadAllCreatures();
        LoadAllSpawnTables();
        LoadAllItems();
        LoadAllItemSpawnTables();

        GD.Print($"DataLoader: Loaded {_creatures.Count} creatures, {_spawnTables.Count} spawn tables, {_items.Count} items, and {_itemSpawnTables.Count} item spawn tables");
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
    /// Gets spawn table data by ID (filename without extension).
    /// </summary>
    public CreatureSpawnTable GetSpawnTable(string tableId)
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
    /// Gets item spawn table data by ID (filename without extension).
    /// </summary>
    public ItemSpawnTable GetItemSpawnTable(string tableId)
    {
        if (_itemSpawnTables.TryGetValue(tableId, out var table))
        {
            return table;
        }

        GD.PrintErr($"DataLoader: Item spawn table '{tableId}' not found!");
        return null;
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
            if (!dir.CurrentIsDir() && fileName.EndsWith(".yaml"))
            {
                string filePath = CreaturesPath + fileName;
                string creatureId = fileName.Replace(".yaml", "");

                var creature = LoadYamlFile<CreatureData>(filePath);
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
            if (!dir.CurrentIsDir() && fileName.EndsWith("_creatures.yaml"))
            {
                string filePath = SpawnTablesPath + fileName;
                string tableId = fileName.Replace(".yaml", "");

                var table = LoadYamlFile<CreatureSpawnTable>(filePath);
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

    private void LoadAllItems()
    {
        _items.Clear();

        if (!DirAccess.DirExistsAbsolute(ItemsPath))
        {
            GD.PrintErr($"DataLoader: Items directory not found at {ItemsPath}");
            return;
        }

        var dir = DirAccess.Open(ItemsPath);
        if (dir == null)
        {
            GD.PrintErr($"DataLoader: Failed to open items directory");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (fileName != string.Empty)
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".yaml"))
            {
                string filePath = ItemsPath + fileName;
                string itemId = fileName.Replace(".yaml", "");

                var item = LoadYamlFile<ItemData>(filePath);
                if (item != null)
                {
                    _items[itemId] = item;
                    GD.Print($"DataLoader: Loaded item '{itemId}' - {item.Name}");
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    private void LoadAllItemSpawnTables()
    {
        _itemSpawnTables.Clear();

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
            // Only load files ending with "_items.yaml"
            if (!dir.CurrentIsDir() && fileName.EndsWith("_items.yaml"))
            {
                string filePath = SpawnTablesPath + fileName;
                string tableId = fileName.Replace(".yaml", "");

                var table = LoadYamlFile<ItemSpawnTable>(filePath);
                if (table != null)
                {
                    _itemSpawnTables[tableId] = table;
                    GD.Print($"DataLoader: Loaded item spawn table '{tableId}' - {table.Name}");
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
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
