using Godot;
using PitsOfDespair.Systems.Input.Services;

namespace PitsOfDespair.Systems.Input.Processors;

/// <summary>
/// Processes input during cursor targeting mode (examine, ranged attacks, targeted items).
/// </summary>
public class CursorInputProcessor
{
    private readonly KeybindingService _keybindingService;
    private CursorTargetingSystem _cursorSystem;

    public CursorInputProcessor(KeybindingService keybindingService)
    {
        _keybindingService = keybindingService;
    }

    public void SetCursorTargetingSystem(CursorTargetingSystem cursorSystem) => _cursorSystem = cursorSystem;

    /// <summary>
    /// Processes cursor input and returns true if input was handled.
    /// </summary>
    public bool ProcessInput(InputEventKey keyEvent)
    {
        if (_cursorSystem == null || !_cursorSystem.IsActive)
            return false;

        bool isExamineMode = _cursorSystem.CurrentMode == CursorTargetingSystem.TargetingMode.Examine;

        // Check for specific cursor keys first (before action resolution)
        // This is needed because some keys (Space, F) have dual purposes

        // Cancel in all modes
        if (keyEvent.Keycode == Key.Escape)
        {
            _cursorSystem.Cancel();
            return true;
        }

        // X also exits examine mode
        if (isExamineMode && keyEvent.Keycode == Key.X)
        {
            _cursorSystem.Cancel();
            return true;
        }

        // Examine details in examine mode
        if (isExamineMode)
        {
            bool isExamineKey = keyEvent.Keycode == Key.Enter ||
                                keyEvent.Keycode == Key.KpEnter ||
                                keyEvent.Keycode == Key.Space;

            if (isExamineKey)
            {
                _cursorSystem.ExamineTarget();
                return true;
            }
        }

        // Confirm target in action modes
        if (!isExamineMode)
        {
            // Universal confirmation keys
            bool isConfirmKey = keyEvent.Keycode == Key.Enter ||
                                keyEvent.Keycode == Key.KpEnter ||
                                keyEvent.Keycode == Key.Space;

            // Add mode-specific confirmation keys
            if (_cursorSystem.CurrentMode == CursorTargetingSystem.TargetingMode.RangedAttack)
                isConfirmKey |= keyEvent.Keycode == Key.F;
            else if (_cursorSystem.CurrentMode == CursorTargetingSystem.TargetingMode.ReachAttack)
                isConfirmKey |= keyEvent.Keycode == Key.A;

            if (isConfirmKey)
            {
                _cursorSystem.ConfirmTarget();
                return true;
            }

            // Tab cycles targets
            if (keyEvent.Keycode == Key.Tab)
            {
                if (keyEvent.ShiftPressed)
                    _cursorSystem.CyclePreviousTarget();
                else
                    _cursorSystem.CycleNextTarget();
                return true;
            }

            // Numpad +/- for cycling
            if (keyEvent.Keycode == Key.KpAdd)
            {
                _cursorSystem.CycleNextTarget();
                return true;
            }
            if (keyEvent.Keycode == Key.KpSubtract)
            {
                _cursorSystem.CyclePreviousTarget();
                return true;
            }
        }

        // Movement works in all modes - use action resolution for this
        if (_keybindingService.TryGetAction(keyEvent, out var action))
        {
            if (_keybindingService.IsMovementAction(action))
            {
                if (_keybindingService.TryGetMovementDirection(action, out var direction))
                {
                    _cursorSystem.MoveCursor(direction);
                    return true;
                }
            }
        }

        return false;
    }
}
