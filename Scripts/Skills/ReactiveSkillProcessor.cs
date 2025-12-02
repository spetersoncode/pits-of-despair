using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Effects;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;
using PitsOfDespair.Systems;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes reactive skills, triggering their effects in response to combat events.
/// Listens to combat signals and checks for matching reactive skill triggers.
/// </summary>
public partial class ReactiveSkillProcessor : Node
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private StatsComponent? _statsComponent;
    private HealthComponent? _healthComponent;
    private WillpowerComponent? _willpowerComponent;
    private DataLoader? _dataLoader;
    private CombatSystem? _combatSystem;

    /// <summary>
    /// Tracks cooldowns for reactive skills (skill ID -> remaining cooldown).
    /// Used for skills that can only trigger once per floor/combat/etc.
    /// </summary>
    private readonly Dictionary<string, int> _cooldowns = new();

    /// <summary>
    /// Flags for one-time-per-floor reactive skills.
    /// Reset when descending to a new floor.
    /// </summary>
    private readonly HashSet<string> _usedThisFloor = new();

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity == null)
        {
            GD.PushWarning("ReactiveSkillProcessor: Must be child of BaseEntity");
            return;
        }

        _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");
        _statsComponent = _entity.GetNodeOrNull<StatsComponent>("StatsComponent");
        _healthComponent = _entity.GetNodeOrNull<HealthComponent>("HealthComponent");
        _willpowerComponent = _entity.GetNodeOrNull<WillpowerComponent>("WillpowerComponent");

        if (_skillComponent == null)
        {
            GD.PushWarning("ReactiveSkillProcessor: SkillComponent not found");
            return;
        }

        // Find DataLoader
        _dataLoader = GetTree()?.Root.GetNodeOrNull<DataLoader>("DataLoader");
        if (_dataLoader == null)
        {
            GD.PushWarning("ReactiveSkillProcessor: DataLoader not found");
            return;
        }

        // Connect to health signals for damage/death triggers
        if (_healthComponent != null)
        {
            _healthComponent.Connect(HealthComponent.SignalName.DamageTaken, Callable.From<int>(OnDamageTaken));
        }

        // Find CombatSystem for combat event triggers
        CallDeferred(nameof(ConnectToCombatSystem));
    }

    /// <summary>
    /// Connects to CombatSystem after the scene tree is ready.
    /// </summary>
    private void ConnectToCombatSystem()
    {
        // Find CombatSystem by traversing up to GameLevel
        Node? current = this;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                _combatSystem = current.GetNodeOrNull<CombatSystem>("CombatSystem");
                break;
            }
            current = current.GetParent();
        }

        if (_combatSystem != null)
        {
            _combatSystem.Connect(CombatSystem.SignalName.AttackHit,
                Callable.From<BaseEntity, BaseEntity, int, string, AttackType, DamageType>(OnAttackHit));
            _combatSystem.Connect(CombatSystem.SignalName.AttackMissed,
                Callable.From<BaseEntity, BaseEntity, string>(OnAttackMissed));
        }
    }

    public override void _ExitTree()
    {
        if (_healthComponent != null && IsInstanceValid(_healthComponent))
        {
            _healthComponent.Disconnect(HealthComponent.SignalName.DamageTaken, Callable.From<int>(OnDamageTaken));
        }

        if (_combatSystem != null && IsInstanceValid(_combatSystem))
        {
            _combatSystem.Disconnect(CombatSystem.SignalName.AttackHit,
                Callable.From<BaseEntity, BaseEntity, int, string, AttackType, DamageType>(OnAttackHit));
            _combatSystem.Disconnect(CombatSystem.SignalName.AttackMissed,
                Callable.From<BaseEntity, BaseEntity, string>(OnAttackMissed));
        }
    }

    /// <summary>
    /// Resets per-floor cooldowns. Call when descending to a new floor.
    /// </summary>
    public void ResetFloorCooldowns()
    {
        _usedThisFloor.Clear();
        GD.Print("ReactiveSkillProcessor: Floor cooldowns reset");
    }

    #region Combat Event Handlers

    /// <summary>
    /// Called when an attack hits and deals damage.
    /// Checks for on_kill and on_hit triggers.
    /// </summary>
    private void OnAttackHit(BaseEntity attacker, BaseEntity target, int damage, string attackName, AttackType attackType, DamageType damageType)
    {
        if (_entity == null || _dataLoader == null) return;

        // Check for on_kill: player killed an enemy
        if (attacker == _entity && target != _entity)
        {
            var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (targetHealth != null && !targetHealth.IsAlive())
            {
                TriggerReactiveSkills("on_kill", target, attackType: attackType);
            }
        }

        // Check for on_hit: player hit an enemy
        if (attacker == _entity && target != _entity)
        {
            TriggerReactiveSkills("on_hit", target, attackType: attackType);
        }
    }

    /// <summary>
    /// Called when an attack misses.
    /// Checks for on_enemy_miss trigger (when enemy misses this entity).
    /// </summary>
    private void OnAttackMissed(BaseEntity attacker, BaseEntity target, string attackName)
    {
        if (_entity == null || _dataLoader == null) return;

        // Check for on_enemy_miss: enemy missed this entity (e.g., Riposte)
        if (target == _entity && attacker != _entity)
        {
            TriggerReactiveSkills("on_enemy_miss", attacker);
        }
    }

    /// <summary>
    /// Called when this entity takes damage.
    /// Checks for on_damage triggers.
    /// </summary>
    private void OnDamageTaken(int amount)
    {
        if (_entity == null || _dataLoader == null || _healthComponent == null) return;

        // Check for on_damage triggers
        TriggerReactiveSkills("on_damage", null, amount);

        // Check for on_low_hp triggers (below 25% HP)
        float hpPercent = (float)_healthComponent.CurrentHealth / _healthComponent.MaxHealth;
        if (hpPercent <= 0.25f)
        {
            TriggerReactiveSkills("on_low_hp", null, amount);
        }
    }

    #endregion

    #region Reactive Skill Triggering

    /// <summary>
    /// Finds and triggers all learned reactive skills with the given trigger type.
    /// </summary>
    private void TriggerReactiveSkills(string triggerType, BaseEntity? triggerSource, int damageAmount = 0, AttackType? attackType = null)
    {
        if (_skillComponent == null || _dataLoader == null) return;

        var learnedSkills = _skillComponent.GetLearnedSkillsByCategory(_dataLoader);
        if (!learnedSkills.TryGetValue(SkillCategory.Reactive, out var reactiveSkills))
            return;

        foreach (var skill in reactiveSkills)
        {
            if (skill.Trigger?.ToLower() == triggerType)
            {
                // Check weapon category requirement
                if (!string.IsNullOrEmpty(skill.RequireWeaponCategory))
                {
                    if (!CheckWeaponCategory(skill.RequireWeaponCategory, attackType))
                        continue;
                }

                // Check distracted requirement
                if (skill.RequireTargetDistracted)
                {
                    if (!ConditionHelper.IsDistracted(triggerSource))
                        continue;
                }

                TryTriggerSkill(skill, triggerSource, damageAmount);
            }
        }
    }

    /// <summary>
    /// Checks if the equipped weapon matches the required category.
    /// </summary>
    private bool CheckWeaponCategory(string requiredCategory, AttackType? attackType)
    {
        if (_entity == null) return false;

        var equipComponent = _entity.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null) return false;

        // Determine which slot to check based on attack type
        var slot = attackType == AttackType.Ranged
            ? EquipmentSlot.RangedWeapon
            : EquipmentSlot.MeleeWeapon;

        var equippedItem = equipComponent.GetEquippedItem(slot);
        if (equippedItem?.Template?.Category == null)
            return false;

        return equippedItem.Template.Category.Equals(requiredCategory, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to trigger a single reactive skill.
    /// </summary>
    private void TryTriggerSkill(SkillDefinition skill, BaseEntity? triggerSource, int damageAmount)
    {
        // Check cooldown
        if (_usedThisFloor.Contains(skill.Id))
        {
            return;
        }

        // Check WP cost
        if (skill.TriggerCost > 0 && _willpowerComponent != null)
        {
            if (_willpowerComponent.CurrentWillpower < skill.TriggerCost)
            {
                return;
            }
        }

        // Auto-trigger or would need player confirmation
        if (skill.AutoTrigger)
        {
            ExecuteReactiveSkill(skill, triggerSource, damageAmount);
        }
        else
        {
            // For non-auto-trigger skills, we'd need a prompt system
            // For now, auto-trigger all skills
            ExecuteReactiveSkill(skill, triggerSource, damageAmount);
        }
    }

    /// <summary>
    /// Executes a reactive skill's effects.
    /// </summary>
    private void ExecuteReactiveSkill(SkillDefinition skill, BaseEntity? triggerSource, int damageAmount)
    {
        if (_entity == null) return;

        // Spend WP if required
        if (skill.TriggerCost > 0 && _willpowerComponent != null)
        {
            if (!_willpowerComponent.SpendWillpower(skill.TriggerCost))
            {
                return;
            }
        }

        // Mark as used if it has a floor cooldown (check for tag or specific skills)
        if (skill.Tags.Contains("once_per_floor"))
        {
            _usedThisFloor.Add(skill.Id);
        }

        GD.Print($"ReactiveSkillProcessor: Triggered '{skill.Name}'");

        // Execute effects based on skill type
        foreach (var effect in skill.Effects)
        {
            ExecuteReactiveEffect(effect, skill, triggerSource, damageAmount);
        }
    }

    /// <summary>
    /// Executes a single reactive effect.
    /// Supports both step-based composable effects and legacy type-based effects.
    /// </summary>
    private void ExecuteReactiveEffect(SkillEffectDefinition effect, SkillDefinition skill,
        BaseEntity? triggerSource, int damageAmount)
    {
        if (_entity == null) return;

        // Handle step-based composable effects
        if (effect.Steps != null && effect.Steps.Count > 0)
        {
            ExecuteStepBasedEffect(effect, skill);
            return;
        }

        // Legacy type-based effects
        switch (effect.Type?.ToLower())
        {
            case "apply_condition":
                // Apply a condition to self (e.g., damage bonus from Rampage)
                ApplyConditionEffect(effect);
                break;

            case "counter_attack":
                // Perform a counter-attack against the trigger source (e.g., Riposte)
                if (triggerSource != null)
                {
                    PerformCounterAttack(triggerSource);
                }
                break;

            case "extra_weapon_attack":
                // Deal additional weapon damage to trigger source (e.g., Stab)
                if (triggerSource != null)
                {
                    PerformExtraWeaponAttack(triggerSource, skill.Name);
                }
                break;

            case "survive_lethal":
                // Die Hard effect - would need to intercept lethal damage
                // This is handled specially in HealthComponent
                break;

            case "reduce_damage":
                // Mana Shield effect - reduce incoming damage
                // This would need to intercept damage before it's applied
                break;

            default:
                GD.Print($"ReactiveSkillProcessor: Unknown effect type '{effect.Type}'");
                break;
        }
    }

    /// <summary>
    /// Executes a step-based composable effect on self.
    /// </summary>
    private void ExecuteStepBasedEffect(SkillEffectDefinition effectDef, SkillDefinition skill)
    {
        if (_entity == null) return;

        var effect = Effect.CreateFromSkillDefinition(effectDef, skill.Name, skill.Range, skill.Radius);
        if (effect == null)
        {
            GD.PrintErr($"ReactiveSkillProcessor: Failed to create effect from steps for '{skill.Name}'");
            return;
        }

        // Create a minimal action context for self-targeting effects
        var actionContext = CreateActionContext();
        if (actionContext == null)
        {
            GD.PrintErr($"ReactiveSkillProcessor: Could not create action context");
            return;
        }

        var effectContext = EffectContext.ForSkill(_entity, _entity, actionContext, skill, null);
        effect.Apply(effectContext);
    }

    /// <summary>
    /// Creates an ActionContext for executing reactive effects.
    /// </summary>
    private ActionContext? CreateActionContext()
    {
        // Find GameLevel to get system references
        Node? current = this;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                var mapSystem = current.GetNodeOrNull<MapSystem>("MapSystem");
                var entityManager = current.GetNodeOrNull<Systems.Entity.EntityManager>("EntityManager");
                var entityFactory = current.GetNodeOrNull<Systems.Entity.EntityFactory>("EntityFactory");
                var visualEffectSystem = current.GetNodeOrNull<Systems.VisualEffects.VisualEffectSystem>("VisualEffectSystem");
                var tileHazardManager = current.GetNodeOrNull<TileHazardManager>("TileHazardManager");
                var playerVisionSystem = current.GetNodeOrNull<Systems.Vision.PlayerVisionSystem>("PlayerVisionSystem");

                // Find player - could be the entity itself or we need to find it
                var player = _entity as Player ?? current.GetNodeOrNull<Player>("Player");

                if (mapSystem != null && entityManager != null && player != null &&
                    _combatSystem != null && entityFactory != null && visualEffectSystem != null)
                {
                    return new ActionContext(
                        mapSystem,
                        entityManager,
                        player,
                        _combatSystem,
                        entityFactory,
                        visualEffectSystem,
                        tileHazardManager,
                        playerVisionSystem
                    );
                }
                break;
            }
            current = current.GetParent();
        }
        return null;
    }

    /// <summary>
    /// Applies a condition effect to the entity.
    /// Uses the unified ConditionFactory.
    /// </summary>
    private void ApplyConditionEffect(SkillEffectDefinition effect)
    {
        if (_entity == null || string.IsNullOrEmpty(effect.ConditionType))
            return;

        string duration = !string.IsNullOrEmpty(effect.Duration) ? effect.Duration : "1";

        // Handle special reactive condition types that map to standard conditions
        string conditionType = effect.ConditionType.ToLower() switch
        {
            "damage_buff" => "strength_modifier",     // Damage bonus via STR modifier
            "power_attack" => "strength_modifier",    // Power attack is also a STR modifier
            _ => effect.ConditionType
        };

        int amount = effect.Amount;
        if (conditionType == "strength_modifier" && effect.ConditionType.ToLower() == "power_attack" && amount == 0)
        {
            amount = 3; // Default power attack bonus
        }

        var condition = ConditionFactory.Create(conditionType, amount, duration);

        if (condition != null)
        {
            _entity.AddCondition(condition);
            GD.Print($"ReactiveSkillProcessor: Applied condition '{effect.ConditionType}'");
        }
        else
        {
            GD.PrintErr($"ReactiveSkillProcessor: Unknown condition type '{effect.ConditionType}'");
        }
    }

    /// <summary>
    /// Performs a counter-attack against an enemy.
    /// </summary>
    private void PerformCounterAttack(BaseEntity target)
    {
        if (_entity == null) return;

        var attackComponent = _entity.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null) return;

        // Check if target is still valid and in range
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null || !targetHealth.IsAlive())
            return;

        // Check if adjacent (melee range)
        int distance = DistanceHelper.ChebyshevDistance(_entity.GridPosition, target.GridPosition);
        if (distance > 1)
            return;

        // Request counter-attack
        attackComponent.RequestAttack(target, 0);
        GD.Print($"ReactiveSkillProcessor: Counter-attack against {target.DisplayName}");
    }

    /// <summary>
    /// Performs extra weapon damage against an enemy (no attack roll, already hit).
    /// Used by Stab to deal bonus damage against distracted enemies.
    /// </summary>
    private void PerformExtraWeaponAttack(BaseEntity target, string skillName)
    {
        if (_entity == null) return;

        var attackComponent = _entity.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null) return;

        // Check if target is still valid
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth == null || !targetHealth.IsAlive())
            return;

        // Get the primary melee attack for damage calculation
        var attackData = attackComponent.GetAttack(0);
        if (attackData == null || attackData.Type != AttackType.Melee)
            return;

        // Get stats for damage calculation
        var attackerStats = _entity.GetNodeOrNull<StatsComponent>("StatsComponent");
        var targetStats = target.GetNodeOrNull<StatsComponent>("StatsComponent");

        // Calculate damage (weapon dice + STR bonus - armor)
        int baseDamage = DiceRoller.Roll(attackData.DiceNotation);
        int strBonus = attackerStats?.GetDamageBonus(isMelee: true) ?? 0;
        // Cap STR bonus at weapon's max base roll
        int maxStrBonus = attackData.GetMaxStrengthBonus();
        strBonus = Mathf.Min(strBonus, maxStrBonus);
        int armor = targetStats?.TotalArmor ?? 0;

        int finalDamage = Mathf.Max(0, baseDamage + strBonus - armor);

        if (finalDamage > 0)
        {
            // Calculate actual damage after resistances
            int actualDamage = targetHealth.CalculateDamage(finalDamage, attackData.DamageType);

            // Emit flavorful message based on damage type
            string[] flavors = attackData.DamageType switch
            {
                DamageType.Piercing => new[] {
                    "finding the gap",
                    "driving it home",
                    "slipping between the ribs"
                },
                DamageType.Slashing => new[] {
                    "opening a deep gash",
                    "carving through flesh",
                    "leaving a bloody trail"
                },
                _ => new[] {
                    "striking true",
                    "exploiting the opening"
                }
            };
            string flavor = flavors[GD.RandRange(0, flavors.Length - 1)];
            _combatSystem?.EmitActionMessage(
                _entity,
                $"You stab the {target.DisplayName}, {flavor}!",
                Palette.ToHex(Palette.CombatDamage)
            );

            // Apply the damage
            targetHealth.TakeDamage(finalDamage, attackData.DamageType, _entity);
        }
    }

    #endregion
}
