using Godot;
using System.Collections.Generic;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a scrolling log of combat messages and game events.
/// </summary>
public partial class MessageLog : PanelContainer
{
	private const int MaxMessages = 100;
	private const string ColorDamageTaken = "#ff6666"; // Red
	private const string ColorDeath = "#ffff66"; // Yellow
	private const string ColorDefault = "#ffffff"; // White

	private RichTextLabel _logLabel;
	private readonly Queue<string> _messageHistory = new();

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
		combatSystem.AttackHit += OnAttackHit;
		combatSystem.AttackBlocked += OnAttackBlocked;
		combatSystem.AttackMissed += OnAttackMissed;

		combatSystem.ActionMessage += OnActionMessage;
	}

	/// <summary>
	/// Connects to an entity's health component to receive death notifications.
	/// </summary>
	public void ConnectToHealthComponent(Components.HealthComponent healthComponent, string entityName)
	{
		healthComponent.Died += () => OnEntityDied(entityName);
	}

	/// <summary>
	/// Adds a message to the log with optional color.
	/// </summary>
	public void AddMessage(string message, string color = ColorDefault)
	{
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
	/// Handles successful attacks that deal damage.
	/// Displays single-line format with damage.
	/// </summary>
	private void OnAttackHit(Entities.BaseEntity attacker, Entities.BaseEntity target, int damage, string attackName)
	{
		// Determine if this is the player taking or dealing damage
		bool isPlayerAttacker = attacker.DisplayName == "Player";
		bool isPlayerTarget = target.DisplayName == "Player";
		string color = isPlayerTarget ? ColorDamageTaken : ColorDefault;

		if (isPlayerAttacker)
		{
			AddMessage($"You hit the {target.DisplayName} for {damage} damage.", color);
		}
		else if (isPlayerTarget)
		{
			AddMessage($"The {attacker.DisplayName} hits you for {damage} damage.", color);
		}
		else
		{
			// NPC vs NPC combat
			AddMessage($"The {attacker.DisplayName} hits the {target.DisplayName} for {damage} damage.", color);
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

		if (isPlayerAttacker)
		{
			AddMessage($"You hit the {target.DisplayName} but your attack glances off their armor!", color);
		}
		else if (isPlayerTarget)
		{
			AddMessage($"The {attacker.DisplayName} hits you but the attack bounces off your armor!", color);
		}
		else
		{
			AddMessage($"The {attacker.DisplayName} hits the {target.DisplayName} but the attack glances off armor!", color);
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

	private void UpdateDisplay()
	{
		_logLabel.Clear();
		foreach (var message in _messageHistory)
		{
			_logLabel.AppendText(message + "\n");
		}
	}
}
