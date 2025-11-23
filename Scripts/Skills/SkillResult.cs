using System.Collections.Generic;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Skills;

/// <summary>
/// Represents the result of executing a skill.
/// Contains success state, messages for UI feedback, and affected entities.
/// </summary>
public class SkillResult
{
    /// <summary>
    /// Whether the skill was successfully executed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Messages describing what happened during skill execution.
    /// </summary>
    public List<string> Messages { get; } = new();

    /// <summary>
    /// Entities affected by the skill (for UI feedback, kill tracking, etc.).
    /// </summary>
    public List<BaseEntity> AffectedEntities { get; } = new();

    /// <summary>
    /// Amount of Willpower spent executing the skill.
    /// </summary>
    public int WillpowerSpent { get; set; } = 0;

    /// <summary>
    /// Creates a successful skill result.
    /// </summary>
    public static SkillResult CreateSuccess(string message = "")
    {
        var result = new SkillResult { Success = true };
        if (!string.IsNullOrEmpty(message))
        {
            result.Messages.Add(message);
        }
        return result;
    }

    /// <summary>
    /// Creates a failed skill result.
    /// </summary>
    public static SkillResult CreateFailure(string message = "")
    {
        var result = new SkillResult { Success = false };
        if (!string.IsNullOrEmpty(message))
        {
            result.Messages.Add(message);
        }
        return result;
    }

    /// <summary>
    /// Adds a message to the result.
    /// </summary>
    public void AddMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Messages.Add(message);
        }
    }

    /// <summary>
    /// Adds an affected entity to the result.
    /// </summary>
    public void AddAffectedEntity(BaseEntity entity)
    {
        if (entity != null && !AffectedEntities.Contains(entity))
        {
            AffectedEntities.Add(entity);
        }
    }

    /// <summary>
    /// Gets all messages as a single string.
    /// </summary>
    public string GetCombinedMessage()
    {
        return string.Join("\n", Messages);
    }
}
