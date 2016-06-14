namespace Brimstone
{
	public class Behaviour
	{
		// Defaulting to null for unimplemented cards or actions
		public ActionGraph Battlecry;
		public ActionGraph Deathrattle;
	}

	public partial class CardBehaviour
	{
		// Factory functions for DSL syntax
		public static ActionGraph RandomOpponentMinion { get { return new RandomOpponentMinion(); } }
		public static ActionGraph AllMinions { get { return new AllMinions(); } }
		public static ActionGraph RandomAmount(ActionGraph min, ActionGraph max) { return new RandomAmount { Args = { min, max } }; }
		public static ActionGraph Give(ActionGraph target, ActionGraph card) { return new Give { Args = { target, card } }; }
		public static ActionGraph Play(ActionGraph player, ActionGraph entity) { return new Play { Args = { player, entity } }; }
		public static ActionGraph Damage(ActionGraph target, ActionGraph amount) { return new Damage { Args = { target, amount } }; }
	}
}