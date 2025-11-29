using Godot;
using System;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;

namespace PitsOfDespair.Data.Loaders;

/// <summary>
/// Loads and provides access to item data from YAML sheet files.
/// </summary>
public class ItemLoader
{
    private const string DataPath = "res://Data/Items/";

    private readonly Dictionary<string, ItemData> _data = new();

    public void Load()
    {
        _data.Clear();

        SheetLoader.LoadSheets<ItemSheetData>(DataPath, ProcessSheet);
        GD.Print($"[ItemLoader] Loaded {_data.Count} items");
    }

    private void ProcessSheet(ItemSheetData sheet, string fileName)
    {
        if (sheet.Entries == null || sheet.Entries.Count == 0)
        {
            GD.PushWarning($"[ItemLoader] Item sheet '{fileName}' has no entries");
            return;
        }

        foreach (var (entryKey, item) in sheet.Entries)
        {
            // Apply sheet type to entry if not specified
            if (string.IsNullOrEmpty(item.Type) && !string.IsNullOrEmpty(sheet.Type))
            {
                item.Type = sheet.Type;
            }

            // Apply sheet category (for weapons)
            if (!string.IsNullOrEmpty(sheet.Category))
            {
                item.Category = sheet.Category;
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
                GD.PushWarning($"[ItemLoader] Item '{entryKey}' in {fileName} has no type, using key as ID");
            }

            // Apply sheet defaults
            ApplyDefaults(item, sheet.Defaults);

            // Set data file ID for stacking
            item.DataFileId = id;

            // Apply type-based defaults from code (fallback)
            item.ApplyDefaults();

            // Validate critical fields
            if (string.IsNullOrEmpty(item.Glyph))
            {
                GD.PushWarning($"[ItemLoader] Item '{id}' has no glyph defined");
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                GD.PushWarning($"[ItemLoader] Item '{id}' has no name defined");
            }

            if (_data.ContainsKey(id))
            {
                GD.PushWarning($"[ItemLoader] Duplicate item ID '{id}' from {fileName}, skipping");
            }
            else
            {
                _data[id] = item;
            }
        }
    }

    private void ApplyDefaults(ItemData item, ItemDefaults defaults)
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

        if (!item.SpawnWeight.HasValue && defaults.SpawnWeight.HasValue)
            item.SpawnWeight = defaults.SpawnWeight.Value;

        // Apply attack defaults if item has an attack
        if (item.Attack != null && defaults.Attack != null)
        {
            // Apply damage type if not explicitly set (check against Bludgeoning default)
            if (item.Attack.DamageType == DamageType.Bludgeoning && defaults.Attack.DamageType != null)
            {
                if (Enum.TryParse<DamageType>(defaults.Attack.DamageType, out var dt))
                    item.Attack.DamageType = dt;
            }

            // Apply delay if using default (1.0)
            if (item.Attack.Delay == 1.0f && defaults.Attack.Delay.HasValue)
                item.Attack.Delay = defaults.Attack.Delay.Value;
        }
    }

    // === Public Accessors ===

    public ItemData Get(string id)
    {
        if (_data.TryGetValue(id, out var item))
        {
            return item;
        }
        GD.PrintErr($"[ItemLoader] Item '{id}' not found!");
        return null;
    }

    public bool TryGet(string id, out ItemData item)
    {
        return _data.TryGetValue(id, out item);
    }

    public IEnumerable<string> GetAllIds() => _data.Keys;

    public IEnumerable<ItemData> GetAll() => _data.Values;

    public int Count => _data.Count;
}
