using Godot;
using PitsOfDespair.Core;
using System.Collections.Generic;

namespace PitsOfDespair.UI;

/// <summary>
/// Displays a scrolling log of game messages.
/// Pure UI component - receives pre-formatted messages from MessageSystem.
/// </summary>
public partial class MessageLog : PanelContainer
{
    private const int MaxMessages = 100;
    private static readonly string ColorDefault = Palette.ToHex(Palette.Default);

    private RichTextLabel _logLabel;
    private readonly Queue<string> _messageHistory = new();

    // Message coalescing - track the last raw message and its repeat count
    private string _lastRawMessage = null;
    private string _lastMessageColor = null;
    private int _lastMessageCount = 0;

    public override void _Ready()
    {
        _logLabel = GetNode<RichTextLabel>("MarginContainer/LogLabel");
        _logLabel.BbcodeEnabled = true;
        _logLabel.ScrollFollowing = true;
    }

    /// <summary>
    /// Adds a message to the log with optional color.
    /// Consecutive identical messages are coalesced (e.g., "You wait. x3").
    /// </summary>
    public void AddMessage(string message, string color = null)
    {
        color ??= ColorDefault;

        // Check if this is a repeat of the last message
        if (message == _lastRawMessage && color == _lastMessageColor && _messageHistory.Count > 0)
        {
            _lastMessageCount++;
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

    private void UpdateDisplay()
    {
        _logLabel.Clear();
        foreach (var message in _messageHistory)
        {
            _logLabel.AppendText(message + "\n");
        }
    }
}
