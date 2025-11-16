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
    private const string ItemsPath = "res://Data/Items/";

    private Dictionary<string, JsonCreatureData> _creatures = new();
    private Dictionary<string, JsonSpawnTable> _spawnTables = new();
    private Dictionary<string, JsonItemData> _items = new();
    private Dictionary<string, JsonItemSpawnTable> _itemSpawnTables = new();

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

    /// <summary>
    /// Gets item data by ID (filename without extension).
    /// </summary>
    public JsonItemData GetItem(string itemId)
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
    public JsonItemSpawnTable GetItemSpawnTable(string tableId)
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
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json"))
            {
                string filePath = ItemsPath + fileName;
                string itemId = fileName.Replace(".json", "");

                var item = LoadJsonFile<JsonItemData>(filePath);
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
            // Only load files ending with "_items.json"
            if (!dir.CurrentIsDir() && fileName.EndsWith("_items.json"))
            {
                string filePath = SpawnTablesPath + fileName;
                string tableId = fileName.Replace(".json", "");

                var table = LoadJsonFile<JsonItemSpawnTable>(filePath);
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
