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

	public partial class CardBehaviour {
		// Factory functions for DSL syntax
		public static ActionGraph BeginTurn { get { return new BeginTurn(); } }

		public static ActionGraph Draw(ActionGraph Player = null) { return new Draw { Args = { Player } }; }
		public static ActionGraph Give(ActionGraph Player = null, ActionGraph Card = null) { return new Give { Args = { Player, Card } }; }
		public static ActionGraph Play(ActionGraph Entity = null) { return new Play { Args = { Entity } }; }

		public static ActionGraph CreateMulligan { get { return Select(p => p.Hand.Slice(p.NumCardsDrawnThisTurn)); } }

		// TODO: Write all common selectors
		public static ActionGraph Self { get { return Select(e => e); } }
		public static ActionGraph Controller { get { return Select(e => e.Controller); } }
		public static ActionGraph CurrentPlayer { get { return Select(e => e.Game.CurrentPlayer); } }
		public static ActionGraph AllMinions { get { return Select(g => g.CurrentPlayer.Board.Concat(g.CurrentPlayer.Opponent.Board)); } }
		public static ActionGraph OpponentMinions { get { return Select(p => p.Opponent.Board); } }

		public static ActionGraph RandomOpponentMinion { get { return new RandomChoice { Args = { OpponentMinions } }; } }
		public static ActionGraph RandomAmount(ActionGraph Min, ActionGraph Max) { return new RandomAmount { Args = { Min, Max } }; }

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

		public static ActionGraph Damage(ActionGraph Targets = null, ActionGraph Amount = null) { return new Damage { Args = { Targets, Amount } }; }
		public static ActionGraph Death(ActionGraph Targets = null) { return new Death { Args = { Targets } }; }

		// Event helpers
		public static Trigger When(ActionGraph trigger, ActionGraph action) { return Trigger.When(trigger, action); }
		public static Trigger After(ActionGraph trigger, ActionGraph action) { return Trigger.After(trigger, action); }
	}
}