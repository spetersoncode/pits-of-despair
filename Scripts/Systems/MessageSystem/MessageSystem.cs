using Godot;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using System.Collections.Generic;
using System.Linq;

namespace PitsOfDespair.Systems;

/// <summary>
/// Central system for handling all game messages. Connects to combat/health signals,
/// sequences messages in correct narrative order, and outputs to MessageLog UI.
///
/// Responsibilities:
/// - Signal connections to CombatSystem, HealthComponent
/// - Message formatting (weapon colors, damage messages, etc.)
/// - Sequencing and combining same-target messages
/// - Outputting formatted messages to MessageLog
///
/// Usage:
/// 1. TurnManager calls BeginSequence() at turn start
/// 2. Combat events are recorded via signal handlers
/// 3. TurnManager calls EndSequence() to flush messages in correct order
/// </summary>
public partial class MessageSystem : Node
{
    private static readonly string ColorCombatDamage = Palette.ToHex(Palette.CombatDamage);
    private static readonly string ColorCombatBlocked = Palette.ToHex(Palette.CombatBlocked);
    private static readonly string ColorDeath = Palette.ToHex(Palette.HealthMedium);
    private static readonly string ColorDefault = Palette.ToHex(Palette.Default);
    private static readonly string ColorImmunity = Palette.ToHex(Palette.StatusBuff);
    private static readonly string ColorResistance = Palette.ToHex(Palette.CombatBlocked);
    private static readonly string ColorVulnerability = Palette.ToHex(Palette.StatusDebuff);
    private static readonly string ColorDanger = Palette.ToHex(Palette.Danger);
    private static readonly string ColorWizard = Palette.ToHex(Palette.Wizard);

    /// <summary>
    /// Whether message sequencing is currently active.
    /// When false, messages are passed through immediately.
    /// </summary>
    public bool IsSequencing { get; private set; }

    /// <summary>
    /// Combat messages grouped by target entity for combining.
    /// </summary>
    private readonly Dictionary<string, CombatMessageData> _combatMessages = new();

    /// <summary>
    /// Hazard damage messages grouped by target entity for combining with death.
    /// </summary>
    private readonly Dictionary<BaseEntity, HazardMessageData> _hazardMessages = new();

    /// <summary>
    /// Non-combat messages that don't get combined (discovery, status effects, etc.).
    /// </summary>
    private readonly List<(MessagePriority priority, string message, string color)> _genericMessages = new();

    /// <summary>
    /// Tracks last attacker per entity for death message attribution.
    /// </summary>
    private readonly Dictionary<BaseEntity, BaseEntity> _lastAttacker = new();

    /// <summary>
    /// Tracks last damage source name per entity for death messages.
    /// </summary>
    private readonly Dictionary<BaseEntity, string> _lastDamageSourceName = new();

    /// <summary>
    /// Tracks connected health components for cleanup.
    /// </summary>
    private readonly List<(HealthComponent healthComponent, BaseEntity entity)> _healthConnections = new();

    private UI.MessageLog _messageLog;
    private Player _player;
    private EntityManager _entityManager;
    private CombatSystem _combatSystem;

    #region Setup Methods

    /// <summary>
    /// Sets the MessageLog reference for output.
    /// </summary>
    public void SetMessageLog(UI.MessageLog messageLog)
    {
        _messageLog = messageLog;
    }

    /// <summary>
    /// Sets the player reference for weapon display formatting.
    /// </summary>
    public void SetPlayer(Player player)
    {
        _player = player;
    }

    /// <summary>
    /// Sets the EntityManager reference for XP reward lookups.
    /// </summary>
    public void SetEntityManager(EntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Connects to the CombatSystem to receive attack and skill damage events.
    /// </summary>
    public void ConnectToCombatSystem(CombatSystem combatSystem)
    {
        _combatSystem = combatSystem;

        _combatSystem.Connect(CombatSystem.SignalName.AttackHit, Callable.From<BaseEntity, BaseEntity, int, string>(OnAttackHit));
        _combatSystem.Connect(CombatSystem.SignalName.AttackBlocked, Callable.From<BaseEntity, BaseEntity, string>(OnAttackBlocked));
        _combatSystem.Connect(CombatSystem.SignalName.AttackMissed, Callable.From<BaseEntity, BaseEntity, string>(OnAttackMissed));
        _combatSystem.Connect(CombatSystem.SignalName.ActionMessage, Callable.From<BaseEntity, string, string>(OnActionMessage));
        _combatSystem.Connect(CombatSystem.SignalName.SkillDamageDealt, Callable.From<BaseEntity, BaseEntity, int, string>(OnSkillDamageDealt));
    }

    /// <summary>
    /// Connects to an entity's health component to receive death and damage modifier events.
    /// </summary>
    public void ConnectToHealthComponent(HealthComponent healthComponent, BaseEntity entity)
    {
        healthComponent.Connect(HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
        healthComponent.Connect(HealthComponent.SignalName.DamageModifierApplied, Callable.From<int, string>((damageType, modifierType) => OnDamageModifierApplied(entity, damageType, modifierType)));

        _healthConnections.Add((healthComponent, entity));
    }

    /// <summary>
    /// Connects to the TileHazardManager to receive hazard damage events.
    /// </summary>
    public void ConnectToTileHazardManager(TileHazardManager hazardManager)
    {
        hazardManager.Connect(TileHazardManager.SignalName.HazardDamageDealt, Callable.From<BaseEntity, int, string>(OnHazardDamageDealt));
    }

    #endregion

    #region Sequencing Control

    /// <summary>
    /// Begins a new message sequence. Messages will be buffered until EndSequence() is called.
    /// </summary>
    public void BeginSequence()
    {
        IsSequencing = true;
        _combatMessages.Clear();
        _hazardMessages.Clear();
        _genericMessages.Clear();
    }

    /// <summary>
    /// Ends the current sequence, combining and flushing all buffered messages in correct order.
    /// </summary>
    public void EndSequence()
    {
        if (!IsSequencing)
            return;

        IsSequencing = false;
        FlushMessages();
    }

    #endregion

    #region Signal Handlers

    private void OnAttackHit(BaseEntity attacker, BaseEntity target, int damage, string attackName)
    {
        // Track for death message attribution
        _lastAttacker[target] = attacker;
        _lastDamageSourceName[target] = attackName;

        RecordDamage(attacker, target, damage, attackName, isSkill: false);
    }

    private void OnSkillDamageDealt(BaseEntity caster, BaseEntity target, int damage, string skillName)
    {
        // Track for death message attribution
        _lastAttacker[target] = caster;
        _lastDamageSourceName[target] = skillName;

        RecordDamage(caster, target, damage, skillName, isSkill: true);
    }

    private void OnAttackBlocked(BaseEntity attacker, BaseEntity target, string attackName)
    {
        RecordBlocked(attacker, target, attackName);
    }

    private void OnAttackMissed(BaseEntity attacker, BaseEntity target, string attackName)
    {
        RecordMiss(attacker, target, attackName);
    }

    private void OnActionMessage(BaseEntity actor, string message, string color)
    {
        QueueMessage(MessagePriority.Generic, message, color);
    }

    private void OnEntityDied(BaseEntity victim)
    {
        BaseEntity killer = null;
        string sourceName = null;

        if (_lastAttacker.TryGetValue(victim, out var attacker))
        {
            killer = attacker;
            _lastDamageSourceName.TryGetValue(victim, out sourceName);

            // Clean up trackers
            _lastAttacker.Remove(victim);
            _lastDamageSourceName.Remove(victim);
        }

        RecordDeath(victim, killer, sourceName);
    }

    private void OnDamageModifierApplied(BaseEntity target, int damageTypeInt, string modifierType)
    {
        DamageType damageType = (DamageType)damageTypeInt;

        // Try to find who attacked this target for better message combining
        _lastAttacker.TryGetValue(target, out var attacker);
        _lastDamageSourceName.TryGetValue(target, out var sourceName);

        RecordModifier(target, damageType, modifierType, attacker, sourceName);
    }

    private void OnHazardDamageDealt(BaseEntity entity, int damage, string hazardType)
    {
        // Track for death message attribution
        _lastDamageSourceName[entity] = hazardType;

        if (!IsSequencing)
        {
            EmitHazardMessageImmediate(entity, damage, hazardType, targetDied: false);
            return;
        }

        // Record for combining with potential death
        if (!_hazardMessages.TryGetValue(entity, out var data))
        {
            data = new HazardMessageData
            {
                Target = entity,
                HazardType = hazardType
            };
            _hazardMessages[entity] = data;
        }

        data.Damage += damage;
        data.HazardType = hazardType; // Use most recent hazard type
    }

    private string FormatHazardName(string hazardType)
    {
        return hazardType switch
        {
            "poison_cloud" => "poison cloud",
            "fire" => "flames",
            "ice" => "freezing cold",
            "acid" => "acid",
            _ => hazardType.Replace("_", " ")
        };
    }

    #endregion

    #region Record Methods

    /// <summary>
    /// Records a damage event (hit, skill damage).
    /// </summary>
    private void RecordDamage(BaseEntity attacker, BaseEntity target, int damage, string sourceName, bool isSkill)
    {
        if (!IsSequencing)
        {
            EmitDamageMessageImmediate(attacker, target, damage, sourceName, isSkill);
            return;
        }

        var key = GetCombatKey(attacker, target, sourceName);
        if (!_combatMessages.TryGetValue(key, out var data))
        {
            data = new CombatMessageData
            {
                Attacker = attacker,
                Target = target,
                SourceName = sourceName,
                IsSkill = isSkill,
                DamageType = DamageType.Bludgeoning
            };
            _combatMessages[key] = data;
        }

        data.Damage = damage;
    }

    /// <summary>
    /// Records a damage modifier event (vulnerable, resistant, immune).
    /// </summary>
    private void RecordModifier(BaseEntity target, DamageType damageType, string modifierType, BaseEntity attacker = null, string sourceName = null)
    {
        if (!IsSequencing)
        {
            EmitModifierMessageImmediate(target, damageType, modifierType);
            return;
        }

        var matchingKey = FindCombatKeyForTarget(target, attacker, sourceName);
        if (matchingKey != null && _combatMessages.TryGetValue(matchingKey, out var data))
        {
            data.Modifier = modifierType;
            data.DamageType = damageType;
        }
        else
        {
            string message = FormatModifierMessage(target, damageType, modifierType);
            string color = GetModifierColor(target, modifierType);
            _genericMessages.Add((MessagePriority.DamageModifier, message, color));
        }
    }

    /// <summary>
    /// Records a death event.
    /// </summary>
    private void RecordDeath(BaseEntity victim, BaseEntity killer = null, string sourceName = null)
    {
        if (!IsSequencing)
        {
            EmitDeathMessageImmediate(victim, killer, sourceName);
            return;
        }

        // Check if death was from hazard damage
        if (_hazardMessages.TryGetValue(victim, out var hazardData))
        {
            hazardData.TargetDied = true;
            // Player gets XP for hazard kills
            if (_entityManager != null)
            {
                hazardData.XPReward = _entityManager.GetXPReward(victim);
            }
            return;
        }

        var matchingKey = FindCombatKeyForTarget(victim, killer, sourceName);
        if (matchingKey != null && _combatMessages.TryGetValue(matchingKey, out var data))
        {
            data.TargetDied = true;
            if (data.Attacker?.DisplayName == "Player" && _entityManager != null)
            {
                data.XPReward = _entityManager.GetXPReward(victim);
            }
        }
        else
        {
            string message = FormatDeathMessage(victim, killer, sourceName);
            _genericMessages.Add((MessagePriority.Death, message, ColorDeath));

            if (killer?.DisplayName == "Player" && _entityManager != null)
            {
                int xp = _entityManager.GetXPReward(victim);
                if (xp > 0)
                {
                    _genericMessages.Add((MessagePriority.Reward, $"Defeated {victim.DisplayName} for {xp} XP.", ColorDefault));
                }
            }
        }
    }

    /// <summary>
    /// Records a blocked attack (hit but 0 damage due to armor).
    /// </summary>
    private void RecordBlocked(BaseEntity attacker, BaseEntity target, string sourceName)
    {
        if (!IsSequencing)
        {
            EmitBlockedMessageImmediate(attacker, target, sourceName);
            return;
        }

        var key = GetCombatKey(attacker, target, sourceName);
        if (!_combatMessages.TryGetValue(key, out var data))
        {
            data = new CombatMessageData
            {
                Attacker = attacker,
                Target = target,
                SourceName = sourceName
            };
            _combatMessages[key] = data;
        }

        data.WasBlocked = true;
    }

    /// <summary>
    /// Records a missed attack.
    /// </summary>
    private void RecordMiss(BaseEntity attacker, BaseEntity target, string sourceName)
    {
        if (!IsSequencing)
        {
            EmitMissMessageImmediate(attacker, target);
            return;
        }

        var key = GetCombatKey(attacker, target, sourceName);
        if (!_combatMessages.TryGetValue(key, out var data))
        {
            data = new CombatMessageData
            {
                Attacker = attacker,
                Target = target,
                SourceName = sourceName
            };
            _combatMessages[key] = data;
        }

        data.WasMissed = true;
    }

    /// <summary>
    /// Queues a generic message with the specified priority.
    /// </summary>
    public void QueueMessage(MessagePriority priority, string message, string color = null)
    {
        color ??= ColorDefault;

        if (!IsSequencing)
        {
            _messageLog?.AddMessage(message, color);
            return;
        }

        _genericMessages.Add((priority, message, color));
    }

    /// <summary>
    /// Adds a message directly to the log, bypassing sequencing.
    /// Use for messages that should always appear immediately (e.g., system messages).
    /// </summary>
    public void AddMessageImmediate(string message, string color = null)
    {
        _messageLog?.AddMessage(message, color ?? ColorDefault);
    }

    #endregion

    #region Flush & Format

    private void FlushMessages()
    {
        if (_messageLog == null)
            return;

        var allMessages = new List<(MessagePriority priority, string message, string color)>();

        allMessages.AddRange(_genericMessages);

        foreach (var data in _combatMessages.Values)
        {
            var (message, color) = FormatCombinedCombatMessage(data);
            allMessages.Add((MessagePriority.ActionDamage, message, color));

            if (data.TargetDied && data.XPReward > 0)
            {
                allMessages.Add((MessagePriority.Reward, $"Defeated {data.Target.DisplayName} for {data.XPReward} XP.", ColorDefault));
            }
        }

        // Add hazard messages
        foreach (var data in _hazardMessages.Values)
        {
            var (message, color) = FormatCombinedHazardMessage(data);
            allMessages.Add((MessagePriority.ActionDamage, message, color));

            if (data.TargetDied && data.XPReward > 0)
            {
                allMessages.Add((MessagePriority.Reward, $"Defeated {data.Target.DisplayName} for {data.XPReward} XP.", ColorDefault));
            }
        }

        foreach (var (_, message, color) in allMessages.OrderBy(m => m.priority))
        {
            _messageLog.AddMessage(message, color);
        }

        _combatMessages.Clear();
        _hazardMessages.Clear();
        _genericMessages.Clear();
    }

    private (string message, string color) FormatCombinedCombatMessage(CombatMessageData data)
    {
        bool isPlayerAttacker = data.Attacker?.DisplayName == "Player";
        bool isPlayerTarget = data.Target?.DisplayName == "Player";
        string color = isPlayerTarget ? ColorCombatDamage : ColorDefault;

        // Handle miss
        if (data.WasMissed)
        {
            string missMsg = isPlayerAttacker
                ? $"You miss the {data.Target.DisplayName}."
                : isPlayerTarget
                    ? $"The {data.Attacker.DisplayName} misses you."
                    : $"The {data.Attacker.DisplayName} misses the {data.Target.DisplayName}.";
            return (missMsg, ColorDefault);
        }

        // Handle blocked
        if (data.WasBlocked)
        {
            string weaponDisplay = GetWeaponDisplay(data.Attacker, data.SourceName);
            color = isPlayerTarget ? ColorCombatBlocked : ColorDefault;
            string blockMsg = isPlayerAttacker
                ? $"You hit the {data.Target.DisplayName} with your {weaponDisplay} but it glances off their armor!"
                : isPlayerTarget
                    ? $"The {data.Attacker.DisplayName} hits you with its {weaponDisplay} but it bounces off your armor!"
                    : $"The {data.Attacker.DisplayName} hits the {data.Target.DisplayName} with its {weaponDisplay} but it glances off armor!";
            return (blockMsg, color);
        }

        // Build combined damage message
        string sourceDisplay = data.IsSkill
            ? $"[color={ColorWizard}]{data.SourceName}[/color]"
            : GetWeaponDisplay(data.Attacker, data.SourceName);

        string message;
        if (isPlayerAttacker)
        {
            message = data.IsSkill
                ? $"Your {sourceDisplay} hits the {data.Target.DisplayName} for {data.Damage} damage"
                : $"You hit the {data.Target.DisplayName} with your {sourceDisplay} for {data.Damage} damage";
        }
        else if (isPlayerTarget)
        {
            message = data.IsSkill
                ? $"The {data.Attacker.DisplayName}'s {sourceDisplay} hits you for {data.Damage} damage"
                : $"The {data.Attacker.DisplayName} hits you with its {sourceDisplay} for {data.Damage} damage";
        }
        else
        {
            message = data.IsSkill
                ? $"The {data.Attacker.DisplayName}'s {sourceDisplay} hits the {data.Target.DisplayName} for {data.Damage} damage"
                : $"The {data.Attacker.DisplayName} hits the {data.Target.DisplayName} with its {sourceDisplay} for {data.Damage} damage";
        }

        // Add modifier if present
        if (!string.IsNullOrEmpty(data.Modifier))
        {
            string damageTypeName = data.DamageType.ToString().ToLower();
            message += $" ({data.Modifier} to {damageTypeName})";
        }

        // Add death if applicable
        if (data.TargetDied)
        {
            message += ", killing it!";
        }
        else
        {
            message += ".";
        }

        return (message, color);
    }

    private (string message, string color) FormatCombinedHazardMessage(HazardMessageData data)
    {
        bool isPlayer = data.Target?.DisplayName == "Player";
        string hazardName = FormatHazardName(data.HazardType);
        string color = isPlayer ? ColorCombatDamage : ColorDefault;

        string message = isPlayer
            ? $"You take {data.Damage} damage from the {hazardName}"
            : $"The {data.Target.DisplayName} takes {data.Damage} damage from the {hazardName}";

        if (data.TargetDied)
        {
            message += isPlayer ? ", killing you!" : ", killing it!";
        }
        else
        {
            message += isPlayer ? "!" : ".";
        }

        return (message, color);
    }

    #endregion

    #region Helper Methods

    private string GetCombatKey(BaseEntity attacker, BaseEntity target, string sourceName)
    {
        return $"{attacker?.GetInstanceId()}_{target?.GetInstanceId()}_{sourceName}";
    }

    private string FindCombatKeyForTarget(BaseEntity target, BaseEntity attacker = null, string sourceName = null)
    {
        if (attacker != null && sourceName != null)
        {
            var exactKey = GetCombatKey(attacker, target, sourceName);
            if (_combatMessages.ContainsKey(exactKey))
                return exactKey;
        }

        foreach (var kvp in _combatMessages)
        {
            if (kvp.Value.Target == target)
                return kvp.Key;
        }

        return null;
    }

    private string GetWeaponDisplay(BaseEntity attacker, string attackName)
    {
        if (attacker?.DisplayName != "Player" || _player == null)
            return attackName;

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null)
            return attackName;

        var meleeKey = equipComponent.GetEquippedKey(EquipmentSlot.MeleeWeapon);
        if (meleeKey.HasValue)
        {
            var slot = _player.GetInventorySlot(meleeKey.Value);
            if (slot != null && slot.Item.Template.Name == attackName)
            {
                var itemColor = slot.Item.Template.GetColor();
                string colorHex = $"#{(int)(itemColor.R * 255):X2}{(int)(itemColor.G * 255):X2}{(int)(itemColor.B * 255):X2}";
                return $"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]";
            }
        }

        var rangedKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
        if (rangedKey.HasValue)
        {
            var slot = _player.GetInventorySlot(rangedKey.Value);
            if (slot != null && slot.Item.Template.Name == attackName)
            {
                var itemColor = slot.Item.Template.GetColor();
                string colorHex = $"#{(int)(itemColor.R * 255):X2}{(int)(itemColor.G * 255):X2}{(int)(itemColor.B * 255):X2}";
                return $"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]";
            }
        }

        return attackName;
    }

    private string FormatModifierMessage(BaseEntity target, DamageType damageType, string modifierType)
    {
        bool isPlayer = target.DisplayName == "Player";
        string damageTypeName = damageType.ToString().ToLower();

        return modifierType switch
        {
            "immune" => isPlayer
                ? $"You are immune to {damageTypeName} damage!"
                : $"The {target.DisplayName} is immune to {damageTypeName} damage!",
            "resisted" => isPlayer
                ? $"You resist the {damageTypeName} damage!"
                : $"The {target.DisplayName} resists the {damageTypeName} damage!",
            "vulnerable" => isPlayer
                ? $"You are vulnerable to {damageTypeName} damage!"
                : $"The {target.DisplayName} is vulnerable to {damageTypeName} damage!",
            _ => ""
        };
    }

    private string GetModifierColor(BaseEntity target, string modifierType)
    {
        bool isPlayer = target.DisplayName == "Player";
        return modifierType switch
        {
            "immune" => ColorImmunity,
            "resisted" => ColorResistance,
            "vulnerable" => isPlayer ? ColorDanger : ColorVulnerability,
            _ => ColorDefault
        };
    }

    private string FormatDeathMessage(BaseEntity victim, BaseEntity killer, string sourceName)
    {
        bool isPlayer = victim.DisplayName == "Player";

        if (killer != null)
        {
            bool killerIsPlayer = killer.DisplayName == "Player";
            if (killerIsPlayer)
                return $"You kill the {victim.DisplayName}!";
            else if (isPlayer)
                return $"The {killer.DisplayName} kills you!";
            else
                return $"The {killer.DisplayName} kills the {victim.DisplayName}!";
        }

        return $"{victim.DisplayName} dies!";
    }

    #endregion

    #region Immediate Message Fallbacks

    private void EmitDamageMessageImmediate(BaseEntity attacker, BaseEntity target, int damage, string sourceName, bool isSkill)
    {
        if (_messageLog == null) return;

        bool isPlayerAttacker = attacker?.DisplayName == "Player";
        bool isPlayerTarget = target?.DisplayName == "Player";
        string color = isPlayerTarget ? ColorCombatDamage : ColorDefault;

        string sourceDisplay = isSkill
            ? $"[color={ColorWizard}]{sourceName}[/color]"
            : GetWeaponDisplay(attacker, sourceName);

        string message;
        if (isPlayerAttacker)
        {
            message = isSkill
                ? $"Your {sourceDisplay} hits the {target.DisplayName} for {damage} damage."
                : $"You hit the {target.DisplayName} with your {sourceDisplay} for {damage} damage.";
        }
        else if (isPlayerTarget)
        {
            message = isSkill
                ? $"The {attacker.DisplayName}'s {sourceDisplay} hits you for {damage} damage."
                : $"The {attacker.DisplayName} hits you with its {sourceDisplay} for {damage} damage.";
        }
        else
        {
            message = isSkill
                ? $"The {attacker.DisplayName}'s {sourceDisplay} hits the {target.DisplayName} for {damage} damage."
                : $"The {attacker.DisplayName} hits the {target.DisplayName} with its {sourceDisplay} for {damage} damage.";
        }

        _messageLog.AddMessage(message, color);
    }

    private void EmitModifierMessageImmediate(BaseEntity target, DamageType damageType, string modifierType)
    {
        if (_messageLog == null) return;

        string message = FormatModifierMessage(target, damageType, modifierType);
        string color = GetModifierColor(target, modifierType);
        _messageLog.AddMessage(message, color);
    }

    private void EmitDeathMessageImmediate(BaseEntity victim, BaseEntity killer, string sourceName)
    {
        if (_messageLog == null) return;

        string message = FormatDeathMessage(victim, killer, sourceName);
        _messageLog.AddMessage(message, ColorDeath);

        if (killer?.DisplayName == "Player" && _entityManager != null)
        {
            int xp = _entityManager.GetXPReward(victim);
            if (xp > 0)
            {
                _messageLog.AddMessage($"Defeated {victim.DisplayName} for {xp} XP.", ColorDefault);
            }
        }
    }

    private void EmitBlockedMessageImmediate(BaseEntity attacker, BaseEntity target, string sourceName)
    {
        if (_messageLog == null) return;

        bool isPlayerAttacker = attacker?.DisplayName == "Player";
        bool isPlayerTarget = target?.DisplayName == "Player";
        string weaponDisplay = GetWeaponDisplay(attacker, sourceName);
        string color = isPlayerTarget ? ColorCombatBlocked : ColorDefault;

        string message = isPlayerAttacker
            ? $"You hit the {target.DisplayName} with your {weaponDisplay} but it glances off their armor!"
            : isPlayerTarget
                ? $"The {attacker.DisplayName} hits you with its {weaponDisplay} but it bounces off your armor!"
                : $"The {attacker.DisplayName} hits the {target.DisplayName} with its {weaponDisplay} but it glances off armor!";

        _messageLog.AddMessage(message, color);
    }

    private void EmitMissMessageImmediate(BaseEntity attacker, BaseEntity target)
    {
        if (_messageLog == null) return;

        bool isPlayerAttacker = attacker?.DisplayName == "Player";
        bool isPlayerTarget = target?.DisplayName == "Player";

        string message = isPlayerAttacker
            ? $"You miss the {target.DisplayName}."
            : isPlayerTarget
                ? $"The {attacker.DisplayName} misses you."
                : $"The {attacker.DisplayName} misses the {target.DisplayName}.";

        _messageLog.AddMessage(message, ColorDefault);
    }

    private void EmitHazardMessageImmediate(BaseEntity entity, int damage, string hazardType, bool targetDied)
    {
        if (_messageLog == null) return;

        bool isPlayer = entity.DisplayName == "Player";
        string hazardName = FormatHazardName(hazardType);
        string color = isPlayer ? ColorCombatDamage : ColorDefault;

        string message = isPlayer
            ? $"You take {damage} damage from the {hazardName}"
            : $"The {entity.DisplayName} takes {damage} damage from the {hazardName}";

        if (targetDied)
        {
            message += isPlayer ? ", killing you!" : ", killing it!";
        }
        else
        {
            message += isPlayer ? "!" : ".";
        }

        _messageLog.AddMessage(message, color);
    }

    #endregion

    #region Cleanup

    public override void _ExitTree()
    {
        // Disconnect from combat system
        if (_combatSystem != null)
        {
            _combatSystem.Disconnect(CombatSystem.SignalName.AttackHit, Callable.From<BaseEntity, BaseEntity, int, string>(OnAttackHit));
            _combatSystem.Disconnect(CombatSystem.SignalName.AttackBlocked, Callable.From<BaseEntity, BaseEntity, string>(OnAttackBlocked));
            _combatSystem.Disconnect(CombatSystem.SignalName.AttackMissed, Callable.From<BaseEntity, BaseEntity, string>(OnAttackMissed));
            _combatSystem.Disconnect(CombatSystem.SignalName.ActionMessage, Callable.From<BaseEntity, string, string>(OnActionMessage));
            _combatSystem.Disconnect(CombatSystem.SignalName.SkillDamageDealt, Callable.From<BaseEntity, BaseEntity, int, string>(OnSkillDamageDealt));
        }

        // Disconnect from all health components
        foreach (var (healthComponent, entity) in _healthConnections)
        {
            if (healthComponent != null && GodotObject.IsInstanceValid(healthComponent))
            {
                healthComponent.Disconnect(HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
                healthComponent.Disconnect(HealthComponent.SignalName.DamageModifierApplied, Callable.From<int, string>((damageType, modifierType) => OnDamageModifierApplied(entity, damageType, modifierType)));
            }
        }
        _healthConnections.Clear();
    }

    #endregion
}
