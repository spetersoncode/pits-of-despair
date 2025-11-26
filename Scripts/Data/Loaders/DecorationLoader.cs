using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to decorations and decoration sets from YAML files.
/// </summary>
public class DecorationLoader
{
    private const string DataPath = "res://Data/Decorations/";

    private readonly Dictionary<string, DecorationData> _decorations = new();
    private readonly Dictionary<string, DecorationSet> _sets = new();

    public void Load()
    {
        _decorations.Clear();
        _sets.Clear();

        if (!DirAccess.DirExistsAbsolute(DataPath))
        {
            GD.Print("[DecorationLoader] No Decorations directory found, decorations not loaded");
            return;
        }

        RecursiveLoader.LoadFiles(DataPath, _sets, "decoration set", (set, id) =>
        {
            if (string.IsNullOrEmpty(set.Id))
            {
                set.Id = id;
            }

            // Process entries from the set and add to global decoration dictionary
            if (set.Entries != null)
            {
                foreach (var (entryKey, decoration) in set.Entries)
                {
                    var decorationId = entryKey.ToLower();

                    // Set ID if not specified
                    if (string.IsNullOrEmpty(decoration.Id))
                    {
                        decoration.Id = decorationId;
                    }

                    // Set theme from parent set if not specified
                    if (string.IsNullOrEmpty(decoration.ThemeId) && !string.IsNullOrEmpty(set.ThemeId))
                    {
                        decoration.ThemeId = set.ThemeId;
                    }

                    if (_decorations.ContainsKey(decorationId))
                    {
                        GD.PushWarning($"[DecorationLoader] Duplicate decoration ID '{decorationId}', skipping");
                    }
                    else
                    {
                        _decorations[decorationId] = decoration;
                    }
                }
            }
        });

        GD.Print($"[DecorationLoader] Loaded {_decorations.Count} decorations in {_sets.Count} sets");
    }

    // === Decoration Accessors ===

    public DecorationData Get(string id)
    {
        if (_decorations.TryGetValue(id, out var decoration))
        {
            return decoration;
        }
        GD.PushWarning($"[DecorationLoader] Decoration '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out DecorationData decoration)
    {
        return _decorations.TryGetValue(id, out decoration);
    }

    public IEnumerable<string> GetAllIds() => _decorations.Keys;

    public IEnumerable<DecorationData> GetAll() => _decorations.Values;

    public int Count => _decorations.Count;

    // === Set Accessors ===

    /// <summary>
    /// Gets decoration set by theme ID.
    /// Pass null to get the generic decoration set.
    /// </summary>
    public DecorationSet GetSet(string themeId)
    {
        foreach (var set in _sets.Values)
        {
            // Both null = generic set match
            // Both same string = themed set match
            if (set.ThemeId == themeId)
            {
                return set;
            }
        }
        return null;
    }

    public IEnumerable<DecorationSet> GetAllSets() => _sets.Values;

    public int SetCount => _sets.Count;
}
