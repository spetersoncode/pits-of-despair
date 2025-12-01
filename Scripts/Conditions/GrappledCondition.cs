using PitsOfDespair.Core;
using PitsOfDespair.Entities;

namespace PitsOfDespair.Conditions;

/// <summary>
/// Condition that prevents an entity from moving.
/// Applied by the Grapple skill - breaks automatically if the grappler moves away.
/// Note: Turn loss is handled by applying Daze condition separately.
/// </summary>
public class GrappledCondition : Condition
{
	public override string Name => "Grappled";

	public override string TypeId => "grappled";

	public override string? ExamineDescription => "grappled";

	/// <summary>
	/// The entity holding this creature in a grapple.
	/// Used to break the grapple when the grappler moves.
	/// </summary>
	public BaseEntity? Grappler { get; set; }

	/// <summary>
	/// Flag set when the grapple is broken by the grappler moving.
	/// Used to display the appropriate removal message.
	/// </summary>
	public bool WasBrokenByMovement { get; set; }

	public GrappledCondition()
	{
		Duration = "3";
	}

	public GrappledCondition(string duration)
	{
		Duration = duration;
	}

	public override ConditionMessage OnApplied(BaseEntity target)
	{
		return new ConditionMessage(
			$"The {target.DisplayName} is grappled!",
			Palette.ToHex(Palette.StatusDebuff)
		);
	}

	public override ConditionMessage OnTurnProcessed(BaseEntity target)
	{
		return ConditionMessage.Empty;
	}

	public override ConditionMessage OnRemoved(BaseEntity target)
	{
		if (WasBrokenByMovement)
		{
			// Message handled by MoveAction for the grappler's perspective
			return ConditionMessage.Empty;
		}

		return new ConditionMessage(
			$"The {target.DisplayName} breaks free from the grapple!",
			Palette.ToHex(Palette.Default)
		);
	}
}
