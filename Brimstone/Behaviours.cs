using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Behaviour {
		// Defaulting to null for unimplemented cards or actions
		public ActionGraph Battlecry;
		public ActionGraph Deathrattle;
		public List<Trigger> Triggers = new List<Trigger>();
	}

	public partial class CardBehaviour
	{
		// Factory functions for DSL syntax
		public static ActionGraph BeginTurn { get { return new BeginTurn(); } }

		public static ActionGraph Draw(ActionGraph player) { return new Draw { Args = { player } }; }
		public static ActionGraph Give(ActionGraph player, ActionGraph card) { return new Give { Args = { player, card } }; }
		public static ActionGraph Play(ActionGraph entity) { return new Play { Args = { entity } }; }

		public static ActionGraph CreateMulligan(ActionGraph player) { return Select(p => p.Hand.Slice(1, p.NumCardsDrawnThisTurn)); }

		// TODO: Write all common selectors
		public static ActionGraph Self { get { return Select(e => e); } }
		public static ActionGraph Controller { get { return Select(e => e.Controller); } }
		public static ActionGraph AllMinions { get { return Select(g => g.CurrentPlayer.Board.Concat(g.CurrentPlayer.Opponent.Board)); } }
		public static ActionGraph OpponentMinions { get { return Select(p => p.Opponent.Board); } }

		public static ActionGraph RandomOpponentMinion { get { return new RandomChoice { Args = { OpponentMinions } }; } }
		public static ActionGraph RandomAmount(ActionGraph min, ActionGraph max) { return new RandomAmount { Args = { min, max } }; }

		// TODO: Add selector set ops

		public static ActionGraph Select(Func<IEntity, IEntity> selector) {
			return new Selector {
				SelectionSource = SelectionSource.ActionSource,
				Lambda = e => new List<IEntity> { selector(e) }
			};
		}
		public static ActionGraph Select(Func<IEntity, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.ActionSource,
				Lambda = selector
			};
		}
		public static ActionGraph Select(Func<Player, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.Player,
				Lambda = e => selector((Player)e)
			};
		}
		public static ActionGraph Select(Func<Game, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.Game,
				Lambda = e => selector((Game)e)
			};
		}

		public static ActionGraph Damage(ActionGraph target = null, ActionGraph amount = null) { return new Damage { Args = { target, amount } }; }

		// Event helpers
		public static Trigger When(ActionGraph trigger, ActionGraph action) { return Trigger.When(trigger, action); }
		public static Trigger After(ActionGraph trigger, ActionGraph action) { return Trigger.After(trigger, action); }
	}
}