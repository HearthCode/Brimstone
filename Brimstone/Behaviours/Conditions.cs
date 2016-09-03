namespace Brimstone
{
	// All of the trigger conditions you can use
	public partial class Behaviours
	{
		public static Condition IsSelf = new Condition((me, other) => me == other);
		public static Condition IsFriendlySpell = new Condition((me, other) => me.Controller == other.Controller && other is Spell);
		public static Condition IsFriendly = new Condition((me, other) => me.Controller == other.Controller);
		public static Condition IsOpposing = new Condition((me, other) => me.Controller == other.Controller.Opponent);
	}
}
