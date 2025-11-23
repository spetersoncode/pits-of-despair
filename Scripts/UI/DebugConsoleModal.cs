using System.Collections.Generic;
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

        if (string.IsNullOrEmpty(cleanInput))
        {
            ClearAutocomplete();
            return;
        }

        // Parse the context to understand what we're completing
        _currentContext = DebugAutocomplete.ParseContext(cleanInput);
        _currentSuggestions = DebugAutocomplete.GetSuggestions(cleanInput);

        if (_currentSuggestions.Count == 0)
        {
            ClearAutocomplete();
            return;
        }

        // Populate suggestion list
        _suggestionList.Clear();
        foreach (var suggestion in _currentSuggestions)
        {
            // For command context, prefix with /
            // For argument context, show just the argument value
            var displayText = _currentContext.IsArgumentContext ? suggestion : "/" + suggestion;
            _suggestionList.AddItem(displayText);
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

        var selectedSuggestion = _selectedIndex >= 0 && _selectedIndex < _currentSuggestions.Count
            ? _currentSuggestions[_selectedIndex]
            : null;

        var suffix = DebugAutocomplete.GetCompletionSuffix(input, selectedSuggestion);

        // Ghost text shows the full line with completion
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

        var suggestion = _currentSuggestions[_selectedIndex];
        var currentText = _commandInput.Text;
        var cleanInput = currentText.StartsWith('/') ? currentText.Substring(1) : currentText;

        string newText;
        string commandName;
        int nextArgIndex;

        if (_currentContext.IsArgumentContext)
        {
            // Replace just the current argument being typed
            // Find where the current value starts and replace from there
            var beforeCurrentValue = cleanInput.Substring(0, cleanInput.Length - _currentContext.CurrentValue.Length);
            newText = "/" + beforeCurrentValue + suggestion;
            commandName = _currentContext.CommandName;
            nextArgIndex = _currentContext.ArgIndex + 1;
        }
        else
        {
            // Replace the whole command
            newText = "/" + suggestion;
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

            var aiComponent = creature.GetNodeOrNull<Components.AIComponent>("AIComponent");
            if (aiComponent != null)
            {
                _debugContext.AISystem?.RegisterAIComponent(aiComponent);
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
