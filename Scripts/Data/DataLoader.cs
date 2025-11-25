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
using PitsOfDespair.Generation.Prefabs;
using PitsOfDespair.Generation.Passes;

namespace PitsOfDespair.Data;

/// <summary>
/// Singleton autoload that loads and provides access to game data from YAML files.
/// Supports sheet format where multiple entities are defined in a single file.
/// </summary>
public partial class DataLoader : Node
{
    private const string CreaturesPath = "res://Data/Creatures/";
    private const string SpawnTablesPath = "res://Data/SpawnTables/";
    private const string ItemsPath = "res://Data/Items/";
    private const string BandsPath = "res://Data/Bands/";
    private const string SkillsPath = "res://Data/Skills/";
    private const string FloorsPath = "res://Data/Floors/";
    private const string PrefabsPath = "res://Data/Prefabs/";
    private const string ThemesPath = "res://Data/Themes/";
    private const string EncountersPath = "res://Data/Encounters/";
    private const string SpawnConfigsPath = "res://Data/SpawnConfigs/";

    private Dictionary<string, CreatureData> _creatures = new();
    private Dictionary<string, ItemData> _items = new();
    private Dictionary<string, SkillDefinition> _skills = new();

    // Spawning system data (legacy)
    private Dictionary<string, SpawnTableData> _spawnTables = new();
    private Dictionary<string, BandData> _bands = new();

    // New spawning system data
    private Dictionary<string, FactionTheme> _factionThemes = new();
    private Dictionary<string, EncounterTemplate> _encounterTemplates = new();
    private Dictionary<string, FloorSpawnConfig> _floorSpawnConfigs = new();

    // Floor generation data
    private Dictionary<string, Generation.Config.FloorGenerationConfig> _floorConfigs = new();
    private Dictionary<string, PrefabData> _prefabs = new();

    public override void _Ready()
    {
        LoadAllCreatures();
        LoadAllItems();
        LoadAllSpawnTables();
        LoadAllBands();
        LoadAllSkills();
        LoadAllFloorConfigs();
        LoadAllPrefabs();
        LoadAllFactionThemes();
        LoadAllEncounterTemplates();
        LoadAllFloorSpawnConfigs();
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
    /// Gets all loaded item IDs.
    /// </summary>
    public IEnumerable<string> GetAllItemIds()
    {
        return _items.Keys;
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

    /// <summary>
    /// Gets floor generation config by ID (filename without extension).
    /// </summary>
    public Generation.Config.FloorGenerationConfig GetFloorConfig(string configId)
    {
        if (_floorConfigs.TryGetValue(configId, out var config))
        {
            return config;
        }

        GD.PrintErr($"DataLoader: Floor config '{configId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets floor generation config for a specific floor depth.
    /// Returns the first config where MinFloor <= depth <= MaxFloor.
    /// Falls back to "default" if no matching config found.
    /// </summary>
    public Generation.Config.FloorGenerationConfig GetFloorConfigForDepth(int depth)
    {
        foreach (var config in _floorConfigs.Values)
        {
            if (depth >= config.MinFloor && depth <= config.MaxFloor)
            {
                return config;
            }
        }

        // Fallback to default
        if (_floorConfigs.TryGetValue("default", out var defaultConfig))
        {
            return defaultConfig;
        }

        GD.PushWarning($"DataLoader: No floor config found for depth {depth}, returning null");
        return null;
    }

    /// <summary>
    /// Gets all loaded floor config IDs.
    /// </summary>
    public IEnumerable<string> GetAllFloorConfigIds()
    {
        return _floorConfigs.Keys;
    }

    /// <summary>
    /// Gets prefab data by ID (filename without extension).
    /// </summary>
    public PrefabData GetPrefab(string prefabId)
    {
        if (_prefabs.TryGetValue(prefabId, out var prefab))
        {
            return prefab;
        }

        GD.PrintErr($"DataLoader: Prefab '{prefabId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets all prefabs valid for a specific floor depth.
    /// </summary>
    public List<PrefabData> GetPrefabsForFloor(int depth)
    {
        var result = new List<PrefabData>();
        foreach (var prefab in _prefabs.Values)
        {
            if (depth >= prefab.Placement.MinFloor && depth <= prefab.Placement.MaxFloor)
            {
                result.Add(prefab);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets all loaded prefab IDs.
    /// </summary>
    public IEnumerable<string> GetAllPrefabIds()
    {
        return _prefabs.Keys;
    }

    /// <summary>
    /// Gets skill data by ID (filename without extension).
    /// </summary>
    public SkillDefinition GetSkill(string skillId)
    {
        if (_skills.TryGetValue(skillId, out var skill))
        {
            return skill;
        }

        GD.PrintErr($"DataLoader: Skill '{skillId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets all loaded skill IDs.
    /// </summary>
    public IEnumerable<string> GetAllSkillIds()
    {
        return _skills.Keys;
    }

    /// <summary>
    /// Gets all loaded skill definitions.
    /// </summary>
    public IEnumerable<SkillDefinition> GetAllSkills()
    {
        return _skills.Values;
    }

    /// <summary>
    /// Gets faction theme by ID.
    /// </summary>
    public FactionTheme GetFactionTheme(string themeId)
    {
        if (_factionThemes.TryGetValue(themeId, out var theme))
        {
            return theme;
        }
        GD.PrintErr($"DataLoader: Faction theme '{themeId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets all loaded faction themes.
    /// </summary>
    public IEnumerable<FactionTheme> GetAllFactionThemes()
    {
        return _factionThemes.Values;
    }

    /// <summary>
    /// Gets all faction theme IDs.
    /// </summary>
    public IEnumerable<string> GetAllFactionThemeIds()
    {
        return _factionThemes.Keys;
    }

    /// <summary>
    /// Gets encounter template by ID.
    /// </summary>
    public EncounterTemplate GetEncounterTemplate(string templateId)
    {
        if (_encounterTemplates.TryGetValue(templateId, out var template))
        {
            return template;
        }
        GD.PrintErr($"DataLoader: Encounter template '{templateId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets all loaded encounter templates.
    /// </summary>
    public IEnumerable<EncounterTemplate> GetAllEncounterTemplates()
    {
        return _encounterTemplates.Values;
    }

    /// <summary>
    /// Gets floor spawn config by ID.
    /// </summary>
    public FloorSpawnConfig GetFloorSpawnConfig(string configId)
    {
        if (_floorSpawnConfigs.TryGetValue(configId, out var config))
        {
            return config;
        }
        GD.PrintErr($"DataLoader: Floor spawn config '{configId}' not found!");
        return null;
    }

    /// <summary>
    /// Gets floor spawn config for a specific floor depth.
    /// Returns the first config where MinFloor <= depth <= MaxFloor.
    /// </summary>
    public FloorSpawnConfig GetFloorSpawnConfigForDepth(int depth)
    {
        foreach (var config in _floorSpawnConfigs.Values)
        {
            if (depth >= config.MinFloor && depth <= config.MaxFloor)
            {
                return config;
            }
        }
        GD.PushWarning($"DataLoader: No floor spawn config found for depth {depth}");
        return null;
    }

    /// <summary>
    /// Gets all loaded floor spawn configs.
    /// </summary>
    public IEnumerable<FloorSpawnConfig> GetAllFloorSpawnConfigs()
    {
        return _floorSpawnConfigs.Values;
    }

    private void LoadAllCreatures()
    {
        _creatures.Clear();
        LoadSheetFiles<CreatureSheetData>(CreaturesPath, (sheet, fileName) =>
        {
            foreach (var (entryKey, creature) in sheet.Entries)
            {
                var id = entryKey.ToLower();

                // Apply sheet type to entry if not specified
                if (string.IsNullOrEmpty(creature.Type) && !string.IsNullOrEmpty(sheet.Type))
                {
                    creature.Type = sheet.Type;
                }

                // Apply sheet defaults
                ApplyCreatureDefaults(creature, sheet.Defaults);

                // Apply type-based defaults from code (fallback)
                creature.ApplyDefaults();

                if (_creatures.ContainsKey(id))
                {
                    GD.PushWarning($"DataLoader: Duplicate creature ID '{id}' from {fileName}, skipping");
                }
                else
                {
                    _creatures[id] = creature;
                }
            }
        });
        GD.Print($"DataLoader: Loaded {_creatures.Count} creatures");
    }

    private void ApplyCreatureDefaults(CreatureData creature, CreatureDefaults defaults)
    {
        if (defaults == null) return;

        // Only apply defaults where creature hasn't specified a value
        if (creature.Glyph == DataDefaults.UnknownGlyph && defaults.Glyph != null)
            creature.Glyph = defaults.Glyph;

        if (creature.Color == DataDefaults.DefaultColor && defaults.Color != null)
            creature.Color = defaults.Color;

        if (creature.Threat == 1 && defaults.Threat.HasValue)
            creature.Threat = defaults.Threat.Value;

        if (creature.VisionRange == 10 && defaults.VisionRange.HasValue)
            creature.VisionRange = defaults.VisionRange.Value;

        if (creature.Faction == "Hostile" && defaults.Faction != null)
            creature.Faction = defaults.Faction;

        // Stats default to 0, so we check HasValue
        if (creature.Strength == 0 && defaults.Strength.HasValue)
            creature.Strength = defaults.Strength.Value;
        if (creature.Agility == 0 && defaults.Agility.HasValue)
            creature.Agility = defaults.Agility.Value;
        if (creature.Endurance == 0 && defaults.Endurance.HasValue)
            creature.Endurance = defaults.Endurance.Value;
        if (creature.Will == 0 && defaults.Will.HasValue)
            creature.Will = defaults.Will.Value;

        // Boolean defaults
        if (creature.HasMovement && defaults.HasMovement.HasValue)
            creature.HasMovement = defaults.HasMovement.Value;
        if (creature.HasAI && defaults.HasAI.HasValue)
            creature.HasAI = defaults.HasAI.Value;
    }

    private void LoadAllItems()
    {
        _items.Clear();
        LoadSheetFiles<ItemSheetData>(ItemsPath, (sheet, fileName) =>
        {
            if (sheet.Entries == null || sheet.Entries.Count == 0)
            {
                GD.PushWarning($"DataLoader: Item sheet '{fileName}' has no entries");
                return;
            }

            foreach (var (entryKey, item) in sheet.Entries)
            {
                // Apply sheet type to entry if not specified
                if (string.IsNullOrEmpty(item.Type) && !string.IsNullOrEmpty(sheet.Type))
                {
                    item.Type = sheet.Type;
                }

                // Generate ID as {type}_{key}
                string id;
                if (!string.IsNullOrEmpty(item.Type))
                {
                    id = $"{item.Type.ToLower()}_{entryKey.ToLower()}";
                }
                else
                {
                    id = entryKey.ToLower();
                    GD.PushWarning($"DataLoader: Item '{entryKey}' in {fileName} has no type, using key as ID");
                }

                // Apply sheet defaults
                ApplyItemDefaults(item, sheet.Defaults);

                // Set data file ID for stacking
                item.DataFileId = id;

                // Apply type-based defaults from code (fallback)
                item.ApplyDefaults();

                if (_items.ContainsKey(id))
                {
                    GD.PushWarning($"DataLoader: Duplicate item ID '{id}' from {fileName}, skipping");
                }
                else
                {
                    _items[id] = item;
                    GD.Print($"DataLoader: Loaded item '{id}'");
                }
            }
        });
        GD.Print($"DataLoader: Loaded {_items.Count} items");
    }

    private void ApplyItemDefaults(ItemData item, ItemDefaults defaults)
    {
        if (defaults == null) return;

        // Only apply defaults where item hasn't specified a value
        if (item.Glyph == null && defaults.Glyph != null)
            item.Glyph = defaults.Glyph;

        if (item.Color == DataDefaults.DefaultColor && defaults.Color != null)
            item.Color = defaults.Color;

        if (!item.IsConsumable.HasValue && defaults.IsConsumable.HasValue)
            item.IsConsumable = defaults.IsConsumable.Value;

        if (!item.IsEquippable.HasValue && defaults.IsEquippable.HasValue)
            item.IsEquippable = defaults.IsEquippable.Value;

        if (item.EquipSlot == null && defaults.EquipSlot != null)
            item.EquipSlot = defaults.EquipSlot;

        if (!item.AutoPickup && defaults.AutoPickup.HasValue)
            item.AutoPickup = defaults.AutoPickup.Value;
    }

    private void LoadAllSpawnTables()
    {
        _spawnTables.Clear();
        LoadYamlFilesRecursive(SpawnTablesPath, "", _spawnTables, "spawn table");
        GD.Print($"DataLoader: Loaded {_spawnTables.Count} spawn tables");
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

    private void LoadAllFloorConfigs()
    {
        _floorConfigs.Clear();

        // Floors folder is optional
        if (!DirAccess.DirExistsAbsolute(FloorsPath))
        {
            GD.Print("DataLoader: No Floors directory found, floor configs not loaded");
            return;
        }

        LoadYamlFilesRecursive(FloorsPath, "", _floorConfigs, "floor config");
        GD.Print($"DataLoader: Loaded {_floorConfigs.Count} floor configs");
    }

    private void LoadAllPrefabs()
    {
        _prefabs.Clear();

        // Prefabs folder is optional
        if (!DirAccess.DirExistsAbsolute(PrefabsPath))
        {
            GD.Print("DataLoader: No Prefabs directory found, prefabs not loaded");
            return;
        }

        LoadYamlFilesRecursive(PrefabsPath, "", _prefabs, "prefab", (prefab, id) =>
        {
            // Set the name from the file ID if not specified
            if (string.IsNullOrEmpty(prefab.Name))
            {
                prefab.Name = id;
            }
        });

        // Register prefabs with the static accessor for generation passes
        PrefabLoader.SetPrefabs(_prefabs);

        GD.Print($"DataLoader: Loaded {_prefabs.Count} prefabs");
    }

    private void LoadAllSkills()
    {
        _skills.Clear();

        if (!DirAccess.DirExistsAbsolute(SkillsPath))
        {
            return;
        }

        LoadSheetFiles<SkillSheetData>(SkillsPath, (sheet, fileName) =>
        {
            foreach (var (entryKey, skill) in sheet.Entries)
            {
                var id = entryKey.ToLower();

                // Apply sheet defaults
                ApplySkillDefaults(skill, sheet.Defaults);

                // Set ID from entry key
                skill.Id = id;

                if (_skills.ContainsKey(id))
                {
                    GD.PushWarning($"DataLoader: Duplicate skill ID '{id}' from {fileName}, skipping");
                }
                else
                {
                    _skills[id] = skill;
                }
            }
        });

        GD.Print($"DataLoader: Loaded {_skills.Count} skills");
    }

    private void ApplySkillDefaults(SkillDefinition skill, SkillDefaults defaults)
    {
        if (defaults == null) return;

        if (skill.Category == "active" && defaults.Category != null)
            skill.Category = defaults.Category;

        if (skill.Targeting == "self" && defaults.Targeting != null)
            skill.Targeting = defaults.Targeting;

        if (skill.Tier == 1 && defaults.Tier.HasValue)
            skill.Tier = defaults.Tier.Value;

        if (skill.WillpowerCost == 0 && defaults.WillpowerCost.HasValue)
            skill.WillpowerCost = defaults.WillpowerCost.Value;

        if (skill.Range == 0 && defaults.Range.HasValue)
            skill.Range = defaults.Range.Value;
    }

    private void LoadAllFactionThemes()
    {
        _factionThemes.Clear();

        if (!DirAccess.DirExistsAbsolute(ThemesPath))
        {
            GD.Print("DataLoader: No Themes directory found, faction themes not loaded");
            return;
        }

        LoadYamlFilesRecursive(ThemesPath, "", _factionThemes, "faction theme", (theme, id) =>
        {
            if (string.IsNullOrEmpty(theme.Id))
            {
                theme.Id = id;
            }
            if (string.IsNullOrEmpty(theme.Name))
            {
                theme.Name = id;
            }
        });

        GD.Print($"DataLoader: Loaded {_factionThemes.Count} faction themes");
    }

    private void LoadAllEncounterTemplates()
    {
        _encounterTemplates.Clear();

        if (!DirAccess.DirExistsAbsolute(EncountersPath))
        {
            GD.Print("DataLoader: No Encounters directory found, encounter templates not loaded");
            return;
        }

        LoadYamlFilesRecursive(EncountersPath, "", _encounterTemplates, "encounter template", (template, id) =>
        {
            if (string.IsNullOrEmpty(template.Id))
            {
                template.Id = id;
            }
            if (string.IsNullOrEmpty(template.Name))
            {
                template.Name = id;
            }
        });

        GD.Print($"DataLoader: Loaded {_encounterTemplates.Count} encounter templates");
    }

    private void LoadAllFloorSpawnConfigs()
    {
        _floorSpawnConfigs.Clear();

        if (!DirAccess.DirExistsAbsolute(SpawnConfigsPath))
        {
            GD.Print("DataLoader: No SpawnConfigs directory found, floor spawn configs not loaded");
            return;
        }

        LoadYamlFilesRecursive(SpawnConfigsPath, "", _floorSpawnConfigs, "floor spawn config", (config, id) =>
        {
            if (string.IsNullOrEmpty(config.Id))
            {
                config.Id = id;
            }
            if (string.IsNullOrEmpty(config.Name))
            {
                config.Name = id;
            }
        });

        GD.Print($"DataLoader: Loaded {_floorSpawnConfigs.Count} floor spawn configs");
    }

    /// <summary>
    /// Loads all sheet YAML files from a directory (non-recursive, flat structure).
    /// Each file is expected to contain a sheet with type, defaults, and entries.
    /// </summary>
    private void LoadSheetFiles<T>(string basePath, Action<T, string> sheetHandler) where T : class
    {
        if (!DirAccess.DirExistsAbsolute(basePath))
        {
            GD.PushWarning($"DataLoader: Directory not found: {basePath}");
            return;
        }

        var dir = DirAccess.Open(basePath);
        if (dir == null)
        {
            GD.PrintErr($"DataLoader: Failed to open directory: {basePath}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        int filesProcessed = 0;

        while (fileName != string.Empty)
        {
            // Skip hidden files, templates, and directories
            if (fileName.StartsWith('.') || fileName.StartsWith('_') || dir.CurrentIsDir())
            {
                fileName = dir.GetNext();
                continue;
            }

            if (fileName.EndsWith(".yaml"))
            {
                string filePath = basePath + fileName;
                var sheet = LoadYamlFile<T>(filePath);
                if (sheet != null)
                {
                    try
                    {
                        sheetHandler(sheet, fileName);
                        filesProcessed++;
                    }
                    catch (System.Exception ex)
                    {
                        GD.PrintErr($"DataLoader: Error processing sheet '{fileName}': {ex.Message}");
                    }
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();

        if (filesProcessed == 0)
        {
            GD.PushWarning($"DataLoader: No sheet files processed in {basePath}");
        }
    }

    /// <summary>
    /// Recursively loads YAML files from a directory and its subdirectories.
    /// Converts folder paths to IDs (e.g., "Goblins/warrior.yaml" -> "goblins_warrior").
    /// Used for spawn tables and bands which still use the old format.
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
            // Skip hidden files/folders and templates
            if (fileName.StartsWith('.') || fileName.StartsWith('_'))
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

    private const string PalettePrefix = "Palette.";

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        // If it's already a hex color (starts with #), return as-is
        if (value.StartsWith('#'))
        {
            return value;
        }

        // Only convert strings that explicitly request palette colors via "Palette." prefix
        if (value.StartsWith(PalettePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var colorName = value.Substring(PalettePrefix.Length).ToLower();
            if (_colorCache.TryGetValue(colorName, out var hexValue))
            {
                return hexValue;
            }
            GD.PushWarning($"PaletteColorConverter: Unknown palette color '{colorName}', using as-is");
        }

        // Return non-prefixed strings unchanged
        return value;
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar((string)value));
    }
}
