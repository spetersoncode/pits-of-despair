using Godot;
using System.Collections.Generic;
using System.Linq;
using PitsOfDespair.Components;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Systems;

/// <summary>
/// Manages the energy-based time system for turn scheduling.
/// Time advances when the player acts, and creatures act when they have enough accumulated time.
/// </summary>
public partial class TimeSystem : Node
{
    #region Signals

    /// <summary>
    /// Emitted when time advances for all entities.
    /// </summary>
    [Signal]
    public delegate void TimeAdvancedEventHandler(int amount);

    #endregion

    #region Fields

    private readonly List<SpeedComponent> _trackedEntities = new();
    private SpeedComponent? _playerSpeed;

    #endregion

    #region Registration

    /// <summary>
    /// Registers the player's speed component (tracked separately).
    /// </summary>
    public void RegisterPlayer(SpeedComponent speedComponent)
    {
        _playerSpeed = speedComponent;
    }

    /// <summary>
    /// Registers a creature's speed component for time tracking.
    /// </summary>
    public void RegisterCreature(SpeedComponent speedComponent)
    {
        if (!_trackedEntities.Contains(speedComponent))
        {
            _trackedEntities.Add(speedComponent);
        }
    }

    /// <summary>
    /// Unregisters a creature's speed component.
    /// </summary>
    public void UnregisterCreature(SpeedComponent speedComponent)
    {
        _trackedEntities.Remove(speedComponent);
    }

    /// <summary>
    /// Clears all tracked creatures (e.g., on floor change).
    /// </summary>
    public void ClearCreatures()
    {
        _trackedEntities.Clear();
    }

    #endregion

    #region Time Management

    /// <summary>
    /// Advances time for all entities by the player's action delay.
    /// Called after the player completes an action.
    /// </summary>
    /// <param name="playerDelay">The delay cost of the player's action.</param>
    public void AdvanceTime(int playerDelay)
    {
        // Add time to all creatures
        foreach (var speed in _trackedEntities)
        {
            if (IsInstanceValid(speed))
            {
                speed.AddTime(playerDelay);
            }
        }

        EmitSignal(SignalName.TimeAdvanced, playerDelay);
    }

    /// <summary>
    /// Gets the next creature ready to act (fastest first).
    /// Returns null if no creatures have enough time to act.
    /// </summary>
    /// <param name="actionDelayCost">The base delay cost to check against (typically standard delay).</param>
    /// <returns>The SpeedComponent of the next creature to act, or null.</returns>
    public SpeedComponent? GetNextReadyCreature(int actionDelayCost)
    {
        SpeedComponent? fastest = null;
        int highestSpeed = 0;

        foreach (var speed in _trackedEntities)
        {
            if (!IsInstanceValid(speed))
            {
                continue;
            }

            // Check if creature has enough time to act based on its actual delay
            // Slow creatures need more accumulated time to act
            int actualDelay = speed.CalculateDelay(actionDelayCost);
            if (speed.AccumulatedTime >= actualDelay)
            {
                // Prefer faster creatures (higher EffectiveSpeed)
                if (fastest == null || speed.EffectiveSpeed > highestSpeed)
                {
                    fastest = speed;
                    highestSpeed = speed.EffectiveSpeed;
                }
            }
        }

        return fastest;
    }

    /// <summary>
    /// Gets all creatures ready to act, sorted by speed (fastest first).
    /// </summary>
    /// <param name="actionDelayCost">The base delay cost to check against.</param>
    /// <returns>List of ready creatures sorted by speed descending.</returns>
    public List<SpeedComponent> GetAllReadyCreatures(int actionDelayCost)
    {
        return _trackedEntities
            .Where(s => IsInstanceValid(s) && s.AccumulatedTime >= s.CalculateDelay(actionDelayCost))
            .OrderByDescending(s => s.EffectiveSpeed)
            .ToList();
    }

    /// <summary>
    /// Checks if any creatures are ready to act.
    /// </summary>
    public bool HasReadyCreatures(int actionDelayCost)
    {
        foreach (var speed in _trackedEntities)
        {
            if (IsInstanceValid(speed) && speed.AccumulatedTime >= speed.CalculateDelay(actionDelayCost))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Deducts time from a creature after it acts.
    /// </summary>
    public void DeductCreatureTime(SpeedComponent speed, int delay)
    {
        speed.DeductTime(delay);
    }

    /// <summary>
    /// Resets all accumulated time (e.g., on floor change).
    /// </summary>
    public void ResetAllTime()
    {
        _playerSpeed?.ResetTime();

        foreach (var speed in _trackedEntities)
        {
            if (IsInstanceValid(speed))
            {
                speed.ResetTime();
            }
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Removes invalid (freed) speed components from tracking.
    /// </summary>
    public void CleanupInvalid()
    {
        _trackedEntities.RemoveAll(s => !IsInstanceValid(s));
    }

    #endregion
}
