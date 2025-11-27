using System;
using System.Collections.Generic;
using PitsOfDespair.Generation.Pipeline;

namespace PitsOfDespair.Generation.Passes.Config;

/// <summary>
/// Configuration for a single generation pass in the pipeline.
/// Maps directly from YAML floor configuration.
/// </summary>
public class PassConfig
{
    /// <summary>
    /// Name of the pass type (e.g., "bsp", "cellular_automata", "connectivity").
    /// Used to look up the factory function.
    /// </summary>
    public string Pass { get; set; }

    /// <summary>
    /// Execution priority. Lower values execute first.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Pass-specific configuration parameters.
    /// Structure varies by pass type.
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Extract the role from config, defaulting to Base for most passes.
    /// Role can be overridden in config for dual-mode passes like cellular_automata.
    /// </summary>
    public PassRole GetRole()
    {
        if (Config.TryGetValue("role", out var roleValue) && roleValue != null)
        {
            if (Enum.TryParse<PassRole>(roleValue.ToString(), ignoreCase: true, out var role))
                return role;
        }

        // Default roles based on pass name
        var passName = Pass?.ToLowerInvariant() ?? "";
        return passName switch
        {
            "validation" => PassRole.PostProcess,
            "connectivity" => PassRole.PostProcess,
            "metadata" => PassRole.PostProcess,
            "prefabs" => PassRole.Modifier,
            _ => PassRole.Base
        };
    }

    /// <summary>
    /// Get a typed config value with fallback.
    /// </summary>
    public T GetConfigValue<T>(string key, T defaultValue = default)
    {
        if (Config.TryGetValue(key, out var value) && value != null)
        {
            try
            {
                if (value is T typedValue)
                    return typedValue;

                // Handle numeric conversions
                if (typeof(T) == typeof(int) && value is long longVal)
                    return (T)(object)(int)longVal;
                if (typeof(T) == typeof(float) && value is double doubleVal)
                    return (T)(object)(float)doubleVal;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}
