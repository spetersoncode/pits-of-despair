using System.Linq;
using Godot;
using PitsOfDespair.ItemProperties;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
// Using FactionExtensions from BaseEntity.cs

namespace PitsOfDespair.Systems;

/// <summary>
/// System that validates and executes combat between entities.
/// Coordinates AttackComponents and HealthComponents.
/// </summary>
public partial class CombatSystem : Node
{
    private readonly System.Collections.Generic.List<AttackComponent> _registeredComponents = new();

    /// <summary>
    /// Emitted when an attack hits and deals damage (attacker, target, damage, attackName, attackType, damageType)
    /// </summary>
    [Signal]
    public delegate void AttackHitEventHandler(BaseEntity attacker, BaseEntity target, int damage, string attackName, AttackType attackType, DamageType damageType);

    /// <summary>
    /// Emitted when an attack hits but deals no damage due to armor (attacker, target, attackName)
    /// </summary>
    [Signal]
    public delegate void AttackBlockedEventHandler(BaseEntity attacker, BaseEntity target, string attackName);

    /// <summary>
    /// Emitted when an attack misses (attacker, target, attackName)
    /// </summary>
    [Signal]
    public delegate void AttackMissedEventHandler(BaseEntity attacker, BaseEntity target, string attackName);

    /// <summary>
    /// Emitted when an attack occurs (attacker, target, damage, attackName)
    /// DEPRECATED: Use AttackHit, AttackBlocked, or AttackMissed instead
    /// </summary>
    [Signal]
    public delegate void AttackExecutedEventHandler(BaseEntity attacker, BaseEntity target, int damage, string attackName);

    /// <summary>
    /// Emitted when an entity performs an action with a message (actor, message, color)
    /// </summary>
    [Signal]
    public delegate void ActionMessageEventHandler(BaseEntity actor, string message, string color);

    /// <summary>
    /// Emitted when a skill deals damage (caster, target, damage, skillName)
    /// Used for message log display of skill damage.
    /// </summary>
    [Signal]
    public delegate void SkillDamageDealtEventHandler(BaseEntity caster, BaseEntity target, int damage, string skillName);

    /// <summary>
    /// Emitted when a property effect triggers on hit (attacker, target, verb, damage, color)
    /// Used for combining property damage into attack messages.
    /// </summary>
    [Signal]
    public delegate void PropertyEffectAppliedEventHandler(BaseEntity attacker, BaseEntity target, string verb, int damage, string color);

    /// <summary>
    /// Emitted when lifesteal occurs from a vampiric property (attacker, target, healing)
    /// </summary>
    [Signal]
    public delegate void LifestealAppliedEventHandler(BaseEntity attacker, BaseEntity target, int healing);

    /// <summary>
    /// Register an AttackComponent to listen for attack requests.
    /// Called by GameLevel or EntityManager when entities with AttackComponents are created.
    /// </summary>
    /// <param name="component">The AttackComponent to register.</param>
    public void RegisterAttackComponent(AttackComponent component)
    {
        // Use lambda to capture the component reference in a closure
        component.Connect(AttackComponent.SignalName.AttackRequested, Callable.From<BaseEntity, int>((target, attackIndex) => OnAttackRequested(component, target, attackIndex)));
        _registeredComponents.Add(component);
    }

    /// <summary>
    /// Handle attack requests from AttackComponents.
    /// Validates the attack and applies damage to target's HealthComponent using opposed 2d6 rolls.
    /// </summary>
    /// <param name="component">The AttackComponent that requested the attack.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="attackIndex">Index of the attack to use.</param>
    private void OnAttackRequested(AttackComponent component, BaseEntity target, int attackIndex)
    {
        var attacker = component.GetEntity();
        if (attacker == null)
        {
            GD.PushWarning("CombatSystem: AttackComponent has no parent entity");
            return;
        }

        // Validate target has health
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null)
        {
            GD.PushWarning($"CombatSystem: Target {target.DisplayName} has no HealthComponent");
            return;
        }

        // Check if target is already dead
        if (!targetHealth.IsAlive())
        {
            return;
        }

        // Prevent friendly fire - don't allow attacks between entities on the same side
        // Friendly faction (player + allies) cannot attack each other
        // Hostile faction cannot attack each other
        if (attacker.Faction.IsFriendlyTo(target.Faction))
        {
            return;
        }

        // Get attack data
        var attackData = component.GetAttack(attackIndex);
        if (attackData == null)
        {
            GD.PushWarning($"CombatSystem: Invalid attack index {attackIndex}");
            return;
        }

        // Validate attack range
        int distance = DistanceHelper.ChebyshevDistance(attacker.GridPosition, target.GridPosition);
        if (distance > attackData.Range)
        {
            GD.PushWarning($"CombatSystem: Target out of range (distance: {distance}, range: {attackData.Range})");
            return;
        }

        // Get stats components (targetStats may be null for destructible objects like decorations)
        var attackerStats = attacker.GetNodeOrNull<StatsComponent>("StatsComponent");
        var targetStats = target.GetNodeOrNull<StatsComponent>("StatsComponent");

        if (attackerStats == null)
        {
            GD.PushWarning($"CombatSystem: Attacker {attacker.DisplayName} has no StatsComponent");
            return;
        }

        // Determine if this is a melee or ranged attack
        bool isMelee = attackData.Type == AttackType.Melee;

        // Check for prepared attack bonuses (melee only)
        var preparedAttack = isMelee ? GetPreparedAttack(attacker) : null;
        int prepareHitBonus = preparedAttack?.GetHitBonus() ?? 0;
        int prepareDamageBonus = preparedAttack?.GetDamageBonus() ?? 0;

        // Get weapon and ammo instances
        var weaponInstance = GetEquippedWeaponInstance(attacker, isMelee);
        var ammoInstance = isMelee ? null : GetEquippedAmmoInstance(attacker);
        var (propertyHitBonus, propertyDamageBonus) = GetWeaponPropertyBonuses(attacker, isMelee);

        // Get armor piercing from ammo (ranged attacks only)
        int armorPiercing = isMelee ? 0 : GetAmmoArmorPiercing(attacker);

        int baseDamage;
        int finalDamage;

        // Targets with HealthComponent but no StatsComponent (e.g., destructible decorations)
        // Auto-hit and deal full weapon damage (no opposed roll, no armor)
        if (targetStats == null)
        {
            baseDamage = DiceRoller.Roll(attackData.DiceNotation);
            int damageBonus = attackerStats.GetDamageBonus(isMelee) + prepareDamageBonus + propertyDamageBonus;
            finalDamage = Mathf.Max(0, baseDamage + damageBonus);
        }
        else
        {
            // PHASE 1: Opposed Attack Roll (2d6 + modifiers)
            int attackModifier = attackerStats.GetAttackModifier(isMelee) + prepareHitBonus + propertyHitBonus;
            int defenseModifier = targetStats.GetDefenseModifier();

            int attackRoll = DiceRoller.Roll(2, 6, attackModifier);
            int defenseRoll = DiceRoller.Roll(2, 6, defenseModifier);

            // Check if attack hits (attacker roll >= defender roll, ties go to attacker)
            bool hit = attackRoll >= defenseRoll;

            if (!hit)
            {
                // Attack missed - prime persists (not consumed)
                EmitSignal(SignalName.AttackMissed, attacker, target, attackData.Name);
                EmitSignal(SignalName.AttackExecuted, attacker, target, 0, attackData.Name); // Legacy support
                return;
            }

            // PHASE 2: Damage Calculation (weapon damage + STR [if melee] + prime + property bonus - armor)
            baseDamage = DiceRoller.Roll(attackData.DiceNotation);
            int damageBonus = attackerStats.GetDamageBonus(isMelee) + prepareDamageBonus + propertyDamageBonus;
            int effectiveArmor = Mathf.Max(0, targetStats.TotalArmor - armorPiercing);
            finalDamage = Mathf.Max(0, baseDamage + damageBonus - effectiveArmor);
        }

        // PHASE 3: Calculate Actual Damage, Emit Feedback, Then Apply Damage
        if (finalDamage > 0)
        {
            // Calculate what the actual damage will be after resistances/vulnerabilities
            int actualDamage = targetHealth.CalculateDamage(finalDamage, attackData.DamageType);

            // Emit hit signal BEFORE applying damage (so "hit" message appears before "death" message)
            EmitSignal(SignalName.AttackHit, attacker, target, actualDamage, attackData.Name, (int)attackData.Type, (int)attackData.DamageType);
            EmitSignal(SignalName.AttackExecuted, attacker, target, actualDamage, attackData.Name); // Legacy support

            // Now apply the damage (which may trigger death signals)
            targetHealth.TakeDamage(finalDamage, attackData.DamageType, attacker);

            // PHASE 4: Process on-hit property effects (elemental damage, vampiric, etc.)
            ProcessOnHitProperties(attacker, target, weaponInstance, ammoInstance, actualDamage);
        }
        else
        {
            // Hit but armor absorbed all damage
            EmitSignal(SignalName.AttackBlocked, attacker, target, attackData.Name);
            EmitSignal(SignalName.AttackExecuted, attacker, target, 0, attackData.Name); // Legacy support
        }
    }

    /// <summary>
    /// Emit attack feedback for actions that execute combat directly.
    /// Used by the Action system to maintain consistent combat event signaling.
    /// </summary>
    public void EmitAttackFeedback(BaseEntity attacker, BaseEntity target, int damage, string attackName)
    {
        EmitSignal(SignalName.AttackExecuted, attacker, target, damage, attackName);
    }

    /// <summary>
    /// Emit a general action message for display in the message log.
    /// Used by actions to provide feedback about non-combat activities.
    /// </summary>
    public void EmitActionMessage(BaseEntity actor, string message, string? color = null)
    {
        color ??= Palette.ToHex(Palette.Default);
        EmitSignal(SignalName.ActionMessage, actor, message, color);
    }

    /// <summary>
    /// Emit skill damage feedback for the message log.
    /// Used by skill effects to report damage dealt via projectiles or other delayed effects.
    /// </summary>
    public void EmitSkillDamageDealt(BaseEntity caster, BaseEntity target, int damage, string skillName)
    {
        EmitSignal(SignalName.SkillDamageDealt, caster, target, damage, skillName);
    }

    /// <summary>
    /// Gets the active prepared attack condition on an entity, if any.
    /// </summary>
    private static PreparedAttackCondition? GetPreparedAttack(BaseEntity entity)
    {
        return entity.GetActiveConditions()
            .FirstOrDefault(c => c.TypeId == "prepared_attack") as PreparedAttackCondition;
    }

    /// <summary>
    /// Gets the equipped weapon ItemInstance for an entity based on attack type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="isMelee">True for melee weapon, false for ranged weapon.</param>
    /// <returns>The ItemInstance of the equipped weapon, or null if none equipped.</returns>
    private static ItemInstance? GetEquippedWeaponInstance(BaseEntity entity, bool isMelee)
    {
        var equipComponent = entity.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null) return null;

        var slot = isMelee ? EquipmentSlot.MeleeWeapon : EquipmentSlot.RangedWeapon;
        var inventoryKey = equipComponent.GetEquippedKey(slot);
        if (inventoryKey == null) return null;

        var inventoryComponent = entity.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventoryComponent == null) return null;

        var inventorySlot = inventoryComponent.GetSlot(inventoryKey.Value);
        return inventorySlot?.Item;
    }

    /// <summary>
    /// Gets the equipped ammo ItemInstance for an entity.
    /// </summary>
    private static ItemInstance? GetEquippedAmmoInstance(BaseEntity entity)
    {
        var equipComponent = entity.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null) return null;

        var inventoryKey = equipComponent.GetEquippedKey(EquipmentSlot.Ammo);
        if (inventoryKey == null) return null;

        var inventoryComponent = entity.GetNodeOrNull<InventoryComponent>("InventoryComponent");
        if (inventoryComponent == null) return null;

        var inventorySlot = inventoryComponent.GetSlot(inventoryKey.Value);
        return inventorySlot?.Item;
    }

    /// <summary>
    /// Gets hit and damage bonuses from weapon and ammo properties.
    /// </summary>
    private static (int hitBonus, int damageBonus) GetWeaponPropertyBonuses(BaseEntity attacker, bool isMelee)
    {
        var weapon = GetEquippedWeaponInstance(attacker, isMelee);
        int hitBonus = weapon?.GetTotalHitBonus() ?? 0;
        int damageBonus = weapon?.GetTotalDamageBonus() ?? 0;

        // For ranged attacks, also include ammo bonuses
        if (!isMelee)
        {
            var ammo = GetEquippedAmmoInstance(attacker);
            if (ammo != null)
            {
                hitBonus += ammo.GetTotalHitBonus();
                damageBonus += ammo.GetTotalDamageBonus();
            }
        }

        return (hitBonus, damageBonus);
    }

    /// <summary>
    /// Gets armor piercing from ammo properties.
    /// </summary>
    private static int GetAmmoArmorPiercing(BaseEntity attacker)
    {
        var ammo = GetEquippedAmmoInstance(attacker);
        if (ammo == null) return 0;

        int armorPiercing = 0;
        foreach (var property in ammo.GetProperties())
        {
            if (property is IArmorPiercingProperty piercingProperty)
            {
                armorPiercing += piercingProperty.GetArmorPiercing();
            }
        }
        return armorPiercing;
    }

    /// <summary>
    /// Processes on-hit property effects after a successful attack.
    /// Emits signals for property damage/healing so messages can be combined with attack message.
    /// </summary>
    private void ProcessOnHitProperties(BaseEntity attacker, BaseEntity target, ItemInstance? weapon, ItemInstance? ammo, int damage)
    {
        if (damage <= 0) return;

        // Process weapon on-hit properties
        if (weapon != null)
        {
            ProcessItemOnHitProperties(attacker, target, weapon, damage);
        }

        // Process ammo on-hit properties (for ranged attacks)
        if (ammo != null)
        {
            ProcessItemOnHitProperties(attacker, target, ammo, damage);
        }
    }

    /// <summary>
    /// Processes on-hit properties for a single item.
    /// </summary>
    private void ProcessItemOnHitProperties(BaseEntity attacker, BaseEntity target, ItemInstance item, int damage)
    {
        foreach (var property in item.GetOnHitProperties())
        {
            var result = property.OnHit(attacker, target, damage);

            // Emit property effect for elemental/damage properties
            if (result.DamageDealt > 0 && !string.IsNullOrEmpty(result.Verb))
            {
                string color = result.MessageColor ?? Palette.ToHex(Palette.Default);
                EmitSignal(SignalName.PropertyEffectApplied, attacker, target, result.Verb, result.DamageDealt, color);
            }

            // Emit lifesteal for vampiric properties
            if (result.HealingDone > 0)
            {
                EmitSignal(SignalName.LifestealApplied, attacker, target, result.HealingDone);
            }
        }
    }

    public override void _ExitTree()
    {
        // Disconnect from all registered attack components
        foreach (var component in _registeredComponents)
        {
            if (component != null && GodotObject.IsInstanceValid(component))
            {
                component.Disconnect(AttackComponent.SignalName.AttackRequested, Callable.From<BaseEntity, int>((target, attackIndex) => OnAttackRequested(component, target, attackIndex)));
            }
        }
        _registeredComponents.Clear();
    }
}
