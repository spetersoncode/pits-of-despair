using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Conditions;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Helpers;

using PitsOfDespair.Systems;

namespace PitsOfDespair.Skills;

/// <summary>
/// Processes aura skills, applying effects to entities within range.
/// Updates aura targets when entities move or when the caster moves.
/// </summary>
public partial class AuraProcessor : Node
{
    private BaseEntity? _entity;
    private SkillComponent? _skillComponent;
    private DataLoader? _dataLoader;
    private EntityManager? _entityManager;
    private TurnManager? _turnManager;

    /// <summary>
    /// Tracks which auras are currently active and their affected entities.
    /// Key: skill ID, Value: set of affected entity IDs
    /// </summary>
    private readonly Dictionary<string, HashSet<int>> _activeAuras = new();

    /// <summary>
    /// Cache of aura skill definitions for quick lookup.
    /// </summary>
    private readonly Dictionary<string, SkillDefinition> _auraSkillCache = new();

    public override void _Ready()
    {
        _entity = GetParent<BaseEntity>();
        if (_entity == null)
        {
            GD.PushWarning("AuraProcessor: Must be child of BaseEntity");
            return;
        }

        _skillComponent = _entity.GetNodeOrNull<SkillComponent>("SkillComponent");

        if (_skillComponent == null)
        {
            GD.PushWarning("AuraProcessor: SkillComponent not found");
            return;
        }

        // Connect to skill learned signal
        _skillComponent.Connect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));

        // Connect to entity position changes to update auras
        _entity.Connect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnPositionChanged));

        // Deferred initialization for systems that may not be ready yet
        CallDeferred(nameof(DeferredInit));
    }

    /// <summary>
    /// Deferred initialization to find game systems.
    /// </summary>
    private void DeferredInit()
    {
        // Find DataLoader
        _dataLoader = GetTree()?.Root.GetNodeOrNull<DataLoader>("DataLoader");

        // Find EntityManager and TurnManager by traversing up to GameLevel
        Node? current = this;
        while (current != null)
        {
            if (current.Name == "GameLevel")
            {
                _entityManager = current.GetNodeOrNull<EntityManager>("EntityManager");
                _turnManager = current.GetNodeOrNull<TurnManager>("TurnManager");
                break;
            }
            current = current.GetParent();
        }

        // Connect to turn signal to update auras each turn
        if (_turnManager != null && _entity is Player)
        {
            _turnManager.Connect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }

        // Initialize auras for any already-learned skills
        InitializeExistingAuras();
    }

    public override void _ExitTree()
    {
        if (_skillComponent != null && IsInstanceValid(_skillComponent))
        {
            _skillComponent.Disconnect(SkillComponent.SignalName.SkillLearned, Callable.From<string>(OnSkillLearned));
        }

        if (_entity != null && IsInstanceValid(_entity))
        {
            _entity.Disconnect(BaseEntity.SignalName.PositionChanged, Callable.From<int, int>(OnPositionChanged));
        }

        if (_turnManager != null && IsInstanceValid(_turnManager))
        {
            _turnManager.Disconnect(TurnManager.SignalName.PlayerTurnStarted, Callable.From(OnTurnStarted));
        }

        // Remove all aura effects on cleanup
        RemoveAllAuraEffects();
    }

    /// <summary>
    /// Called when a skill is learned. Initializes aura if it's an aura skill.
    /// </summary>
    private void OnSkillLearned(string skillId)
    {
        if (_dataLoader == null) return;

        var skill = _dataLoader.Skills.Get(skillId);
        if (skill == null || skill.GetCategory() != SkillCategory.Aura)
            return;

        InitializeAura(skill);
    }

    /// <summary>
    /// Called when the entity moves. Updates aura targets.
    /// </summary>
    private void OnPositionChanged(int x, int y)
    {
        UpdateAllAuras();
    }

    /// <summary>
    /// Called at the start of each turn. Updates aura targets.
    /// </summary>
    private void OnTurnStarted()
    {
        UpdateAllAuras();
    }

    /// <summary>
    /// Initializes auras for any already-learned aura skills.
    /// </summary>
    private void InitializeExistingAuras()
    {
        if (_skillComponent == null || _dataLoader == null) return;

        var learnedSkills = _skillComponent.GetLearnedSkillsByCategory(_dataLoader);
        if (!learnedSkills.TryGetValue(SkillCategory.Aura, out var auraSkills))
            return;

        foreach (var skill in auraSkills)
        {
            InitializeAura(skill);
        }
    }

    /// <summary>
    /// Initializes a single aura skill.
    /// </summary>
    private void InitializeAura(SkillDefinition skill)
    {
        if (_activeAuras.ContainsKey(skill.Id))
            return;

        _auraSkillCache[skill.Id] = skill;
        _activeAuras[skill.Id] = new HashSet<int>();

        GD.Print($"AuraProcessor: Initialized aura '{skill.Name}' (radius: {skill.AuraRadius}, target: {skill.AuraTarget})");

        // Immediately update this aura to affect nearby entities
        UpdateAura(skill);
    }

    /// <summary>
    /// Updates all active auras, applying/removing effects as needed.
    /// </summary>
    private void UpdateAllAuras()
    {
        foreach (var skillId in _activeAuras.Keys.ToList())
        {
            if (_auraSkillCache.TryGetValue(skillId, out var skill))
            {
                UpdateAura(skill);
            }
        }
    }

    /// <summary>
    /// Updates a single aura, applying effects to entities in range
    /// and removing effects from entities out of range.
    /// </summary>
    private void UpdateAura(SkillDefinition skill)
    {
        if (_entity == null || _entityManager == null) return;

        var currentlyAffected = _activeAuras[skill.Id];
        var shouldBeAffected = GetEntitiesInAuraRange(skill);

        // Find entities that left the aura
        var leftAura = currentlyAffected.Except(shouldBeAffected).ToList();
        foreach (var entityId in leftAura)
        {
            var entity = _entityManager.GetEntityById(entityId);
            if (entity != null)
            {
                RemoveAuraEffectFromEntity(skill, entity);
            }
            currentlyAffected.Remove(entityId);
        }

        // Find entities that entered the aura
        var enteredAura = shouldBeAffected.Except(currentlyAffected).ToList();
        foreach (var entityId in enteredAura)
        {
            var entity = _entityManager.GetEntityById(entityId);
            if (entity != null)
            {
                ApplyAuraEffectToEntity(skill, entity);
                currentlyAffected.Add(entityId);
            }
        }
    }

    /// <summary>
    /// Gets IDs of entities within the aura's range that match its target type.
    /// </summary>
    private HashSet<int> GetEntitiesInAuraRange(SkillDefinition skill)
    {
        var result = new HashSet<int>();
        if (_entity == null || _entityManager == null) return result;

        var entities = _entityManager.GetEntitiesInRadius(_entity.GridPosition, skill.AuraRadius);

        foreach (var entity in entities)
        {
            // Skip self
            if (entity == _entity) continue;

            // Check if entity matches aura target type
            if (IsValidAuraTarget(skill, entity))
            {
                result.Add(entity.GetInstanceId().GetHashCode());
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if an entity is a valid target for the aura.
    /// </summary>
    private bool IsValidAuraTarget(SkillDefinition skill, BaseEntity target)
    {
        if (_entity == null) return false;

        string auraTarget = skill.AuraTarget?.ToLower() ?? "all";

        return auraTarget switch
        {
            "allies" => _entity.Faction.IsFriendlyTo(target.Faction) && target != _entity,
            "enemies" => !_entity.Faction.IsFriendlyTo(target.Faction),
            "all" => target != _entity,
            _ => target != _entity
        };
    }

    /// <summary>
    /// Applies the aura's effects to an entity.
    /// </summary>
    private void ApplyAuraEffectToEntity(SkillDefinition skill, BaseEntity target)
    {
        string source = GetAuraSource(skill.Id);

        foreach (var effect in skill.Effects)
        {
            ApplyAuraEffect(effect, target, source);
        }

        GD.Print($"AuraProcessor: Applied aura '{skill.Name}' to {target.DisplayName}");
    }

    /// <summary>
    /// Removes the aura's effects from an entity.
    /// Uses condition system's source-based removal.
    /// </summary>
    private void RemoveAuraEffectFromEntity(SkillDefinition skill, BaseEntity target)
    {
        string source = GetAuraSource(skill.Id);

        // Remove all conditions from this aura source (handles all stat modifiers)
        target.RemoveConditionsBySource(source);

        GD.Print($"AuraProcessor: Removed aura '{skill.Name}' from {target.DisplayName}");
    }

    /// <summary>
    /// Applies a single aura effect to a target using WhileActive conditions.
    /// </summary>
    private void ApplyAuraEffect(SkillEffectDefinition effect, BaseEntity target, string source)
    {
        switch (effect.Type?.ToLower())
        {
            case "stat_bonus":
                ApplyAuraStatBonus(effect, target, source);
                break;

            case "armor_bonus":
                ApplyAuraCondition(target, "armor_modifier", effect.Amount, source);
                break;

            case "attack_bonus":
                // Apply as strength modifier (affects melee attack)
                ApplyAuraCondition(target, "strength_modifier", effect.Amount, source);
                break;

            case "debuff":
                // Apply negative stat modifier
                ApplyAuraDebuff(effect, target, source);
                break;

            default:
                GD.PushWarning($"AuraProcessor: Unknown aura effect type '{effect.Type}'");
                break;
        }
    }

    /// <summary>
    /// Applies a stat bonus aura effect using WhileActive conditions.
    /// </summary>
    private void ApplyAuraStatBonus(SkillEffectDefinition effect, BaseEntity target, string source)
    {
        string? conditionType = StatConditionMapper.GetConditionType(effect.Stat);

        if (conditionType != null)
        {
            ApplyAuraCondition(target, conditionType, effect.Amount, source);
        }
    }

    /// <summary>
    /// Applies an aura debuff (negative modifier) using WhileActive conditions.
    /// </summary>
    private void ApplyAuraDebuff(SkillEffectDefinition effect, BaseEntity target, string source)
    {
        // Default to attack/strength if no stat specified
        string stat = effect.Stat ?? "attack";
        int amount = -Mathf.Abs(effect.Amount); // Ensure negative

        string? conditionType = StatConditionMapper.GetConditionType(stat);

        if (conditionType != null)
        {
            ApplyAuraCondition(target, conditionType, amount, source);
        }
    }

    /// <summary>
    /// Helper to apply a WhileActive condition from an aura.
    /// </summary>
    private void ApplyAuraCondition(BaseEntity target, string conditionType, int amount, string source)
    {
        var condition = ConditionFactory.Create(
            conditionType,
            amount,
            "1", // Duration doesn't matter for WhileActive
            ConditionDuration.WhileActive,
            source
        );

        if (condition != null)
        {
            target.AddCondition(condition);
        }
    }

    /// <summary>
    /// Removes all aura effects from all affected entities.
    /// </summary>
    private void RemoveAllAuraEffects()
    {
        foreach (var kvp in _activeAuras)
        {
            if (!_auraSkillCache.TryGetValue(kvp.Key, out var skill))
                continue;

            foreach (var entityId in kvp.Value)
            {
                if (_entityManager == null) continue;
                var entity = _entityManager.GetEntityById(entityId);
                if (entity != null)
                {
                    RemoveAuraEffectFromEntity(skill, entity);
                }
            }
        }

        _activeAuras.Clear();
        _auraSkillCache.Clear();
    }

    /// <summary>
    /// Gets the modifier source string for an aura skill.
    /// </summary>
    private string GetAuraSource(string skillId) => $"aura_{skillId}_{_entity?.GetInstanceId()}";
}
