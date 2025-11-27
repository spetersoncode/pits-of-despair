using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.Systems.Audio;

/// <summary>
/// Central audio management system with player pooling and volume control.
/// Provides static access for easy sound playback from anywhere in the codebase.
/// </summary>
public partial class AudioManager : Node
{
    private const int PoolSize = 8;
    private const string SystemSoundsBasePath = "res://Resources/Audio/SoundEffects/";

    private static AudioManager? _instance;

    private readonly List<AudioStreamPlayer> _playerPool = new();
    private readonly Dictionary<string, AudioStream> _audioCache = new();
    private readonly AudioRegistry _registry = new();

    private float _masterVolume = 1.0f;
    private float _sfxVolume = 1.0f;

    public float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = Mathf.Clamp(value, 0f, 1f);
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Mathf.Clamp(value, 0f, 1f);
    }

    public override void _Ready()
    {
        _instance = this;

        _registry.Load();

        for (int i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer { Name = $"AudioPlayer_{i}" };
            AddChild(player);
            _playerPool.Add(player);
        }

        GD.Print("[AudioManager] Initialized with pool of " + PoolSize + " players");
    }

    public override void _ExitTree()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Plays a sound from a direct resource path.
    /// </summary>
    /// <param name="path">Full resource path to the audio file</param>
    public static void Play(string path)
    {
        _instance?.PlayInternal(path);
    }

    /// <summary>
    /// Plays the sound associated with an effect type.
    /// </summary>
    /// <param name="effectType">The effect type ID (e.g., "fireball")</param>
    public static void PlayEffectSound(string effectType)
    {
        if (_instance == null)
            return;

        var path = _instance._registry.GetEffectSound(effectType);
        if (path != null)
        {
            _instance.PlayInternal(path);
        }
    }

    /// <summary>
    /// Plays a system sound by relative path.
    /// </summary>
    /// <param name="relativePath">Path relative to Resources/Audio/SoundEffects/</param>
    public static void PlaySystemSound(string relativePath)
    {
        _instance?.PlayInternal(SystemSoundsBasePath + relativePath);
    }

    private void PlayInternal(string path)
    {
        var stream = LoadAudio(path);
        if (stream == null)
            return;

        var player = GetAvailablePlayer();
        if (player == null)
        {
            GD.PrintErr("[AudioManager] No available audio players in pool");
            return;
        }

        player.Stream = stream;
        player.VolumeDb = Mathf.LinearToDb(_masterVolume * _sfxVolume);
        player.Play();
    }

    private AudioStream? LoadAudio(string path)
    {
        if (_audioCache.TryGetValue(path, out var cached))
            return cached;

        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"[AudioManager] Audio file not found: {path}");
            return null;
        }

        var stream = GD.Load<AudioStream>(path);
        if (stream == null)
        {
            GD.PrintErr($"[AudioManager] Failed to load audio: {path}");
            return null;
        }

        _audioCache[path] = stream;
        return stream;
    }

    private AudioStreamPlayer? GetAvailablePlayer()
    {
        foreach (var player in _playerPool)
        {
            if (!player.Playing)
                return player;
        }

        // All players busy, return the first one (will interrupt oldest sound)
        return _playerPool.Count > 0 ? _playerPool[0] : null;
    }
}
