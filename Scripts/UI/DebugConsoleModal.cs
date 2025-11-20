using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Debug;
using PitsOfDespair.Systems.Input.Processors;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal debug console for entering slash commands.
/// </summary>
public partial class DebugConsoleModal : CenterContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    private LineEdit _commandInput;
    private MessageLog _messageLog;
    private DebugContext _debugContext;
    private bool _debugModeActive = false;

    public bool IsDebugModeActive => _debugModeActive;

    public override void _Ready()
    {
        _commandInput = GetNode<LineEdit>("%CommandInput");
        Hide();

        // Connect to input submission
        _commandInput.TextSubmitted += OnCommandSubmitted;
    }

    /// <summary>
    /// Initialize the console with required dependencies.
    /// </summary>
    public void Initialize(MessageLog messageLog, DebugContext debugContext)
    {
        _messageLog = messageLog;
        _debugContext = debugContext;
    }

    /// <summary>
    /// Toggle debug mode on/off.
    /// </summary>
    public void SetDebugMode(bool active)
    {
        _debugModeActive = active;
        string status = active ? "enabled" : "disabled";
        string color = active ? Palette.ToHex(Palette.Success) : Palette.ToHex(Palette.Disabled);
        _messageLog?.AddMessage($"Debug mode {status}.", color);
    }

    /// <summary>
    /// Show the console input.
    /// </summary>
    public void ShowConsole()
    {
        if (!_debugModeActive)
        {
            // Silent when debug mode disabled
            return;
        }

        Show();
        _commandInput.Clear();
        _commandInput.GrabFocus();
    }

    /// <summary>
    /// Hide the console and clear input.
    /// </summary>
    public void HideConsole()
    {
        Hide();
        _commandInput.Clear();
        EmitSignal(SignalName.Cancelled);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (MenuInputProcessor.IsCloseKey(keyEvent))
            {
                HideConsole();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void OnCommandSubmitted(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            HideConsole();
            return;
        }

        // Process command
        ProcessCommand(input.Trim());

        // Close console after command execution
        HideConsole();
    }

    private void ProcessCommand(string input)
    {
        // Strip leading / if present
        if (input.StartsWith('/'))
        {
            input = input.Substring(1);
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        // Echo command to log
        _messageLog?.AddMessage($"> /{input}", Palette.ToHex(Palette.Alert));

        // Parse command and arguments
        string[] parts = input.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        string commandName = parts[0];
        string[] args = parts.Length > 1 ? parts[1..] : System.Array.Empty<string>();

        // Create and execute command
        var command = DebugCommandFactory.CreateCommand(commandName);

        if (command == null)
        {
            _messageLog?.AddMessage(
                $"Unknown command: {commandName}. Type 'help' for available commands.",
                Palette.ToHex(Palette.Danger)
            );
            return;
        }

        // Execute command
        var result = command.Execute(_debugContext, args);

        if (result != null)
        {
            _messageLog?.AddMessage(result.Message, result.MessageColor);
        }
    }

    public override void _ExitTree()
    {
        if (_commandInput != null)
        {
            _commandInput.TextSubmitted -= OnCommandSubmitted;
        }
    }
}
