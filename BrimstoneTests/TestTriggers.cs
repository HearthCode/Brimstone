/*
	Copyright 2016, 2017 Katy Coe
	Copyright 2016 Timothy Stiles

	This file is part of Brimstone.

	Brimstone is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brimstone is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brimstone.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Brimstone;
using Brimstone.QueueActions;
using Brimstone.Entities;
using static Brimstone.Actions;
using static Brimstone.TriggerType;

namespace BrimstoneTests
{
	[TestFixture]
	class TestTriggers
	{
		[Test]
		public void TestTriggerAttachment() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true, ActionHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			IPlayable acolyte = p1.Deck.Add(new Minion("Acolyte of Pain"));
			p1.Deck.Fill();
			p2.Deck.Fill();
			game.Start(1);

			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);
			var cardsInHand = p1.Hand.Count;

			// Acolyte is in deck, should not trigger
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is in hand, should not trigger
			acolyte.Zone = p1.Hand;
			cardsInHand++;
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is in hand, trigger should be checked but not fire
			p1.Give("War Golem").Play();
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is on board, trigger should be checked and fire
			acolyte.Play();
			cardsInHand--;
			p1.Give("Whirlwind").Play();
			cardsInHand++;
			Assert.That(cardsInHand == p1.Hand.Count);

			// Acolyte is in graveyard, should not trigger
			acolyte.Zone = p1.Graveyard;
			p1.Give("Whirlwind").Play();
			Assert.That(cardsInHand == p1.Hand.Count);
		}

		[Test]
		public void TestTriggerZones() {
			// Arrange
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			var illidan = new Minion("Illidan Stormrage") { Zone = p1.Deck };
			var wisp1 = new Minion("Wisp") { Zone = p1.Deck };
			var wisp2 = new Minion("Wisp") { Zone = p1.Deck };
			var wisp3 = new Minion("Wisp") { Zone = p1.Deck };
			p1.Deck.Fill();
			p2.Deck.Fill();
			game.Start(1, SkipMulligan: true, Shuffle: false);

			Assert.That(p1.Hand[1] == illidan);
			Assert.That(p1.Hand[2] == wisp1);
			Assert.That(p1.Hand[3] == wisp2);
			p1.Draw();
			Assert.That(p1.Hand[4] == wisp3);
			Assert.AreEqual(0, p1.Board.Count);

			// Act
			wisp1.Play();

			// Assert: Illidan Stormrage should not trigger in HAND zone
			Assert.AreEqual(1, p1.Board.Count);

			// Act
			illidan.Play();

			// Assert: Illidan Stormrage should not trigger as it is moving to PLAY
			Assert.AreEqual(2, p1.Board.Count);

			// Act
			wisp2.Play();

			// Assert: Illidan Stormrage should trigger in PLAY zone
			Assert.AreEqual(4, p1.Board.Count);
			Assert.That(p1.Board[3] == wisp2);
			Assert.AreEqual("Flame of Azzinoth", p1.Board[4].Card.Name);
			// TODO: Add board full tests, graveyard test etc.
		}

		// Patashu's Timing Gym. Are you Advanced Rulebook Compliant (tm)?

		// 1) events are processed in timestamp order
		[Test]
		public void TestEventsProcessInTimestampOrder() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.Start(1);
			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);

			var dr = new Minion("XXX_024").GiveTo(p1); // Damage Reflector
			var igb = new Minion("Imp Gang Boss").GiveTo(p1);

			igb.Play();
			dr.Play();

			igb.ZonePosition = 2;
			dr.ZonePosition = 1;

			// Going by order of play, the imp should be summoned, then the damage reflector should damage everything, so only the 2nd imp lives
			new Spell("Whirlwind").GiveTo(p1).Play();
			Assert.AreEqual(3, p1.Board.Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "BRM_006").Count()); // Imp Gang Boss
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "BRM_006t").Count()); // Imp
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "XXX_024").Count());
		}

		// 2) each event's queue resolves before the next event's queue is populated
		[Test]
		public void TestQueue1ResolvesBeforeQueue2Populates() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.Start(1);
			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);

			var originalBehaviour = Cards.FromId("EX1_556").Behaviour.Deathrattle;
			Cards.FromId("EX1_556").Behaviour.Deathrattle = Summon(Controller, "Cult Master");

			var hg = new Minion("EX1_556").GiveTo(p1); // Harvest Golem modified (Deathrattle: Summon a Cult Master)
			var wisp = new Minion("Wisp").GiveTo(p1);

			hg.Play();
			wisp.Play();
			var cardsInHand = p1.Hand.Count;

			// Because each queue resolves before the next queue is populated, the Cult Master should be in play soon enough to trigger on the Wisp's death and draw a card.
			new Spell("Flamestrike").GiveTo(p2).Play();

			Cards.FromId("EX1_556").Behaviour.Deathrattle = originalBehaviour;

			Assert.AreEqual(cardsInHand + 1, p1.Hand.Count);
		}

		// 3) triggers on a event are in timestamp order
		[Test]
		public void TestTriggersProcessInTimestampOrder() {
			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.Start(1);
			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);

			Cards.FromId("NEW1_020").Behaviour.Triggers = new Dictionary<TriggerType, Trigger>() { [OnPlay] = IsFriendlySpell > Damage(AllMinions, 1) };

			var wp = new Minion("NEW1_020").GiveTo(p1); // Wild Pyromancer modified (WHEN you cast a spell, deal 1 damage to all minions)
			var vt = new Minion("Violet Teacher").GiveTo(p1);

			vt.Play();
			wp.Play();

			vt.ZonePosition = 2;
			wp.ZonePosition = 1;

			// Going by order of play, the apprentice should be summoned, then the AoE should wipe it out.
			new Spell("Moonfire").GiveTo(p1).Play(p2.Hero);
			Assert.AreEqual(2, p1.Board.Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "NEW1_020").Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "NEW1_026").Count());
		}

		// 4) Deathrattles and on-death triggers are in timestamp order
		[Test]
		public void TestDeathrattlesVsOnDeathTriggers() {
			Condition IsHarvestGolem = new Condition((me, other) => other.Card.Id == "EX1_556");

			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.Start(1);
			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);

			Cards.FromId("EX1_595").Behaviour.Triggers = new Dictionary<TriggerType, Trigger>() { [OnDeath] = IsHarvestGolem > Summon(Controller, "Wisp") };
			Cards.FromId("XXX_024").Behaviour.Triggers = new Dictionary<TriggerType, Trigger>() { [OnDeath] = IsHarvestGolem > Damage(AllMinions, 1) };

			var dr = new Minion("XXX_024").GiveTo(p1); // Damage Reflector modified (when a harvest golem dies, damage all minions for 1)
			var hg = new Minion("Harvest Golem").GiveTo(p1);
			var cm = new Minion("EX1_595").GiveTo(p1); // Cult Master modified (when a harvest golem dies, summon a wisp)

			cm.Play();
			hg.Play();
			dr.Play();

			cm.ZonePosition = 3;
			hg.ZonePosition = 2;
			dr.ZonePosition = 1;

			// Going by order of play, the wisp should be summoned, then the damaged golem should be summoned, then the AoE should wipe them both out.
			new Spell("Darkbomb").GiveTo(p1).Play(hg);
			Assert.AreEqual(2, p1.Board.Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "EX1_595").Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "XXX_024").Count());
		}

		// Dominant Player Bug tests begin. Feel free to stop here if you don't want to emulate the Dominant Player Bug.

		// 5a) Triggers on an event played in dominant/secondary order are in dominant/secondary order
		[Test]
		public void TestDominantPlayerBug1() {
			Condition IsImpGangBoss = new Condition((me, other) => other.Card.Id == "BRM_006");
			Selector AllImps = Select(e => e.Game.Player1.Board.Concat(e.Game.Player2.Board).Where(x => x.Card.Id == "BRM_006t"));

			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.Start(1);
			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);

			Assert.AreEqual(2, p1.Id); // Dominant Player
			Assert.AreEqual(3, p2.Id); // Secondary Player

			Cards.FromId("XXX_024").Behaviour.Triggers = new Dictionary<TriggerType, Trigger>() { [OnDamage] = IsImpGangBoss > Damage(AllImps, 1) };

			var dr = new Minion("XXX_024").GiveTo(p2); // Damage Reflector modified (when an imp gang boss is damaged, damage all imps for 1)
			var igb = new Minion("Imp Gang Boss").GiveTo(p1);

			igb.Play();
			dr.Play();

			//Due to the Dominant Player Bug, the Imp Gang Boss will trigger before the Damage Reflector.
			new Spell("Moonfire").GiveTo(p1).Play(igb);
			Assert.AreEqual(1, p1.Board.Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "BRM_006").Count()); // Imp Gang Boss
		}

		// 5b) Triggers on an event played in secondary/dominant order are in dominant/secondary order
		[Test]
		public void TestDominantPlayerBug2() {
			Condition IsImpGangBoss = new Condition((me, other) => other.Card.Id == "BRM_006");
			Selector AllImps = Select(e => e.Game.Player1.Board.Concat(e.Game.Player2.Board).Where(x => x.Card.Id == "BRM_006t"));

			var game = new Game(HeroClass.Hunter, HeroClass.Warlock, PowerHistory: true);
			var p1 = game.Player1;
			var p2 = game.Player2;
			game.Start(1);
			p1.Choice.Keep(x => x.Cost <= 2);
			p2.Choice.Keep(x => x.Cost <= 2);

			Assert.AreEqual(2, p1.Id); // Dominant Player
			Assert.AreEqual(3, p2.Id); // Secondary Player

			Cards.FromId("XXX_024").Behaviour.Triggers = new Dictionary<TriggerType, Trigger>() { [OnDamage] = IsImpGangBoss > Damage(AllImps, 1) };

			var igb = new Minion("Imp Gang Boss").GiveTo(p2);
			var dr = new Minion("XXX_024").GiveTo(p1); // Damage Reflector modified (when an imp gang boss is damaged, damage all imps for 1)

			igb.Play();
			dr.Play();

			//Due to the Dominant Player Bug, the Damage Reflector will trigger before the Imp Gang Boss.
			new Spell("Moonfire").GiveTo(p1).Play(igb);
			Assert.AreEqual(2, p1.Board.Count());
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "BRM_006").Count()); // Imp Gang Boss
			Assert.AreEqual(1, p1.Board.Where(x => x.Card.Id == "BRM_006t").Count()); // Imp
		}

		// 6) triggers on an event resolves the dominant player queue before the secondary player queue populates
		public void TestDominantQueueResolvesBeforeSecondaryQueuePopulates() {
			// TODO: set Flesheating Ghoul to draw a card, play dominant Deathlord, make secondary Deck have only Flesheating Ghoul in it,
			// Pyroblast Deathlord, assert 1 card drawn (as opposed to 0). (can do it again with secondary Deathlord)
		}

		// 7) queued triggers on an event are invalidated if they change controllers before resolving
		// TODO: Need an implementation of Steal so I can use Sylvanas

		// 8) enchantments don't change controllers when their associated minion does
		// TODO: Enchantments

		// 9) quad queue model
		// TODO: Being able to specify a trigger works only in the hand or only in play
	}
}
