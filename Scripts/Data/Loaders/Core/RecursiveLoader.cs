using Godot;
using System;
using System.Collections.Generic;

namespace PitsOfDespair.Data.Loaders.Core;

/// <summary>
/// Recursively loads YAML files from a directory and its subdirectories.
/// Converts folder paths to IDs (e.g., "Goblins/warrior.yaml" -> "warrior").
/// </summary>
public static class RecursiveLoader
{
    /// <summary>
    /// Recursively loads YAML files from a directory.
    /// </summary>
    /// <typeparam name="T">The data type to deserialize.</typeparam>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="targetDictionary">Dictionary to store loaded data.</param>
    /// <param name="fileTypeName">Display name for logging (e.g., "floor config").</param>
    /// <param name="postLoadAction">Optional action to run after loading each file.</param>
    public static void LoadFiles<T>(
        string basePath,
        Dictionary<string, T> targetDictionary,
        string fileTypeName,
        Action<T, string> postLoadAction = null) where T : class
    {
        LoadFilesRecursive(basePath, "", targetDictionary, fileTypeName, postLoadAction);
    }

    private static void LoadFilesRecursive<T>(
        string basePath,
        string currentRelativePath,
        Dictionary<string, T> targetDictionary,
        string fileTypeName,
        Action<T, string> postLoadAction) where T : class
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

                LoadFilesRecursive(basePath, newRelativePath, targetDictionary, fileTypeName, postLoadAction);
            }
            else if (fileName.EndsWith(".yaml"))
            {
                // Convert path to ID
                string relativePath = string.IsNullOrEmpty(currentRelativePath)
                    ? fileName
                    : currentRelativePath + "/" + fileName;

                string id = PathConverter.ConvertToId(relativePath);
                string filePath = fullPath + fileName;

                var data = YamlLoader.LoadFile<T>(filePath);
                if (data != null)
                {
                    if (targetDictionary.ContainsKey(id))
                    {
                        GD.PushWarning($"[RecursiveLoader] Duplicate {fileTypeName} ID '{id}' from {relativePath}, skipping");
                    }
                    else
                    {
                        // Call post-load action if provided
                        postLoadAction?.Invoke(data, id);
                        targetDictionary[id] = data;
                    }
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }
}
