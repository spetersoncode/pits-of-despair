using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using System.Collections.Generic;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a scrolling log of combat messages and game events.
/// </summary>
public partial class MessageLog : PanelContainer
{
	private const int MaxMessages = 100;
	private static readonly string ColorCombatDamage = Palette.ToHex(Palette.CombatDamage);
	private static readonly string ColorCombatBlocked = Palette.ToHex(Palette.CombatBlocked);
	private static readonly string ColorDeath = Palette.ToHex(Palette.HealthMedium);
	private static readonly string ColorDefault = Palette.ToHex(Palette.Default);
	private static readonly string ColorImmunity = Palette.ToHex(Palette.StatusBuff);
	private static readonly string ColorResistance = Palette.ToHex(Palette.CombatBlocked);
	private static readonly string ColorVulnerability = Palette.ToHex(Palette.StatusDebuff);
	private static readonly string ColorDanger = Palette.ToHex(Palette.Danger);

	private RichTextLabel _logLabel;
	private readonly Queue<string> _messageHistory = new();
	private Entities.Player _player;
	private Systems.EntityManager _entityManager;
	private Systems.CombatSystem _combatSystem;
	private readonly Dictionary<Entities.BaseEntity, Entities.BaseEntity> _lastAttacker = new();
	private readonly Dictionary<Entities.BaseEntity, string> _lastDamageSourceName = new();
	private readonly System.Collections.Generic.List<(Components.HealthComponent healthComponent, Entities.BaseEntity entity)> _healthConnections = new();

	// Message coalescing - track the last raw message and its repeat count
	private string? _lastRawMessage = null;
	private string? _lastMessageColor = null;
	private int _lastMessageCount = 0;

	public override void _Ready()
	{
		_logLabel = GetNode<RichTextLabel>("MarginContainer/LogLabel");
		_logLabel.BbcodeEnabled = true;
		_logLabel.ScrollFollowing = true;
	}

	/// <summary>
	/// Connects to the CombatSystem to receive attack events, action messages, and skill damage events.
	/// </summary>
	public void ConnectToCombatSystem(Systems.CombatSystem combatSystem)
	{
		_combatSystem = combatSystem;

		// Connect to detailed combat signals
		_combatSystem.Connect(Systems.CombatSystem.SignalName.AttackHit, Callable.From<Entities.BaseEntity, Entities.BaseEntity, int, string>(OnAttackHit));
		_combatSystem.Connect(Systems.CombatSystem.SignalName.AttackBlocked, Callable.From<Entities.BaseEntity, Entities.BaseEntity, string>(OnAttackBlocked));
		_combatSystem.Connect(Systems.CombatSystem.SignalName.AttackMissed, Callable.From<Entities.BaseEntity, Entities.BaseEntity, string>(OnAttackMissed));
		_combatSystem.Connect(Systems.CombatSystem.SignalName.ActionMessage, Callable.From<Entities.BaseEntity, string, string>(OnActionMessage));
		_combatSystem.Connect(Systems.CombatSystem.SignalName.SkillDamageDealt, Callable.From<Entities.BaseEntity, Entities.BaseEntity, int, string>(OnSkillDamageDealt));
	}

	/// <summary>
	/// Sets the player reference for looking up weapon information.
	/// </summary>
	public void SetPlayer(Entities.Player player)
	{
		_player = player;
	}

	/// <summary>
	/// Sets the EntityManager reference for XP reward lookups.
	/// </summary>
	public void SetEntityManager(Systems.EntityManager entityManager)
	{
		_entityManager = entityManager;
	}

	/// <summary>
	/// Connects to an entity's health component to receive death notifications and damage modifier events.
	/// </summary>
	public void ConnectToHealthComponent(Components.HealthComponent healthComponent, Entities.BaseEntity entity)
	{
		healthComponent.Connect(Components.HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
		healthComponent.Connect(Components.HealthComponent.SignalName.DamageModifierApplied, Callable.From<int, string>((damageType, modifierType) => OnDamageModifierApplied(entity, damageType, modifierType)));

		// Track this connection for cleanup
		_healthConnections.Add((healthComponent, entity));
	}

	/// <summary>
	/// Adds a message to the log with optional color.
	/// Consecutive identical messages are coalesced (e.g., "You wait. x3").
	/// </summary>
	public void AddMessage(string message, string? color = null)
	{
		color ??= ColorDefault;

		// Check if this is a repeat of the last message
		if (message == _lastRawMessage && color == _lastMessageColor && _messageHistory.Count > 0)
		{
			_lastMessageCount++;
			// Update the last message in the queue with the new count
			UpdateLastMessageWithCount();
			UpdateDisplay();
			return;
		}

		// New message - reset coalescing state
		_lastRawMessage = message;
		_lastMessageColor = color;
		_lastMessageCount = 1;

		var coloredMessage = $"[color={color}]{message}[/color]";

		_messageHistory.Enqueue(coloredMessage);

		// Keep only the last MaxMessages
		while (_messageHistory.Count > MaxMessages)
		{
			_messageHistory.Dequeue();
		}

		UpdateDisplay();
	}

	/// <summary>
	/// Updates the last message in the queue with the current repeat count.
	/// </summary>
	private void UpdateLastMessageWithCount()
	{
		if (_lastRawMessage == null || _lastMessageColor == null || _messageHistory.Count == 0)
			return;

		// Convert queue to array, modify last element, rebuild queue
		var messages = _messageHistory.ToArray();
		var countSuffix = $" [color={Palette.ToHex(Palette.Disabled)}]x{_lastMessageCount}[/color]";
		messages[messages.Length - 1] = $"[color={_lastMessageColor}]{_lastRawMessage}[/color]{countSuffix}";

		_messageHistory.Clear();
		foreach (var msg in messages)
		{
			_messageHistory.Enqueue(msg);
		}
	}

	/// <summary>
	/// Handles successful attacks that deal damage.
	/// Displays single-line format with damage.
	/// </summary>
	private void OnAttackHit(Entities.BaseEntity attacker, Entities.BaseEntity target, int damage, string attackName)
	{
		// Track the last attacker and weapon name for death messages
		_lastAttacker[target] = attacker;
		_lastDamageSourceName[target] = attackName;

		// Determine if this is the player taking or dealing damage
		bool isPlayerAttacker = attacker.DisplayName == "Player";
		bool isPlayerTarget = target.DisplayName == "Player";
		string color = isPlayerTarget ? ColorCombatDamage : ColorDefault;

		// Get colored weapon name for player attacks
		string weaponDisplay = GetWeaponDisplay(attacker, attackName);

		if (isPlayerAttacker)
		{
			AddMessage($"You hit the {target.DisplayName} with your {weaponDisplay} for {damage} damage.", color);
		}
		else if (isPlayerTarget)
		{
			AddMessage($"The {attacker.DisplayName} hits you with its {weaponDisplay} for {damage} damage.", color);
		}
		else
		{
			// NPC vs NPC combat
			AddMessage($"The {attacker.DisplayName} hits the {target.DisplayName} with its {weaponDisplay} for {damage} damage.", color);
		}
	}

	/// <summary>
	/// Handles skill damage dealt by projectiles (e.g., Magic Missile).
	/// </summary>
	private void OnSkillDamageDealt(Entities.BaseEntity caster, Entities.BaseEntity target, int damage, string skillName)
	{
		// Track the last attacker and skill name for death messages
		_lastAttacker[target] = caster;
		_lastDamageSourceName[target] = skillName;

		bool isCasterPlayer = caster.DisplayName == "Player";
		bool isTargetPlayer = target.DisplayName == "Player";
		string color = isTargetPlayer ? ColorCombatDamage : ColorDefault;

		// Color the skill name in wizard color
		string skillDisplay = $"[color={Palette.ToHex(Palette.Wizard)}]{skillName}[/color]";

		if (isCasterPlayer)
		{
			AddMessage($"Your {skillDisplay} hits the {target.DisplayName} for {damage} damage.", color);
		}
		else if (isTargetPlayer)
		{
			AddMessage($"The {caster.DisplayName}'s {skillDisplay} hits you for {damage} damage.", color);
		}
		else
		{
			AddMessage($"The {caster.DisplayName}'s {skillDisplay} hits the {target.DisplayName} for {damage} damage.", color);
		}
	}

	/// <summary>
	/// Handles attacks that hit but deal no damage due to armor.
	/// Displays single-line format indicating armor absorption.
	/// </summary>
	private void OnAttackBlocked(Entities.BaseEntity attacker, Entities.BaseEntity target, string attackName)
	{
		bool isPlayerAttacker = attacker.DisplayName == "Player";
		bool isPlayerTarget = target.DisplayName == "Player";
		string color = isPlayerTarget ? ColorCombatBlocked : ColorDefault;

		// Get colored weapon name for player attacks
		string weaponDisplay = GetWeaponDisplay(attacker, attackName);

		if (isPlayerAttacker)
		{
			AddMessage($"You hit the {target.DisplayName} with your {weaponDisplay} but it glances off their armor!", color);
		}
		else if (isPlayerTarget)
		{
			AddMessage($"The {attacker.DisplayName} hits you with its {weaponDisplay} but it bounces off your armor!", color);
		}
		else
		{
			AddMessage($"The {attacker.DisplayName} hits the {target.DisplayName} with its {weaponDisplay} but it glances off armor!", color);
		}
	}

	/// <summary>
	/// Handles missed attacks.
	/// Displays single-line format based on perspective.
	/// </summary>
	private void OnAttackMissed(Entities.BaseEntity attacker, Entities.BaseEntity target, string attackName)
	{
		bool isPlayerAttacker = attacker.DisplayName == "Player";
		bool isPlayerTarget = target.DisplayName == "Player";

		if (isPlayerAttacker)
		{
			AddMessage($"You miss the {target.DisplayName}.", ColorDefault);
		}
		else if (isPlayerTarget)
		{
			AddMessage($"The {attacker.DisplayName} misses you.", ColorDefault);
		}
		else
		{
			AddMessage($"The {attacker.DisplayName} misses the {target.DisplayName}.", ColorDefault);
		}
	}

	private void OnActionMessage(Entities.BaseEntity actor, string message, string color)
	{
		AddMessage(message, color);
	}

	private void OnEntityDied(Entities.BaseEntity victim)
	{
		string entityName = victim.DisplayName;
		bool isPlayer = entityName == "Player";

		// Check if we know who killed them
		if (_lastAttacker.TryGetValue(victim, out var killer))
		{
			bool killerIsPlayer = killer.DisplayName == "Player";

			// Get the weapon/skill name if available
			string? sourceDisplay = null;
			if (_lastDamageSourceName.TryGetValue(victim, out var sourceName))
			{
				sourceDisplay = GetColoredSourceDisplay(killer, sourceName);
			}

			if (killerIsPlayer)
			{
				// Player killed someone
				string killMessage = sourceDisplay != null
					? $"You kill the {entityName} with {sourceDisplay}!"
					: $"You kill the {entityName}!";
				AddMessage(killMessage, ColorDeath);

				// Add XP message if applicable
				if (_entityManager != null)
				{
					int xpReward = _entityManager.GetXPReward(victim);
					if (xpReward > 0)
					{
						AddMessage($"Defeated {entityName} for {xpReward} XP.", ColorDefault);
					}
				}
			}
			else if (isPlayer)
			{
				// Player was killed by something
				string killMessage = sourceDisplay != null
					? $"The {killer.DisplayName} kills you with {sourceDisplay}!"
					: $"The {killer.DisplayName} kills you!";
				AddMessage(killMessage, ColorDeath);
			}
			else
			{
				// NPC vs NPC
				string killMessage = sourceDisplay != null
					? $"The {killer.DisplayName} kills the {entityName} with {sourceDisplay}!"
					: $"The {killer.DisplayName} kills the {entityName}!";
				AddMessage(killMessage, ColorDeath);
			}

			// Clean up the trackers
			_lastAttacker.Remove(victim);
			_lastDamageSourceName.Remove(victim);
		}
		else
		{
			// Fallback for unknown cause of death
			AddMessage($"{entityName} dies!", ColorDeath);
		}
	}

	/// <summary>
	/// Gets a colored display string for the damage source (weapon or skill).
	/// For player weapons, includes glyph and item color.
	/// For skills, uses wizard color.
	/// For NPC attacks, returns plain name.
	/// </summary>
	private string GetColoredSourceDisplay(Entities.BaseEntity attacker, string sourceName)
	{
		// Only colorize player weapons/skills
		if (attacker.DisplayName != "Player" || _player == null)
		{
			return sourceName;
		}

		// Try to find a matching equipped weapon
		var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent != null)
		{
			// Check melee weapon
			var meleeKey = equipComponent.GetEquippedKey(EquipmentSlot.MeleeWeapon);
			if (meleeKey.HasValue)
			{
				var slot = _player.GetInventorySlot(meleeKey.Value);
				if (slot != null && slot.Item.Template.Name == sourceName)
				{
					var color = slot.Item.Template.GetColor();
					string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
					return $"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]";
				}
			}

			// Check ranged weapon
			var rangedKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
			if (rangedKey.HasValue)
			{
				var slot = _player.GetInventorySlot(rangedKey.Value);
				if (slot != null && slot.Item.Template.Name == sourceName)
				{
					var color = slot.Item.Template.GetColor();
					string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
					return $"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]";
				}
			}
		}

		// Not a weapon - assume it's a skill, use wizard color
		return $"[color={Palette.ToHex(Palette.Wizard)}]{sourceName}[/color]";
	}

	/// <summary>
	/// Handles damage modifier applications (immunity, resistance, vulnerability).
	/// </summary>
	private void OnDamageModifierApplied(Entities.BaseEntity target, int damageTypeInt, string modifierType)
	{
		DamageType damageType = (DamageType)damageTypeInt;
		bool isPlayer = target.DisplayName == "Player";
		string damageTypeName = damageType.ToString().ToLower();

		string message;
		string color;

		switch (modifierType)
		{
			case "immune":
				message = isPlayer
					? $"You are immune to {damageTypeName} damage!"
					: $"The {target.DisplayName} is immune to {damageTypeName} damage!";
				color = ColorImmunity;
				break;

			case "resisted":
				message = isPlayer
					? $"You resist the {damageTypeName} damage!"
					: $"The {target.DisplayName} resists the {damageTypeName} damage!";
				color = ColorResistance;
				break;

			case "vulnerable":
				message = isPlayer
					? $"You are vulnerable to {damageTypeName} damage!"
					: $"The {target.DisplayName} is vulnerable to {damageTypeName} damage!";
				color = isPlayer ? ColorDanger : ColorVulnerability;
				break;

			default:
				return;
		}

		AddMessage(message, color);
	}

	/// <summary>
	/// Gets a colored weapon display string with glyph for player attacks.
	/// For NPCs, returns the plain attack name.
	/// </summary>
	private string GetWeaponDisplay(Entities.BaseEntity attacker, string attackName)
	{
		// Only colorize player weapons
		if (attacker.DisplayName != "Player" || _player == null)
		{
			return attackName;
		}

		var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
		if (equipComponent == null)
		{
			return attackName;
		}

		// Try to find the equipped weapon that matches this attack
		// Check melee weapon first
		var meleeKey = equipComponent.GetEquippedKey(EquipmentSlot.MeleeWeapon);
		if (meleeKey.HasValue)
		{
			var slot = _player.GetInventorySlot(meleeKey.Value);
			if (slot != null && slot.Item.Template.Name == attackName)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				return $"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]";
			}
		}

		// Check ranged weapon
		var rangedKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
		if (rangedKey.HasValue)
		{
			var slot = _player.GetInventorySlot(rangedKey.Value);
			if (slot != null && slot.Item.Template.Name == attackName)
			{
				var color = slot.Item.Template.GetColor();
				string colorHex = $"#{(int)(color.R * 255):X2}{(int)(color.G * 255):X2}{(int)(color.B * 255):X2}";
				return $"[color={colorHex}]{slot.Item.Template.GetGlyph()} {slot.Item.Template.Name}[/color]";
			}
		}

		// Not a weapon attack (natural attack like "punch"), return plain name
		return attackName;
	}

	private void UpdateDisplay()
	{
		_logLabel.Clear();
		foreach (var message in _messageHistory)
		{
			_logLabel.AppendText(message + "\n");
		}
	}

	public override void _ExitTree()
	{
		// Disconnect from combat system
		if (_combatSystem != null)
		{
			_combatSystem.Disconnect(Systems.CombatSystem.SignalName.AttackHit, Callable.From<Entities.BaseEntity, Entities.BaseEntity, int, string>(OnAttackHit));
			_combatSystem.Disconnect(Systems.CombatSystem.SignalName.AttackBlocked, Callable.From<Entities.BaseEntity, Entities.BaseEntity, string>(OnAttackBlocked));
			_combatSystem.Disconnect(Systems.CombatSystem.SignalName.AttackMissed, Callable.From<Entities.BaseEntity, Entities.BaseEntity, string>(OnAttackMissed));
			_combatSystem.Disconnect(Systems.CombatSystem.SignalName.ActionMessage, Callable.From<Entities.BaseEntity, string, string>(OnActionMessage));
			_combatSystem.Disconnect(Systems.CombatSystem.SignalName.SkillDamageDealt, Callable.From<Entities.BaseEntity, Entities.BaseEntity, int, string>(OnSkillDamageDealt));
		}

		// Disconnect from all health components
		foreach (var (healthComponent, entity) in _healthConnections)
		{
			if (healthComponent != null && GodotObject.IsInstanceValid(healthComponent))
			{
				healthComponent.Disconnect(Components.HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entity)));
				healthComponent.Disconnect(Components.HealthComponent.SignalName.DamageModifierApplied, Callable.From<int, string>((damageType, modifierType) => OnDamageModifierApplied(entity, damageType, modifierType)));
			}
		}
		_healthConnections.Clear();
	}
}
