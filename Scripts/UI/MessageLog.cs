using Godot;
using PitsOfDespair.Core;
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
	private static readonly string ColorDamageTaken = Palette.ToHex(Palette.HealthCritical);
	private static readonly string ColorDeath = Palette.ToHex(Palette.HealthMedium);
	private static readonly string ColorDefault = Palette.ToHex(Palette.Default);

	private RichTextLabel _logLabel;
	private readonly Queue<string> _messageHistory = new();
	private Entities.Player _player;

	public override void _Ready()
	{
		_logLabel = GetNode<RichTextLabel>("MarginContainer/LogLabel");
		_logLabel.BbcodeEnabled = true;
		_logLabel.ScrollFollowing = true;
	}

	/// <summary>
	/// Connects to the CombatSystem to receive attack events and action messages.
	/// </summary>
	public void ConnectToCombatSystem(Systems.CombatSystem combatSystem)
	{
		// Connect to detailed combat signals
		combatSystem.Connect(Systems.CombatSystem.SignalName.AttackHit, Callable.From<Entities.BaseEntity, Entities.BaseEntity, int, string>(OnAttackHit));
		combatSystem.Connect(Systems.CombatSystem.SignalName.AttackBlocked, Callable.From<Entities.BaseEntity, Entities.BaseEntity, string>(OnAttackBlocked));
		combatSystem.Connect(Systems.CombatSystem.SignalName.AttackMissed, Callable.From<Entities.BaseEntity, Entities.BaseEntity, string>(OnAttackMissed));

		combatSystem.Connect(Systems.CombatSystem.SignalName.ActionMessage, Callable.From<Entities.BaseEntity, string, string>(OnActionMessage));
	}

	/// <summary>
	/// Sets the player reference for looking up weapon information.
	/// </summary>
	public void SetPlayer(Entities.Player player)
	{
		_player = player;
	}

	/// <summary>
	/// Connects to an entity's health component to receive death notifications.
	/// </summary>
	public void ConnectToHealthComponent(Components.HealthComponent healthComponent, string entityName)
	{
		healthComponent.Connect(Components.HealthComponent.SignalName.Died, Callable.From(() => OnEntityDied(entityName)));
	}

	/// <summary>
	/// Adds a message to the log with optional color.
	/// </summary>
	public void AddMessage(string message, string? color = null)
	{
		var coloredMessage = $"[color={color ?? ColorDefault}]{message}[/color]";

		_messageHistory.Enqueue(coloredMessage);

		// Keep only the last MaxMessages
		while (_messageHistory.Count > MaxMessages)
		{
			_messageHistory.Dequeue();
		}

		UpdateDisplay();
	}

	/// <summary>
	/// Handles successful attacks that deal damage.
	/// Displays single-line format with damage.
	/// </summary>
	private void OnAttackHit(Entities.BaseEntity attacker, Entities.BaseEntity target, int damage, string attackName)
	{
		// Determine if this is the player taking or dealing damage
		bool isPlayerAttacker = attacker.DisplayName == "Player";
		bool isPlayerTarget = target.DisplayName == "Player";
		string color = isPlayerTarget ? ColorDamageTaken : ColorDefault;

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
	/// Handles attacks that hit but deal no damage due to armor.
	/// Displays single-line format indicating armor absorption.
	/// </summary>
	private void OnAttackBlocked(Entities.BaseEntity attacker, Entities.BaseEntity target, string attackName)
	{
		bool isPlayerAttacker = attacker.DisplayName == "Player";
		bool isPlayerTarget = target.DisplayName == "Player";
		string color = isPlayerTarget ? ColorDamageTaken : ColorDefault;

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

	private void OnEntityDied(string entityName)
	{
		AddMessage($"{entityName} dies!", ColorDeath);
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
}
