using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Behaviour {
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
		public static ActionGraph Play(ActionGraph entity) { return new Play { Args = { entity } }; }

		public static ActionGraph CreateMulligan(ActionGraph player) { return Select(p => p.Hand.Slice(1, p.NumCardsDrawnThisTurn)); }

		// TODO: Write all common selectors
		public static QueueAction AllMinions { get { return Select(g => g.CurrentPlayer.Board.Concat(g.CurrentPlayer.Opponent.Board)); } }
		public static QueueAction OpponentMinions { get { return Select(p => p.Opponent.Board); } }

		public static QueueAction RandomOpponentMinion { get { return new RandomChoice { Args = { OpponentMinions } }; } }
		public static ActionGraph RandomAmount(ActionGraph min, ActionGraph max) { return new RandomAmount { Args = { min, max } }; }

		// TODO: Add selector set ops

		public static QueueAction Select(Func<IEntity, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.ActionSource,
				Lambda = selector
			};
		}
		public static QueueAction Select(Func<Player, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.Player,
				Lambda = e => selector((Player)e)
			};
		}
		public static QueueAction Select(Func<Game, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.Game,
				Lambda = e => selector((Game)e)
			};
		}

		public static ActionGraph Damage(ActionGraph target, ActionGraph amount) { return new Damage { Args = { target, amount } }; }
	}
}