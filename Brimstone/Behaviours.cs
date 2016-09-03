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
		public static ActionGraph StartGame { get { return new StartGame(); } }
		public static ActionGraph BeginMulligan { get { return new BeginMulligan(); } }
		public static ActionGraph PerformMulligan { get { return new PerformMulligan(); } }
		public static ActionGraph WaitForMulliganComplete { get { return new WaitForMulliganComplete(); } }
		public static ActionGraph BeginTurn { get { return new BeginTurn(); } }
		public static ActionGraph BeginTurnTriggers { get { return new BeginTurnTriggers(); } }
		public static ActionGraph BeginTurnForPlayer { get { return new BeginTurnForPlayer(); } }
		public static ActionGraph EndTurn { get { return new EndTurn(); } }
		public static ActionGraph EndTurnForPlayer { get { return new EndTurnForPlayer(); } }
		public static ActionGraph EndTurnCleanupForPlayer { get { return new EndTurnCleanupForPlayer(); } }
		public static ActionGraph Concede(ActionGraph Player = null) { return new Concede { Args = { Player } }; }

		public static ActionGraph Draw(ActionGraph Player = null) { return new Draw { Args = { Player } }; }
		public static ActionGraph Give(ActionGraph Player = null, ActionGraph Card = null) { return new Give { Args = { Player, Card } }; }
		public static ActionGraph Play(ActionGraph Entity = null) { return new Play { Args = { Entity } }; }
		public static ActionGraph Attack(ActionGraph Attacker = null, ActionGraph Defender = null) { return new Attack { Args = { Attacker, Defender } }; }

		// Simple selectors
		public static Selector Self { get { return Select(e => e); } }
		public static Selector Controller { get { return Select(e => e.Controller); } }
		public static Selector Opponent { get { return Select(e => e.Controller.Opponent); } }
		public static Selector Target { get { return Select(e => ((ICanTarget)e).Target); } }
		public static Selector Players { get { return Select(e => e.Game.Players); } }
		public static Selector CurrentPlayer { get { return Select(e => e.Game.CurrentPlayer); } }
		public static Selector CurrentInactivePlayer { get { return Select(e => e.Game.CurrentOpponent); } }
		// Selectors for heroes
		public static Selector FriendlyHero { get { return Select(e => e.Controller.Hero); } }
		public static Selector OpponentHero { get { return Select(e => e.Controller.Opponent.Hero); } }
		// Selectors for weapons
		//public static Selector FriendlyWeapon { get { throw new NotImplementedException(); } } // TODO: implement
		//public static Selector OpponentWeapon { get { throw new NotImplementedException(); } } // TODO: implement
		// Selectors for minions and characters - all
		public static Selector AllMinions { get { return Select(e => e.Game.Player1.Board.Concat(e.Game.Player2.Board)); } }
		public static Selector FriendlyMinions { get { return Select(e => e.Controller.Board); } }
		public static Selector OpponentMinions { get { return Select(e => e.Controller.Opponent.Board); } }
		public static Selector AllCharacters { get { return Select(e => e.Game.Characters); } }
		public static Selector FriendlyCharacters { get { return Union(FriendlyMinions, FriendlyHero); } }
		public static Selector OpponentCharacters { get { return Union(OpponentMinions, OpponentHero); } }
		public static Selector FriendlyMinionsInDeck { get { return Select(e => e.Controller.Deck.Where(x => x.Card.Type == CardType.MINION)); } }
		public static Selector OpponentMinionsInDeck { get { return Select(e => e.Controller.Opponent.Deck.Where(x => x.Card.Type == CardType.MINION)); } }
		public static Selector FriendlyMinionsInHand { get { return Select(e => e.Controller.Hand.Where(x => x.Card.Type == CardType.MINION)); } }
		public static Selector AdjacentMinions { get { return Select(e => e.Controller.Board.Where(x => x.ZonePosition == e.ZonePosition + 1 || x.ZonePosition == e.ZonePosition - 1)); } }

		// Selectors for minions/heroes/characters - healthy
		public static Selector FriendlyHealthyHero { get { return Select(e => new List<Hero> { e.Controller.Hero }.Where(x => !x.MortallyWounded)); } }
		public static Selector OpponentHealthyHero { get { return Select(e => new List<Hero> { e.Controller.Opponent.Hero }.Where(x => !x.MortallyWounded)); } }
		public static Selector AllHealthyMinions { get { return Select(e => e.Game.Player1.Board.Concat(e.Game.Player2.Board).Where(x => !x.MortallyWounded)); } }
		public static Selector FriendlyHealthyMinions { get { return Select(e => e.Controller.Board.Where(x => !x.MortallyWounded)); } }
		public static Selector OpponentHealthyMinions { get { return Select(e => e.Controller.Opponent.Board.Where(x => !x.MortallyWounded)); } }
		public static Selector AllHealthyCharacters { get { return Select(e => e.Game.Characters.Where(x => !x.MortallyWounded)); } }
		public static Selector FriendlyHealthyCharacters { get { return Union(FriendlyHealthyMinions, FriendlyHealthyHero); } }
		public static Selector OpponentHealthyCharacters { get { return Union(OpponentHealthyMinions, OpponentHealthyHero); } }

		// Selectors for cards being played
		//public static Selector FriendlySpell { get { return Select(e => e.Game.Environment.LastCardPlayed.Controller == e.Controller && e.Game.Environment.LastCardPlayed.Card.Type == CardType.SPELL)} }
		// TODO: more selectors, more actiongraphs
		// Actiongraphs
		public static ActionGraph Random(Selector Selector = null) { return new RandomChoice { Args = { Selector } }; }
		// Random minion/character - all
		public static ActionGraph RandomMinion { get { return Random(AllMinions); } }
		public static ActionGraph RandomFriendlyMinion { get { return Random(FriendlyMinions); } }
		public static ActionGraph RandomOpponentMinion { get { return Random(OpponentMinions); } }
		public static ActionGraph RandomCharacter { get { return Random(AllCharacters); } }
		public static ActionGraph RandomFriendlyCharacter { get { return Random(FriendlyCharacters); } }
		public static ActionGraph RandomOpponentCharacter { get { return Random(OpponentCharacters); } }
		public static ActionGraph RandomFriendlyMinionInDeck { get { return Random(FriendlyMinionsInDeck); } }
		public static ActionGraph RandomOpponentMinionInDeck { get { return Random(OpponentMinionsInDeck); } }
		public static ActionGraph RandomFriendlyMinionInHand { get { return Random(FriendlyMinionsInHand); } }

		// Random minion/character - healthy
		public static ActionGraph RandomHealthyMinion { get { return Random(AllHealthyMinions); } }
		public static ActionGraph RandomFriendlyHealthyMinion { get { return Random(FriendlyHealthyMinions); } }
		public static ActionGraph RandomOpponentHealthyMinion { get { return Random(OpponentHealthyMinions); } }
		public static ActionGraph RandomHealthyCharacter { get { return Random(AllHealthyCharacters); } }
		public static ActionGraph RandomFriendlyHealthyCharacter { get { return Random(FriendlyHealthyCharacters); } }
		public static ActionGraph RandomOpponentHealthyCharacter { get { return Random(OpponentHealthyCharacters); } }
		// Actiongraphs continue
		public static ActionGraph MulliganChoice(ActionGraph Player = null) { return new CreateChoice { Args = { Player, MulliganSelector, (int)ChoiceType.MULLIGAN } }; }
		public static ActionGraph RandomAmount(ActionGraph Min = null, ActionGraph Max = null) { return new RandomAmount { Args = { Min, Max } }; }

		public static ActionGraph MulliganSelector { get { return Select(e => ((Player)e).Hand.Slice(((Player)e).NumCardsDrawnThisTurn)); } }

		public static ActionGraph Choose(ActionGraph Player = null) { return new Choose { Args = { Player } }; }

		// TODO: Add selector set ops
		public static Selector Union(params Selector[] s) {
			if (s.Length < 2)
				throw new SelectorException("Selector union requires at least 2 arguments");

			if (s.Length > 2)
				s[1] = Union(s.Skip(1).ToArray());

			var sel = new Selector {
				Lambda = e => s[0].Lambda(e).Concat(s[1].Lambda(e))
			};
			return sel;
		}

		public static Selector Select(Func<IEntity, IEntity> selector) {
			return new Selector { Lambda = e => new List<IEntity> { selector(e) } };
		}
		public static Selector Select(Func<IEntity, IEnumerable<IEntity>> selector) {
			return new Selector { Lambda = selector };
		}

		public static ActionGraph Damage(ActionGraph Targets = null, ActionGraph Amount = null) { return new Damage { Args = { Targets, Amount } }; }
		public static ActionGraph Heal(ActionGraph Targets = null, ActionGraph Amount = null) { return new Heal { Args = { Targets, Amount } }; }
		public static ActionGraph Death(ActionGraph Targets = null) { return new Death { Args = { Targets } }; }
		public static ActionGraph Silence(ActionGraph Targets = null) { return new Silence { Args = { Targets } }; }
		public static ActionGraph Bounce(ActionGraph Targets = null) { return new Bounce { Args = { Targets } }; }
		public static ActionGraph Destroy(ActionGraph Targets = null) { return new Destroy { Args = { Targets } }; }
		public static ActionGraph GainMana(ActionGraph Player = null, ActionGraph Amount = null) { return new GainMana { Args = { Player, Amount } }; }
		public static ActionGraph Summon(ActionGraph Player = null, ActionGraph Entity = null, ActionGraph Amount = null) { return new Summon { Args = { Player, Entity, Amount } }; }
		public static ActionGraph Discard(ActionGraph Targets = null) { return new Discard { Args = { Targets } }; }

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
		public static Trigger<IEntity, IEntity> OnBeginTurn(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.PhaseMainStartTriggers, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnEndTurn(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.PhaseMainEnd, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnPlay(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Play, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> AfterPlay(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.AfterPlay, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnSpellbender(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Spellbender, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnPreSummon(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.PreSummon, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnSummon(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Summon, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> AfterSummon(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.AfterSummon, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnProposedAttack(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.ProposedAttack, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnAttack(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Attack, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> AfterAttack(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.AfterAttack, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnInspire(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Inspire, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnDeath(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Death, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnDrawCard(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.DrawCard, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnAddToHand(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.AddToHand, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnPreDamage(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.PreDamage, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnDamage(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Damage, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnHeal(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Heal, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnSilence(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Silence, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnDiscard(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.Discard, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnGainArmour(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.GainArmour, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnRevealSecret(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.RevealSecret, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnEquipWeapon(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.EquipWeapon, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnWeaponAttack(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.WeaponAttack, Action, Condition);
		}
		public static Trigger<IEntity, IEntity> OnMainNext(Condition<IEntity, IEntity> Condition, ActionGraph Action) {
			return Trigger<IEntity, IEntity>.At(TriggerType.PhaseMainNext, Action, Condition);
		}

		// Trigger conditions
		public static Condition<IEntity, IEntity> IsSelf = new Condition<IEntity, IEntity>((me, other) => me == other);
		public static Condition<IEntity, IEntity> IsFriendlySpell = new Condition<IEntity, IEntity>((me, entity) => me.Controller == entity.Controller && entity is Spell);
		public static Condition<IEntity, IEntity> IsFriendly = new Condition<IEntity, IEntity>((me, entity) => me.Controller == entity.Controller);
		public static Condition<IEntity, IEntity> IsOpposing = new Condition<IEntity, IEntity>((me, entity) => me.Controller == entity.Controller.Opponent);
	}
}
