using Godot;

namespace PitsOfDespair.Systems.Audio;

/// <summary>
/// Handles audio for system events by subscribing to game signals.
/// Plays sounds for fixed events like level-ups, floor changes, etc.
/// </summary>
public partial class SystemAudioHandler : Node
{
    private static class SystemSounds
    {
        public const string LevelUp = "Player/level_up.wav";
    }

    private LevelUpSystem? _levelUpSystem;

    public void Initialize(LevelUpSystem levelUpSystem)
    {
        _levelUpSystem = levelUpSystem;

        _levelUpSystem.Connect(
            LevelUpSystem.SignalName.LevelUpMessage,
            Callable.From<int>(OnLevelUpMessage)
        );
    }

    public override void _ExitTree()
    {
        if (_levelUpSystem != null && IsInstanceValid(_levelUpSystem))
        {
            _levelUpSystem.Disconnect(
                LevelUpSystem.SignalName.LevelUpMessage,
                Callable.From<int>(OnLevelUpMessage)
            );
        }
    }

    private void OnLevelUpMessage(int newLevel)
    {
        AudioManager.PlaySystemSound(SystemSounds.LevelUp);
    }
}
