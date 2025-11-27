using Godot;
using System.Collections.Generic;
using PitsOfDespair.Data.Loaders.Core;

namespace PitsOfDespair.Systems.Audio;

/// <summary>
/// YAML data structure for effect_sounds.yaml.
/// </summary>
public class EffectSoundsData
{
    public Dictionary<string, string> Effects { get; set; } = new();
}

/// <summary>
/// Loads and provides access to effect-to-sound mappings from YAML.
/// </summary>
public class AudioRegistry
{
    private const string EffectSoundsPath = "res://Data/Audio/effect_sounds.yaml";
    private const string SoundEffectsBasePath = "res://Resources/Audio/SoundEffects/";

    private readonly Dictionary<string, string> _effectSounds = new();

    public void Load()
    {
        _effectSounds.Clear();

        var data = YamlLoader.LoadFile<EffectSoundsData>(EffectSoundsPath);
        if (data?.Effects == null)
        {
            GD.PrintErr("[AudioRegistry] Failed to load effect sounds from YAML");
            return;
        }

        foreach (var (effectType, relativePath) in data.Effects)
        {
            var fullPath = SoundEffectsBasePath + relativePath;
            _effectSounds[effectType.ToLower()] = fullPath;
        }

        GD.Print($"[AudioRegistry] Loaded {_effectSounds.Count} effect sounds");
    }

    /// <summary>
    /// Gets the sound path for an effect type.
    /// </summary>
    /// <param name="effectType">The effect type ID (e.g., "fireball")</param>
    /// <returns>The full resource path to the sound file, or null if not found</returns>
    public string? GetEffectSound(string effectType)
    {
        if (string.IsNullOrEmpty(effectType))
            return null;

        return _effectSounds.TryGetValue(effectType.ToLower(), out var path) ? path : null;
    }

    public int Count => _effectSounds.Count;
}
