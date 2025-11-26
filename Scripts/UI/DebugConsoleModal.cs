using System.Collections.Generic;
using System.Linq;
using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Debug;
using PitsOfDespair.Systems;
using PitsOfDespair.Systems.Input.Processors;
using PitsOfDespair.Targeting;

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
    private DebugAutocomplete.AutocompleteContext _currentContext;

    // Targeting state for spawn/ally commands
    private TargetingRequest _pendingTargetingRequest;
    private bool _isWaitingForTargeting = false;

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

        // Always show all commands when opening the console
        ShowAllCommands();
    }

    /// <summary>
    /// Display all available commands in the suggestion list.
    /// </summary>
    private void ShowAllCommands()
    {
        _currentSuggestions = DebugCommandFactory.GetRegisteredCommands().ToList();
        _currentContext = new DebugAutocomplete.AutocompleteContext
        {
            IsArgumentContext = false,
            CommandName = string.Empty,
            ArgIndex = 0,
            CurrentValue = string.Empty
        };

        _suggestionList.Clear();
        foreach (var suggestion in _currentSuggestions)
        {
            _suggestionList.AddItem(suggestion);
        }

        _selectedIndex = 0;
        _suggestionList.Select(0);
        _suggestionList.Show();
        _ghostText.Text = string.Empty;
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

    private const int PageSize = 5;

    private bool HandleAutocompleteInput(InputEventKey keyEvent)
    {
        switch (keyEvent.Keycode)
        {
            case Key.Tab:
                // Always consume Tab to prevent focus cycling
                if (_currentSuggestions.Count > 0)
                {
                    AcceptCurrentSuggestion();
                }
                return true;

            case Key.Enter:
            case Key.KpEnter:
                // If suggestions are visible and command is incomplete, accept suggestion
                if (_currentSuggestions.Count > 0 && !IsCommandComplete(_commandInput.Text))
                {
                    AcceptCurrentSuggestion();
                    return true;
                }
                // Block Enter if command is invalid (prevent submission of invalid commands)
                if (!IsCommandComplete(_commandInput.Text))
                {
                    return true;
                }
                return false;

            case Key.Up:
            case Key.Kp8: // Numpad 8
                if (_currentSuggestions.Count > 0)
                {
                    NavigateSuggestions(-1);
                    return true;
                }
                return false;

            case Key.Down:
            case Key.Kp2: // Numpad 2
                if (_currentSuggestions.Count > 0)
                {
                    NavigateSuggestions(1);
                    return true;
                }
                return false;

            case Key.Pageup:
            case Key.Kp9: // Numpad 9
                if (_currentSuggestions.Count > 0)
                {
                    NavigateSuggestions(-PageSize);
                    return true;
                }
                return false;

            case Key.Pagedown:
            case Key.Kp3: // Numpad 3
                if (_currentSuggestions.Count > 0)
                {
                    NavigateSuggestions(PageSize);
                    return true;
                }
                return false;
        }

        return false;
    }

    /// <summary>
    /// Check if the current input represents a complete, valid command.
    /// A command is complete if it has a valid command name and valid arguments.
    /// </summary>
    private bool IsCommandComplete(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Strip leading slash for backward compatibility
        var cleanInput = input.StartsWith('/') ? input.Substring(1) : input;
        if (string.IsNullOrWhiteSpace(cleanInput))
        {
            return false;
        }

        var parts = cleanInput.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var commandName = parts[0];
        var command = DebugCommandFactory.CreateCommand(commandName);

        if (command == null)
        {
            return false;
        }

        // Check if command expects arguments
        var argSuggestions = DebugCommandFactory.GetArgumentSuggestions(commandName, 0, "");
        bool commandExpectsArgs = argSuggestions != null && argSuggestions.Count > 0;
        bool hasArgs = parts.Length > 1;

        // If command expects arguments and none provided, it's incomplete
        if (commandExpectsArgs && !hasArgs)
        {
            return false;
        }

        // If command expects arguments, validate the provided argument is valid
        if (commandExpectsArgs && hasArgs)
        {
            var providedArg = parts[1];
            // Check if the argument exactly matches one of the valid suggestions
            bool isValidArg = argSuggestions.Any(s => s.Equals(providedArg, System.StringComparison.OrdinalIgnoreCase));
            if (!isValidArg)
            {
                return false;
            }
        }

        return true;
    }

    private void OnTextChanged(string newText)
    {
        UpdateSuggestions(newText);
    }

    private void UpdateSuggestions(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            // Show all commands when input is empty
            ShowAllCommands();
            return;
        }

        // Parse the context to understand what we're completing
        _currentContext = DebugAutocomplete.ParseContext(input);
        _currentSuggestions = DebugAutocomplete.GetSuggestions(input);

        if (_currentSuggestions.Count == 0)
        {
            ClearAutocomplete();
            return;
        }

        // Populate suggestion list (no / prefix)
        _suggestionList.Clear();
        foreach (var suggestion in _currentSuggestions)
        {
            _suggestionList.AddItem(suggestion);
        }

        // Select first item by default
        _selectedIndex = 0;
        _suggestionList.Select(0);
        _suggestionList.Show();
        _ghostText.Text = string.Empty;
    }

    private void NavigateSuggestions(int direction)
    {
        if (_currentSuggestions.Count == 0)
        {
            return;
        }

        _selectedIndex += direction;

        // For single-step movement, wrap around; for page jumps, clamp
        bool isPageJump = System.Math.Abs(direction) > 1;
        if (isPageJump)
        {
            // Clamp to valid range for page jumps
            _selectedIndex = System.Math.Clamp(_selectedIndex, 0, _currentSuggestions.Count - 1);
        }
        else
        {
            // Wrap around for single steps
            if (_selectedIndex < 0)
            {
                _selectedIndex = _currentSuggestions.Count - 1;
            }
            else if (_selectedIndex >= _currentSuggestions.Count)
            {
                _selectedIndex = 0;
            }
        }

        _suggestionList.Select(_selectedIndex);
        _suggestionList.EnsureCurrentIsVisible();
    }

    private void OnSuggestionSelected(long index)
    {
        _selectedIndex = (int)index;
    }

    private void AcceptCurrentSuggestion()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _currentSuggestions.Count)
        {
            return;
        }

        var suggestion = _currentSuggestions[_selectedIndex];
        var currentText = _commandInput.Text;

        string newText;
        string commandName;
        int nextArgIndex;

        if (_currentContext.IsArgumentContext)
        {
            // Replace just the current argument being typed
            // Find where the current value starts and replace from there
            var beforeCurrentValue = currentText.Substring(0, currentText.Length - _currentContext.CurrentValue.Length);
            newText = beforeCurrentValue + suggestion;
            commandName = _currentContext.CommandName;
            nextArgIndex = _currentContext.ArgIndex + 1;
        }
        else
        {
            // Replace the whole command
            newText = suggestion;
            commandName = suggestion;
            nextArgIndex = 0;
        }

        // Check if the command expects more arguments and add trailing space if so
        var nextSuggestions = DebugCommandFactory.GetArgumentSuggestions(commandName, nextArgIndex, "");
        if (nextSuggestions != null && nextSuggestions.Count > 0)
        {
            newText += " ";
        }

        _commandInput.Text = newText;
        _commandInput.CaretColumn = _commandInput.Text.Length;

        // Update suggestions for the new input
        UpdateSuggestions(newText);
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

        // Silently prevent invalid command submission
        if (!IsCommandComplete(input))
        {
            // Don't submit - if there are suggestions, the _Input handler should have caught this
            // This is a fallback safety check
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

        _messageLog?.AddMessage($"> {input}", Palette.ToHex(Palette.Alert));

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
                $"Unknown command: {commandName}",
                Palette.ToHex(Palette.Danger)
            );
            return;
        }

        var result = command.Execute(_debugContext, args);

        if (result == null)
        {
            return;
        }

        // Handle targeting requests
        if (result.RequiresTargeting)
        {
            StartTargetingForSpawn(result.TargetingRequest);
            return;
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            _messageLog?.AddMessage(result.Message, result.MessageColor);
        }
    }

    private void StartTargetingForSpawn(TargetingRequest request)
    {
        if (_debugContext?.TargetingSystem == null || _debugContext?.ActionContext?.Player == null)
        {
            _messageLog?.AddMessage("Targeting system not available.", Palette.ToHex(Palette.Danger));
            return;
        }

        _pendingTargetingRequest = request;
        _isWaitingForTargeting = true;

        // Connect to targeting signals
        var targetingSystem = _debugContext.TargetingSystem;
        targetingSystem.Connect(CursorTargetingSystem.SignalName.TargetConfirmed,
            Callable.From<Vector2I>(OnTargetConfirmed));
        targetingSystem.Connect(CursorTargetingSystem.SignalName.CursorCanceled,
            Callable.From<int>(OnTargetingCanceled));

        // Start tile targeting with large range within LOS
        var definition = TargetingDefinition.Tile(range: 20, requiresLOS: true);
        targetingSystem.StartTargeting(
            _debugContext.ActionContext.Player,
            definition,
            _debugContext.ActionContext
        );

        _messageLog?.AddMessage("Select target tile...", Palette.ToHex(Palette.Alert));
    }

    private void OnTargetConfirmed(Vector2I targetPosition)
    {
        DisconnectTargetingSignals();

        if (_pendingTargetingRequest == null)
        {
            return;
        }

        var position = new GridPosition(targetPosition.X, targetPosition.Y);
        var request = _pendingTargetingRequest;
        _pendingTargetingRequest = null;
        _isWaitingForTargeting = false;

        // Check if position is valid for spawning
        var mapSystem = _debugContext.ActionContext.MapSystem;
        var entityManager = _debugContext.ActionContext.EntityManager;

        if (!mapSystem.IsWalkable(position))
        {
            _messageLog?.AddMessage("Cannot spawn on non-walkable tile.", Palette.ToHex(Palette.Danger));
            return;
        }

        if (entityManager.IsPositionOccupied(position))
        {
            _messageLog?.AddMessage("Cannot spawn on occupied tile.", Palette.ToHex(Palette.Danger));
            return;
        }

        // Spawn the entity
        var entityFactory = _debugContext.ActionContext.EntityFactory;

        if (request.EntityType == SpawnEntityType.Creature)
        {
            var creature = entityFactory.CreateCreature(request.EntityId, position);
            if (creature == null)
            {
                _messageLog?.AddMessage($"Failed to create creature: {request.EntityId}", Palette.ToHex(Palette.Danger));
                return;
            }

            if (request.MakeAlly)
            {
                entityFactory.SetupAsFriendlyCompanion(creature, _debugContext.ActionContext.Player);
            }

            entityManager.AddEntity(creature);

            // Register components with their respective systems so the creature functions properly
            var movementComponent = creature.GetNodeOrNull<Components.MovementComponent>("MovementComponent");
            if (movementComponent != null)
            {
                _debugContext.MovementSystem?.RegisterMovementComponent(movementComponent);
            }

            var attackComponent = creature.GetNodeOrNull<Components.AttackComponent>("AttackComponent");
            if (attackComponent != null)
            {
                _debugContext.ActionContext.CombatSystem?.RegisterAttackComponent(attackComponent);
            }

            var speedComponent = creature.GetNodeOrNull<Components.SpeedComponent>("SpeedComponent");
            if (speedComponent != null)
            {
                _debugContext.TimeSystem?.RegisterCreature(speedComponent);
            }

            string allyText = request.MakeAlly ? " as ally" : "";
            _messageLog?.AddMessage($"Spawned {request.EntityId}{allyText} at ({position.X}, {position.Y}).", Palette.ToHex(Palette.Success));
        }
        else
        {
            var item = entityFactory.CreateItem(request.EntityId, position);
            if (item == null)
            {
                _messageLog?.AddMessage($"Failed to create item: {request.EntityId}", Palette.ToHex(Palette.Danger));
                return;
            }

            entityManager.AddEntity(item);
            _messageLog?.AddMessage($"Spawned {request.EntityId} at ({position.X}, {position.Y}).", Palette.ToHex(Palette.Success));
        }
    }

    private void OnTargetingCanceled(int mode)
    {
        DisconnectTargetingSignals();

        _pendingTargetingRequest = null;
        _isWaitingForTargeting = false;

        _messageLog?.AddMessage("Spawn cancelled.", Palette.ToHex(Palette.Disabled));
    }

    private void DisconnectTargetingSignals()
    {
        if (_debugContext?.TargetingSystem == null)
        {
            return;
        }

        var targetingSystem = _debugContext.TargetingSystem;

        if (targetingSystem.IsConnected(CursorTargetingSystem.SignalName.TargetConfirmed,
            Callable.From<Vector2I>(OnTargetConfirmed)))
        {
            targetingSystem.Disconnect(CursorTargetingSystem.SignalName.TargetConfirmed,
                Callable.From<Vector2I>(OnTargetConfirmed));
        }

        if (targetingSystem.IsConnected(CursorTargetingSystem.SignalName.CursorCanceled,
            Callable.From<int>(OnTargetingCanceled)))
        {
            targetingSystem.Disconnect(CursorTargetingSystem.SignalName.CursorCanceled,
                Callable.From<int>(OnTargetingCanceled));
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

        // Clean up any pending targeting connections
        DisconnectTargetingSignals();
    }
}
