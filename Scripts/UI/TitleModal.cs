using Godot;

namespace PitsOfDespair.UI;

/// <summary>
/// Full-screen title modal displayed when the game starts.
/// Shows title image and waits for player to press N to start.
/// </summary>
public partial class TitleModal : PanelContainer
{
    [Signal]
    public delegate void NewGameRequestedEventHandler();

    private TextureRect _titleImage;

    public override void _Ready()
    {
        _titleImage = GetNode<TextureRect>("TitleImage");

        // Start hidden
        Hide();
    }

    /// <summary>
    /// Shows the title modal.
    /// </summary>
    public void ShowTitle()
    {
        Show();
    }

    /// <summary>
    /// Hides the title modal.
    /// </summary>
    public void HideTitle()
    {
        Hide();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Check for N key to start new game
            if (keyEvent.Keycode == Key.N)
            {
                EmitSignal(SignalName.NewGameRequested);
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
