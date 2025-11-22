using Godot;
using PitsOfDespair.Actions;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Atomic goal that moves the entity one tile in a direction.
/// This is the lowest-level movement goal - all movement ultimately
/// becomes MoveDirectionGoals on the stack.
/// </summary>
public class MoveDirectionGoal : Goal
{
    public Vector2I Direction { get; private set; }
    private bool _completed = false;
    private bool _failed = false;

    public MoveDirectionGoal(Vector2I direction, Goal originalIntent = null)
    {
        Direction = direction;
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        return _completed || _failed;
    }

    public override void TakeAction(AIContext context)
    {
        var moveAction = new MoveAction(Direction);
        var result = context.Entity.ExecuteAction(moveAction, context.ActionContext);

        if (result.Success)
        {
            _completed = true;
        }
        else
        {
            // Movement blocked - fail back to original intent for replanning
            _failed = true;
            Fail(context);
        }
    }

    public override string GetName() => $"Move {Direction}";
}
