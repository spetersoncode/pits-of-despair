using Godot;
using PitsOfDespair.Actions;

namespace PitsOfDespair.Components;

/// <summary>
/// Manages entity speed and accumulated time for the energy-based turn system.
/// Speed determines how quickly an entity accumulates time to act.
/// Higher speed = faster actions, lower delay.
/// </summary>
public partial class SpeedComponent : Node
{
    #region Constants

    /// <summary>
    /// Base speed value representing average speed.
    /// Delay = 100 / Speed, so speed 10 = 10 aut delay.
    /// </summary>
    public const int AverageSpeed = 10;

    /// <summary>
    /// Minimum speed (cannot go below this even with penalties).
    /// </summary>
    public const int MinSpeed = 1;

    /// <summary>
    /// Minimum delay floor (speed cannot reduce delay below this except with haste).
    /// </summary>
    public const int MinDelay = 6;

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when speed value changes.
    /// </summary>
    [Signal]
    public delegate void SpeedChangedEventHandler(int newSpeed);

    #endregion

    #region Properties

    /// <summary>
    /// Base speed value. 10 = average.
    /// Higher = faster (acts more often), Lower = slower.
    /// </summary>
    [Export]
    public int BaseSpeed { get; set; } = AverageSpeed;

    /// <summary>
    /// Accumulated time units. Entity can act when this reaches the action's delay cost.
    /// </summary>
    public int AccumulatedTime { get; set; } = 0;

    /// <summary>
    /// Whether this entity is currently hasted (can bypass minimum delay floor).
    /// </summary>
    public bool IsHasted { get; set; } = false;

    /// <summary>
    /// Current effective speed after modifiers.
    /// </summary>
    public int EffectiveSpeed => Mathf.Max(MinSpeed, BaseSpeed + GetSpeedModifierTotal());

    #endregion

    #region Speed Modifiers

    private readonly System.Collections.Generic.Dictionary<string, int> _speedModifiers = new();

    /// <summary>
    /// Adds a speed modifier from a named source.
    /// </summary>
    public void AddSpeedModifier(string sourceId, int amount)
    {
        _speedModifiers[sourceId] = amount;
        EmitSignal(SignalName.SpeedChanged, EffectiveSpeed);
    }

    /// <summary>
    /// Removes a speed modifier by source.
    /// </summary>
    public void RemoveSpeedModifier(string sourceId)
    {
        if (_speedModifiers.Remove(sourceId))
        {
            EmitSignal(SignalName.SpeedChanged, EffectiveSpeed);
        }
    }

    /// <summary>
    /// Gets total speed modifier from all sources.
    /// </summary>
    public int GetSpeedModifierTotal()
    {
        int total = 0;
        foreach (var modifier in _speedModifiers.Values)
        {
            total += modifier;
        }
        return total;
    }

    #endregion

    #region Delay Calculation

    /// <summary>
    /// Calculates the actual delay for an action based on entity speed.
    /// Formula: actionDelay * (AverageSpeed / EffectiveSpeed)
    /// Applies weighted random rounding for fractional results.
    /// </summary>
    /// <param name="actionDelayCost">The base delay cost of the action.</param>
    /// <returns>Actual delay in aut after speed modifier.</returns>
    public int CalculateDelay(int actionDelayCost)
    {
        int speed = EffectiveSpeed;

        // Calculate delay: baseDelay * (10 / speed)
        // Speed 10 = 1x delay, Speed 20 = 0.5x delay, Speed 5 = 2x delay
        float rawDelay = actionDelayCost * ((float)AverageSpeed / speed);

        // Apply weighted random rounding for fractional values
        int delay = WeightedRound(rawDelay);

        // Apply minimum delay floor (unless hasted)
        if (!IsHasted && delay < MinDelay)
        {
            delay = MinDelay;
        }

        return delay;
    }

    /// <summary>
    /// Weighted random rounding. A value of 6.4 has 60% chance of 6, 40% chance of 7.
    /// </summary>
    private static int WeightedRound(float value)
    {
        int floor = (int)value;
        float fraction = value - floor;

        if (fraction == 0)
        {
            return floor;
        }

        // Random roll: if roll < fraction, round up
        return GD.Randf() < fraction ? floor + 1 : floor;
    }

    #endregion

    #region Speed Description

    /// <summary>
    /// Gets a descriptive string for the entity's speed.
    /// Based on DCSS speed descriptors.
    /// </summary>
    public string GetSpeedDescription()
    {
        int speed = EffectiveSpeed;

        // Convert speed to effective delay for a standard action
        float effectiveDelay = (float)ActionDelay.Standard * AverageSpeed / speed;

        return effectiveDelay switch
        {
            <= 6 => "Very Quick",
            <= 7 => "Quick",
            <= 8 => "Quick",
            <= 9 => "Above Average",
            <= 10 => "Average",
            <= 11 => "Below Average",
            <= 12 => "Slow",
            <= 13 => "Slow",
            _ => "Very Slow"
        };
    }

    #endregion

    #region Time Management

    /// <summary>
    /// Adds time to the accumulator.
    /// </summary>
    public void AddTime(int amount)
    {
        AccumulatedTime += amount;
    }

    /// <summary>
    /// Deducts time after taking an action.
    /// </summary>
    public void DeductTime(int amount)
    {
        AccumulatedTime -= amount;
        if (AccumulatedTime < 0)
        {
            AccumulatedTime = 0;
        }
    }

    /// <summary>
    /// Checks if entity has enough accumulated time for an action.
    /// </summary>
    public bool CanAct(int actionDelayCost)
    {
        return AccumulatedTime >= CalculateDelay(actionDelayCost);
    }

    /// <summary>
    /// Resets accumulated time (e.g., on floor change).
    /// </summary>
    public void ResetTime()
    {
        AccumulatedTime = 0;
    }

    #endregion
}
