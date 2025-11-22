using PitsOfDespair.Entities;

namespace PitsOfDespair.AI.Goals;

/// <summary>
/// Goal that defends the protection target by attacking visible enemies.
/// Uses union of VIP's and protector's vision, but prioritizes threats the VIP can see.
/// </summary>
public class DefendTargetGoal : Goal
{
    public DefendTargetGoal(Goal originalIntent = null)
    {
        OriginalIntent = originalIntent;
    }

    public override bool IsFinished(AIContext context)
    {
        var target = context.ProtectionTarget;
        if (target == null || !Godot.GodotObject.IsInstanceValid(target))
            return true; // No target to defend

        // Finished when neither VIP nor protector can see enemies
        var vipThreats = context.GetEnemiesVisibleToEntity(target);
        if (vipThreats.Count > 0)
            return false;

        var protectorThreats = context.GetVisibleEnemies();
        return protectorThreats.Count == 0;
    }

    public override void TakeAction(AIContext context)
    {
        var target = context.ProtectionTarget;
        if (target == null || !Godot.GodotObject.IsInstanceValid(target))
        {
            Fail(context);
            return;
        }

        // Priority 1: Enemies visible to VIP (defend the VIP first)
        var vipThreats = context.GetEnemiesVisibleToEntity(target);
        if (vipThreats.Count > 0)
        {
            var closestThreat = context.GetClosestEnemy(vipThreats);
            if (closestThreat != null)
            {
                var killGoal = new KillTargetGoal(closestThreat, originalIntent: this);
                context.AIComponent.GoalStack.Push(killGoal);
                return;
            }
        }

        // Priority 2: Enemies visible to protector (self-defense)
        var protectorThreats = context.GetVisibleEnemies();
        if (protectorThreats.Count > 0)
        {
            var closestThreat = context.GetClosestEnemy(protectorThreats);
            if (closestThreat != null)
            {
                var killGoal = new KillTargetGoal(closestThreat, originalIntent: this);
                context.AIComponent.GoalStack.Push(killGoal);
                return;
            }
        }

        // No threats - goal will finish on next IsFinished check
    }

    public override string GetName() => "Defend target";
}
