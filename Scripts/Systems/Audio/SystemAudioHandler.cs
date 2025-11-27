using Godot;
using PitsOfDespair.Data;
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
        public const string RangedAttack = "Player/ranged_attack.wav";
        public const string AttackMiss = "Player/attack_miss.wav";
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
            Callable.From<BaseEntity, BaseEntity, int, string, AttackType>(OnAttackHit)
        );

        _combatSystem.Connect(
            CombatSystem.SignalName.AttackMissed,
            Callable.From<BaseEntity, BaseEntity, string>(OnAttackMissed)
        );

        _combatSystem.Connect(
            CombatSystem.SignalName.AttackBlocked,
            Callable.From<BaseEntity, BaseEntity, string>(OnAttackBlocked)
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
                Callable.From<BaseEntity, BaseEntity, int, string, AttackType>(OnAttackHit)
            );

            _combatSystem.Disconnect(
                CombatSystem.SignalName.AttackMissed,
                Callable.From<BaseEntity, BaseEntity, string>(OnAttackMissed)
            );

            _combatSystem.Disconnect(
                CombatSystem.SignalName.AttackBlocked,
                Callable.From<BaseEntity, BaseEntity, string>(OnAttackBlocked)
            );
        }
    }

    private void OnLevelUpMessage(int newLevel)
    {
        AudioManager.PlaySystemSound(SystemSounds.LevelUp);
    }

    private void OnAttackHit(BaseEntity attacker, BaseEntity target, int damage, string attackName, AttackType attackType)
    {
        // Only play sound for player attacks
        if (attacker is Player)
        {
            var sound = attackType == AttackType.Ranged
                ? SystemSounds.RangedAttack
                : SystemSounds.MeleeAttack;
            AudioManager.PlaySystemSound(sound);
        }
    }

    private void OnAttackMissed(BaseEntity attacker, BaseEntity target, string attackName)
    {
        if (attacker is Player)
        {
            AudioManager.PlaySystemSound(SystemSounds.AttackMiss);
        }
    }

    private void OnAttackBlocked(BaseEntity attacker, BaseEntity target, string attackName)
    {
        if (attacker is Player)
        {
            AudioManager.PlaySystemSound(SystemSounds.AttackMiss);
        }
    }
}
