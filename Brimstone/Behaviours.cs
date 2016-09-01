using System;
using System.Collections.Generic;
using System.Linq;

namespace Brimstone
{
	public class Behaviour {
		// Defaulting to null for unimplemented cards or actions
		public ActionGraph Battlecry;
		public ActionGraph Deathrattle;
		public List<ITrigger> Triggers = new List<ITrigger>();
	}

	public class Actions {
		// Factory functions for DSL syntax
		public static ActionGraph BeginTurn { get { return new BeginTurn(); } }
		public static ActionGraph EndTurn { get { return new EndTurn(); } }
		public static ActionGraph Concede(ActionGraph Player = null) { return new Concede { Args = { Player } }; }

		public static ActionGraph Draw(ActionGraph Player = null) { return new Draw { Args = { Player } }; }
		public static ActionGraph Give(ActionGraph Player = null, ActionGraph Card = null) { return new Give { Args = { Player, Card } }; }
		public static ActionGraph Play(ActionGraph Entity = null) { return new Play { Args = { Entity } }; }
		public static ActionGraph Attack(ActionGraph Attacker = null, ActionGraph Defender = null) { return new Attack { Args = { Attacker, Defender } }; }
		public static ActionGraph Damage(ActionGraph Targets = null, ActionGraph Amount = null) { return new Damage { Args = { Targets, Amount } }; }
		public static ActionGraph Death(ActionGraph Targets = null) { return new Death { Args = { Targets } }; }

		// TODO: Write all common selectors
		public static Selector Self { get { return Select(e => e); } }
		public static Selector Controller { get { return Select(e => e.Controller); } }
		public static Selector Target { get { return Select(e => ((ICanTarget)e).Target); } }
		public static Selector CurrentPlayer { get { return Select(e => e.Game.CurrentPlayer); } }
		public static Selector FriendlyHero { get { return Select(e => e.Controller.Hero); } }
		public static Selector OpponentHero { get { return Select(e => e.Controller.Opponent.Hero); } }
		public static Selector AllMinions { get { return Select(e => e.Game.Player1.Board.Concat(e.Game.Player2.Board).Where(x => x.Health > 0)); } }
		public static Selector OpponentCharacters { get { return Union(OpponentMinions, OpponentHero); } }
		public static Selector OpponentMinions { get { return Select(e => e.Controller.Opponent.Board.Where(x => x.Health > 0)); } }
		public static Selector AllCharacters { get { return Select(e => e.Game.Characters); } }
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
				throw new SelectorException("Selector union requires at least 2 arguments");

			if (s.Length > 2)
				s[1] = Union(s.Skip(1).ToArray());

			if (s[0].SelectionSource != s[1].SelectionSource)
				throw new SelectorException("All selectors in a union must use the same selection source");

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

		// Generic triggers (use to create triggers for events not specified in Triggers section below)
		public static Trigger<T, U> At<T, U>(TriggerType TriggerType, Condition<T, U> Condition, ActionGraph Action)
			where T : IEntity where U : IEntity {
			return Trigger<T, U>.At(TriggerType, Action, Condition);
		}

		public static Trigger<T, U> At<T, U>(TriggerType TriggerType, ActionGraph Action)
			where T : IEntity where U : IEntity {
			return Trigger<T, U>.At(TriggerType, Action);
		}

		// Triggers
		public static Trigger<ICharacter, ICharacter> OnDamage(Condition<ICharacter, ICharacter> Condition, ActionGraph Action) {
			return Trigger<ICharacter, ICharacter>.At(TriggerType.Damage, Action, Condition);
		}
		public static Trigger<IEntity, Spell> AfterPlaySpell(Condition<IEntity, Spell> Condition, ActionGraph Action) {
			return Trigger<IEntity, Spell>.At(TriggerType.Damage, Action, Condition);
		}

		// Trigger conditions
		public static Condition<ICharacter, ICharacter> IsSelf = new Condition<ICharacter, ICharacter>((me, other) => me == other);
		public static Condition<IEntity, Spell> IsFriendlySpell = new Condition<IEntity, Spell>((me, spell) => me.Controller == spell.Controller);
	}
}
