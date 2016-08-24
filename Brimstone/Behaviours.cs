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

	public partial class Actions {
		// Factory functions for DSL syntax
		public static ActionGraph BeginTurn { get { return new BeginTurn(); } }
		public static ActionGraph EndTurn { get { return new EndTurn(); } }

		public static ActionGraph Draw(ActionGraph Player = null) { return new Draw { Args = { Player } }; }
		public static ActionGraph Give(ActionGraph Player = null, ActionGraph Card = null) { return new Give { Args = { Player, Card } }; }
		public static ActionGraph Play(ActionGraph Entity = null) { return new Play { Args = { Entity } }; }

		// TODO: Write all common selectors
		public static Selector Self { get { return Select(e => e); } }
		public static Selector Controller { get { return Select(e => e.Controller); } }
		public static Selector CurrentPlayer { get { return Select(e => e.Game.CurrentPlayer); } }
		public static Selector FriendlyHero { get { return Select(e => ((Player)e.Controller).Hero); } }
		public static Selector OpponentHero { get { return Select(e => ((Player)e.Controller).Opponent.Hero); } }
		public static Selector AllMinions { get { return Select(e => e.Game.Player1.Board.Concat(e.Game.Player2.Board).Where(x => ((Character)x).Health > 0)); } }
		public static Selector OpponentCharacters { get { return Union(OpponentMinions, OpponentHero); } }
		public static Selector OpponentMinions { get { return Select(e => ((Player)e.Controller).Opponent.Board.Where(x => ((Character)x).Health > 0)); } }
		public static Selector AllCharacters { get { return Union(AllMinions, FriendlyHero, OpponentHero); } }
		public static ActionGraph MulliganChoice(ActionGraph Player = null) { return new CreateChoice { Args = { Player, MulliganSelector, (int)ChoiceType.MULLIGAN } }; }
		public static ActionGraph Random(Selector Selector = null) { return new RandomChoice { Args = { Selector } }; }
		public static ActionGraph RandomOpponentMinion { get { return Random(OpponentMinions); } }
		public static ActionGraph RandomOpponentCharacter { get { return Random(OpponentCharacters); } }
		public static ActionGraph RandomAmount(ActionGraph Min = null, ActionGraph Max = null) { return new RandomAmount { Args = { Min, Max } }; }

		public static ActionGraph MulliganSelector { get { return Select(e => ((Player)e).Hand.Slice(((Player)e).NumCardsDrawnThisTurn)); } }

		public static ActionGraph Choose(ActionGraph Player = null) { return new Choose { Args = { Player } }; }

		// TODO: Add selector set ops
		public static Selector Union(params Selector[] s) {
			if (s.Length < 2)
				throw new ArgumentException();

			if (s.Length > 2)
				s[1] = Union(s.Skip(1).ToArray());

			if (s[0].SelectionSource != s[1].SelectionSource)
				throw new ArgumentException();

			var sel = new Selector {
				SelectionSource = s[0].SelectionSource,
				Lambda = e => s[0].Lambda(e).Concat(s[1].Lambda(e))
			};
			return sel;
		}

		public static Selector Select(Func<IEntity, IEntity> selector) {
			return new Selector {
				SelectionSource = SelectionSource.ActionSource,
				Lambda = e => new List<IEntity> { selector(e) }
			};
		}
		public static Selector Select(Func<IEntity, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.ActionSource,
				Lambda = selector
			};
		}
		/*
		public static Selector Select(Func<Player, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.Player,
				Lambda = e => selector((Player)e)
			};
		}
		public static Selector Select(Func<Game, IEnumerable<IEntity>> selector) {
			return new Selector {
				SelectionSource = SelectionSource.Game,
				Lambda = e => selector((Game)e)
			};
		}
		*/
		public static ActionGraph Damage(ActionGraph Targets = null, ActionGraph Amount = null) { return new Damage { Args = { Targets, Amount } }; }
		public static ActionGraph Death(ActionGraph Targets = null) { return new Death { Args = { Targets } }; }

		// Event helpers
		public static Trigger When(ActionGraph trigger, ActionGraph action) { return Trigger.When(trigger, action); }
		public static Trigger After(ActionGraph trigger, ActionGraph action) { return Trigger.After(trigger, action); }
	}
}
