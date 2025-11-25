using System;
using System.Collections.Generic;
using PitsOfDespair.Generation.Config;
using PitsOfDespair.Generation.Passes;

namespace PitsOfDespair.Generation.Pipeline;

/// <summary>
/// Factory and registry for generation passes.
/// Built-in passes are registered at startup; custom passes can be registered dynamically.
/// </summary>
public static class GenerationPassFactory
{
    private static readonly Dictionary<string, Func<PassConfig, IGenerationPass>> _factories = new();
    private static bool _initialized = false;

    /// <summary>
    /// Initialize built-in pass factories.
    /// Called automatically on first use.
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        // Built-in base generators
        Register("bsp", cfg => new BSPGenerationPass(cfg));
        Register("cellular_automata", cfg => new CellularAutomataPass(cfg));
        Register("drunkard_walk", cfg => new DrunkardWalkPass(cfg));
        Register("simple_rooms", cfg => new SimpleRoomPlacementPass(cfg));

        // Built-in post-processors
        Register("prefabs", cfg => new PrefabInsertionPass(cfg));
        Register("validation", cfg => new ValidationPass(cfg));
        Register("connectivity", cfg => new ConnectivityPass(cfg));
        Register("metadata", cfg => new MetadataAnalysisPass(cfg));
    }

    /// <summary>
    /// Register a custom generation pass factory.
    /// Call during initialization to add project-specific algorithms.
    /// </summary>
    /// <param name="name">Pass name (case-insensitive).</param>
    /// <param name="factory">Factory function that creates the pass from config.</param>
    public static void Register(string name, Func<PassConfig, IGenerationPass> factory)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _factories[name.ToLowerInvariant()] = factory;
    }

    /// <summary>
    /// Create a generation pass from configuration.
    /// </summary>
    /// <param name="config">Pass configuration from YAML.</param>
    /// <returns>Configured generation pass instance.</returns>
    public static IGenerationPass Create(PassConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (string.IsNullOrEmpty(config.Pass))
            throw new ArgumentException("PassConfig.Pass cannot be null or empty", nameof(config));

        EnsureInitialized();

        var name = config.Pass.ToLowerInvariant();
        if (!_factories.TryGetValue(name, out var factory))
        {
            var available = string.Join(", ", _factories.Keys);
            throw new ArgumentException(
                $"Unknown generation pass: '{config.Pass}'. " +
                $"Available passes: {(available.Length > 0 ? available : "(none registered)")}");
        }

        return factory(config);
    }

    /// <summary>
    /// Check if a pass type is registered.
    /// </summary>
    /// <param name="name">Pass name (case-insensitive).</param>
    public static bool IsRegistered(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        EnsureInitialized();
        return _factories.ContainsKey(name.ToLowerInvariant());
    }

    /// <summary>
    /// Get all registered pass names.
    /// </summary>
    public static IEnumerable<string> GetRegisteredPasses()
    {
        EnsureInitialized();
        return _factories.Keys;
    }

    /// <summary>
    /// Unregister a pass (primarily for testing).
    /// </summary>
    public static bool Unregister(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return _factories.Remove(name.ToLowerInvariant());
    }

    /// <summary>
    /// Clear all registrations and reset initialization state.
    /// Primarily for testing.
    /// </summary>
    public static void Reset()
    {
        _factories.Clear();
        _initialized = false;
    }
}
