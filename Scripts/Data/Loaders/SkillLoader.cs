using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to skill definitions from YAML sheet files.
/// </summary>
public class SkillLoader
{
    private const string DataPath = "res://Data/Skills/";

    private readonly Dictionary<string, SkillDefinition> _data = new();

    public void Load()
    {
        _data.Clear();

        if (!DirAccess.DirExistsAbsolute(DataPath))
        {
            return;
        }

        SheetLoader.LoadSheets<SkillSheetData>(DataPath, ProcessSheet);
        GD.Print($"[SkillLoader] Loaded {_data.Count} skills");
    }

    private void ProcessSheet(SkillSheetData sheet, string fileName)
    {
        foreach (var (entryKey, skill) in sheet.Entries)
        {
            var id = entryKey.ToLower();

            // Apply sheet defaults
            ApplyDefaults(skill, sheet.Defaults);

            // Set ID from entry key
            skill.Id = id;

            if (_data.ContainsKey(id))
            {
                GD.PushWarning($"[SkillLoader] Duplicate skill ID '{id}' from {fileName}, skipping");
            }
            else
            {
                _data[id] = skill;
            }
        }
    }

    private void ApplyDefaults(SkillDefinition skill, SkillDefaults defaults)
    {
        if (defaults == null) return;

        if (skill.Category == "active" && defaults.Category != null)
            skill.Category = defaults.Category;

        if (skill.Targeting == "self" && defaults.Targeting != null)
            skill.Targeting = defaults.Targeting;

        if (skill.WillpowerCost == 0 && defaults.WillpowerCost.HasValue)
            skill.WillpowerCost = defaults.WillpowerCost.Value;

        if (skill.Range == 0 && defaults.Range.HasValue)
            skill.Range = defaults.Range.Value;
    }

    // === Public Accessors ===

    public SkillDefinition Get(string id)
    {
        if (_data.TryGetValue(id, out var skill))
        {
            return skill;
        }
        GD.PrintErr($"[SkillLoader] Skill '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out SkillDefinition skill)
    {
        return _data.TryGetValue(id, out skill);
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<SkillDefinition> GetAll() => _data.Values;

    public int Count => _data.Count;
}
