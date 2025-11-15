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
	/// Connects to the CombatSystem to receive attack events.
	/// </summary>
	public void ConnectToCombatSystem(Systems.CombatSystem combatSystem)
	{
		combatSystem.AttackExecuted += OnAttackExecuted;
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

	private void OnAttackExecuted(Entities.BaseEntity attacker, Entities.BaseEntity target, int damage, string attackName)
	{
		// Determine if this is damage taken by the player
		bool isPlayerDamaged = target.DisplayName == "Player";
		string color = isPlayerDamaged ? ColorDamageTaken : ColorDefault;

		string message = $"{attacker.DisplayName} hits {target.DisplayName} with {attackName} for {damage} damage!";
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
