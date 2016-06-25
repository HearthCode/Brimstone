using System;
using System.Collections.Generic;
using System.Linq;

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
		public static QueueAction BeginTurn { get { return new BeginTurn(); } }

		public static ActionGraph Draw(ActionGraph player) { return new Draw { Args = { player } }; }
		public static ActionGraph Give(ActionGraph player, ActionGraph card) { return new Give { Args = { player, card } }; }
		public static ActionGraph Play(ActionGraph player, ActionGraph entity) { return new Play { Args = { player, entity } }; }

		public static ActionGraph CreateMulligan(ActionGraph player) { return Select(player, p => p.Hand.Slice(1, p.NumCardsDrawnThisTurn)); }

		public static QueueAction RandomOpponentMinion { get { return new RandomChoice { Args = { OpponentMinions } }; } }

		public static QueueAction AllMinions { get { return Select(g => g.CurrentPlayer.InPlay.Concat(g.CurrentPlayer.Opponent.InPlay)); } }
		// TODO: Fix fundamental problem of action source not being sent to Run()
		public static QueueAction OpponentMinions { get { return Select(g => g.CurrentPlayer.Opponent.InPlay); } }
		public static ActionGraph RandomAmount(ActionGraph min, ActionGraph max) { return new RandomAmount { Args = { min, max } }; }

		public static QueueAction Select(Func<Game, IEnumerable<IEntity>> selector) {
			return new Selector {
				Lambda = (g => selector((Game)g))
			};
		}
		public static QueueAction Select(ActionGraph source, Func<IEntity, IEnumerable<IEntity>> selector) {
			return new Selector {
				Args = { source },
				Lambda = selector };
		}
		public static QueueAction Select(ActionGraph source, Func<Player, IEnumerable<IEntity>> selector) {
			return new Selector {
				Args = { source },
				Lambda = (p => selector((Player)p))
			};
		}

		public static ActionGraph Damage(ActionGraph target, ActionGraph amount) { return new Damage { Args = { target, amount } }; }
	}
}