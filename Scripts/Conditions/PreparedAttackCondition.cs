using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Entity;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Targeting modes for prepared attacks.
/// </summary>
public enum PrepareTargetingMode
{
    /// <summary>Single target (default) - only affects the hit target.</summary>
    Single,
    /// <summary>Arc targeting - affects target + adjacent tiles in a 3-tile arc (like Cleave).</summary>
    Arc
}

/// <summary>
/// Condition that prepares the entity's next melee attack with bonus effects.
/// Consumed on successful melee hit, persists through misses, expires after duration.
/// Only one prepared attack can be active at a time (same TypeId replaces).
/// </summary>
public class PreparedAttackCondition : Condition
{
    // Clockwise order for arc calculation (same as CleaveTargetingHandler)
    private static readonly (int dx, int dy)[] ClockwiseOffsets = new[]
    {
        (0, -1),   // N  (index 0)
        (1, -1),   // NE (index 1)
        (1, 0),    // E  (index 2)
        (1, 1),    // SE (index 3)
        (0, 1),    // S  (index 4)
        (-1, 1),   // SW (index 5)
        (-1, 0),   // W  (index 6)
        (-1, -1)   // NW (index 7)
    };

    private readonly string _prepareName;

    public override string Name => _prepareName;
    public override string TypeId => "prepared_attack";
    public override string? ExamineDescription => $"preparing {_prepareName}";

    /// <summary>
    /// Bonus to attack roll.
    /// </summary>
    public int HitBonus { get; set; } = 0;

    /// <summary>
    /// Bonus to damage on hit.
    /// </summary>
    public int DamageBonus { get; set; } = 0;

    /// <summary>
    /// Optional bonus damage type (e.g., Fire for spellsword).
    /// </summary>
    public DamageType? BonusDamageType { get; set; }

    /// <summary>
    /// Dice notation for bonus damage (e.g., "1d6").
    /// </summary>
    public string? BonusDamageDice { get; set; }

    /// <summary>
    /// Targeting mode for the prepared attack (Single or Arc).
    /// </summary>
    public PrepareTargetingMode TargetingMode { get; set; } = PrepareTargetingMode.Single;

    private BaseEntity? _owner;
    private CombatSystem? _combatSystem;
    private EntityManager? _entityManager;

    /// <summary>
    /// Creates a new prepared attack condition.
    /// </summary>
    /// <param name="prepareName">Display name for the prepared attack (e.g., "Power Attack").</param>
    /// <param name="duration">Duration in turns before expiring if unused.</param>
    public PreparedAttackCondition(string prepareName, string duration = "5")
    {
        _prepareName = prepareName;
        Duration = duration;
        DurationMode = ConditionDuration.Temporary;
    }

    public override ConditionMessage OnApplied(BaseEntity target)
    {
        _owner = target;

        // Find CombatSystem and EntityManager by traversing up to GameLevel
        ConnectToSystems(target);

        if (_combatSystem == null)
        {
            GD.PrintErr($"PreparedAttackCondition: Could not find CombatSystem for {target.DisplayName}");
            return ConditionMessage.Empty;
        }

        // Subscribe to combat signals
        _combatSystem.Connect(
            CombatSystem.SignalName.AttackHit,
            Callable.From<BaseEntity, BaseEntity, int, string, int>(OnAttackHit)
        );

        return new ConditionMessage(
            $"Preparing {_prepareName}!",
            Palette.ToHex(Palette.StatusBuff)
        );
    }

    public override ConditionMessage OnRemoved(BaseEntity target)
    {
        DisconnectFromCombatSystem();

        _owner = null;
        _combatSystem = null;
        _entityManager = null;

        return ConditionMessage.Empty;
    }

    /// <summary>
    /// Called when any attack hits. Check if it's our owner's melee attack.
    /// </summary>
    private void OnAttackHit(BaseEntity attacker, BaseEntity target, int damage, string attackName, int attackTypeInt)
    {
        // Only respond to our owner's attacks
        if (attacker != _owner || _owner == null)
            return;

        // Only trigger on melee attacks
        var attackType = (AttackType)attackTypeInt;
        if (attackType != AttackType.Melee)
            return;

        // Apply bonus damage to primary target if configured
        ApplyBonusDamage(target);

        // Handle arc targeting - attack adjacent enemies
        if (TargetingMode == PrepareTargetingMode.Arc)
        {
            ExecuteArcAttacks(target);
        }

        // Consume the prepared attack - remove self from owner
        _owner.RemoveConditionByType(TypeId);
    }

    /// <summary>
    /// Apply bonus damage (e.g., elemental damage from spellsword) to a target.
    /// </summary>
    private void ApplyBonusDamage(BaseEntity target)
    {
        if (!BonusDamageType.HasValue || string.IsNullOrEmpty(BonusDamageDice))
            return;

        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth != null && targetHealth.IsAlive())
        {
            int bonusDamage = Helpers.DiceRoller.Roll(BonusDamageDice);
            targetHealth.TakeDamage(bonusDamage, BonusDamageType.Value, _owner);
        }
    }

    /// <summary>
    /// Execute attacks on adjacent enemies in a 3-tile arc around the primary target.
    /// </summary>
    private void ExecuteArcAttacks(BaseEntity primaryTarget)
    {
        if (_owner == null || _entityManager == null)
            return;

        var attackComponent = _owner.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return;

        // Get arc positions (clockwise and counter-clockwise neighbors of the target direction)
        var arcPositions = GetArcPositions(_owner.GridPosition, primaryTarget.GridPosition);

        foreach (var pos in arcPositions)
        {
            var entity = _entityManager.GetEntityAtPosition(pos);
            if (entity == null || entity == _owner || entity == primaryTarget)
                continue;

            // Only attack hostile entities
            if (_owner.Faction.IsFriendlyTo(entity.Faction))
                continue;

            // Check if entity has health (is attackable)
            var healthComponent = entity.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (healthComponent == null || !healthComponent.IsAlive())
                continue;

            // Request attack on this target (will go through CombatSystem)
            // Note: This won't re-trigger the prepared attack since we're removing it after this method
            attackComponent.RequestAttack(entity, 0);
        }
    }

    /// <summary>
    /// Get the two arc positions (clockwise and counter-clockwise from target direction).
    /// </summary>
    private static GridPosition[] GetArcPositions(GridPosition casterPos, GridPosition targetPos)
    {
        // Find the direction index of the target relative to caster
        var targetOffset = (targetPos.X - casterPos.X, targetPos.Y - casterPos.Y);
        int targetIndex = -1;

        for (int i = 0; i < ClockwiseOffsets.Length; i++)
        {
            if (ClockwiseOffsets[i] == targetOffset)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
            return System.Array.Empty<GridPosition>(); // Not adjacent, shouldn't happen

        // Get clockwise neighbor (index + 1)
        int cwIndex = (targetIndex + 1) % 8;
        var cwOffset = ClockwiseOffsets[cwIndex];
        var cwPos = new GridPosition(casterPos.X + cwOffset.dx, casterPos.Y + cwOffset.dy);

        // Get counter-clockwise neighbor (index - 1)
        int ccwIndex = (targetIndex + 7) % 8; // +7 is same as -1 mod 8
        var ccwOffset = ClockwiseOffsets[ccwIndex];
        var ccwPos = new GridPosition(casterPos.X + ccwOffset.dx, casterPos.Y + ccwOffset.dy);

        return new[] { cwPos, ccwPos };
    }

    /// <summary>
    /// Returns the hit bonus for this prepared attack (used by CombatSystem).
    /// </summary>
    public int GetHitBonus() => HitBonus;

    /// <summary>
    /// Returns the damage bonus for this prepared attack (used by CombatSystem).
    /// </summary>
    public int GetDamageBonus() => DamageBonus;

    private void ConnectToSystems(BaseEntity target)
    {
        // Find GameLevel by traversing up
        Node? current = target;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                _combatSystem = current.GetNodeOrNull<CombatSystem>("CombatSystem");
                _entityManager = current.GetNodeOrNull<EntityManager>("EntityManager");
                break;
            }
            current = current.GetParent();
        }
    }

    private void DisconnectFromCombatSystem()
    {
        if (_combatSystem != null && GodotObject.IsInstanceValid(_combatSystem))
        {
            if (_combatSystem.IsConnected(
                CombatSystem.SignalName.AttackHit,
                Callable.From<BaseEntity, BaseEntity, int, string, int>(OnAttackHit)))
            {
                _combatSystem.Disconnect(
                    CombatSystem.SignalName.AttackHit,
                    Callable.From<BaseEntity, BaseEntity, int, string, int>(OnAttackHit)
                );
            }
        }
    }
}
