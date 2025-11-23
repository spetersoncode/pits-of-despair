using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using PitsOfDespair.Scripts.Skills;
using PitsOfDespair.Skills.Targeting;
using PitsOfDespair.Systems.Input.Services;
using PitsOfDespair.UI;

namespace PitsOfDespair.Systems.Input.Processors;

/// <summary>
/// Processes gameplay input (movement, combat, item actions) during the player's turn.
/// Converts input actions into game actions and executes them.
/// </summary>
public class GameplayInputProcessor
{
    private readonly KeybindingService _keybindingService;
    private Player _player;
    private ActionContext _actionContext;
    private CursorTargetingSystem _cursorSystem;
    private GameHUD _gameHUD;
    private char? _pendingItemKey = null;
    private bool _isReachAttack = false;
    private SkillDefinition _pendingSkill = null;

    public GameplayInputProcessor(KeybindingService keybindingService)
    {
        _keybindingService = keybindingService;
    }

    public void SetPlayer(Player player) => _player = player;
    public void SetActionContext(ActionContext actionContext) => _actionContext = actionContext;
    public void SetCursorTargetingSystem(CursorTargetingSystem cursorSystem) => _cursorSystem = cursorSystem;
    public void SetGameHUD(GameHUD gameHUD) => _gameHUD = gameHUD;

    /// <summary>
    /// Processes gameplay input and returns true if input was handled.
    /// </summary>
    public bool ProcessInput(InputEventKey keyEvent)
    {
        if (_player == null || _actionContext == null)
            return false;

        // Try to get action from key event
        if (!_keybindingService.TryGetAction(keyEvent, out var action))
            return false;

        // Handle movement actions
        if (_keybindingService.IsMovementAction(action))
        {
            if (_keybindingService.TryGetMovementDirection(action, out var direction))
            {
                var moveAction = new MoveAction(direction);
                _player.ExecuteAction(moveAction, _actionContext);
                return true;
            }
        }

        // Handle other gameplay actions
        switch (action)
        {
            case InputAction.Wait:
                var waitAction = new WaitAction();
                _player.ExecuteAction(waitAction, _actionContext);
                return true;

            case InputAction.Pickup:
                var pickupAction = new PickupAction();
                _player.ExecuteAction(pickupAction, _actionContext);
                return true;

            case InputAction.Descend:
                var descendAction = new DescendAction();
                _player.ExecuteAction(descendAction, _actionContext);
                return true;

            case InputAction.FireRanged:
                HandleFireRanged();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Handles the fire ranged weapon action (F key).
    /// Starts targeting mode if player has a ranged weapon equipped.
    /// </summary>
    private void HandleFireRanged()
    {
        if (!HasRangedWeaponEquipped() || _cursorSystem == null || _gameHUD == null)
            return;

        int range = GetRangedWeaponRange();
        if (range > 0)
        {
            _cursorSystem.StartActionTargeting(
                CursorTargetingSystem.TargetingMode.RangedAttack,
                _player.GridPosition,
                range,
                requiresCreature: true,
                useGridDistance: false // Use Euclidean (circular) distance for ranged attacks
            );
            _gameHUD.EnterTargetingMode();
        }
    }

    /// <summary>
    /// Called when user requests to target an item.
    /// </summary>
    public void StartItemTargeting(char itemKey)
    {
        if (_player == null || _cursorSystem == null)
            return;

        _pendingItemKey = itemKey;
        _isReachAttack = false;

        var slot = _player.GetInventorySlot(itemKey);
        if (slot != null)
        {
            int range = slot.Item.Template.GetTargetingRange();
            _cursorSystem.StartActionTargeting(
                CursorTargetingSystem.TargetingMode.TargetedItem,
                _player.GridPosition,
                range,
                requiresCreature: true,
                useGridDistance: false // Use Euclidean (circular) distance for targeted items
            );
        }
    }

    /// <summary>
    /// Called when user requests to use a reach attack.
    /// </summary>
    public void StartReachAttackTargeting(char itemKey)
    {
        if (_player == null || _cursorSystem == null)
            return;

        _pendingItemKey = itemKey;
        _isReachAttack = true;

        var slot = _player.GetInventorySlot(itemKey);
        if (slot != null && slot.Item.Template.Attack != null)
        {
            int range = slot.Item.Template.Attack.Range;
            _cursorSystem.StartActionTargeting(
                CursorTargetingSystem.TargetingMode.ReachAttack,
                _player.GridPosition,
                range,
                requiresCreature: true,
                useGridDistance: true // Use Chebyshev (grid) distance for reach attacks
            );
        }
    }

    /// <summary>
    /// Called when user requests to target a skill.
    /// </summary>
    public void StartSkillTargeting(string skillId)
    {
        if (_player == null || _cursorSystem == null || string.IsNullOrEmpty(skillId))
            return;

        // Look up skill from DataLoader
        var dataLoader = ((SceneTree)Engine.GetMainLoop()).Root.GetNode<DataLoader>("/root/DataLoader");
        if (dataLoader == null)
            return;

        var skill = dataLoader.GetSkill(skillId);
        if (skill == null)
            return;

        _pendingSkill = skill;
        _pendingItemKey = null;
        _isReachAttack = false;

        // Get the targeting handler for this skill
        var handler = TargetingHandler.CreateForType(skill.GetTargetingType());
        var validPositions = handler.GetValidTargetPositions(_player, skill, _actionContext);

        // Determine if this targeting type requires a creature
        var targetingType = skill.GetTargetingType();
        bool requiresCreature = targetingType == TargetingType.Enemy ||
                                targetingType == TargetingType.Ally ||
                                targetingType == TargetingType.Adjacent;

        int range = skill.Range > 0 ? skill.Range : 1;

        _cursorSystem.StartActionTargeting(
            CursorTargetingSystem.TargetingMode.TargetedItem, // Reuse targeted item mode
            _player.GridPosition,
            range,
            requiresCreature: requiresCreature,
            useGridDistance: true, // Skills use grid (Chebyshev) distance
            validPositions: new HashSet<GridPosition>(validPositions)
        );
    }

    /// <summary>
    /// Called when targeting is confirmed. Executes the appropriate action.
    /// </summary>
    public void OnTargetConfirmed(Vector2I targetPosition)
    {
        if (_player == null || _actionContext == null)
            return;

        if (_pendingSkill != null)
        {
            ExecuteTargetedSkill(targetPosition);
            _pendingSkill = null;
        }
        else if (_pendingItemKey.HasValue)
        {
            if (_isReachAttack)
            {
                ExecuteReachAttack(targetPosition);
            }
            else
            {
                ExecuteTargetedItem(targetPosition);
            }

            _pendingItemKey = null;
            _isReachAttack = false;
        }
        else
        {
            ExecuteRangedAttack(targetPosition);
        }
    }

    /// <summary>
    /// Called when targeting is canceled. Clears pending state.
    /// </summary>
    public void OnTargetCanceled()
    {
        _pendingItemKey = null;
        _isReachAttack = false;
        _pendingSkill = null;
    }

    private void ExecuteTargetedSkill(Vector2I targetPosition)
    {
        if (_pendingSkill == null)
            return;

        var skillAction = new UseTargetedSkillAction(_pendingSkill, GridPosition.FromVector2I(targetPosition));
        _player.ExecuteAction(skillAction, _actionContext);
    }

    private void ExecuteReachAttack(Vector2I targetPosition)
    {
        var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent != null)
        {
            var reachAttackAction = new ReachAttackAction(GridPosition.FromVector2I(targetPosition), attackIndex: 0);
            _player.ExecuteAction(reachAttackAction, _actionContext);
        }
    }

    private void ExecuteTargetedItem(Vector2I targetPosition)
    {
        var itemAction = new UseTargetedItemAction(_pendingItemKey.Value, GridPosition.FromVector2I(targetPosition));
        _player.ExecuteAction(itemAction, _actionContext);
    }

    private void ExecuteRangedAttack(Vector2I targetPosition)
    {
        var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return;

        // Find first ranged attack
        int rangedAttackIndex = -1;
        for (int i = 0; i < attackComponent.Attacks.Count; i++)
        {
            var attack = attackComponent.Attacks[i];
            if (attack != null && attack.Type == AttackType.Ranged)
            {
                rangedAttackIndex = i;
                break;
            }
        }

        if (rangedAttackIndex >= 0)
        {
            var rangedAttackAction = new RangedAttackAction(GridPosition.FromVector2I(targetPosition), rangedAttackIndex);
            _player.ExecuteAction(rangedAttackAction, _actionContext);
        }
    }

    private bool HasRangedWeaponEquipped()
    {
        if (_player == null)
            return false;

        var equipComponent = _player.GetNodeOrNull<EquipComponent>("EquipComponent");
        if (equipComponent == null)
            return false;

        var rangedWeaponKey = equipComponent.GetEquippedKey(EquipmentSlot.RangedWeapon);
        return rangedWeaponKey != null;
    }

    private int GetRangedWeaponRange()
    {
        if (_player == null)
            return 0;

        var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return 0;

        for (int i = 0; i < attackComponent.Attacks.Count; i++)
        {
            var attack = attackComponent.Attacks[i];
            if (attack != null && attack.Type == AttackType.Ranged)
            {
                return attack.Range;
            }
        }

        return 0;
    }
}
