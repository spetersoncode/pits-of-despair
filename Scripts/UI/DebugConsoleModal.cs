using System.Collections.Generic;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Debug;
using PitsOfDespair.Systems.Input.Processors;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal debug console for entering slash commands with autocomplete support.
/// </summary>
public partial class DebugConsoleModal : CenterContainer
{
    [Signal]
    public delegate void CancelledEventHandler();

    private LineEdit _commandInput;
    private Label _ghostText;
    private ItemList _suggestionList;
    private MessageLog _messageLog;
    private DebugContext _debugContext;
    private bool _debugModeActive = false;

    private IReadOnlyList<string> _currentSuggestions = new List<string>();
    private int _selectedIndex = -1;

    public bool IsDebugModeActive => _debugModeActive;

    public override void _Ready()
    {
        _commandInput = GetNode<LineEdit>("%CommandInput");
        _ghostText = GetNode<Label>("%GhostText");
        _suggestionList = GetNode<ItemList>("%SuggestionList");
        Hide();

        // Connect signals using Godot pattern
        _commandInput.Connect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(OnCommandSubmitted));
        _commandInput.Connect(LineEdit.SignalName.TextChanged, Callable.From<string>(OnTextChanged));
        _suggestionList.Connect(ItemList.SignalName.ItemSelected, Callable.From<long>(OnSuggestionSelected));
    }

    /// <summary>
    /// Initialize the console with required dependencies.
    /// </summary>
    public void Initialize(MessageLog messageLog, DebugContext debugContext, bool initialDebugMode = false)
    {
        _messageLog = messageLog;
        _debugContext = debugContext;
        _debugModeActive = initialDebugMode;

        if (initialDebugMode)
        {
            _messageLog?.AddMessage("Debug mode enabled.", Palette.ToHex(Palette.Success));
        }
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
            return;
        }

        Show();
        _commandInput.Clear();
        _commandInput.GrabFocus();
        ClearAutocomplete();
    }

    /// <summary>
    /// Hide the console and clear input.
    /// </summary>
    public void HideConsole()
    {
        Hide();
        _commandInput.Clear();
        ClearAutocomplete();
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
                return;
            }

            // Handle autocomplete navigation
            if (HandleAutocompleteInput(keyEvent))
            {
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private bool HandleAutocompleteInput(InputEventKey keyEvent)
    {
        if (_currentSuggestions.Count == 0)
        {
            return false;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Tab:
                AcceptCurrentSuggestion();
                return true;

            case Key.Up:
                NavigateSuggestions(-1);
                return true;

            case Key.Down:
                NavigateSuggestions(1);
                return true;
        }

        return false;
    }

    private void OnTextChanged(string newText)
    {
        UpdateSuggestions(newText);
    }

    private void UpdateSuggestions(string input)
    {
        // Strip leading slash for matching
        var cleanInput = input.StartsWith('/') ? input.Substring(1) : input;

        // Don't show suggestions if input contains spaces (already typing args)
        if (cleanInput.Contains(' '))
        {
            ClearAutocomplete();
            return;
        }

        _currentSuggestions = DebugAutocomplete.GetSuggestions(cleanInput);

        if (_currentSuggestions.Count == 0 || string.IsNullOrEmpty(cleanInput))
        {
            ClearAutocomplete();
            return;
        }

        // Populate suggestion list
        _suggestionList.Clear();
        foreach (var suggestion in _currentSuggestions)
        {
            _suggestionList.AddItem("/" + suggestion);
        }

        // Select first item by default
        _selectedIndex = 0;
        _suggestionList.Select(0);
        _suggestionList.Show();

        // Update ghost text
        UpdateGhostText(cleanInput);
    }

    private void UpdateGhostText(string input)
    {
        if (_currentSuggestions.Count == 0 || string.IsNullOrEmpty(input))
        {
            _ghostText.Text = string.Empty;
            return;
        }

        var selectedCommand = _selectedIndex >= 0 && _selectedIndex < _currentSuggestions.Count
            ? _currentSuggestions[_selectedIndex]
            : null;

        var suffix = DebugAutocomplete.GetCompletionSuffix(input, selectedCommand);

        // Ghost text shows the input + the completion suffix
        // Position it to align with user input (including leading slash if present)
        var prefix = _commandInput.Text.StartsWith('/') ? "/" : "";
        _ghostText.Text = prefix + input + suffix;
    }

    private void NavigateSuggestions(int direction)
    {
        if (_currentSuggestions.Count == 0)
        {
            return;
        }

        _selectedIndex += direction;

        // Wrap around
        if (_selectedIndex < 0)
        {
            _selectedIndex = _currentSuggestions.Count - 1;
        }
        else if (_selectedIndex >= _currentSuggestions.Count)
        {
            _selectedIndex = 0;
        }

        _suggestionList.Select(_selectedIndex);
        _suggestionList.EnsureCurrentIsVisible();

        // Update ghost text to match new selection
        var cleanInput = _commandInput.Text.StartsWith('/')
            ? _commandInput.Text.Substring(1)
            : _commandInput.Text;
        UpdateGhostText(cleanInput);
    }

    private void OnSuggestionSelected(long index)
    {
        _selectedIndex = (int)index;
        var cleanInput = _commandInput.Text.StartsWith('/')
            ? _commandInput.Text.Substring(1)
            : _commandInput.Text;
        UpdateGhostText(cleanInput);
    }

    private void AcceptCurrentSuggestion()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentSuggestions.Count)
        {
            return;
        }

        var command = _currentSuggestions[_selectedIndex];
        _commandInput.Text = "/" + command;
        _commandInput.CaretColumn = _commandInput.Text.Length;
        ClearAutocomplete();
    }

    private void ClearAutocomplete()
    {
        _suggestionList.Clear();
        _suggestionList.Hide();
        _ghostText.Text = string.Empty;
        _currentSuggestions = new List<string>();
        _selectedIndex = -1;
    }

    private void OnCommandSubmitted(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            HideConsole();
            return;
        }

        ProcessCommand(input.Trim());
        HideConsole();
    }

    private void ProcessCommand(string input)
    {
        if (input.StartsWith('/'))
        {
            input = input.Substring(1);
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        _messageLog?.AddMessage($"> /{input}", Palette.ToHex(Palette.Alert));

        string[] parts = input.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        string commandName = parts[0];
        string[] args = parts.Length > 1 ? parts[1..] : System.Array.Empty<string>();

        var command = DebugCommandFactory.CreateCommand(commandName);

        if (command == null)
        {
            _messageLog?.AddMessage(
                $"Unknown command: {commandName}. Type 'help' for available commands.",
                Palette.ToHex(Palette.Danger)
            );
            return;
        }

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
            _commandInput.Disconnect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(OnCommandSubmitted));
            _commandInput.Disconnect(LineEdit.SignalName.TextChanged, Callable.From<string>(OnTextChanged));
        }

        if (_suggestionList != null)
        {
            _suggestionList.Disconnect(ItemList.SignalName.ItemSelected, Callable.From<long>(OnSuggestionSelected));
        }
    }
}
