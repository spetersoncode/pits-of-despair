using Godot;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Main game level controller that wires together all game systems.
/// </summary>
public partial class GameLevel : Node
{
    private MapSystem _mapSystem;
    private ASCIIRenderer _renderer;
    private Player _player;
    private InputHandler _inputHandler;

    public override void _Ready()
    {
        // Get references to child nodes
        _mapSystem = GetNode<MapSystem>("MapSystem");
        _renderer = GetNode<ASCIIRenderer>("ASCIIRenderer");
        _player = GetNode<Player>("Player");
        _inputHandler = GetNode<InputHandler>("InputHandler");

        // Wire up the systems
        _player.SetMapSystem(_mapSystem);
        _renderer.SetMapSystem(_mapSystem);
        _renderer.SetPlayer(_player);
        _inputHandler.SetPlayer(_player);
    }
}
