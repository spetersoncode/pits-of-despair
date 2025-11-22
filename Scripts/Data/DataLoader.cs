using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using PitsOfDespair.Core;
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
        LoadYamlFilesRecursive(CreaturesPath, "", _creatures, "creature", (creature, id) =>
        {
            creature.ApplyDefaults(); // Apply type-based defaults
        });
    }

    private void LoadAllItems()
    {
        _items.Clear();
        LoadItemsRecursive(ItemsPath, "");
    }

    /// <summary>
    /// Specialized loader for items that uses type-based IDs.
    /// ID format: {type}_{filename} (e.g., "weapon_club", "potion_cure_light_wounds")
    /// </summary>
    private void LoadItemsRecursive(string basePath, string currentRelativePath)
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
            if (fileName.StartsWith('.'))
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

                LoadItemsRecursive(basePath, newRelativePath);
            }
            else if (fileName.EndsWith(".yaml"))
            {
                string filePath = fullPath + fileName;
                string fileNameWithoutExt = fileName.Replace(".yaml", "");

                // Load the YAML to get the type field
                var item = LoadYamlFile<ItemData>(filePath);
                if (item != null)
                {
                    // Generate ID as {type}_{filename}
                    string id;
                    if (!string.IsNullOrEmpty(item.Type))
                    {
                        id = $"{item.Type.ToLower()}_{fileNameWithoutExt}";
                    }
                    else
                    {
                        // Fallback: use filename only if no type specified
                        id = fileNameWithoutExt;
                        GD.PushWarning($"DataLoader: Item '{fileNameWithoutExt}' has no type, using filename as ID");
                    }

                    if (_items.ContainsKey(id))
                    {
                        string relativePath = string.IsNullOrEmpty(currentRelativePath)
                            ? fileName
                            : currentRelativePath + "/" + fileName;
                        GD.PushWarning($"DataLoader: Duplicate item ID '{id}' from {relativePath}, skipping");
                    }
                    else
                    {
                        item.DataFileId = id; // Set unique ID for inventory stacking
                        item.ApplyDefaults(); // Apply type-based defaults
                        _items[id] = item;
                        // GD.Print($"DataLoader: Loaded item '{id}' from {fileName}");
                    }
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
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
            if (fileName.StartsWith('.'))
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
                    }
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    /// <summary>
    /// Converts a file path to a valid ID by extracting just the filename.
    /// Examples: "Goblins/warrior.yaml" -> "warrior"
    ///           "Vermin/rat.yaml" -> "rat"
    /// </summary>
    private string ConvertPathToId(string path)
    {
        // Remove .yaml extension
        string withoutExtension = path.Replace(".yaml", "");

        // Extract just the filename (last part after /)
        string[] parts = withoutExtension.Split('/');
        string filename = parts[parts.Length - 1];

        // Convert to lowercase
        return filename.ToLower();
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
                .WithTypeConverter(new PaletteColorConverter())
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

/// <summary>
/// YamlDotNet type converter that allows YAML files to reference Palette colors by name.
/// Supports both formats: "color: Iron" and "color: "#665544"".
/// </summary>
public class PaletteColorConverter : IYamlTypeConverter
{
    private static readonly Dictionary<string, string> _colorCache = new();

    static PaletteColorConverter()
    {
        // Build cache of Palette color names to hex values using reflection
        var paletteType = typeof(Palette);
        var colorFields = paletteType.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in colorFields)
        {
            if (field.FieldType == typeof(Color))
            {
                var color = (Color)field.GetValue(null);
                var hexValue = Palette.ToHex(color);
                _colorCache[field.Name.ToLower()] = hexValue;
            }
        }
    }

    public bool Accepts(Type type)
    {
        return type == typeof(string);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        // If it's already a hex color (starts with #), return as-is
        if (value.StartsWith('#'))
        {
            return value;
        }

        // Try to map it to a Palette color name
        var colorNameLower = value.ToLower();
        if (_colorCache.TryGetValue(colorNameLower, out var hexValue))
        {
            return hexValue;
        }

        // If not found, return the value as-is and let validation handle it
        GD.PushWarning($"PaletteColorConverter: Unknown palette color '{value}', using as-is");
        return value;
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar((string)value));
    }
}
