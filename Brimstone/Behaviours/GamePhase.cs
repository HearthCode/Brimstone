using Brimstone.Actions;

namespace Brimstone
{
	// All of the game phase actions
	public partial class Behaviours
	{
		public static ActionGraph StartGame => new StartGame();
		public static ActionGraph BeginMulligan => new BeginMulligan();
		public static ActionGraph MulliganSelector => Select(e => ((Player)e).Hand.Slice(((Player)e).NumCardsDrawnThisTurn));
		public static ActionGraph MulliganChoice(ActionGraph Player = null) { return new CreateChoice { Args = { Player, MulliganSelector, (int)ChoiceType.MULLIGAN } }; }
		public static ActionGraph PerformMulligan => new PerformMulligan();
		public static ActionGraph WaitForMulliganComplete => new WaitForMulliganComplete();
		public static ActionGraph BeginTurn => new BeginTurn();
		public static ActionGraph BeginTurnTriggers => new BeginTurnTriggers();
		public static ActionGraph BeginTurnForPlayer => new BeginTurnForPlayer();
		public static ActionGraph EndTurn => new EndTurn();
		public static ActionGraph EndTurnForPlayer => new EndTurnForPlayer();
		public static ActionGraph EndTurnCleanupForPlayer => new EndTurnCleanupForPlayer();
	}
}
