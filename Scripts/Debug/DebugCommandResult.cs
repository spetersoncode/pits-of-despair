namespace PitsOfDespair.Debug;

/// <summary>
/// Describes what type of entity to spawn when targeting completes.
/// </summary>
public enum SpawnEntityType
{
    Creature,
    Item
}

/// <summary>
/// Request for targeting to spawn an entity at a selected position.
/// </summary>
public class TargetingRequest
{
    /// <summary>
    /// The entity ID to spawn (creature or item ID from data files).
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// Whether to spawn a creature or item.
    /// </summary>
    public SpawnEntityType EntityType { get; }

    /// <summary>
    /// If true, spawned creature will be added to Player faction as an ally.
    /// </summary>
    public bool MakeAlly { get; }

    public TargetingRequest(string entityId, SpawnEntityType entityType, bool makeAlly = false)
    {
        EntityId = entityId;
        EntityType = entityType;
        MakeAlly = makeAlly;
    }
}

/// <summary>
/// Result object returned by debug command execution.
/// </summary>
public class DebugCommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string MessageColor { get; set; }

    /// <summary>
    /// If set, the command requires targeting before completing.
    /// The console should initiate targeting and spawn the entity on confirmation.
    /// </summary>
    public TargetingRequest TargetingRequest { get; set; }

    /// <summary>
    /// True if this result requires targeting interaction before completion.
    /// </summary>
    public bool RequiresTargeting => TargetingRequest != null;

    public DebugCommandResult(bool success, string message, string messageColor)
    {
        Success = success;
        Message = message;
        MessageColor = messageColor;
    }

    public static DebugCommandResult CreateSuccess(string message, string color)
    {
        return new DebugCommandResult(true, message, color);
    }

    public static DebugCommandResult CreateFailure(string message, string color)
    {
        return new DebugCommandResult(false, message, color);
    }

    /// <summary>
    /// Create a result that requires targeting to spawn an entity.
    /// </summary>
    public static DebugCommandResult CreateTargetingRequest(string entityId, SpawnEntityType entityType, bool makeAlly = false)
    {
        return new DebugCommandResult(true, null, null)
        {
            TargetingRequest = new TargetingRequest(entityId, entityType, makeAlly)
        };
    }
}
