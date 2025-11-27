using Godot;
using PitsOfDespair.Entities;

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
        public const string MeleeAttack = "Player/melee_attack.wav";
    }

    private LevelUpSystem? _levelUpSystem;
    private CombatSystem? _combatSystem;

    public void Initialize(LevelUpSystem levelUpSystem, CombatSystem combatSystem)
    {
        _levelUpSystem = levelUpSystem;
        _combatSystem = combatSystem;

        _levelUpSystem.Connect(
            LevelUpSystem.SignalName.LevelUpMessage,
            Callable.From<int>(OnLevelUpMessage)
        );

        _combatSystem.Connect(
            CombatSystem.SignalName.AttackHit,
            Callable.From<BaseEntity, BaseEntity, int, string>(OnAttackHit)
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

        if (_combatSystem != null && IsInstanceValid(_combatSystem))
        {
            _combatSystem.Disconnect(
                CombatSystem.SignalName.AttackHit,
                Callable.From<BaseEntity, BaseEntity, int, string>(OnAttackHit)
            );
        }
    }

    private void OnLevelUpMessage(int newLevel)
    {
        AudioManager.PlaySystemSound(SystemSounds.LevelUp);
    }

    private void OnAttackHit(BaseEntity attacker, BaseEntity target, int damage, string attackName)
    {
        // Only play sound for player attacks
        if (attacker is Player)
        {
            AudioManager.PlaySystemSound(SystemSounds.MeleeAttack);
        }
    }
}
