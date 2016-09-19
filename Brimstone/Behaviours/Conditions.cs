using Brimstone.Entities;

namespace Brimstone
{
	// All of the trigger conditions you can use
	public partial class Behaviours
	{
		public static Condition IsSelf { get; } = new Condition((me, other) => me == other);
		public static Condition IsFriendlySpell { get; } = new Condition((me, other) => me.Controller == other.Controller && other is Spell);
		public static Condition IsFriendly { get; } = new Condition((me, other) => me.Controller == other.Controller);
		public static Condition IsFriendlyMinion { get; } = new Condition((me, other) => me.Controller == other.Controller && other is Minion);
		public static Condition IsOpposing { get; } = new Condition((me, other) => me.Controller == other.Controller.Opponent);
	}
}
