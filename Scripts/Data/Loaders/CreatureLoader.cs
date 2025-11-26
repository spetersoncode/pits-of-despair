using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to creature data from YAML sheet files.
/// </summary>
public class CreatureLoader
{
    private const string DataPath = "res://Data/Creatures/";

    private readonly Dictionary<string, CreatureData> _data = new();

    public void Load()
    {
        _data.Clear();

        SheetLoader.LoadSheets<CreatureSheetData>(DataPath, ProcessSheet);
        GD.Print($"[CreatureLoader] Loaded {_data.Count} creatures");
    }

    private void ProcessSheet(CreatureSheetData sheet, string fileName)
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
            ApplyDefaults(creature, sheet.Defaults);

            // Apply type-based defaults from code (fallback)
            creature.ApplyDefaults();

            // Validate critical fields
            if (creature.Glyph == DataDefaults.UnknownGlyph)
            {
                GD.PushWarning($"[CreatureLoader] Creature '{id}' has no glyph defined");
            }
            if (string.IsNullOrEmpty(creature.Name))
            {
                GD.PushWarning($"[CreatureLoader] Creature '{id}' has no name defined");
            }

            if (_data.ContainsKey(id))
            {
                GD.PushWarning($"[CreatureLoader] Duplicate creature ID '{id}' from {fileName}, skipping");
            }
            else
            {
                _data[id] = creature;
            }
        }
    }

    private void ApplyDefaults(CreatureData creature, CreatureDefaults defaults)
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

    // === Public Accessors ===

    public CreatureData Get(string id)
    {
        if (_data.TryGetValue(id, out var creature))
        {
            return creature;
        }
        GD.PrintErr($"[CreatureLoader] Creature '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out CreatureData creature)
    {
        return _data.TryGetValue(id, out creature);
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<CreatureData> GetAll() => _data.Values;

    public int Count => _data.Count;

    /// <summary>
    /// Check if a creature ID exists. Used for validation by other loaders.
    /// </summary>
    public bool Contains(string id) => _data.ContainsKey(id);
}
