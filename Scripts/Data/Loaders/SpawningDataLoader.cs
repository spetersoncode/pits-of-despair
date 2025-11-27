using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;
using PitsOfDespair.Systems.Spawning.Data;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to spawning-related data:
/// - Faction themes
/// - Encounter templates
/// Note: Floor spawn configs are now handled via Pipeline + Floor configs.
/// </summary>
public class SpawningDataLoader
{
    private const string ThemesPath = "res://Data/Themes/";
    private const string EncountersPath = "res://Data/Encounters/";

    private readonly Dictionary<string, FactionTheme> _factionThemes = new();
    private readonly Dictionary<string, EncounterTemplate> _encounterTemplates = new();

    public void Load()
    {
        LoadFactionThemes();
        LoadEncounterTemplates();
    }

    private void LoadFactionThemes()
    {
        _factionThemes.Clear();

        if (!DirAccess.DirExistsAbsolute(ThemesPath))
        {
            GD.Print("[SpawningDataLoader] No Themes directory found, faction themes not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(ThemesPath, _factionThemes, "faction theme", (theme, id) =>
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

        GD.Print($"[SpawningDataLoader] Loaded {_factionThemes.Count} faction themes");
    }

    private void LoadEncounterTemplates()
    {
        _encounterTemplates.Clear();

        if (!DirAccess.DirExistsAbsolute(EncountersPath))
        {
            GD.Print("[SpawningDataLoader] No Encounters directory found, encounter templates not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(EncountersPath, _encounterTemplates, "encounter template", (template, id) =>
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

        GD.Print($"[SpawningDataLoader] Loaded {_encounterTemplates.Count} encounter templates");
    }


    /// <summary>
    /// Validates that all creature IDs in faction themes reference existing creatures.
    /// Should be called after creatures are loaded.
    /// </summary>
    public void ValidateFactionThemes(CreatureLoader creatures)
    {
        foreach (var (themeId, theme) in _factionThemes)
        {
            if (theme.Creatures == null || theme.Creatures.Count == 0)
            {
                GD.PushWarning($"[SpawningDataLoader] Theme '{themeId}' has no creatures defined");
                continue;
            }

            var invalidCreatures = new List<string>();
            foreach (var creatureId in theme.Creatures)
            {
                if (!creatures.Contains(creatureId))
                {
                    invalidCreatures.Add(creatureId);
                }
            }

            if (invalidCreatures.Count > 0)
            {
                GD.PushWarning($"[SpawningDataLoader] Theme '{themeId}' references unknown creatures: {string.Join(", ", invalidCreatures)}");
            }
        }
    }

    // === Faction Theme Accessors ===

    public FactionTheme GetFactionTheme(string id)
    {
        if (_factionThemes.TryGetValue(id, out var theme))
        {
            return theme;
        }
        GD.PrintErr($"[SpawningDataLoader] Faction theme '{id}' not found!");
        return null;
    }

    public bool TryGetFactionTheme(string id, out FactionTheme theme)
    {
        return _factionThemes.TryGetValue(id, out theme);
    }

    public IEnumerable<string> GetAllFactionThemeIds() => _factionThemes.Keys;

    public IEnumerable<FactionTheme> GetAllFactionThemes() => _factionThemes.Values;

    public int FactionThemeCount => _factionThemes.Count;

    // === Encounter Template Accessors ===

    public EncounterTemplate GetEncounterTemplate(string id)
    {
        if (_encounterTemplates.TryGetValue(id, out var template))
        {
            return template;
        }
        GD.PrintErr($"[SpawningDataLoader] Encounter template '{id}' not found!");
        return null;
    }

    public bool TryGetEncounterTemplate(string id, out EncounterTemplate template)
    {
        return _encounterTemplates.TryGetValue(id, out template);
    }

    public IEnumerable<string> GetAllEncounterTemplateIds() => _encounterTemplates.Keys;

    public IEnumerable<EncounterTemplate> GetAllEncounterTemplates() => _encounterTemplates.Values;

    public int EncounterTemplateCount => _encounterTemplates.Count;
}
