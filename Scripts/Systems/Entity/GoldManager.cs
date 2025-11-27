using Godot;

namespace PitsOfDespair.Systems.Entity;

/// <summary>
/// Manages the player's gold currency for the current game session.
/// Tracks gold collected and spent, emits signals for UI updates.
/// </summary>
public partial class GoldManager : Node
{
    /// <summary>
    /// Emitted when gold amount changes.
    /// Parameters: amount changed, total gold.
    /// </summary>
    [Signal]
    public delegate void GoldChangedEventHandler(int amount, int totalGold);

    /// <summary>
    /// Current gold amount.
    /// </summary>
    public int Gold { get; private set; } = 0;

    /// <summary>
    /// Adds gold to the player's total.
    /// </summary>
    /// <param name="amount">Amount of gold to add (must be positive).</param>
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            GD.PushWarning($"GoldManager: Attempted to add non-positive gold amount: {amount}");
            return;
        }

        Gold += amount;
        EmitSignal(SignalName.GoldChanged, amount, Gold);
    }

    /// <summary>
    /// Removes gold from the player's total.
    /// </summary>
    /// <param name="amount">Amount of gold to remove (must be positive).</param>
    /// <returns>True if gold was removed successfully, false if insufficient gold.</returns>
    public bool RemoveGold(int amount)
    {
        if (amount <= 0)
        {
            GD.PushWarning($"GoldManager: Attempted to remove non-positive gold amount: {amount}");
            return false;
        }

        if (Gold < amount)
        {
            return false; // Insufficient gold
        }

        Gold -= amount;
        EmitSignal(SignalName.GoldChanged, -amount, Gold);
        return true;
    }

    /// <summary>
    /// Resets gold to zero.
    /// Used when starting a new game.
    /// </summary>
    public void ResetGold()
    {
        int oldGold = Gold;
        Gold = 0;
        if (oldGold != 0)
        {
            EmitSignal(SignalName.GoldChanged, -oldGold, Gold);
        }
    }
}
