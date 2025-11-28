using Godot;
using PitsOfDespair.Core;
using PitsOfDespair.Systems.Input.Processors;

namespace PitsOfDespair.UI;

/// <summary>
/// Modal for entering the player's character name.
/// Displayed after pressing N on the title screen.
/// </summary>
public partial class CharacterNameModal : Control
{
    [Signal]
    public delegate void NameEnteredEventHandler(string name);

    [Signal]
    public delegate void CancelledEventHandler();

    private LineEdit _nameInput;
    private RichTextLabel _promptLabel;

    private const string DefaultPlayerName = "Player";

    public override void _Ready()
    {
        _nameInput = GetNode<LineEdit>("%NameInput");
        _promptLabel = GetNode<RichTextLabel>("%PromptLabel");

        // Start hidden
        Hide();

        // Connect signals using Godot pattern
        _nameInput.Connect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(OnNameSubmitted));

        // Set up prompt content
        UpdatePromptContent();
    }

    /// <summary>
    /// Shows the character name modal with input focused.
    /// </summary>
    public void ShowNameEntry()
    {
        _nameInput.Clear();
        _nameInput.PlaceholderText = DefaultPlayerName;
        Show();
        _nameInput.GrabFocus();
    }

    /// <summary>
    /// Hides the character name modal.
    /// </summary>
    public void HideNameEntry()
    {
        Hide();
    }

    /// <summary>
    /// Updates the prompt label content.
    /// </summary>
    private void UpdatePromptContent()
    {
        string titleColor = Palette.ToHex(Palette.Player);
        string hintColor = Palette.ToHex(Palette.Disabled);

        _promptLabel.Text = $@"[center][color={titleColor}]Enter your name[/color]

[color={hintColor}]Press Enter to confirm, Esc to cancel[/color][/center]";
    }

    private void OnNameSubmitted(string text)
    {
        // Use default name if blank
        string playerName = string.IsNullOrWhiteSpace(text) ? DefaultPlayerName : text.Trim();

        HideNameEntry();
        EmitSignal(SignalName.NameEntered, playerName);
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Handle ESC to cancel and return to title
            if (MenuInputProcessor.IsCloseKey(keyEvent))
            {
                HideNameEntry();
                EmitSignal(SignalName.Cancelled);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _ExitTree()
    {
        if (_nameInput != null)
        {
            _nameInput.Disconnect(LineEdit.SignalName.TextSubmitted, Callable.From<string>(OnNameSubmitted));
        }
    }
}
