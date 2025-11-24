using System.Collections.Generic;
using Godot;
using PitsOfDespair.Actions;
using PitsOfDespair.Components;
using PitsOfDespair.Core;
using PitsOfDespair.Data;
using PitsOfDespair.Entities;
using PitsOfDespair.Scripts.Components;
using PitsOfDespair.Scripts.Data;
using PitsOfDespair.Targeting;
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
            var definition = TargetingDefinition.Ranged(range);
            _cursorSystem.StartTargeting(_player, definition, _actionContext, Key.F);
            _gameHUD.EnterTargetingMode();
        }
    }

    /// <summary>
    /// Called when user requests to target an item.
    /// </summary>
    public void StartItemTargeting(char itemKey)
    {
        if (_player == null || _cursorSystem == null || _actionContext == null)
            return;

        _pendingItemKey = itemKey;
        _isReachAttack = false;

        var slot = _player.GetInventorySlot(itemKey);
        if (slot != null)
        {
            var definition = TargetingDefinition.FromItem(slot.Item.Template);
            var initiatingKey = CharToKey(itemKey);
            _cursorSystem.StartTargeting(_player, definition, _actionContext, initiatingKey);
        }
    }

    /// <summary>
    /// Called when user requests to use a reach attack.
    /// </summary>
    public void StartReachAttackTargeting(char itemKey)
    {
        if (_player == null || _cursorSystem == null || _actionContext == null)
            return;

        _pendingItemKey = itemKey;
        _isReachAttack = true;

        var slot = _player.GetInventorySlot(itemKey);
        if (slot != null && slot.Item.Template.Attack != null)
        {
            int range = slot.Item.Template.Attack.Range;
            var definition = TargetingDefinition.Reach(range);
            var initiatingKey = CharToKey(itemKey);
            _cursorSystem.StartTargeting(_player, definition, _actionContext, initiatingKey);
        }
    }

    /// <summary>
    /// Called when user requests to target a skill.
    /// </summary>
    /// <param name="skillId">The skill ID to target</param>
    /// <param name="key">The key that was pressed to activate this skill (for spam targeting)</param>
    public void StartSkillTargeting(string skillId, char key)
    {
        if (_player == null || _cursorSystem == null || _actionContext == null || string.IsNullOrEmpty(skillId))
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

        // Create targeting definition from skill
        var definition = TargetingDefinition.FromSkill(skill);

        // For reach targeting skills, use weapon reach if available
        if (skill.Targeting?.ToLower() == "reach")
        {
            int weaponReach = GetEquippedMeleeWeaponReach();
            if (weaponReach > definition.Range)
            {
                definition = TargetingDefinition.Reach(weaponReach);
            }
        }

        // Convert char to Key for initiating key (enables spam targeting with same key)
        var initiatingKey = key != '\0' ? CharToKey(key) : null;
        _cursorSystem.StartTargeting(_player, definition, _actionContext, initiatingKey);
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

    /// <summary>
    /// Gets the reach of the equipped melee weapon (default 1 for adjacent attacks).
    /// </summary>
    private int GetEquippedMeleeWeaponReach()
    {
        if (_player == null)
            return 1;

        var attackComponent = _player.GetNodeOrNull<AttackComponent>("AttackComponent");
        if (attackComponent == null)
            return 1;

        // Find the first melee attack and return its range
        for (int i = 0; i < attackComponent.Attacks.Count; i++)
        {
            var attack = attackComponent.Attacks[i];
            if (attack != null && attack.Type == AttackType.Melee)
            {
                return attack.Range > 0 ? attack.Range : 1;
            }
        }

        return 1;
    }

    /// <summary>
    /// Converts a character (a-z) to a Godot Key enum value.
    /// </summary>
    private static Key? CharToKey(char c)
    {
        c = char.ToLower(c);
        if (c >= 'a' && c <= 'z')
        {
            return (Key)((int)Key.A + (c - 'a'));
        }
        return null;
    }
}
