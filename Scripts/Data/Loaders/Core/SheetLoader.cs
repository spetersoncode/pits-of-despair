using Godot;
using System;

namespace PitsOfDespair.Data.Loaders.Core;

/// <summary>
/// Loads sheet-format YAML files from a flat directory.
/// Sheet format contains type, defaults, and entries in a single file.
/// </summary>
public static class SheetLoader
{
    /// <summary>
    /// Loads all sheet YAML files from a directory (non-recursive, flat structure).
    /// Each file is expected to contain a sheet with type, defaults, and entries.
    /// </summary>
    /// <typeparam name="T">The sheet data type to deserialize.</typeparam>
    /// <param name="basePath">The directory path to load from.</param>
    /// <param name="sheetHandler">Handler called for each successfully loaded sheet.</param>
    public static void LoadSheets<T>(string basePath, Action<T, string> sheetHandler) where T : class
    {
        if (!DirAccess.DirExistsAbsolute(basePath))
        {
            GD.PushWarning($"[SheetLoader] Directory not found: {basePath}");
            return;
        }

        var dir = DirAccess.Open(basePath);
        if (dir == null)
        {
            GD.PrintErr($"[SheetLoader] Failed to open directory: {basePath}");
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
                var sheet = YamlLoader.LoadFile<T>(filePath);
                if (sheet != null)
                {
                    try
                    {
                        sheetHandler(sheet, fileName);
                        filesProcessed++;
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"[SheetLoader] Error processing sheet '{fileName}': {ex.Message}");
                    }
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();

        if (filesProcessed == 0)
        {
            GD.PushWarning($"[SheetLoader] No sheet files processed in {basePath}");
        }
    }
}
